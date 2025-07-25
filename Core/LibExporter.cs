using Core;
using Core.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib
{
    public class LibExporter
    {
        private readonly IPlayniteProgressService ProgressService;
        private readonly IPlayniteGameRepository PlayniteGameRepository;
        private readonly IPlayniteInsightsWebServerService WebServerService;
        private readonly IHashService HashService;
        private readonly IAppLogger Logger;
        private readonly IFileSystemService Fs;
        public string LibraryFilesDir { get; }

        public LibExporter(
            IPlayniteProgressService ProgressService,
            IPlayniteGameRepository PlayniteGameRepository,
        IPlayniteInsightsWebServerService WebServerService,
            IAppLogger Logger,
            IHashService HashService,
            string LibraryFilesDir,
            IFileSystemService FileSystemService
        )
        {
            this.ProgressService = ProgressService;
            this.PlayniteGameRepository = PlayniteGameRepository;
            this.WebServerService = WebServerService;
            this.Logger = Logger;
            this.HashService = HashService;
            this.LibraryFilesDir = LibraryFilesDir;
            Fs = FileSystemService;
        }

        private object GetGameMetadata(Game g, string contentHash)
        {
            return new
            {
                g.Id,
                g.Name,
                g.Platforms,
                g.Genres,
                g.Developers,
                g.Publishers,
                g.ReleaseDate,
                g.Playtime,
                g.LastActivity,
                g.Added,
                g.InstallDirectory,
                g.IsInstalled,
                g.BackgroundImage,
                g.CoverImage,
                g.Icon,
                g.Description,
                ContentHash = contentHash
            };
        }

        private string CommandToJsonString(SyncGameListCommand command)
        {
            try
            {
                return JsonConvert.SerializeObject(command, Formatting.Indented);
            }
            catch (Exception)
            {
                return "[]";
            }
        }

        /// <summary>
        /// Compares the server's manifest with the current list of games and returns a list of games that should be removed from the server.
        /// </summary>
        /// <returns>List of game IDs</returns>
        private List<string> GetItemsToRemove(PlayniteLibraryManifest manifest)
        {
            var mediaExistsFor = manifest?.mediaExistsFor?
                .Select((mef) => mef.gameId)
                .ToList() ?? new List<string>();
            var gamesIdList = PlayniteGameRepository.GetIdList();
            var itemsToRemove = new List<string>();
            // Remove games that are removed from library but still present in the manifest
            foreach (var gameId in mediaExistsFor)
            {
                if (!gamesIdList.Contains(gameId))
                {
                    itemsToRemove.Add(gameId);
                }
            }
            return itemsToRemove;
        }

        /// <summary>
        /// Compares the server's manifest with the current list of games and returns lists of games that should be added or updated from the server.
        /// </summary>
        /// <returns></returns>
        private (IEnumerable<object> itemsToAdd, IEnumerable<object> itemsToUpdate) GetGameMetadataToAddAndUpdate(
            PlayniteLibraryManifest manifest,
            IEnumerable<Game> games
        ) {
            List<object> itemsToUpdate = new List<object>();
            List<object> itemsToAdd = new List<object>();
            foreach (var game in games)
            {
                var hash = HashService.GetHashFromPlayniteGame(game);
                var gameInLibrary = manifest?.gamesInLibrary?
                    .Where((gil) => gil.gameId == game.Id.ToString())
                    .FirstOrDefault() ?? null;
                if (gameInLibrary != null)
                {
                    if (gameInLibrary.contentHash != hash)
                    {
                        itemsToUpdate.Add(GetGameMetadata(game, hash));
                        continue;
                    }
                }
                else
                {
                    itemsToAdd.Add(GetGameMetadata(game, hash));
                }
            }
            return (itemsToAdd, itemsToUpdate);
        }

        private (IEnumerable<Game> itemsToAdd, IEnumerable<Game> itemsToUpdate)
            FilterGamesToAddAndUpdate(
                PlayniteLibraryManifest manifest,
                IEnumerable<Game> games
        ) {
            var itemsToUpdate = new List<Game>();
            var itemsToAdd = new List<Game>();
            foreach (var game in games)
            {
                var hash = HashService.GetHashFromPlayniteGame(game);
                var gameInLibrary = manifest?.gamesInLibrary?
                    .Where((gil) => gil.gameId == game.Id.ToString())
                    .FirstOrDefault() ?? null;
                if (gameInLibrary != null)
                {
                    if (gameInLibrary.contentHash != hash)
                    {
                        itemsToUpdate.Add(game);
                        continue;
                    }
                }
                else
                {
                    itemsToAdd.Add(game);
                }
            }
            return (itemsToAdd, itemsToUpdate);
        }

        private async Task<bool> _RunLibrarySyncAsync(
            IEnumerable<Game> itemsToAdd = null,
            IEnumerable<Game> itemsToUpdate = null,
            IEnumerable<Game> itemsToRemove = null
        ) {
            var manifest = await WebServerService.GetManifestAsync();
            IEnumerable<string> removedItems = new List<string>();
            IEnumerable<object> updatedItems = new List<object>();
            IEnumerable<object> addedItems = new List<object>();
            if (itemsToRemove == null)
            {
                removedItems = GetItemsToRemove(manifest);
            }
            else
            {
                removedItems = itemsToRemove.Select(g => g.Id.ToString()).ToList();
            }
            if (itemsToUpdate == null || itemsToAdd == null)
            {
                var (added, updated) = GetGameMetadataToAddAndUpdate(
                    manifest, PlayniteGameRepository.GetAll());
                if (itemsToAdd == null)
                {
                    addedItems = added;
                }
                else
                {
                    addedItems = itemsToAdd
                        .Select(g => GetGameMetadata(g, HashService.GetHashFromPlayniteGame(g)))
                        .ToList();
                }
                if (itemsToUpdate == null)
                {
                    updatedItems = updated;
                }
                else
                {
                    updatedItems = itemsToUpdate
                        .Select(g => GetGameMetadata(g, HashService.GetHashFromPlayniteGame(g)))
                        .ToList();
                }
            }
            if (!removedItems.Any() && !addedItems.Any() && !updatedItems.Any()) return true;
            var syncGameListCommand = new SyncGameListCommand(
                addedItems.ToList(), removedItems.ToList(), updatedItems.ToList());
            return await WebServerService.PostJson(endpoint: WebAppEndpoints.SyncGames, syncGameListCommand);
        }

        /// <summary>
        /// Syncs the entire library database with the server
        /// </summary>
        /// <returns></returns>
        public async Task<bool> RunLibrarySyncAsync(
            bool showProgress = false,
            IEnumerable<Game> itemsToAdd = null,
            IEnumerable<Game> itemsToUpdate = null,
            IEnumerable<Game> itemsToRemove = null
        ) {
            if (showProgress == true)
            {
                var message = ResourceProvider.GetString("LOC_Loading_SyncClientServer");
                return ProgressService.ActivateGlobalProgress(
                    message,
                    false,
                    async (progress) =>
                    {
                        progress.IsIndeterminate = true;
                        return await _RunLibrarySyncAsync(
                            itemsToAdd: itemsToAdd,
                            itemsToUpdate: itemsToUpdate,
                            itemsToRemove: itemsToRemove
                        );
                    }
                );
            }
            return await _RunLibrarySyncAsync(
                itemsToAdd: itemsToAdd,
                itemsToUpdate: itemsToUpdate,
                itemsToRemove: itemsToRemove
            );
        }

        /// <summary>
        /// Syncs a list of games with the server
        /// </summary>
        /// <param name="itemsToSync"></param>
        /// <returns></returns>
        public async Task<bool> RunGameListSyncAsync(List<Game> itemsToSync)
        {
            var manifest = await WebServerService.GetManifestAsync();
            var (itemsToAdd, itemsToUpdate) = FilterGamesToAddAndUpdate(manifest, itemsToSync);
            if (!itemsToAdd.Any() && !itemsToUpdate.Any()) return true;
            return await RunLibrarySyncAsync(
                true,
                itemsToAdd: itemsToAdd,
                itemsToUpdate: itemsToUpdate,
                itemsToRemove: new List<Game>());
        }

        /// <summary>
        /// Syncronizes library media files for games with the web server.
        /// </summary>
        /// <param name="games">List of games to check for media files changes. If null, media files for all games will be checked.</param>
        /// <returns>ValidationResult</returns>
        public async Task RunMediaFilesSyncAsync(IEnumerable<Game> games = null)
        {
            var lod_syncing_media_files = ResourceProvider.GetString("LOC_Loading_SyncClientServer");
            var lod_progress_syncing_media_files = ResourceProvider.GetString("LOC_Progress_SyncingMediaFiles");
            var manifest = await WebServerService.GetManifestAsync();
            var resolvedGameList = games ?? PlayniteGameRepository.GetAll();
            ProgressService.ActivateGlobalProgress(
                lod_syncing_media_files,
                true, 
                async (progress) =>
                {
                    progress.CurrentProgressValue = 0;
                    progress.ProgressMaxValue = resolvedGameList.Count();
                    progress.IsIndeterminate = false;
                    foreach (var game in resolvedGameList)
                    {   
                        if (progress.CancelToken.IsCancellationRequested)
                        {
                            Logger.Info("Media files sync cancelled by user.");
                            return true;
                        }
                        var progressText = lod_progress_syncing_media_files
                            .Replace("{{current}}", (progress.CurrentProgressValue + 1).ToString())
                            .Replace("{{total}}", (progress.ProgressMaxValue).ToString())
                            .Replace("{{gameName}}", game.Name);
                        progress.Text = progressText;
                        System.Threading.Thread.Sleep(60); // Give some time for UI to update
                        var gameId = game.Id.ToString();
                        var mediaFolder = Fs.PathCombine(LibraryFilesDir, gameId);
                        string contentHash = HashService.HashFolderContents(mediaFolder);
                        if (string.IsNullOrEmpty(contentHash))
                        {
                            Logger.Warn($"Failed to create library files content hash for game with id {gameId}. If this game does not have any media files, you can safely ignore this warning.");
                            progress.CurrentProgressValue++;
                            continue;
                        }
                        if (manifest != null)
                        {
                            // If game not present in manifest, skip sending media files
                            var gameInLibrary = manifest?.gamesInLibrary?
                                .Where((gil) => gil.gameId == gameId)
                                .FirstOrDefault() ?? null;
                            if (gameInLibrary == null)
                            {
                                progress.CurrentProgressValue++;
                                continue;
                            }
                            var mediaExistsForEntry = manifest?.mediaExistsFor?
                                .Where(m => m.gameId == gameId)
                                .FirstOrDefault() ?? null;
                            // Compare generated hash with manifest's hash
                            if (mediaExistsForEntry != null)
                            {
                                if (mediaExistsForEntry.contentHash == contentHash)
                                {
                                    progress.CurrentProgressValue++;
                                    continue;
                                }
                            }
                        }
                        using (var content = new MultipartFormDataContent())
                        {
                            if (Fs.DirectoryExists(mediaFolder) == false)
                            {
                                Logger.Warn($"Game library files directory not found: {mediaFolder}");
                                progress.CurrentProgressValue++;
                                continue;
                            }
                            content.Add(new StringContent(gameId), "gameId");
                            content.Add(new StringContent(contentHash), "contentHash");
                            foreach (var file in Fs.DirectoryGetFiles(mediaFolder))
                            {
                                var fileContent = new StreamContent(Fs.FileOpenRead(file));
                                var fileName = Fs.PathGetFileName(file);
                                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                                content.Add(fileContent, "files", fileName);
                            }
                            var result = await WebServerService
                                .Post(endpoint: WebAppEndpoints.SyncFiles, content: content);
                            if (result == false)
                            {
                                Logger.Warn($"Request to sync library files for {gameId} failed");
                                progress.CurrentProgressValue++;
                                continue;
                            }
                        }
                        progress.CurrentProgressValue++;
                    }
                    return true;
                }
            );
        }
    }
}

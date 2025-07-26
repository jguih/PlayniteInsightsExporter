using Core.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core
{
    public class LibExporter : ILibExporter
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

        /// <summary>
        /// Compares the server's manifest with the current list of games and returns a list of games that should be removed from the server.
        /// </summary>
        /// <returns>List of game IDs</returns>
        private List<string> GetItemsToRemove(PlayniteLibraryManifest manifest)
        {
            var gameInLibrary = manifest?.gamesInLibrary?
                .Select((gil) => gil.gameId)
                .ToList() ?? new List<string>();
            var gamesIdList = PlayniteGameRepository.GetIdList();
            var itemsToRemove = new List<string>();
            // Remove games that are removed from library but still present in the manifest
            foreach (var gameId in gameInLibrary)
            {
                if (!gamesIdList.Contains(gameId))
                {
                    itemsToRemove.Add(gameId);
                }
            }
            return itemsToRemove;
        }

        /// <summary>
        /// Compares the server's manifest with the current list of games in the database and returns lists of games that should be added or updated by the server.
        /// </summary>
        /// <returns></returns>
        private (List<PlayniteGameDTO> itemsToAdd, List<PlayniteGameDTO> itemsToUpdate) GetGamesToAddAndUpdateFromLibrary(PlayniteLibraryManifest manifest)
        {
            var itemsToUpdate = new List<PlayniteGameDTO>();
            var itemsToAdd = new List<PlayniteGameDTO>();
            var games = PlayniteGameRepository.GetAll();
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
                        itemsToUpdate.Add(GetGameDTO(game));
                        continue;
                    }
                }
                else
                {
                    itemsToAdd.Add(GetGameDTO(game));
                }
            }
            return (itemsToAdd, itemsToUpdate);
        }

        private (List<Game> itemsToAdd, List<Game> itemsToUpdate)
            FilterGamesToAddAndUpdate(
                PlayniteLibraryManifest manifest,
                List<Game> games
        )
        {
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
            List<Game> itemsToAdd = null,
            List<Game> itemsToUpdate = null,
            List<Game> itemsToRemove = null
        )
        {
            PlayniteLibraryManifest manifest = null;
            List<PlayniteGameDTO> libAddedGames = null;
            List<PlayniteGameDTO> libUpdatedGames = null;

            bool needManifest = itemsToAdd == null || itemsToUpdate == null || itemsToRemove == null;
            if (needManifest)
                manifest = await WebServerService.GetManifestAsync();

            if ((itemsToAdd == null || itemsToUpdate == null) && (libAddedGames == null || libUpdatedGames == null))
            {
                var (added, updated) = GetGamesToAddAndUpdateFromLibrary(manifest);
                libAddedGames = added;
                libUpdatedGames = updated;
            }

            var resolvedGamesToRemove = itemsToRemove != null
               ? itemsToRemove.Select(g => g.Id.ToString()).ToList()
               : GetItemsToRemove(manifest);

            var resolvedGamesToUpdate = itemsToUpdate != null
                ? itemsToUpdate.Select(GetGameDTO).ToList()
                : libUpdatedGames;

            var resolvedGamesToAdd = itemsToAdd != null
                ? itemsToAdd.Select(GetGameDTO).ToList()
                : libAddedGames;

            if (!resolvedGamesToRemove.Any() && !resolvedGamesToAdd.Any() && !resolvedGamesToUpdate.Any())
                return true;

            var syncGameListCommand = new SyncGameListCommand(
                resolvedGamesToAdd,
                resolvedGamesToRemove,
                resolvedGamesToUpdate
            );

            return await WebServerService.PostJson(
                endpoint: WebAppEndpoints.SyncGames,
                syncGameListCommand
            );
        }

        private async Task<bool> _RunMediaFilesSync(
            IEnumerable<Game> games,
            Action OnContinue = null,
            Action<Game> OnEvery = null,
            CancellationToken cancellationToken = default
        )
        {
            var manifest = await WebServerService.GetManifestAsync();
            foreach (var game in games)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.Info("Media files sync cancelled by user.");
                    return true;
                }
                OnEvery?.Invoke(game);
                var gameId = game.Id.ToString();
                var mediaFolder = Fs.PathCombine(LibraryFilesDir, gameId);
                string contentHash = HashService.HashFolderContents(mediaFolder);
                if (string.IsNullOrEmpty(contentHash))
                {
                    Logger.Warn($"Failed to create library files content hash for game with id {gameId}. If this game does not have any media files, you can safely ignore this warning.");
                    OnContinue?.Invoke();
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
                        OnContinue?.Invoke();
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
                            OnContinue?.Invoke();
                            continue;
                        }
                    }
                }
                using (var content = new MultipartFormDataContent())
                {
                    if (Fs.DirectoryExists(mediaFolder) == false)
                    {
                        Logger.Warn($"Game library files directory not found: {mediaFolder}");
                        OnContinue?.Invoke();
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
                        OnContinue?.Invoke();
                        continue;
                    }
                }
                OnContinue?.Invoke();
            }
            return true;
        }

        public PlayniteGameDTO GetGameDTO(Game g)
        {
            var hash = HashService.GetHashFromPlayniteGame(g);
            return new PlayniteGameDTO
            {
                Id = g.Id,
                Name = g.Name,
                Platforms = g.Platforms,
                Genres = g.Genres,
                Developers = g.Developers,
                Publishers = g.Publishers,
                ReleaseDate = g.ReleaseDate,
                Playtime = g.Playtime,
                LastActivity = g.LastActivity,
                Added = g.Added,
                InstallDirectory = g.InstallDirectory,
                IsInstalled = g.IsInstalled,
                BackgroundImage = g.BackgroundImage,
                CoverImage = g.CoverImage,
                Icon = g.Icon,
                Description = g.Description,
                ContentHash = hash
            };
        }

        public bool RunLibrarySync(
            List<Game> itemsToAdd = null,
            List<Game> itemsToUpdate = null,
            List<Game> itemsToRemove = null
        )
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

        public async Task<bool> RunLibrarySyncAsync(
            List<Game> itemsToAdd = null,
            List<Game> itemsToUpdate = null,
            List<Game> itemsToRemove = null
        )
        {
            return await _RunLibrarySyncAsync(
                itemsToAdd: itemsToAdd,
                itemsToUpdate: itemsToUpdate,
                itemsToRemove: itemsToRemove
            );
        }

        public bool RunGameListSync(List<Game> itemsToSync)
        {
            var message = ResourceProvider.GetString("LOC_Loading_SyncClientServer");
            return ProgressService.ActivateGlobalProgress(
                message,
                false,
                async (progress) =>
                {
                    progress.IsIndeterminate = true;
                    var manifest = await WebServerService.GetManifestAsync();
                    var (itemsToAdd, itemsToUpdate) = FilterGamesToAddAndUpdate(manifest, itemsToSync);
                    if (!itemsToAdd.Any() && !itemsToUpdate.Any()) return true;
                    return await _RunLibrarySyncAsync(
                        itemsToAdd: itemsToAdd,
                        itemsToUpdate: itemsToUpdate,
                        itemsToRemove: new List<Game>());
                }
            );
        }

        public bool RunMediaFilesSync(IEnumerable<Game> games = null)
        {
            var lod_syncing_media_files = ResourceProvider.GetString("LOC_Loading_SyncClientServer");
            var lod_progress_syncing_media_files = ResourceProvider.GetString("LOC_Progress_SyncingMediaFiles");
            return ProgressService.ActivateGlobalProgress(
                lod_syncing_media_files,
                true,
                async (progress) =>
                {
                    var resolvedGameList = games ?? PlayniteGameRepository.GetAll();
                    progress.CurrentProgressValue = 0;
                    progress.ProgressMaxValue = resolvedGameList.Count();
                    progress.IsIndeterminate = false;
                    return await _RunMediaFilesSync(
                        games: resolvedGameList,
                        OnContinue: () => progress.CurrentProgressValue++,
                        OnEvery: (game) =>
                        {
                            var progressText = lod_progress_syncing_media_files
                                .Replace("{{current}}", (progress.CurrentProgressValue + 1).ToString())
                                .Replace("{{total}}", (progress.ProgressMaxValue).ToString())
                                .Replace("{{gameName}}", game.Name);
                            progress.Text = progressText;
                        },
                        cancellationToken: progress.CancelToken
                    );
                }
            );
        }

        public Task<bool> RunMediaFilesSyncAsync(
            IEnumerable<Game> games = null
        ) {
            var resolvedGameList = games ?? PlayniteGameRepository.GetAll();
            return _RunMediaFilesSync(
                games: resolvedGameList,
                OnContinue: null,
                OnEvery: null,
                cancellationToken: CancellationToken.None
            );
        }
    }
}

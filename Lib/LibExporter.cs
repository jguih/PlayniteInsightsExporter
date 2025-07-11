using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteInsightsExporter.Lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib
{
    public class LibExporter
    {
        private readonly PlayniteInsightsExporter Plugin;
        private readonly IPlayniteAPI PlayniteApi;
        private readonly PlayniteInsightsWebServerService WebServerService;
        private readonly HashService HashService;
        private readonly ILogger Logger;
        public string LibraryFilesDir { get; }

        public LibExporter(
            PlayniteInsightsExporter Plugin,
            PlayniteInsightsWebServerService WebServerService,
            ILogger Logger
        )
        {
            this.Plugin = Plugin;
            this.PlayniteApi = Plugin.PlayniteApi;
            this.LibraryFilesDir = Path.Combine(PlayniteApi.Paths.ConfigurationPath, "library", "files");
            this.WebServerService = WebServerService;
            this.Logger = Logger;
            HashService = new HashService(Logger);
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

        private List<string> GetGamesIdList()
        {
            return PlayniteApi.Database.Games
                .Select(g => g.Id.ToString())
                .ToList();
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
            var gamesIdList = GetGamesIdList();
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
        private (List<object> itemsToAdd, List<object> itemsToUpdate) GetItemsToAddAndUpdate(
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

        /// <summary>
        /// Sends a request to delete the games in itemsToRemove
        /// </summary>
        /// <param name="itemsToRemove">List of Game ids</param>
        /// <returns></returns>
        public async Task<bool> RunRemovedGamesSyncAsync(List<string> itemsToRemove)
        {
            if (itemsToRemove == null) return false;
            if (itemsToRemove.Count() == 0) return true;

            var syncGameListCommand = new SyncGameListCommand(
                AddedItems: new List<object>(),
                RemovedItems: itemsToRemove,
                UpdatedItems: new List<object>());
            var jsonCommand = CommandToJsonString(syncGameListCommand);
            using (var content = new StringContent(jsonCommand, Encoding.UTF8, "application/json"))
            {
                return await WebServerService.Post(endpoint: WebAppEndpoints.SyncGames, content: content);
            }
        }

        /// <summary>
        /// Sends a request to delete the games in itemsToRemove and remove their media files
        /// </summary>
        /// <param name="itemsToRemove"></param>
        /// <returns></returns>
        public async Task<bool> RunFullRemovedGamesSyncAsync(List<Game> itemsToRemove)
        {
            if (itemsToRemove == null) return false;
            if (itemsToRemove.Count() == 0) return true;
            List<string> gameIdList = new List<string>();
            foreach (var game in itemsToRemove)
            {
                gameIdList.Add(game.Id.ToString());
            }
            var result = await RunRemovedGamesSyncAsync(gameIdList);
            if (result == false) return false;
            return await RunMediaFilesSyncAsync(gameIdList);
        }

        /// <summary>
        /// Sends a request to update the games in itemsToUpdate
        /// </summary>
        /// <param name="itemsToUpdate"></param>
        /// <param name="force">If true, updates games without comparing with the server</param>
        /// <returns></returns>
        public async Task<bool> RunUpdatedGamesSyncAsync(
            List<Game> itemsToUpdate,
            bool force = false
        ) {
            if (itemsToUpdate == null) return false;
            if (itemsToUpdate.Count() == 0) return true;

            var manifest = await WebServerService.GetManifestAsync();
            var gameMetadataList = new List<object>();
            foreach (var game in itemsToUpdate)
            {
                var hash = HashService.GetHashFromPlayniteGame(game);
                if (force == true)
                {
                    gameMetadataList.Add(GetGameMetadata(game, hash));
                    continue;
                }
                var gameInLibrary = manifest?.gamesInLibrary?
                    .Where((gil) => gil.gameId == game.Id.ToString())
                    .FirstOrDefault() ?? null;
                if (gameInLibrary != null)
                {
                    if (gameInLibrary.contentHash != hash)
                    {
                        gameMetadataList.Add(GetGameMetadata(game, hash));
                        continue;
                    }
                }
            }
            if (gameMetadataList.Count() == 0) return true;
            var syncGameListCommand = new SyncGameListCommand(
                AddedItems: new List<object>(),
                RemovedItems: new List<string>(),
                UpdatedItems: gameMetadataList);
            var jsonCommand = CommandToJsonString(syncGameListCommand);
            using (var content = new StringContent(jsonCommand, Encoding.UTF8, "application/json"))
            {
                return await WebServerService.Post(endpoint: WebAppEndpoints.SyncGames, content: content);
            }
        }

        /// <summary>
        /// Sends a request to update the games in itemsToUpdate along with their media files
        /// </summary>
        /// <param name="itemsToUpdate"></param>
        /// <param name="force">If true, updates games without comparing with the server</param>
        /// <returns></returns>
        public async Task<bool> RunFullUpdatedGamesSyncAsync(
            List<Game> itemsToUpdate,
            bool force = false
        ) {
            if (itemsToUpdate == null) return false;
            if (itemsToUpdate.Count() == 0) return true;
            List<string> gameIdList = new List<string>();
            foreach (var game in itemsToUpdate)
            {
                gameIdList.Add(game.Id.ToString());
            }
            var result = await RunUpdatedGamesSyncAsync(itemsToUpdate, force);
            if (result == false) return false;
            return await RunMediaFilesSyncAsync(gameIdList);
        }

        /// <summary>
        /// Sends a request to add the games in itemsToAdd
        /// </summary>
        /// <param name="itemsToAdd"></param>
        /// <returns></returns>
        public async Task<bool> RunAddedGamesSyncAsync(List<Game> itemsToAdd)
        {
            if (itemsToAdd == null) return false;
            if (itemsToAdd.Count() == 0) return true;

            var gameMetadataList = new List<object>();
            foreach (var game in itemsToAdd)
            {
                var hash = HashService.GetHashFromPlayniteGame(game);
                var metadata = GetGameMetadata(game, hash);
                gameMetadataList.Add(metadata);
            }
            var syncGameListCommand = new SyncGameListCommand(
                AddedItems: gameMetadataList, 
                RemovedItems: new List<string>(), 
                UpdatedItems: new List<object>());
            var jsonCommand = CommandToJsonString(syncGameListCommand);
            using (var content = new StringContent(jsonCommand, Encoding.UTF8, "application/json"))
            {
                return await WebServerService.Post(endpoint: WebAppEndpoints.SyncGames, content: content);
            }
        }

        /// <summary>
        /// Sends a request to add the games in itemsToAdd along with their media files
        /// </summary>
        /// <param name="itemsToUpdate"></param>
        /// <returns></returns>
        public async Task<bool> RunFullAddedGamesSyncAsync(List<Game> itemsToAdd)
        {
            if (itemsToAdd == null) return false;
            if (itemsToAdd.Count() == 0) return true;
            List<string> gameIdList = new List<string>();
            foreach (var game in itemsToAdd)
            {
                gameIdList.Add(game.Id.ToString());
            }
            var result = await RunAddedGamesSyncAsync(itemsToAdd);
            if (result == false) return false;
            return await RunMediaFilesSyncAsync(gameIdList);
        }

        /// <summary>
        /// Syncs the entire library database with the server
        /// </summary>
        /// <returns></returns>
        public async Task<bool> RunLibrarySyncAsync()
        {
            var manifest = await WebServerService.GetManifestAsync();
            var itemsToRemove = GetItemsToRemove(manifest);
            var (itemsToAdd, itemsToUpdate) = GetItemsToAddAndUpdate(manifest, PlayniteApi.Database.Games);
            var syncGameListCommand = new SyncGameListCommand(itemsToAdd, itemsToRemove, itemsToUpdate);
            string json = CommandToJsonString(syncGameListCommand);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                return await WebServerService.Post(endpoint: WebAppEndpoints.SyncGames, content: content); 
            }
        }

        /// <summary>
        /// Syncs a list of games with the server
        /// </summary>
        /// <param name="itemsToSync"></param>
        /// <returns></returns>
        public async Task<bool> RunFullGamesSyncAsync(List<Game> itemsToSync)
        {
            var manifest = await WebServerService.GetManifestAsync();
            var (itemsToAdd, itemsToUpdate) = GetItemsToAddAndUpdate(manifest, itemsToSync);
            var syncGameListCommand = new SyncGameListCommand(itemsToAdd, new List<string>(), itemsToUpdate);
            string json = CommandToJsonString(syncGameListCommand);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var result = await WebServerService.Post(endpoint: WebAppEndpoints.SyncGames, content: content);
                if (result == false) return false;
            }
            var gameIds = new List<string>();
            foreach (var game in itemsToSync)
            {
                gameIds.Add(game.Id.ToString());
            }
            return await RunMediaFilesSyncAsync(gameIds);
        }

        /// <summary>
        /// Syncs the entire library database and media files with the server
        /// </summary>
        /// <returns></returns>
        public async Task<bool> RunFullLibrarySyncAsync()
        {
            var manifest = await WebServerService.GetManifestAsync();
            var itemsToRemove = GetItemsToRemove(manifest);
            var (itemsToAdd, itemsToUpdate) = GetItemsToAddAndUpdate(manifest, PlayniteApi.Database.Games);
            var syncGameListCommand = new SyncGameListCommand(itemsToAdd, itemsToRemove, itemsToUpdate);
            string json = CommandToJsonString(syncGameListCommand);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var result = await WebServerService.Post(endpoint: WebAppEndpoints.SyncGames, content: content);
                if (result == false) return false;
                return await RunMediaFilesSyncAsync();
            }
        }

        /// <summary>
        /// Syncronizes library media files for games with the web server.
        /// </summary>
        /// <param name="gameIdList">List of game ids to check for media files changes. If null, media files for all games will be checked.</param>
        /// <returns>ValidationResult</returns>
        public async Task<bool> RunMediaFilesSyncAsync(IEnumerable<string> gameIdList = null)
        {
            var manifest = await WebServerService.GetManifestAsync();
            var resolvedGameIdList = gameIdList ?? PlayniteApi.Database.Games.Select(g => g.Id.ToString());
            foreach (var gameId in resolvedGameIdList)
            {
                var mediaFolder = Path.Combine(LibraryFilesDir, gameId);
                string contentHash = HashService.HashFolderContents(mediaFolder);
                if (string.IsNullOrEmpty(contentHash))
                {
                    Logger.Warn($"Failed to create library files content hash for game with id {gameId}. If this game does not have any media files, you can safely ignore this warning.");
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
                            continue;
                        }
                    }
                }
                using (var content = new MultipartFormDataContent())
                {
                    if (Directory.Exists(mediaFolder) == false)
                    {
                        Logger.Warn($"Game library files directory not found: {mediaFolder}");
                        continue;
                    }
                    content.Add(new StringContent(gameId), "gameId");
                    content.Add(new StringContent(contentHash), "contentHash");
                    foreach (var file in Directory.GetFiles(mediaFolder))
                    {
                        var fileContent = new StreamContent(File.OpenRead(file));
                        var fileName = Path.GetFileName(file);
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        content.Add(fileContent, "files", fileName);
                    }
                    var result = await WebServerService
                        .Post(endpoint: WebAppEndpoints.SyncFiles, content: content);
                    if (result == false)
                    {
                        Logger.Warn($"Request to sync library files for {gameId} failed");
                        continue;
                    }
                }
            }
            return true;
        }
    }
}

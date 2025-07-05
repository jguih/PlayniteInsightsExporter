using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteInsightsExporter.Lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
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

        private string GetTempLibraryZipPath()
        {
            return Path.Combine(Plugin.GetPluginUserDataPath(), "library.zip");
        }

        private bool DeleteLibraryZip()
        {
            string tmpZipPath = GetTempLibraryZipPath();
            try
            {
                if (File.Exists(tmpZipPath))
                {
                    File.Delete(tmpZipPath);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to delete library zip file");
                return false;
            }
        }

        private (int includedFiles, ValidationResult result) CreateLibraryZip(
            PlayniteLibraryManifest manifest,
            List<string> gameIdList = null
        ) {
            string tmpZipPath = GetTempLibraryZipPath();
            try
            {
                var includedFiles = 0;
                DeleteLibraryZip();
                using (var zip = ZipFile.Open(tmpZipPath, ZipArchiveMode.Create))
                {
                    foreach (var folder in Directory.GetDirectories(LibraryFilesDir))
                    {
                        string gameId = Path.GetFileName(folder);
                        // Only send game files for games included in the gameIdList when list is not null, otherwise send all files
                        if (gameIdList != null && !gameIdList.Contains(gameId))
                        {
                            continue;
                        }
                        string contentHash = HashService.HashFolderContents(folder);
                        if (manifest != null)
                        {
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
                            // If game not present in manifest, skip sending media files
                            var gameInLibrary = manifest?.gamesInLibrary?
                                .Where((gil) => gil.gameId == gameId)
                                .FirstOrDefault() ?? null;
                            if (gameInLibrary == null)
                            {
                                continue;
                            }
                        }
                        // Add contentHash.txt file with content hash inside
                        string contentHashFilePath = Path.Combine(gameId, "contentHash.txt");
                        var hashEntry = zip.CreateEntry(contentHashFilePath, CompressionLevel.Optimal);
                        using (var entryStream = hashEntry.Open())
                        using (var writer = new StreamWriter(entryStream))
                        {
                            writer.Write(contentHash);
                        }
                        // Add media files
                        foreach (var file in Directory.GetFiles(folder))
                        {
                            string relativePath = Path.Combine(gameId, Path.GetFileName(file));
                            zip.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
                            includedFiles++;
                        }
                    }
                    return (includedFiles, 
                        new ValidationResult(
                            IsValid: true,
                            Message: "",
                            HttpCode: 200
                        ));
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to create library zip file");
                return (0, 
                    new ValidationResult(
                        IsValid: false,
                        Message: "Failed to create library zip file",
                        HttpCode: 500
                    ));
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
        private (List<object> itemsToAdd, List<object> itemsToUpdate) GetItemsToAddAndUpdate(PlayniteLibraryManifest manifest) {
            List<object> itemsToUpdate = new List<object>();
            List<object> itemsToAdd = new List<object>();
            foreach (var game in PlayniteApi.Database.Games)
            {
                var hash = HashService.HashGameMetadata(game);
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

        public async Task<ValidationResult> RunFullWebAppSyncAsync()
        {
            var manifest = await WebServerService.GetManifestAsync();
            var itemsToRemove = GetItemsToRemove(manifest);
            var (itemsToAdd, itemsToUpdate) = GetItemsToAddAndUpdate(manifest);
            var syncGameListCommand = new SyncGameListCommand(itemsToAdd, itemsToRemove, itemsToUpdate);
            string json = CommandToJsonString(syncGameListCommand);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var result = await WebServerService
                    .Post(
                        endpoint: WebAppEndpoints.SyncGames, 
                        content: content);
                if (!result.IsValid)
                {
                    return result;
                }
                return new ValidationResult(
                        IsValid: true,
                        Message: "Library zip file sent to the server sucessfully",
                        HttpCode: 200
                    );
            }
        }

        /// <summary>
        ///     Send library media files to the web server.
        /// </summary>
        /// <param name="gameIdList">List of game IDs to send to the server. If null all games will be sent.</param>
        /// <returns>ValidationResult</returns>
        public async Task<ValidationResult> SendLibraryFilesToWebAppAsync(List<string> gameIdList = null)
        {
            var manifest = await WebServerService.GetManifestAsync();
            var (includedFiles, result) = CreateLibraryZip(manifest, gameIdList);
            if (!result.IsValid)
            {
                return result;
            }
            if (includedFiles == 0)
            {
                DeleteLibraryZip();
                return new ValidationResult(
                        IsValid: true,
                        Message: "Library zip file sent to the server sucessfully",
                        HttpCode: 200
                    );
            }
            string tmpZipPath = GetTempLibraryZipPath();
            using (var content = new MultipartFormDataContent())
            using (var fileStream = File.OpenRead(tmpZipPath))
            using (var fileContent = new StreamContent(fileStream))
            {
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
                result = await WebServerService
                    .Post(
                        endpoint: WebAppEndpoints.SyncFiles,
                        content: fileContent);
                if (!result.IsValid)
                {
                    return result;
                }
                DeleteLibraryZip();
                return new ValidationResult(
                        IsValid: true,
                        Message: "Library zip file sent to the server sucessfully",
                        HttpCode: 200
                    );
            }
        }
    }
}

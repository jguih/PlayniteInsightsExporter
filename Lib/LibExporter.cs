using Newtonsoft.Json;
using Playnite.SDK;
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
        public string LibraryFilesDir { get; }

        public LibExporter(
            PlayniteInsightsExporter Plugin,
            PlayniteInsightsWebServerService WebServerService
        )
        {
            this.Plugin = Plugin;
            this.PlayniteApi = Plugin.PlayniteApi;
            this.LibraryFilesDir = Path.Combine(PlayniteApi.Paths.ConfigurationPath, "library", "files");
            this.WebServerService = WebServerService;
            HashService = new HashService();
        }

        private List<object> GetGamesList()
        {
            return PlayniteApi.Database.Games.Select(g => new
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
                g.CoverImage
            }).Cast<object>().ToList();
        }

        private string ExportGamesToJsonString()
        {
            var games = GetGamesList();
            try
            {
                return JsonConvert.SerializeObject(games, Formatting.Indented);
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

        private ValidationResult DeleteLibraryZip()
        {
            string tmpZipPath = GetTempLibraryZipPath();
            try
            {
                if (File.Exists(tmpZipPath))
                {
                    File.Delete(tmpZipPath);
                }
                return new ValidationResult(
                        IsValid: true,
                        Message: "Library zip file deleted successfully",
                        HttpCode: 200
                    );
            }
            catch (Exception e)
            {
                return new ValidationResult(
                        IsValid: false,
                        Message: "Failed to delete library zip file",
                        HttpCode: 500
                    );
            }
        }

        private async Task<ValidationResult> CreateLibraryZip(List<string> gameIdList = null)
        {
            string tmpZipPath = GetTempLibraryZipPath();
            string locFailedToCreateLibArchive = ResourceProvider.GetString("LOCFailedToCreateLibArchive");
            var result = DeleteLibraryZip();
            if (!result.IsValid)
            {
                return result;
            }
            try
            {
                var manifest = await WebServerService.GetManifestAsync();
                using (var zip = ZipFile.Open(tmpZipPath, ZipArchiveMode.Create))
                {
                    foreach (var folder in Directory.GetDirectories(LibraryFilesDir))
                    {
                        string gameId = Path.GetFileName(folder);
                        if (gameIdList != null && !gameIdList.Contains(gameId))
                        {
                            continue;
                        }
                        string contentHash = HashService.HashFolderContents(folder);
                        var manifestEntry = manifest.mediaExistsFor
                            .Where(m => m.gameId == gameId)
                            .FirstOrDefault();
                        if (manifestEntry != null)
                        {
                            if (manifestEntry.contentHash == contentHash) 
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
                        }
                    }
                    return new ValidationResult(
                            IsValid: true,
                            Message: "",
                            HttpCode: 200
                        );
                }
            }
            catch (Exception e)
            {
                return new ValidationResult(
                        IsValid: false,
                        Message: locFailedToCreateLibArchive,
                        HttpCode: 500
                    );
            }
        }

        public async Task<ValidationResult> SendJsonToWebAppAsync()
        {
            string locSendingLibraryMetadataToServer = ResourceProvider.GetString("LOCSendingLibraryMetadataToServer");
            string json = ExportGamesToJsonString();
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var result = await WebServerService
                    .Post(
                        endpoint: WebAppEndpoints.SyncGames, 
                        content: content, 
                        loadingText: locSendingLibraryMetadataToServer);
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
        public async Task<ValidationResult> SendFilesToWebAppAsync(List<string> gameIdList = null)
        {
            string locSendingLibraryFilesToServer = ResourceProvider.GetString("LOCSendingLibraryFilesToServer");
            ValidationResult result;
            result = await CreateLibraryZip(gameIdList);
            if (!result.IsValid)
            {
                return result;
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
                        content: fileContent,
                        loadingText: locSendingLibraryFilesToServer);
                if (!result.IsValid)
                {
                    return result;
                }
                //result = DeleteLibraryZip();
                //if (!result.IsValid)
                //{
                //    return result;
                //}
                return new ValidationResult(
                        IsValid: true,
                        Message: "Library zip file sent to the server sucessfully",
                        HttpCode: 200
                    );
            }
        }
    }
}

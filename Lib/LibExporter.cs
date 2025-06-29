using Newtonsoft.Json;
using Playnite.SDK;
using PlayniteInsightsExporter.Lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib
{
    class LibExporter
    {
        private PlayniteInsightsExporter Plugin;
        private PlayniteInsightsExporterSettings Settings;
        private IPlayniteAPI PlayniteApi;
        private PlayniteInsightsWebServerService WebServerService;
        public string ExportedGamesFilePath { get; }
        public string FilesDirPath { get; }

        public LibExporter(
            PlayniteInsightsExporter Plugin,
            PlayniteInsightsExporterSettings Settings
        )
        {
            this.Plugin = Plugin;
            this.Settings = Settings;
            this.PlayniteApi = Plugin.PlayniteApi;
            this.ExportedGamesFilePath = Path.Combine(Plugin.GetPluginUserDataPath(), "exported_games.json");
            this.FilesDirPath = Path.Combine(PlayniteApi.Paths.ConfigurationPath, "library", "files");
            WebServerService = new PlayniteInsightsWebServerService(Settings, PlayniteApi);
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

        private string CreateFilesArchive()
        {
            string tmpZipPath = GetTempLibraryZipPath();
            string locPackagingLibFiles = ResourceProvider.GetString("LOCPackagingLibFiles");
            string locFailedToCreateLibArchive = ResourceProvider.GetString("LOCFailedToCreateLibArchive");
            if (File.Exists(tmpZipPath))
            {
                File.Delete(tmpZipPath);
            }
            try
            {
                var result = PlayniteApi.Dialogs.ActivateGlobalProgress(
                    (args) => ZipFile.CreateFromDirectory(
                    FilesDirPath,
                    tmpZipPath,
                    CompressionLevel.Optimal,
                    false
                ), new GlobalProgressOptions(locPackagingLibFiles));
                if (result.Error != null)
                {
                    throw result.Error;
                }
                return "OK";
            }
            catch (Exception e)
            {
                if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                {
                    return $"{locFailedToCreateLibArchive}: {e.InnerException.Message}";
                }
                return $"{locFailedToCreateLibArchive}: {e.Message}";
            }
        }

        public string SendJsonToWebAppAsync()
        {
            string locSendingLibraryMetadataToServer = ResourceProvider.GetString("LOCSendingLibraryMetadataToServer");
            string locFailedToSendLibraryMetadata = ResourceProvider.GetString("LOCLibraryMetadataSendFailed");
            string json = ExportGamesToJsonString();
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var result = WebServerService.Post(WebAppEndpoints.SyncGames, content, locSendingLibraryMetadataToServer);
                if (result != "OK")
                {
                    return $"{locFailedToSendLibraryMetadata}: {result}";
                }
                return "OK";
            }
        }

        public string SendFilesToWebApp()
        {
            string locFailedToSendLibraryFilesToServer = ResourceProvider.GetString("LOCLibraryFilesSendFailed");
            string locSendingLibraryFilesToServer = ResourceProvider.GetString("LOCSendingLibraryFilesToServer");
            var createArchiveResult = CreateFilesArchive();
            if (createArchiveResult != "OK")
            {
                return createArchiveResult;
            }
            string tmpZipPath = GetTempLibraryZipPath();
            using (var content = new MultipartFormDataContent())
            using (var fileStream = File.OpenRead(tmpZipPath))
            using (var fileContent = new StreamContent(fileStream))
            {
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
                var result = WebServerService.Post(WebAppEndpoints.SyncFiles, fileContent, locSendingLibraryFilesToServer);
                if (File.Exists(tmpZipPath))
                {
                    File.Delete(tmpZipPath);
                }
                if (result != "OK")
                {
                    return $"{locFailedToSendLibraryFilesToServer}: {result}";
                }
                return "OK";
            }
        }
    }
}

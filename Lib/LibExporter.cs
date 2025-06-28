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
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib
{
    class LibExporter
    {
        private PlayniteInsightsExporter Plugin;
        private PlayniteInsightsExporterSettings Settings;
        private IPlayniteAPI PlayniteApi;
        private WebAppEndpoints WebAppEndpoints;
        public string ExportedGamesFilePath { get; }
        public string FilesDirPath { get; }

        public LibExporter(
            PlayniteInsightsExporter Plugin,
            PlayniteInsightsExporterSettings Settings,
            WebAppEndpoints WebAppEndpoints = null
        )
        {
            this.Plugin = Plugin;
            this.Settings = Settings;
            this.PlayniteApi = Plugin.PlayniteApi;
            this.ExportedGamesFilePath = Path.Combine(Plugin.GetPluginUserDataPath(), "exported_games.json");
            this.WebAppEndpoints = WebAppEndpoints ?? new WebAppEndpoints();
            this.FilesDirPath = Path.Combine(PlayniteApi.Paths.ConfigurationPath, "library", "files");
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

        public string SendJsonToWebAppAsync(string json = "")
        {
            string locServerURLNotSet = ResourceProvider.GetString("LOCServerURLNotSet");
            string locSendingLibraryMetadataToServer = ResourceProvider.GetString("LOCSendingLibraryMetadataToServer");
            string locFailedToSendLibraryMetadata = ResourceProvider.GetString("LOCLibraryMetadataSendFailed");
            if (string.IsNullOrEmpty(json))
            {
                json = ExportGamesToJsonString();
            }
            if (string.IsNullOrEmpty(Settings.WebAppURL))
            {
                return locServerURLNotSet;
            }
            var client = new HttpClient();
            try 
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                string url = Settings.WebAppURL.TrimEnd('/') + '/' + WebAppEndpoints.SyncGames.TrimStart('/');
                var result = PlayniteApi.Dialogs.ActivateGlobalProgress(
                    async (args) =>
                    {
                        var response = await client.PostAsync(url, content);
                        response.EnsureSuccessStatusCode();
                    },
                    new GlobalProgressOptions(locSendingLibraryMetadataToServer));
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
                    return $"{locFailedToSendLibraryMetadata}: {e.InnerException.Message}";
                }
                return $"{locFailedToSendLibraryMetadata}: {e.Message}";
            } 
            finally
            {
                client.Dispose();
            }
        }

        public string CreateFilesArchive()
        {
            string tmpZipPath = Path.Combine(Plugin.GetPluginUserDataPath(), "library.zip");
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
    }
}

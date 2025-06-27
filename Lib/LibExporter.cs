using Newtonsoft.Json;
using Playnite.SDK;
using PlayniteInsightsExporter.Lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
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

        public string ExportGamesToJsonString()
        {
            var games = GetGamesList();
            try
            {
                return JsonConvert.SerializeObject(games, Formatting.Indented);
            }
            catch (Exception)
            {
                return "";
            }
        }

        public int? ExportGamesToJson()
        {
            var games = this.GetGamesList();
            try
            {
                var json = JsonConvert.SerializeObject(games, Formatting.Indented);
                File.WriteAllText(ExportedGamesFilePath, json);
            }
            catch (Exception)
            {
                return null;
            }
            return games.Count();
        }

        public async Task<string> SendJsonToWebAppAsync(string json = "")
        {
            if (string.IsNullOrEmpty(json))
            {
                json = ExportGamesToJsonString();
            }
            var client = new HttpClient();
            try 
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                string url = Settings.WebAppURL.TrimEnd('/') + '/' + WebAppEndpoints.SyncGames.TrimStart('/');
                HttpResponseMessage response = await client.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    return "OK";
                } 
                else
                {
                    return response.ReasonPhrase;
                }
            } 
            catch (Exception e)
            {
                return e.InnerException.Message;
            } 
            finally
            {
                client.Dispose();
            }
        }
    }
}

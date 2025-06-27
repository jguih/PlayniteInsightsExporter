using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlayniteInsightsExporter.Lib
{
    class LibExporter
    {
        private PlayniteInsightsExporter Plugin;
        private IPlayniteAPI PlayniteApi;
        public string ExportedGamesFilePath { get; }

        public LibExporter(PlayniteInsightsExporter Plugin)
        {
            this.Plugin = Plugin;
            this.PlayniteApi = Plugin.PlayniteApi;
            this.ExportedGamesFilePath = Path.Combine(Plugin.GetPluginUserDataPath(), "exported_games.json");
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

        public int? ExportGamesToJson()
        {
            var games = this.GetGamesList();
            try
            {
                var json = JsonConvert.SerializeObject(games, Formatting.Indented);
                File.WriteAllText(ExportedGamesFilePath, json);
            } catch (Exception e)
            {
                return null;
            }
            return games.Count();
        }
    }
}

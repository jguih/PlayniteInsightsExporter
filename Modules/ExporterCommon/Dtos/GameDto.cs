using ExporterCommon.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Dtos
{
    [JsonObject]
    public class GameDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<AppPlatform> Platforms { get; set; } = new List<AppPlatform>();
        public List<AppGenre> Genres { get; set; } = new List<AppGenre>();
        public List<AppCompany> Developers { get; set; } = new List<AppCompany>();
        public List<AppCompany> Publishers { get; set; } = new List<AppCompany>();
        public DateTime? ReleaseDate { get; set; } = null;
        public ulong Playtime { get; set; } = 0;
        public DateTime? LastActivity { get; set; } = null;
        public DateTime? Added { get; set; } = null;
        public string InstallDirectory { get; set; } = null;
        public bool IsInstalled { get; set; } = false;
        public string BackgroundImage { get; set; } = null;
        public string CoverImage { get; set; } = null;
        public string Icon { get; set; } = null;
        public string Description { get; set; } = null;
        public bool Hidden { get; set; } = false;
        public AppCompletionStatus CompletionStatus { get; set; }
        public string ContentHash { get; set; }

        public GameDto() { }

        public static GameDto FromGame(AppGame game, string contentHash)
        {
            return new GameDto()
            {
                Id = game.Id,
                Name = game.Name,
                Platforms = game.Platforms,
                Genres = game.Genres,
                Developers = game.Developers,
                Publishers = game.Publishers,
                ReleaseDate = game.ReleaseDate.Value,
                Playtime = game.Playtime,
                LastActivity = game.LastActivity,
                Added = game.Added,
                InstallDirectory = game.InstallDirectory,
                IsInstalled = game.IsInstalled,
                BackgroundImage = game.BackgroundImage,
                CoverImage = game.CoverImage,
                Icon = game.Icon,
                Description = game.Description,
                Hidden = game.IsHidden,
                CompletionStatus = game.CompletionStatus,
                ContentHash = contentHash
            };
        }
    }
}

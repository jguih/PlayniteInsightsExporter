using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dtos
{
    public class GameDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<Platform> Platforms { get; set; } = new List<Platform>();
        public List<Genre> Genres { get; set; } = new List<Genre>();
        public List<Company> Developers { get; set; } = new List<Company>();
        public List<Company> Publishers { get; set; } = new List<Company>();
        public ReleaseDate? ReleaseDate { get; set; }
        public ulong Playtime { get; set; }
        public DateTime? LastActivity { get; set; }
        public DateTime? Added { get; set; }
        public string InstallDirectory { get; set; }
        public bool IsInstalled { get; set; }
        public string BackgroundImage { get; set; }
        public string CoverImage { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public bool Hidden { get; set; }
        public CompletionStatus CompletionStatus { get; set; }
        public string ContentHash { get; set; }

        public GameDto() { }

        public static GameDto FromGame(Game game, string contentHash)
        {
            return new GameDto()
            {
                Id = game.Id,
                Name = game.Name,
                Platforms = game.Platforms,
                Genres = game.Genres,
                Developers = game.Developers,
                Publishers = game.Publishers,
                ReleaseDate = game.ReleaseDate,
                Playtime = game.Playtime,
                LastActivity = game.LastActivity,
                Added = game.Added,
                InstallDirectory = game.InstallDirectory,
                IsInstalled = game.IsInstalled,
                BackgroundImage = game.BackgroundImage,
                CoverImage = game.CoverImage,
                Icon = game.Icon,
                Description = game.Description,
                Hidden = game.Hidden,
                CompletionStatus = game.CompletionStatus,
                ContentHash = contentHash
            };
        }
    }
}

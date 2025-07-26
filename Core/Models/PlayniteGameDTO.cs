using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class PlayniteGameDTO
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
        public string ContentHash { get; set; }

        public PlayniteGameDTO() { }
    }
}

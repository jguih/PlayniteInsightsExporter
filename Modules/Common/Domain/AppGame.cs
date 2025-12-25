using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Domain
{
    public class AppGame
    {
        public Guid Id { get; }
        public string Name { get; set; }
        public List<Platform> Platforms { get; set; } = new List<Platform>();
        public List<Genre> Genres { get; set; } = new List<Genre>();
        public List<AppCompany> Developers { get; set; } = new List<AppCompany>();
        public List<AppCompany> Publishers { get; set; } = new List<AppCompany>();
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
    }
}

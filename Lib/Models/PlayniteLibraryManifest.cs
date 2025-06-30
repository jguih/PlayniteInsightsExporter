using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib.Models
{
    public class PlayniteLibraryManifestMediaExistsFor
    {
        public string gameId { get; set; }
        public string contentHash { get; set; }
    }

    public class PlayniteLibraryManifest
    {
        public int totalGamesInLibrary { get; set; }
        public List<string> gamesInLibrary { get; set; } = new List<string>();
        public PlayniteLibraryManifestMediaExistsFor[] mediaExistsFor { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib.Models
{
    public class PlayniteLibraryManifestMediaExistsFor
    {
        public string gameId { get; set; } = null;
        public string contentHash { get; set; } = null;
    }

    public class PlayniteLibraryManifest
    {
        public Nullable<int> totalGamesInLibrary { get; set; }
        public List<string> gamesInLibrary { get; set; } = new List<string>();
        public List<PlayniteLibraryManifestMediaExistsFor> mediaExistsFor { get; set; } = new List<PlayniteLibraryManifestMediaExistsFor>();
    }
}

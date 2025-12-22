using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Application
{
    public class PlayAtlasLibraryManifestMediaExistsFor
    {
        public string gameId { get; set; } = null;
        public string contentHash { get; set; } = null;
    }

    public class PlayAtlasLibraryManifestGameInLibrary
    {
        public string gameId { get; set; } = null;
        public string contentHash { get; set; } = null;
    }

    public class PlayAtlasLibraryManifest
    {
        public int totalGamesInLibrary { get; set; } = 0;
        public List<PlayAtlasLibraryManifestGameInLibrary> gamesInLibrary { get; set; } = new List<PlayAtlasLibraryManifestGameInLibrary>();
        public List<PlayAtlasLibraryManifestMediaExistsFor> mediaExistsFor { get; set; } = new List<PlayAtlasLibraryManifestMediaExistsFor>();
    }
}

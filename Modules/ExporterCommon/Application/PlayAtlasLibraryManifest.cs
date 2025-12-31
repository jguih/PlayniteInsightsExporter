using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Application
{
    public class PlayAtlasLibraryManifestItem
    {
        public readonly string GameId;
        public readonly string ContentHash;

        public PlayAtlasLibraryManifestItem(string gameId, string contentHash)
        {
            GameId = gameId;
            ContentHash = contentHash;
        }
    }

    public class PlayAtlasLibraryManifest
    {
        public readonly int TotalGamesInLibrary = 0;
        public readonly IReadOnlyCollection<PlayAtlasLibraryManifestItem> GamesInLibrary = new List<PlayAtlasLibraryManifestItem>();
        public readonly IReadOnlyCollection<PlayAtlasLibraryManifestItem> MediaExistsFor = new List<PlayAtlasLibraryManifestItem>();

        public PlayAtlasLibraryManifest(
            int totalGamesInLibrary, 
            IReadOnlyCollection<PlayAtlasLibraryManifestItem> gamesInLibrary, 
            IReadOnlyCollection<PlayAtlasLibraryManifestItem> mediaExistsFor
        )
        {
            TotalGamesInLibrary = totalGamesInLibrary;
            GamesInLibrary = gamesInLibrary;
            MediaExistsFor = mediaExistsFor;
        }
    }
}

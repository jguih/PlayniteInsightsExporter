﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class PlayniteLibraryManifestMediaExistsFor
    {
        public string gameId { get; set; } = null;
        public string contentHash { get; set; } = null;
    }

    public class PlayniteLibraryManifestGameInLibrary
    {
        public string gameId { get; set; } = null;
        public string contentHash { get; set; } = null;
    }

    public class PlayniteLibraryManifest
    {
        public Nullable<int> totalGamesInLibrary { get; set; }
        public List<PlayniteLibraryManifestGameInLibrary> gamesInLibrary { get; set; } = new List<PlayniteLibraryManifestGameInLibrary>();
        public List<PlayniteLibraryManifestMediaExistsFor> mediaExistsFor { get; set; } = new List<PlayniteLibraryManifestMediaExistsFor>();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib.Models
{
    public class WebAppEndpoints
    {
        public const string SyncGames = "/api/sync/games";
        public const string SyncFiles = "/api/sync/files";
        public const string SyncManifest = "/api/sync/manifest";
        public const string OpenSession = "/api/session/open";
        public const string CloseSession = "/api/session/close";
    }
}
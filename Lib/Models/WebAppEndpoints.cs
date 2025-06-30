using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib.Models
{
    class WebAppEndpoints
    {
        public const string SyncGames = "/api/sync/games";
        public const string SyncFiles = "/api/sync/files";
        public const string SyncManifest = "/api/sync/manifest";

        public static string SyncGamesUrl(string webAppUrl)
        {
            return $"{webAppUrl.TrimEnd('/')}{SyncGames}";
        }

        public static string SyncFilesUrl(string webAppUrl)
        {
            return $"{webAppUrl.TrimEnd('/')}{SyncFiles}";
        }
    }
}
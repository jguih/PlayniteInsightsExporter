using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class WebAppEndpoints
    {
        public const string SyncGames = "/api/extension/sync/games";
        public const string SyncFiles = "/api/extension/sync/files";
        public const string SyncManifest = "/api/extension/sync/manifest";
        public const string OpenSession = "/api/extension/session/open";
        public const string CloseSession = "/api/extension/session/close";
        public const string HealthCheck = "/api/extension/health";
        public const string Register = "/api/extension/register";
    }
}
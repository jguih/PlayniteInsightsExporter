using Core;
using Infra;
using Playnite.SDK;
using PlayniteInsightsExporter.Lib.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib
{
    public class ServiceLocator
    {
        public IAppLogger AppLogger { get; }
        public IPlayniteInsightsWebServerService WebServerService { get; }
        public LibExporter LibExporter { get; }
        public IHashService HashService { get; }
        public IFileSystemService FileSystemService { get; }
        public IGameSessionService SessionTrackingService { get; }

        public ServiceLocator(
            PlayniteInsightsExporter plugin, 
            ILogger logger,
            PlayniteInsightsExporterSettings settings
        ) {
            var libDir = Path.Combine(plugin.PlayniteApi.Paths.ConfigurationPath, "library", "files");
            var PlayniteApiCtx = new PlayniteApiCtx(plugin.PlayniteApi);
            AppLogger = new PlayniteLogger(logger);
            FileSystemService = new FileSystemService();
            WebServerService = new PlayniteInsightsWebServerService(settings.WebAppURL, AppLogger);
            HashService = new HashService(AppLogger);
            LibExporter = new LibExporter(
                PlayniteApiCtx, 
                WebServerService, 
                AppLogger, 
                HashService, 
                libDir, 
                FileSystemService);
            SessionTrackingService = new GameSessionService(
                plugin, 
                AppLogger, 
                HashService,
                WebServerService,
                FileSystemService);
        }
    }
}

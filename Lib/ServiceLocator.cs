using Playnite.SDK;
using PlayniteInsightsExporter.Lib.Logger;
using System;
using System.Collections.Generic;
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
        public ISessionTrackingService SessionTrackingService { get; }

        public ServiceLocator(
            PlayniteInsightsExporter plugin, 
            ILogger logger,
            PlayniteInsightsExporterSettings settings
        ) {
            AppLogger = new PlayniteLogger(logger);
            WebServerService = new PlayniteInsightsWebServerService(plugin, settings, logger);
            LibExporter = new LibExporter(plugin, WebServerService, logger);
            HashService = new HashService(logger);
            FileSystemService = new FileSystemService();
            SessionTrackingService = new SessionTrackingService(
                plugin, 
                AppLogger, 
                HashService,
                WebServerService,
                FileSystemService);
        }
    }
}

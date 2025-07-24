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
        public IGameSessionService GameSessionService { get; }

        public ServiceLocator(
            PlayniteInsightsExporter plugin, 
            ILogger logger,
            PlayniteInsightsExporterSettings settings
        ) {
            AppLogger = new PlayniteLogger(logger);
            var libDir = Path.Combine(plugin.PlayniteApi.Paths.ConfigurationPath, "library", "files");
            IPlayniteProgressService progressService = new PlayniteProgressService(plugin.PlayniteApi, AppLogger);
            IPlayniteGameRepository gameRepository = new PlayniteGameRepository(plugin.PlayniteApi, AppLogger);
            FileSystemService = new FileSystemService();
            WebServerService = new PlayniteInsightsWebServerService(settings.WebAppURL, AppLogger);
            HashService = new HashService(AppLogger);
            LibExporter = new LibExporter(
                progressService, 
                gameRepository,
                WebServerService, 
                AppLogger, 
                HashService, 
                libDir, 
                FileSystemService);

            var gameSessionConfig = new GameSessionConfig
            {
                SESSIONS_DIR_PATH = Path.Combine(plugin.GetPluginUserDataPath(), "sessions"),
            };
            GameSessionService = new GameSessionService(
                plugin, 
                AppLogger, 
                HashService,
                WebServerService,
                FileSystemService,
                gameSessionConfig);
        }
    }
}

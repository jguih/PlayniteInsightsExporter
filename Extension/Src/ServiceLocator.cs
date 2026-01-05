using Core;
using Core.ExtensionRegistration;
using Core.Screencapture;
using Infra;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter
{
    public class ServiceLocator
    {
        public IAppLogger AppLogger { get; }
        public IPlayAtlasWebServerService WebServerService { get; }
        public LibExporter LibExporter { get; }
        public IHashService HashService { get; }
        public IFileSystemService FileSystemService { get; }
        public IGameSessionService GameSessionService { get; }
        public IPlayniteProgressService ProgressService { get; }
        public IPlayniteGameRepository GameRepository { get; }
        public IScreenCaptureService ScreenCaptureService { get; }
        public HttpServer HttpServer { get; }
        public IExtensionRegistrationService ExtensionRegistrationService { get; }

        public ServiceLocator(
            PlayniteInsightsExporter plugin,
            ILogger logger
        )
        {
            var libDir = Path.Combine(plugin.PlayniteApi.Paths.ConfigurationPath, "library", "files");
            var gameSessionConfig = new GameSessionConfig
            {
                SESSIONS_DIR_PATH = Path.Combine(plugin.GetPluginUserDataPath(), "sessions"),
            };
            var securityDir = Path.Combine(plugin.GetPluginUserDataPath(), "security");
            var shareXService = new ShareXService(plugin);
            var keyManager = new KeyManager(plugin);
            var signatureService = new SignatureService(keyManager);
            var fileSystemService = new FileSystemService();

            AppLogger = new PlayniteLogger(logger);
            ProgressService = new PlayniteProgressService(plugin.PlayniteApi, AppLogger);
            GameRepository = new PlayniteGameRepository(plugin.PlayniteApi, AppLogger);
            FileSystemService = new FileSystemService();
            HashService = new HashService(AppLogger, fileSystemService);
            WebServerService = new PlayAtlasWebServerService(
                plugin, 
                AppLogger, 
                signatureService, 
                HashService,
                () => ExtensionRegistrationService.GetRegistrationId());
            LibExporter = new LibExporter(
                ProgressService,
                GameRepository,
                WebServerService,
                AppLogger,
                HashService,
                libDir,
                FileSystemService);
            GameSessionService = new GameSessionService(
                plugin,
                AppLogger,
                HashService,
                WebServerService,
                FileSystemService,
                gameSessionConfig,
                ProgressService);
            ScreenCaptureService = new ScreenCaptureService(shareXService);
            HttpServer = new HttpServer(plugin, AppLogger, ScreenCaptureService, signatureService);
            ExtensionRegistrationService = new ExtensionRegistrationService(keyManager, WebServerService, plugin, fileSystemService);
        }
    }
}

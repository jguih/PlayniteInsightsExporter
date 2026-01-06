using ExporterBootstrap.Application;
using ExporterBootstrap.Application.Module;
using ExporterBootstrap.Application.Modules;
using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterSystem.Infra;
using Playnite.SDK;
using PlayniteInsightsExporter.Adapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter
{
    public sealed class ExporterCompositionRoot
    {
        private readonly ILogger playniteLogger;
        private readonly IPlayniteAPI playniteApi;
        private readonly IExporterPluginContextPort plugin;

        public ExporterCompositionRoot(
            ILogger playniteLogger,
            IPlayniteAPI playniteApi,
            IExporterPluginContextPort plugin
        )
        {
            this.playniteLogger = playniteLogger;
            this.playniteApi = playniteApi;
            this.plugin = plugin;
        }

        public ExporterApi Build()
        {
            IAppLoggerPort appLogger = new AppLoggerAdapter(playniteLogger);
            IPlayniteGameRepositoryPort gameRepository = new PlayniteGameRepositoryAdapter(playniteApi);
            IFileSystemServicePort fileSystem = new FileSystemService();

            ISystemConfigPort systemConfig = new SystemConfig(
                fileSystemService: fileSystem,
                configDirPath: plugin.GetConfigurationDirPath(),
                dataDirPath: plugin.GetExtensionDataDirPath()
            );

            IInfraModulePort infra = new InfraModule(
                fileSystem,
                systemConfig,
                plugin,
                appLogger,
                gameRepository
            );

            IPlayAtlasClientModulePort playAtlasClient = new PlayAtlasClientModule(
                fileSystem,
                plugin,
                appLogger,
                systemConfig,
                infra.SignatureService,
                infra.HashService,
                infra.KeyManager
            );

            ILibrarySyncModulePort librarySync = new LibrarySyncModule(
                appLogger,
                playAtlasClient.Client,
                infra.HashService,
                fileSystem,
                systemConfig,
                gameRepository
            );

            IPlayniteIntegrationModulePort playniteIntegration = 
                new PlayniteIntegrationModule(gameRepository);

            IGameSessionModulePort gameSession = new GameSessionModule(
                    appLogger,
                    infra.HashService,
                    playAtlasClient.Client,
                    fileSystem,
                    systemConfig
                );

            var bootstrapper = new ExporterBootstraper(
                    appLogger,
                    infra,
                    playAtlasClient,
                    librarySync,
                    playniteIntegration,
                    gameSession
                );
            return bootstrapper.BootstrapExporterApi();
        }
    }

}

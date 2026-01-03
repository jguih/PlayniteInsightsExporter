using ExporterBootstrap.Adapters;
using ExporterBootstrap.Application;
using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterLibraryExporter.Application;
using ExporterPlayAtlasClient.Application;
using ExporterPlayAtlasClient.Infra;
using ExporterPlayniteIntegration.Queries.GetAllGames;
using ExporterSystem.Infra;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application
{
    public class ExporterBootstraper
    {
        private readonly IExporterPluginContextPort plugin;
        private readonly IPlayniteAPI playniteAPI;
        private readonly ILogger logger;
        private ExporterApi Api { get; set; } = null;

        public ExporterBootstraper(
            IExporterPluginContextPort pluginContext,
            IPlayniteAPI playniteAPI,
            ILogger logger
        )
        {
            this.plugin = pluginContext;
            this.playniteAPI = playniteAPI;
            this.logger = logger;
        }

        public ExporterApi BootstrapExporterApi()
        {
            if (Api != null)
            {
                return Api;
            }

            IAppLoggerPort appLogger = new AppLoggerAdapter(logger);
            IFileSystemServicePort fileSystemService = new FileSystemService();
            // Config
            var systemConfig = new SystemConfig(plugin, fileSystemService);
            // Infra
            var keyManager = new KeyManager(systemConfig, fileSystemService, appLogger);
            var hashService = new HashService(fileSystemService);
            var signatureService = new SignatureService(
                appLogger, 
                keyManager, 
                plugin
            );
            var playniteGameRepository = new PlayniteGameRepositoryAdapter(
                api: playniteAPI
            );
            var environmentInitializer = new InfraEnvironmentInitializer(
               fileSystemService: fileSystemService,
               keyManager: keyManager,
               systemConfig: systemConfig,
               appLogger: appLogger
            );
            // PlayAtlas Client
            var syncGamesHttpContentBuilder = new SyncGamesHttpContentBuilder();
            var syncMediaFilesHttpContentBuilder = new SyncMediaFilesHttpContentBuilder(fileSystemService);
            var syncGamesDtoMapper = new SyncGamesDtoMapper();
            var playAtlasHttpClient = new PlayAtlasHttpClient(
                appLogger: appLogger,
                pluginContext: plugin,
                systemConfig: systemConfig,
                signatureService: signatureService,
                hashService: hashService,
                syncGamesHttpContentBuilder: syncGamesHttpContentBuilder,
                syncMediaFilesHttpContentBuilder: syncMediaFilesHttpContentBuilder,
                syncGamesDtoMapper: syncGamesDtoMapper
            );
            // Library Sync
            var librarySyncService = new LibrarySyncService(
                 appLogger: appLogger,
                 playAtlasHttpClient: playAtlasHttpClient,
                 hashService: hashService,
                 fileSystemService: fileSystemService,
                 systemConfig: systemConfig,
                 gameRepository: playniteGameRepository
            );
            // Playnite Integration
            var getAllGamesQueryHandler = new GetAllGamesQueryHandler(
                playniteGameRepository: playniteGameRepository
            );

            var configApi = new ExporterConfigApi(
                systemConfig: systemConfig    
            );
            var infraApi = new ExporterInfraApi(
                fileSystemService: fileSystemService,
                hashService: hashService,
                keyManager: keyManager,
                signatureService: signatureService,
                playniteGameRepository: playniteGameRepository
            );
            var playAtlasClientApi = new ExporterPlayAtlasClientApi(playAtlasHttpClient);
            var librarySyncApi = new ExporterLibrarySyncApi(librarySyncService);
            var playniteIntegrationApi = new ExporterPlayniteIntegrationApi(
                query: new ExporterPlayniteIntegrationApiQuery(
                    getAllGamesQueryHandler
                )
            );

            Api = new ExporterApi(
                playAtlasClientApi,
                librarySyncApi,
                playniteIntegrationApi,
                environmentInitializer,
                appLogger
            );
            return Api;
        }
    }
}

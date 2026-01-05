using ExporterBootstrap.Application.Module;
using ExporterBootstrap.Application.Modules;
using ExporterCommon.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application
{
    public class ExporterBootstraper
    {
        private readonly IAppLoggerPort AppLogger;
        private readonly IInfraModulePort Infra;
        private readonly IPlayAtlasClientModulePort PlayAtlasClient;
        private readonly ILibrarySyncModulePort LibrarySync;
        private readonly IPlayniteIntegrationModulePort PlayniteIntegration;
        private readonly IGameSessionModulePort GameSession;
        private ExporterApi Api { get; set; } = null;

        public ExporterBootstraper(
            IAppLoggerPort appLogger,
            IInfraModulePort infra,
            IPlayAtlasClientModulePort playAtlasClient,
            ILibrarySyncModulePort librarySync,
            IPlayniteIntegrationModulePort playniteIntegration,
            IGameSessionModulePort gameSession
        )
        {
            AppLogger = appLogger;
            Infra = infra;
            PlayAtlasClient = playAtlasClient;
            LibrarySync = librarySync;
            PlayniteIntegration = playniteIntegration;
            GameSession = gameSession;
        }

        public ExporterApi BootstrapExporterApi()
        {
            if (Api != null)
            {
                return Api;
            }

            var playAtlasClientApi = new ExporterPlayAtlasClientApi(PlayAtlasClient.Client);

            var librarySyncApi = new ExporterLibrarySyncApi(
                LibrarySync.LibrarySyncService,
                LibrarySync.GameSyncItemFactory
            );

            var playniteIntegrationApi = new ExporterPlayniteIntegrationApi(
                query: new ExporterPlayniteIntegrationApiQuery(
                    PlayniteIntegration.GetAllGamesQueryHandler
                )
            );

            var gameSessionApi = new ExporterGameSessionApi(GameSession.GameSessionService);

            Api = new ExporterApi(
                playAtlasClientApi,
                librarySyncApi,
                playniteIntegrationApi,
                gameSessionApi,
                Infra.EnvironmentInitializer,
                AppLogger
            );
            return Api;
        }
    }
}

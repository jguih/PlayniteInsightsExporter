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
        private ExporterApi Api { get; set; } = null;

        public ExporterBootstraper(
            IAppLoggerPort appLogger,
            IInfraModulePort infra,
            IPlayAtlasClientModulePort playAtlasClient,
            ILibrarySyncModulePort librarySync,
            IPlayniteIntegrationModulePort playniteIntegration
        )
        {
            AppLogger = appLogger;
            Infra = infra;
            PlayAtlasClient = playAtlasClient;
            LibrarySync = librarySync;
            PlayniteIntegration = playniteIntegration;
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

            Api = new ExporterApi(
                playAtlasClientApi,
                librarySyncApi,
                playniteIntegrationApi,
                Infra.EnvironmentInitializer,
                AppLogger
            );
            return Api;
        }
    }
}

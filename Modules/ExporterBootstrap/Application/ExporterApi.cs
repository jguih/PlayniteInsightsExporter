using ExporterCommon.Application;
using ExporterSystem.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application
{
    public class ExporterApi
    {
        public readonly ExporterPlayAtlasClientApi PlayAtlasClient;
        public readonly ExporterLibrarySyncApi LibrarySync;
        public readonly ExporterPlayniteIntegrationApi PlayniteIntegration;
        public readonly ExporterGameSessionApi GameSession;
        public readonly IEnvironmentInitializerPort EnvironmentInitializer;
        public readonly IAppLoggerPort Logger;

        public ExporterApi(
            ExporterPlayAtlasClientApi playAtlasClientApi,
            ExporterLibrarySyncApi libraryExporterApi,
            ExporterPlayniteIntegrationApi playniteIntegrationApi,
            ExporterGameSessionApi gameSessionApi,
            IEnvironmentInitializerPort environmentInitializer,
            IAppLoggerPort appLogger
        )
        {
            PlayAtlasClient = playAtlasClientApi;
            LibrarySync = libraryExporterApi;
            PlayniteIntegration = playniteIntegrationApi;
            GameSession = gameSessionApi;
            EnvironmentInitializer = environmentInitializer;
            Logger = appLogger;
        }
    }
}

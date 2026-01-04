using ExporterBootstrap.Adapters;
using ExporterCommon.Application;
using ExporterCommon.Infra;
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
        public readonly InfraEnvironmentInitializer EnvironmentInitializer;
        public readonly IAppLoggerPort Logger;
        public readonly IPlayniteGameMapperPort PlayniteGameMapper;
        public readonly IPlayniteGameExtractorPort PlayniteGameExtractor;

        public ExporterApi(
            ExporterPlayAtlasClientApi playAtlasClientApi,
            ExporterLibrarySyncApi libraryExporterApi,
            ExporterPlayniteIntegrationApi playniteIntegrationApi,
            InfraEnvironmentInitializer environmentInitializer,
            IAppLoggerPort appLogger,
            IPlayniteGameMapperPort playniteGameMapper,
            IPlayniteGameExtractorPort playniteGameExtractor
        )
        {
            this.PlayAtlasClient = playAtlasClientApi;
            this.LibrarySync = libraryExporterApi;
            this.PlayniteIntegration = playniteIntegrationApi;
            this.EnvironmentInitializer = environmentInitializer;
            this.Logger = appLogger;
            this.PlayniteGameMapper = playniteGameMapper;
            this.PlayniteGameExtractor = playniteGameExtractor;
        }
    }
}

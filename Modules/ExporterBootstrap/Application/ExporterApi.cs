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
        public readonly ExporterLibraryExporterApi LibraryExporter;
        public readonly ExporterPlayniteIntegrationApi PlayniteIntegration;
        public readonly InfraEnvironmentInitializer EnvironmentInitializer;
        public readonly IAppLoggerPort Logger;

        public ExporterApi(
            ExporterPlayAtlasClientApi playAtlasClientApi,
            ExporterLibraryExporterApi libraryExporterApi,
            ExporterPlayniteIntegrationApi playniteIntegrationApi,
            InfraEnvironmentInitializer environmentInitializer,
            IAppLoggerPort appLogger
        )
        {
            this.PlayAtlasClient = playAtlasClientApi;
            this.LibraryExporter = libraryExporterApi;
            this.PlayniteIntegration = playniteIntegrationApi;
            this.EnvironmentInitializer = environmentInitializer;
            this.Logger = appLogger;
        }
    }
}

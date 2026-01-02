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
        public readonly ExporterConfigApi Config;
        public readonly ExporterInfraApi Infra;
        public readonly ExporterPlayAtlasClientApi PlayAtlasClient;
        public readonly ExporterLibraryExporterApi LibraryExporter;

        public ExporterApi(
            IAppLoggerPort appLogger,
            IExporterPluginContextPort pluginContext
        )
        {
            IFileSystemServicePort fileSystemService = new FileSystemService();

            Config = new ExporterConfigApi(pluginContext, fileSystemService);
            Infra = new ExporterInfraApi(appLogger, Config.SystemConfig, fileSystemService);
            PlayAtlasClient = new ExporterPlayAtlasClientApi(
                appLogger, 
                pluginContext, 
                Config.SystemConfig, 
                Infra.SignatureService,
                Infra.HashService,
                Infra.FileSystemService
            );
            LibraryExporter = new ExporterLibraryExporterApi(
                appLogger,
                PlayAtlasClient.PlayAtlasHttpClient,
                Infra.HashService,
                Infra.FileSystemService,
                Config.SystemConfig
            );
        }

        public void InitEnvironment()
        {
            Infra.InitInfra();
        }
    }
}

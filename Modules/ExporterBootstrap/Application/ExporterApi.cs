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

        public ExporterApi(
            IAppLoggerPort appLogger,
            IExporterPluginContextPort pluginContext
        )
        {
            IFileSystemServicePort fileSystemService = new FileSystemService();

            Config = new ExporterConfigApi(pluginContext, fileSystemService);
            Infra = new ExporterInfraApi(Config, appLogger, fileSystemService);
            PlayAtlasClient = new ExporterPlayAtlasClientApi(appLogger, pluginContext, Config, Infra);
        }

        public void InitEnvironment()
        {
            Infra.InitInfra();
        }
    }
}

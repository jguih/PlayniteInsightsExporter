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
        public readonly ExporterConfigApi config;
        public readonly ExporterInfraApi infra;

        public ExporterApi(
            IAppLoggerPort appLogger,
            IExporterPluginContextPort pluginContext
        )
        {
            IFileSystemServicePort fileSystemService = new FileSystemService();

            config = new ExporterConfigApi(pluginContext, fileSystemService);
            infra = new ExporterInfraApi(config, appLogger, fileSystemService);
        }
    }
}

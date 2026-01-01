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
    public class ExporterConfigApi
    {
        public ISystemConfigPort SystemConfig { get; }

        public ExporterConfigApi(
            IExporterPluginContextPort pluginContext,
            IFileSystemServicePort fileSystemService
        ) 
        {
            SystemConfig = new SystemConfig(pluginContext, fileSystemService);
        }
    }
}

using ExporterCommon.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application
{
    public class ExporterConfigApi
    {
        public readonly ISystemConfigPort SystemConfig;

        public ExporterConfigApi(ISystemConfigPort systemConfig)
        {
            SystemConfig = systemConfig;
        }
    }
}

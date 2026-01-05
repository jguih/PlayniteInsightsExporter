using ExporterCommon.Application;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Adapters
{
    public class AppLoggerAdapter : IAppLoggerPort
    {
        private readonly ILogger logger;

        public AppLoggerAdapter(ILogger logger) 
        {
            this.logger = logger;
        }

        public void Debug(string message)
        {
            logger.Debug(message);
        }

        public void Error(string message, Exception ex = null)
        {
            logger.Error(ex, message);
        }

        public void Info(string message)
        {
            logger.Info(message);
        }

        public void Warn(string message)
        {
            logger.Warn(message);
        }
    }
}

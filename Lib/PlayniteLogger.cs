using Core;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib.Logger
{
    public class PlayniteLogger : IAppLogger
    {
        private readonly ILogger Logger;

        public PlayniteLogger(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Error(Exception ex, string message)
        {
            Logger.Error(ex, message);
        }

        public void Info(string message)
        {
            Logger.Info(message);
        }

        public void Warn(string message)
        {
            Logger.Warn(message);
        }

        public void Debug(string message)
        {
            Logger.Debug(message);
        }
    }
}

using ExporterCommon.Application;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Application.SseEventHandlers
{
    public sealed class TakeScreenshotSseHandler : ISseEventHandlerPort
    {
        public string EventType => "take-screenshot";

        private readonly IAppLoggerPort logger;
        private readonly IExporterPluginContextPort pluginContext;

        public TakeScreenshotSseHandler(
            IAppLoggerPort logger
        )
        {
            this.logger = logger;
        }

        public void Handle(string json, string eventId)
        {
            var cmd = JsonConvert.DeserializeObject<TakeScreenshotCommand>(json);
            var shareXPath = pluginContext.GetShareXExePath();

            logger.Debug($"Handling TakeScreenshot ({eventId})");

            var psi = new ProcessStartInfo
            {
                FileName = shareXPath,
                Arguments = "-ActiveWindow -silent -autoclose",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                process.WaitForExit();
            }
        }
    }

}

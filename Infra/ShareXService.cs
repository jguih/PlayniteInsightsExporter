using Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infra
{
    public class ShareXService : IShareXService
    {
        private readonly IPlayniteInsightsExporterContext Ctx;

        public ShareXService(IPlayniteInsightsExporterContext Ctx)
        {
            this.Ctx = Ctx;
        }

        public void TakeScreenshot()
        {
            var shareXPath = Ctx.GetShareXExePath();

            var psi = new ProcessStartInfo
            {
                FileName = shareXPath,
                Arguments = "-ActiveWindow -silent -autoclose",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi)) {
                process.WaitForExit();
            }
        }
    }
}

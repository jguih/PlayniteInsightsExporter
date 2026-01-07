using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infra
{
    public class ShareXService
    {
        public void TakeScreenshot()
        {
            var shareXPath = ""; // TODO

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

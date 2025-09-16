using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Screencapture
{
    public class ScreenCaptureService : IScreenCaptureService
    {
        private readonly IShareXService ShareXService;

        public ScreenCaptureService(IShareXService shareXService)
        {
            ShareXService = shareXService;
        }

        public void TakeScreenshot()
        {
            ShareXService.TakeScreenshot();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Src.Notifications
{
    public interface ISyncFeedbackChannelPort
    {
        void ShowError(string message, string title);
        void ShowWarning(string message, string title);
        void ShowSuccess(string message, string title);
    }

}

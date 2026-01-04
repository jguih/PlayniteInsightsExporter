using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Src.Notifications
{
    public class DialogFeedbackChannel : ISyncFeedbackChannelPort
    {
        private readonly IPlayniteAPI PlayniteApi;

        public DialogFeedbackChannel(IPlayniteAPI PlayniteApi)
        {
            this.PlayniteApi = PlayniteApi;
        }

        public void ShowError(string message, string title)
        {
            PlayniteApi.Dialogs.ShowErrorMessage(message, title);
        }

        public void ShowSuccess(string message, string title)
        {
            PlayniteApi.Dialogs.ShowMessage(message, title);
        }

        public void ShowWarning(string message, string title)
        {
            PlayniteApi.Dialogs.ShowMessage(message, title);
        }
    }
}

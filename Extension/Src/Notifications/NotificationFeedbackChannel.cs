using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Src.Notifications
{
    public class NotificationFeedbackChannel : ISyncFeedbackChannelPort
    {

        private readonly IPlayniteAPI PlayniteApi;

        public NotificationFeedbackChannel(IPlayniteAPI PlayniteApi)
        {
            this.PlayniteApi = PlayniteApi;
        }

        public void ShowError(string message, string title)
        {
            PlayniteApi
                .Notifications
                .Add($"{message}-{title}", message, NotificationType.Error);
        }

        public void ShowSuccess(string message, string title)
        {
            PlayniteApi
                .Notifications
                .Add($"{message}-{title}", message, NotificationType.Info);
        }

        public void ShowWarning(string message, string title)
        {
            PlayniteApi
                .Notifications
                .Add($"{message}-{title}", message, NotificationType.Info);
        }
    }
}

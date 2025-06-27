using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteInsightsExporter.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PlayniteInsightsExporter
{
    public class PlayniteInsightsExporter : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private PlayniteInsightsExporterSettingsViewModel Settings { get; set; }
        private LibExporter LibExporter { get; set; }

        public readonly string Name = "Playnite Insights Exporter";
        public override Guid Id { get; } = Guid.Parse("ccbe324c-c160-4ad5-b749-5c64f8cbc113");

        public PlayniteInsightsExporter(IPlayniteAPI api) : base(api)
        {
            Settings = new PlayniteInsightsExporterSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
            LibExporter = new LibExporter(this, Settings.Settings);
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            // Add code to be executed when game is started running.
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            // Add code to be executed when Playnite is started.
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override async void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            var result = await LibExporter.SendJsonToWebAppAsync();
            if (result != "OK")
            {
                PlayniteApi.Notifications.Add(
                    new NotificationMessage(
                        $"{Name} Error",
                        $"{Name}\n\nFailed to export library. Please check the settings and try again. \n\nError: {result}",
                        NotificationType.Error)
                    );
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new PlayniteInsightsExporterSettingsView();
        }
    }
}
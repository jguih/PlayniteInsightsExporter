using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteInsightsExporter.Lib;
using PlayniteInsightsExporter.Lib.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ValidationResult = PlayniteInsightsExporter.Lib.Models.ValidationResult;

namespace PlayniteInsightsExporter
{
    public class PlayniteInsightsExporter : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private PlayniteInsightsExporterSettingsViewModel Settings { get; set; }
        private LibExporter LibExporter { get; set; }
        private PlayniteInsightsWebServerService WebServerService { get; set; }

        public readonly string Name = "Playnite Insights Exporter";
        public override Guid Id { get; } = Guid.Parse("ccbe324c-c160-4ad5-b749-5c64f8cbc113");

        public PlayniteInsightsExporter(IPlayniteAPI api) : base(api)
        {
            Settings = new PlayniteInsightsExporterSettingsViewModel(this, LibExporter);
            WebServerService = new PlayniteInsightsWebServerService(this);
            LibExporter = new LibExporter(this, WebServerService);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };

            PlayniteApi.Database.Games.ItemCollectionChanged += OnItemCollectionChanged;
            PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
        }

        private async void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            if (e.UpdatedItems.Count() > 0)
            {
                List<string> gameIds = new List<string>();
                foreach (var game in e.UpdatedItems)
                {
                    gameIds.Add(game.NewData.Id.ToString());
                }
                ValidationResult result;
                result = await LibExporter.SendJsonToWebAppAsync();
                if (!result.IsValid)
                {
                    PlayniteApi
                        .Notifications
                        .Add(
                        Name,
                        $"Failed to send game metadata to web server. \nError: {result.Message}",
                        NotificationType.Error);
                    return;
                }
                result = await LibExporter.SendFilesToWebAppAsync(gameIds);
                if (!result.IsValid)
                {
                    PlayniteApi
                        .Notifications
                        .Add(
                        Name,
                        $"Failed to send library files to web server. \nError: {result.Message}",
                        NotificationType.Error);
                    return;
                }
            }

        }

        private async void OnItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
        {
            if (e.AddedItems.Count() > 0)
            {
                // To something
            }
            if (e.RemovedItems.Count() > 0)
            {
                ValidationResult result;
                var removedIds = e.RemovedItems.Select(g => g.Id.ToString()).ToList();
                result = await LibExporter.SendJsonToWebAppAsync(removedItems: removedIds);
                if (!result.IsValid)
                {
                    PlayniteApi
                        .Notifications
                        .Add(
                        Name,
                        $"Failed to send game metadata to web server. \nError: {result.Message}",
                        NotificationType.Error);
                    return;
                }
            }
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
            if (!result.IsValid)
            {
                PlayniteApi.Notifications.Add(
                    new NotificationMessage(
                        $"{Name} Error",
                        $"Failed to export library. Please check the settings and try again. \nError: {result.Message}",
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

        public PlayniteInsightsExporterSettings GetUserSettings()
        {
            return Settings.Settings;
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            yield return new GameMenuItem
            {
                Description = "Playnite Insights Server Sync",
                Action = (_args) =>
                {
                    List<string> gameIds = new List<string>();
                    foreach(Game game in _args.Games)
                    {
                        gameIds.Add(game.Id.ToString());
                    }
                    var progressResult = PlayniteApi
                        .Dialogs
                        .ActivateGlobalProgress(
                        async (progressArgs) =>
                        {
                            ValidationResult result;
                            result = await LibExporter.SendJsonToWebAppAsync();
                            if (!result.IsValid)
                            {
                                throw new Exception($"Failed to send game metadata to web server. \nError: {result.Message}");
                            }
                            result = await LibExporter.SendFilesToWebAppAsync(gameIds);
                            if (!result.IsValid)
                            {
                                throw new Exception($"Failed to send library files to web server. \nError: {result.Message}");
                            }
                        }, new GlobalProgressOptions("Syncing..."));
                    if (progressResult.Error != null)
                    {
                        PlayniteApi
                            .Dialogs
                            .ShowErrorMessage(progressResult.Error.Message, Name);
                    }
                    else
                    {
                        PlayniteApi
                            .Dialogs
                            .ShowMessage(
                            "Game media files synced sucessfully!");

                    }
                }
            };
        }
    }
}
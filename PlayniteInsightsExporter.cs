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
        private readonly LibExporter LibExporter;
        private readonly PlayniteInsightsWebServerService WebServerService;

        public readonly string Name = "Playnite Insights Exporter";
        public override Guid Id { get; } = Guid.Parse("ccbe324c-c160-4ad5-b749-5c64f8cbc113");

        public PlayniteInsightsExporter(IPlayniteAPI api) : base(api)
        {
            Settings = new PlayniteInsightsExporterSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
            WebServerService = new PlayniteInsightsWebServerService(this, Settings.Settings);
            LibExporter = new LibExporter(this, WebServerService);
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
                result = await LibExporter.SendLibraryJsonToWebAppAsync();
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
                result = await LibExporter.SendLibraryFilesToWebAppAsync(gameIds);
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
                result = await LibExporter.SendLibraryJsonToWebAppAsync();
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
            var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
            var loc_loading_syncClientServer = ResourceProvider.GetString("LOC_Loading_SyncClientServer");
            var loc_success_syncClientServer = ResourceProvider.GetString("LOC_Success_SyncClientServer");
            if (Settings.Settings.EnableMetadataLibrarySyncOnUpdate)
            {
                var result = await LibExporter.SendLibraryJsonToWebAppAsync();
                if (!result.IsValid)
                {
                    PlayniteApi.Notifications.Add(
                        new NotificationMessage(
                            $"{Name} Error",
                            $"{loc_failed_syncClientServer} \nError: {result.Message}",
                            NotificationType.Error)
                        );
                }
            }
            if (Settings.Settings.EnableMediaFilesLibrarySyncOnUpdate)
            {
                var progressResult = PlayniteApi
                            .Dialogs
                            .ActivateGlobalProgress(
                            async (progressArgs) =>
                            {
                                ValidationResult libFilesSyncResult;
                                libFilesSyncResult = await LibExporter.SendLibraryFilesToWebAppAsync();
                                if (!libFilesSyncResult.IsValid)
                                {
                                    throw new Exception(libFilesSyncResult.Message);
                                }
                            }, new GlobalProgressOptions(loc_loading_syncClientServer));
                if (progressResult.Error != null)
                {
                    PlayniteApi.Notifications.Add(
                        new NotificationMessage(
                                $"{Name} Error",
                                $"{loc_failed_syncClientServer} \nError: {progressResult.Error.Message}",
                                NotificationType.Error
                            ));
                }
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

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var loc_loading_syncClientServer = ResourceProvider.GetString("LOC_Loading_SyncClientServer");
            var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
            var loc_success_syncClientServer = ResourceProvider.GetString("LOC_Success_SyncClientServer");
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
                            result = await LibExporter.SendLibraryJsonToWebAppAsync();
                            if (!result.IsValid)
                            {
                                throw new Exception(result.Message);
                            }
                            result = await LibExporter.SendLibraryFilesToWebAppAsync(gameIds);
                            if (!result.IsValid)
                            {
                                throw new Exception(result.Message);
                            }
                        }, new GlobalProgressOptions(loc_loading_syncClientServer));
                    if (progressResult.Error != null)
                    {
                        PlayniteApi
                            .Dialogs
                            .ShowErrorMessage($"{loc_failed_syncClientServer} \nError: {progressResult.Error.Message}", Name);
                    }
                    else
                    {
                        PlayniteApi
                            .Dialogs
                            .ShowMessage(loc_success_syncClientServer);
                    }
                }
            };
        }
    }
}
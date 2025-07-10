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
            Settings = new PlayniteInsightsExporterSettingsViewModel(this, logger);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
            WebServerService = new PlayniteInsightsWebServerService(this, Settings.Settings, logger);
            LibExporter = new LibExporter(this, WebServerService, logger);
            PlayniteApi.Database.Games.ItemCollectionChanged += OnItemCollectionChanged;
        }

        private async void OnItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
        {
            var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
            if (e.AddedItems.Count() > 0)
            {
                var result = await LibExporter.RunFullAddedGamesSyncAsync(e.AddedItems);
                if (result == false)
                {
                    PlayniteApi.Notifications.Add(
                        Name,
                        loc_failed_syncClientServer,
                        NotificationType.Error);
                    return;
                }
            }
            if (e.RemovedItems.Count() > 0)
            {
                var result = await LibExporter.RunFullRemovedGamesSyncAsync(e.RemovedItems);
                if (result == false)
                {
                    PlayniteApi.Notifications.Add(
                        Name,
                        loc_failed_syncClientServer,
                        NotificationType.Error);
                    return;
                }
            }
        }

        public override async void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
            if (args.Game != null)
            {
                List<Game> games = new List<Game>() { args.Game };
                var result = await LibExporter.RunFullUpdatedGamesSyncAsync(games);
                if (result == false)
                {
                    PlayniteApi.Notifications.Add(
                        Name,
                        loc_failed_syncClientServer,
                        NotificationType.Error);
                    return;
                }
            }
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            // Add code to be executed when game is started running.
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override async void OnGameStopped(OnGameStoppedEventArgs args)
        {
            // Add code to be executed when game is stopping.
            var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
            if (args.Game != null)
            {
                List<Game> games = new List<Game>() { args.Game };
                var result = await LibExporter.RunFullUpdatedGamesSyncAsync(games);
                if (result == false)
                {
                    PlayniteApi.Notifications.Add(
                        Name,
                        loc_failed_syncClientServer,
                        NotificationType.Error);
                    return;
                }
            }
        }

        public override async void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
            if (args.Game != null)
            {
                List<Game> games = new List<Game>() { args.Game };
                var result = await LibExporter.RunFullUpdatedGamesSyncAsync(games);
                if (result == false)
                {
                    PlayniteApi.Notifications.Add(
                        Name,
                        loc_failed_syncClientServer,
                        NotificationType.Error);
                    return;
                }
            }
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            // Add code to be executed when Playnite is started.
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
            PlayniteApi.Database.Games.ItemCollectionChanged -= OnItemCollectionChanged;
        }

        public override async void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
            if (Settings?.Settings?.EnableLibrarySyncOnUpdate == true)
            {
                var result = await LibExporter.RunFullLibrarySyncAsync();
                if (result == false)
                {
                    PlayniteApi.Notifications.Add(
                        new NotificationMessage(
                            $"{Name} Error",
                            $"{loc_failed_syncClientServer}",
                            NotificationType.Error)
                        );
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
                    var games = _args.Games;
                    List<string> gameIds = new List<string>();
                    foreach(Game game in games)
                    {
                        gameIds.Add(game.Id.ToString());
                    }
                    var progressResult = PlayniteApi
                        .Dialogs
                        .ActivateGlobalProgress(
                        async (progressArgs) =>
                        {
                            bool result;
                            result = await LibExporter.RunFullGamesSyncAsync(games);
                            if (result == false)
                            {
                                throw new Exception("Full game update sync failed");
                            }
                        }, new GlobalProgressOptions(loc_loading_syncClientServer));
                    if (progressResult.Error != null)
                    {
                        PlayniteApi
                            .Dialogs
                            .ShowErrorMessage(loc_failed_syncClientServer, Name);
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
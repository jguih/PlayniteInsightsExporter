using Core;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteInsightsExporter.Lib;
using PlayniteInsightsExporter.Lib.Logger;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PlayniteInsightsExporter
{
    public class PlayniteInsightsExporter : GenericPlugin, IPlayniteInsightsExporterContext
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private PlayniteInsightsExporterSettingsViewModel Settings { get; set; }
        private readonly LibExporter LibExporter;
        private readonly IGameSessionService SessionTrackingService;

        public readonly string Name = "Playnite Insights Exporter";
        public override Guid Id { get; } = Guid.Parse("ccbe324c-c160-4ad5-b749-5c64f8cbc113");

        public PlayniteInsightsExporter(IPlayniteAPI api) : base(api)
        {
            Settings = new PlayniteInsightsExporterSettingsViewModel(this, logger);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };

            var locator = new ServiceLocator(
                this, 
                logger, 
                Settings.Settings);
            LibExporter = locator.LibExporter;
            SessionTrackingService = locator.SessionTrackingService;

            PlayniteApi.Database.Games.ItemCollectionChanged += OnItemCollectionChanged;
            PlayniteApi.Database.Games.ItemUpdated += OnItemUpdated;
        }

        private async void OnItemUpdated(object sender, ItemUpdatedEventArgs<Game> args)
        {
            var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
            if (args.UpdatedItems != null)
            {
                foreach (var game in args.UpdatedItems)
                {
                    var oldGame = game.OldData;
                    var newGame = game.NewData;
                    if (oldGame == null) continue;
                    await LibExporter.RunLibrarySyncAsync(
                        true, 
                        itemsToAdd: new List<Game>(),
                        itemsToUpdate: new List<Game> { newGame });
                    if (oldGame.CoverImage != newGame.CoverImage ||
                        oldGame.BackgroundImage != newGame.BackgroundImage ||
                        oldGame.Icon != newGame.Icon)
                    {
                        await LibExporter.RunMediaFilesSyncAsync(new List<Game> { newGame });
                    }
                }
            }
        }

        private async void OnItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
        {
            var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
            if (e.AddedItems.Count() > 0)
            {
                var result = await LibExporter.RunLibrarySyncAsync(true, itemsToAdd: e.AddedItems);
                if (result == false)
                {
                    PlayniteApi.Notifications.Add(
                        Name,
                        loc_failed_syncClientServer,
                        NotificationType.Error);
                    return;
                }
                await LibExporter.RunMediaFilesSyncAsync(e.AddedItems);
            }
            if (e.RemovedItems.Count() > 0)
            {
                var result = await LibExporter.RunLibrarySyncAsync(true, itemsToRemove: e.RemovedItems);
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
                var result = await LibExporter.RunLibrarySyncAsync(true, itemsToUpdate: games);
                if (result == false)
                {
                    PlayniteApi.Notifications.Add(
                        Name,
                        loc_failed_syncClientServer,
                        NotificationType.Error);
                    return;
                }
                await LibExporter.RunMediaFilesSyncAsync(games);
            }
        }

        public override async void OnGameStarted(OnGameStartedEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }
            await SessionTrackingService.OpenSession(args.Game.Id.ToString());
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override async void OnGameStopped(OnGameStoppedEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }
            await SessionTrackingService.CloseSession(args.Game.Id.ToString(), args.ElapsedSeconds);
            var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
            List<Game> games = new List<Game>() { args.Game };
            var result = await LibExporter.RunLibrarySyncAsync(itemsToUpdate: games);
            if (result == false)
            {
                PlayniteApi.Notifications.Add(
                    Name,
                    loc_failed_syncClientServer,
                    NotificationType.Error);
                return;
            }
        }

        public override async void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
            if (args.Game != null)
            {
                List<Game> games = new List<Game>() { args.Game };
                var result = await LibExporter.RunLibrarySyncAsync(true, itemsToUpdate: games);
                if (result == false)
                {
                    PlayniteApi.Notifications.Add(
                        Name,
                        loc_failed_syncClientServer,
                        NotificationType.Error);
                    return;
                }
                await LibExporter.RunMediaFilesSyncAsync(games);
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
            PlayniteApi.Database.Games.ItemUpdated -= OnItemUpdated;
        }

        public override async void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            if (Settings?.Settings?.EnableLibrarySyncOnUpdate == true)
            {
                var result = await LibExporter.RunLibrarySyncAsync();
                if (result == false)
                {
                    var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
                    PlayniteApi.Notifications.Add(
                        new NotificationMessage(
                            $"{Name} Error",
                            $"{loc_failed_syncClientServer}",
                            NotificationType.Error)
                        );
                }
            }
            if (Settings?.Settings?.EnableMediaFilesSyncOnUpdate == true)
            {
                await LibExporter.RunMediaFilesSyncAsync();
            }
            if(!(await SessionTrackingService.Sync()))
            {
                var loc_failed_sync_sessions = ResourceProvider.GetString("LOC_Failed_SyncSessions");
                PlayniteApi.Notifications.Add(
                        new NotificationMessage(
                            $"{Name} Error",
                            $"{loc_failed_sync_sessions}",
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

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var loc_loading_syncClientServer = ResourceProvider.GetString("LOC_Loading_SyncClientServer");
            var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
            var loc_success_syncClientServer = ResourceProvider.GetString("LOC_Success_SyncClientServer");
            var loc_run_manual_sync = ResourceProvider.GetString("LOC_Label_MenuItem_ManualSync");
            yield return new GameMenuItem
            {
                Description = loc_run_manual_sync,
                Action = async (_args) =>
                {
                    var games = _args.Games;
                    var result = await LibExporter.RunGameListSyncAsync(games);
                    if (result == false)
                    {
                        PlayniteApi.Dialogs.ShowErrorMessage(loc_failed_syncClientServer, Name);
                        return;
                    }
                    await LibExporter.RunMediaFilesSyncAsync(games);
                    PlayniteApi.Dialogs.ShowMessage(loc_success_syncClientServer);
                }
            };
        }

        public string CtxGetExtensionDataFolderPath()
        {
            return GetPluginUserDataPath();
        }
    }
}
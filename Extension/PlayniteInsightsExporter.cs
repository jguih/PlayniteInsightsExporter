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
        private readonly IGameSessionService GameSessionService;

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
            GameSessionService = locator.GameSessionService;

            PlayniteApi.Database.Games.ItemCollectionChanged += OnItemCollectionChanged;
        }

        private void OnItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
        {
            var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
            if (e.AddedItems.Count() > 0)
            {
                var libSyncResult = LibExporter.RunLibrarySync(
                    itemsToAdd: e.AddedItems,
                    itemsToUpdate: new List<Game>(),
                    itemsToRemove: new List<Game>());
                var mediaSyncResult = LibExporter.RunMediaFilesSync(e.AddedItems);
                if (libSyncResult == false || mediaSyncResult == false)
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
                var result = LibExporter.RunLibrarySync(
                    itemsToAdd: new List<Game>(),
                    itemsToUpdate: new List<Game>(),
                    itemsToRemove: e.RemovedItems);
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

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
            if (args.Game != null)
            {
                var _ = Task.Run(async () =>
                {
                    await LibExporter.RunLibrarySyncAsync(
                        itemsToAdd: new List<Game>(),
                        itemsToUpdate: new List<Game>() { args.Game }, 
                        itemsToRemove: new List<Game>()
                    );
                });
            }
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }
            _ = Task.Run(async () =>
            {
                await GameSessionService.OpenSession(args.Game.Id.ToString(), DateTime.UtcNow);
                await LibExporter.RunLibrarySyncAsync(
                        itemsToAdd: new List<Game>(),
                        itemsToUpdate: new List<Game>() { args.Game },
                        itemsToRemove: new List<Game>()
                );
            });
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }
            _ = Task.Run(async () =>
            {
                var now = DateTime.UtcNow;
                await GameSessionService.CloseSession(args.Game.Id.ToString(), args.ElapsedSeconds, now);
                await LibExporter.RunLibrarySyncAsync(
                    itemsToAdd: new List<Game>(),
                    itemsToUpdate: new List<Game>() { args.Game },
                    itemsToRemove: new List<Game>()
                );
            });
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            if (args.Game != null)
            {
                _ = Task.Run(async () =>
                {
                    await LibExporter.RunLibrarySyncAsync(
                        itemsToAdd: new List<Game>(),
                        itemsToUpdate: new List<Game>() { args.Game },
                        itemsToRemove: new List<Game>()
                    );
                });
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
            //PlayniteApi.Database.Games.ItemUpdated -= OnItemUpdated;
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            bool libSyncResult = false;
            if (Settings?.Settings?.EnableLibrarySyncOnUpdate == true)
            {
                libSyncResult = LibExporter.RunLibrarySync();
                if (libSyncResult == false)
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
            if (Settings?.Settings?.EnableMediaFilesSyncOnUpdate == true && libSyncResult == true)
            {
                _ = Task.Run(async () =>
                {
                    await LibExporter.RunMediaFilesSyncAsync();
                });
            }
            _ = Task.Run(async () =>
            {
                await GameSessionService.SyncAsync(DateTime.UtcNow);
            });
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
                Action = (_args) =>
                {
                    var games = _args.Games;
                    var libSyncResult = LibExporter.RunGameListSync(games);
                    var mediaSyncResult = LibExporter.RunMediaFilesSync(games);
                    if (libSyncResult == false || mediaSyncResult == false)
                    {
                        PlayniteApi.Dialogs.ShowErrorMessage(loc_failed_syncClientServer, Name);
                        return;
                    }
                    PlayniteApi.Dialogs.ShowMessage(loc_success_syncClientServer);
                }
            };
        }

        public string CtxGetExtensionDataFolderPath()
        {
            return GetPluginUserDataPath();
        }

        public string CtxGetWebServerURL()
        {
            return Settings?.Settings?.WebAppURL ?? string.Empty;
        }
    }
}
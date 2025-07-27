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
        private readonly IPlayniteProgressService ProgressService;
        private readonly IPlayniteInsightsWebServerService WebServerService;
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
            ProgressService = locator.ProgressService;
            WebServerService = locator.WebServerService;

            PlayniteApi.Database.Games.ItemCollectionChanged += OnItemCollectionChanged;
        }

        private void OnItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
        {
            _ = Task.Run(async () =>
            {
                if (e.AddedItems.Any())
                {
                    try
                    {
                        var syncResult = await LibExporter.RunLibrarySyncAsync(
                            itemsToAdd: e.AddedItems,
                            itemsToUpdate: new List<Game>(),
                            itemsToRemove: new List<Game>());
                        if (syncResult == true)
                            await LibExporter.RunMediaFilesSyncAsync(e.AddedItems);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to sync added items in Playnite Insights Exporter.");
                    }
                }
                if (e.RemovedItems.Any())
                {
                    try
                    {
                        await LibExporter.RunLibrarySyncAsync(
                            itemsToAdd: new List<Game>(),
                            itemsToUpdate: new List<Game>(),
                            itemsToRemove: e.RemovedItems);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to sync removed items in Playnite Insights Exporter.");
                    }
                }
            });
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }
            _ = Task.Run(async () =>
            {
                try
                {
                    await LibExporter.RunLibrarySyncAsync(
                        itemsToAdd: new List<Game>(),
                        itemsToUpdate: new List<Game>() { args.Game },
                        itemsToRemove: new List<Game>()
                    );
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to sync installed game in Playnite Insights Exporter.");
                }
            });
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }
            _ = Task.Run(async () =>
            {
                try
                {
                    await GameSessionService.OpenSession(args.Game.Id.ToString(), DateTime.UtcNow);
                    await LibExporter.RunLibrarySyncAsync(
                            itemsToAdd: new List<Game>(),
                            itemsToUpdate: new List<Game>() { args.Game },
                            itemsToRemove: new List<Game>()
                    );
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to sync started game in Playnite Insights Exporter.");
                }
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
                try
                {
                    var now = DateTime.UtcNow;
                    await GameSessionService.CloseSession(args.Game.Id.ToString(), args.ElapsedSeconds, now);
                    await LibExporter.RunLibrarySyncAsync(
                        itemsToAdd: new List<Game>(),
                        itemsToUpdate: new List<Game>() { args.Game },
                        itemsToRemove: new List<Game>()
                    );
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to sync stopped game in Playnite Insights Exporter.");
                }
            });
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }
            _ = Task.Run(async () =>
            {
                try
                {
                    await LibExporter.RunLibrarySyncAsync(
                        itemsToAdd: new List<Game>(),
                        itemsToUpdate: new List<Game>() { args.Game },
                        itemsToRemove: new List<Game>()
                    );
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to sync uninstalled game in Playnite Insights Exporter.");
                }
            });
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

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            bool isServerHealthy = ProgressService.ActivateGlobalProgress(
                "Checking Playnite Insights Web Server health...",
                false,
                async (progress) =>
                {
                    progress.IsIndeterminate = true;
                    return await WebServerService.IsHealthy();
                }
            );
            if (isServerHealthy == false)
            {
                var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
                PlayniteApi.Notifications.Add(
                    new NotificationMessage(
                        $"{Name} Error",
                        $"{loc_failed_syncClientServer}",
                        NotificationType.Error)
                    );
                return;
            }
            if (Settings?.Settings?.EnableLibrarySyncOnUpdate == true)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await LibExporter.RunLibrarySyncAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to sync library in Playnite Insights Exporter.");
                    }
                });
            }
            if (Settings?.Settings?.EnableMediaFilesSyncOnUpdate == true)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await LibExporter.RunMediaFilesSyncAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to sync media files in Playnite Insights Exporter.");
                    }
                });
            }
            _ = Task.Run(async () =>
            {
                try
                {
                    await GameSessionService.SyncAsync(DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to sync game sessions in Playnite Insights Exporter.");
                }
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
                    if (!LibExporter.RunGameListSync(games))
                    {
                        PlayniteApi.Dialogs.ShowErrorMessage(loc_failed_syncClientServer, Name);
                        return;
                    }
                    if (!LibExporter.RunMediaFilesSync(games))
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
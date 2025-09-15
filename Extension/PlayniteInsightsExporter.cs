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
using System.Windows.Shapes;

namespace PlayniteInsightsExporter
{
    public class PlayniteInsightsExporter : GenericPlugin, IPlayniteInsightsExporterContext
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private PlayniteInsightsExporterSettingsViewModel Settings { get; set; }
        private readonly ServiceLocator locator;
        public readonly string Name = "Playnite Insights Exporter";
        public override Guid Id { get; } = Guid.Parse("ccbe324c-c160-4ad5-b749-5c64f8cbc113");

        public PlayniteInsightsExporter(IPlayniteAPI api) : base(api)
        {
            locator = new ServiceLocator(this, logger);
            Settings = new PlayniteInsightsExporterSettingsViewModel(this, logger, locator);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
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
                        var syncResult = await locator.LibExporter.RunLibrarySyncAsync(
                            itemsToAdd: e.AddedItems,
                            itemsToUpdate: new List<Game>(),
                            itemsToRemove: new List<Game>());
                        if (syncResult == true)
                            await locator.LibExporter.RunMediaFilesSyncAsync(e.AddedItems);
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
                        await locator.LibExporter.RunLibrarySyncAsync(
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
                    await locator.LibExporter.RunLibrarySyncAsync(
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
                    await locator.GameSessionService.OpenSession(args.Game.Id.ToString(), DateTime.UtcNow);
                    await locator.LibExporter.RunLibrarySyncAsync(
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
                    await locator.GameSessionService.CloseSession(args.Game.Id.ToString(), args.ElapsedSeconds, now);
                    await locator.LibExporter.RunLibrarySyncAsync(
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
                    await locator.LibExporter.RunLibrarySyncAsync(
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
            var shouldStartHttpServer = Settings?.Settings?.HttpServerStartOnStartUp ?? false;
            if (shouldStartHttpServer)
            {
                try
                {
                    locator.HttpServer.Start();
                }
                catch (Exception)
                {
                    var LOC_Label_HttpServer_Failed_To_Start = ResourceProvider.GetString("LOC_Label_HttpServer_Failed_To_Start");
                    PlayniteApi.Dialogs.ShowErrorMessage(
                            LOC_Label_HttpServer_Failed_To_Start, Name);
                }
            }
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
            PlayniteApi.Database.Games.ItemCollectionChanged -= OnItemCollectionChanged;
            var shouldStopHttpServer = Settings?.HttpServerRunning ?? false;
            if (shouldStopHttpServer)
            {
                try
                {
                    locator.HttpServer.Stop();
                }
                catch (Exception)
                {
                    var LOC_Label_HttpServer_Failed_To_Stop = ResourceProvider.GetString("LOC_Label_HttpServer_Failed_To_Stop");
                    PlayniteApi.Dialogs.ShowErrorMessage(
                        LOC_Label_HttpServer_Failed_To_Stop, Name);

                }
            }
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            bool isServerHealthy = locator.ProgressService.ActivateGlobalProgress(
                "Checking Playnite Insights Web Server health...",
                false,
                async (progress) =>
                {
                    progress.IsIndeterminate = true;
                    return await locator.WebServerService.IsHealthy();
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
                        await locator.LibExporter.RunLibrarySyncAsync();
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
                        await locator.LibExporter.RunMediaFilesSyncAsync();
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
                    await locator.GameSessionService.SyncAsync(DateTime.UtcNow);
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
                    if (!locator.LibExporter.RunGameListSync(games))
                    {
                        PlayniteApi.Dialogs.ShowErrorMessage(loc_failed_syncClientServer, Name);
                        return;
                    }
                    if (!locator.LibExporter.RunMediaFilesSync(games))
                    {
                        PlayniteApi.Dialogs.ShowErrorMessage(loc_failed_syncClientServer, Name);
                        return;
                    }
                    PlayniteApi.Dialogs.ShowMessage(loc_success_syncClientServer);
                }
            };
        }

        public string GetExtensionDataFolderPath()
        {
            return GetPluginUserDataPath();
        }

        public string GetWebServerURL()
        {
            var url = Settings?.Settings?.WebAppURL ?? string.Empty;
            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("PlayAtlas server URL is not set in settings.");
            return url;
        }

        public string GetShareXExePath()
        {
            var path = Settings?.Settings?.ShareXExePath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("ShareX executable path is not set in settings.");
            return path;
        }

        public string GetWebServerPublicKeyPath()
        {
            var path = Settings?.Settings?.PlayAtlasServerPubKeyPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("PlayAtlas server public key path is not set in settings.");
            return path;
        }

        public string GetHttpServerPort()
        {
            var path = Settings?.Settings?.HttpServerPort ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("HTTP server port is not set in settings.");
            return path;
        }
    }
}
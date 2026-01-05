using Core;
using ExporterBootstrap.Application;
using ExporterCommon.Application;
using ExporterCommon.Domain;
using ExporterLibraryExporter.Application;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteInsightsExporter.Adapters;
using PlayniteInsightsExporter.Notifications;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace PlayniteInsightsExporter
{
    public class PlayniteInsightsExporter : GenericPlugin, IPlayAtlasExporterContext, IExporterPluginContextPort
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private PlayniteInsightsExporterSettingsViewModel Settings { get; set; }
        private readonly ServiceLocator locator;

        private readonly ExporterApi exporterApi;
        private readonly SyncGameLibraryWorkflow syncGameLibrary;
        private readonly ISyncFeedbackChannelPort dialogFeedbackChannel;
        private readonly ISyncFeedbackChannelPort notificationFeedbackChannel;
        private readonly IPlayniteGameMapperPort playniteGameMapper;

        public readonly string Name = "PlayAtlas Exporter";
        public override Guid Id { get; } = Guid.Parse("ccbe324c-c160-4ad5-b749-5c64f8cbc113");

        public PlayniteInsightsExporter(IPlayniteAPI api) : base(api)
        {
            // New API
            var compositionRoot = new ExporterCompositionRoot(
                logger,
                PlayniteApi,
                this
            );
            exporterApi = compositionRoot.Build();
            syncGameLibrary = new SyncGameLibraryWorkflow(exporterApi, PlayniteApi);
            dialogFeedbackChannel = new DialogFeedbackChannel(PlayniteApi);
            notificationFeedbackChannel = new NotificationFeedbackChannel(PlayniteApi);
            playniteGameMapper = new PlayniteGameMapper();

            // TODO: Remove
            locator = new ServiceLocator(this, logger);

            Settings = new PlayniteInsightsExporterSettingsViewModel(
                this, 
                locator, 
                exporterApi,
                syncGameLibrary,
                dialogFeedbackChannel
            );
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
            PlayniteApi.Database.Games.ItemCollectionChanged += OnItemCollectionChanged;
        }

        private void FullGameSync(Game game)
        {
            var entity = playniteGameMapper.Map(game);
            var syncItem = exporterApi.LibrarySync.GameSyncItemFactory.Create(entity);
            GameLibrarySyncDiff diff = new GameLibrarySyncDiff(
                        added: new List<SyncGameCommandItem>(),
                        updated: new List<SyncGameCommandItem>() { syncItem },
                        deleted: new List<string>()
                    );
            var games = new List<AppGame>() { syncItem.Game };

            var sycnGamesResult = syncGameLibrary.SyncGames(diff);
            var syncGamesOutcome = syncGameLibrary.InterpretSyncGamesResult(sycnGamesResult);

            if (!syncGamesOutcome.Success)
            {
                PresentSyncOutcome(syncGamesOutcome, notificationFeedbackChannel);
                return;
            }

            var syncMediaFilesResult = syncGameLibrary.SyncMediaFiles(games);
            var syncMediaFilesOutcome = syncGameLibrary.InterpretSyncMediaFilesResult(syncMediaFilesResult);
            PresentSyncOutcome(syncMediaFilesOutcome, notificationFeedbackChannel);
        }

        private void OnItemCollectionChanged(
            object sender, 
            ItemCollectionChangedEventArgs<Game> e
        )
        {
            if (e.AddedItems.Any() || e.RemovedItems.Any())
            {
                var syncItems = e.AddedItems?
                    .Select(playniteGameMapper.Map)
                    .Select(exporterApi.LibrarySync.GameSyncItemFactory.Create)
                    .ToList()
                    ?? new List<SyncGameCommandItem>();
                var games = syncItems
                    .Select(i => i.Game)
                    .ToList();
                var toDelete = e.RemovedItems?
                    .Select(g => g.Id.ToString())
                    .ToList()
                    ?? new List<string>();
                var loc_failedSyncGameLibrary = ResourceProvider.GetString("LOC_Failed_SyncGameLibrary");

                GameLibrarySyncDiff diff = new GameLibrarySyncDiff(
                        added: syncItems,
                        updated: new List<SyncGameCommandItem>(),
                        deleted: toDelete
                    );

                var sycnGamesResult = syncGameLibrary.SyncGames(diff);
                var syncGamesOutcome = syncGameLibrary.InterpretSyncGamesResult(sycnGamesResult);

                if (!syncGamesOutcome.Success)
                {
                    PresentSyncOutcome(syncGamesOutcome, notificationFeedbackChannel);
                    return;
                }

                var syncMediaFilesResult = syncGameLibrary.SyncMediaFiles(games);
                var syncMediaFilesOutcome = syncGameLibrary.InterpretSyncMediaFilesResult(syncMediaFilesResult);
                PresentSyncOutcome(syncMediaFilesOutcome, notificationFeedbackChannel);
            }
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }

            FullGameSync(args.Game);
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }

            FullGameSync(args.Game);
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

            FullGameSync(args.Game);
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }

            FullGameSync(args.Game);
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            // Add code to be executed when Playnite is started.

            try
            {
                exporterApi.EnvironmentInitializer.EnsureDirectories();
                exporterApi.EnvironmentInitializer.EnsureIdentity();
            }
            catch (Exception ex)
            {
                exporterApi.Logger.Error("Failed to initialize extension environment", ex);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    var newRegistration = await locator.ExtensionRegistrationService.RegisterAsync();
                    if (newRegistration)
                        PlayniteApi.MainView.UIDispatcher.Invoke(() =>
                        {
                            var message = ResourceProvider.GetString("LOC_Success_Extension_Registration");
                            PlayniteApi.Dialogs.ShowMessage(
                                message,
                                Name,
                                System.Windows.MessageBoxButton.OK);
                        });
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to register extension with PlayAtlas server");
                    PlayniteApi.MainView.UIDispatcher.Invoke(() =>
                    {
                        var message = ResourceProvider.GetString("LOC_Failed_Extension_Registration");
                        PlayniteApi.Dialogs.ShowMessage(
                            message,
                            Name,
                            System.Windows.MessageBoxButton.OK);
                    });
                }
            });

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
            if (Settings?.Settings?.EnableLibrarySyncOnUpdate == true)
            {
                var syncGamesProgressResult = syncGameLibrary.SyncGames();
                var syncGamesOutcome = syncGameLibrary.InterpretSyncGamesResult(syncGamesProgressResult);

                if (!syncGamesOutcome.Success)
                {
                    PresentSyncOutcome(syncGamesOutcome, notificationFeedbackChannel);
                    return;
                }
            }
            if (Settings?.Settings?.EnableMediaFilesSyncOnUpdate == true)
            {
                var syncMediaFilesResult = syncGameLibrary.SyncMediaFiles();
                var syncMediaFilesOutcome = syncGameLibrary.InterpretSyncMediaFilesResult(syncMediaFilesResult);
                PresentSyncOutcome(syncMediaFilesOutcome, notificationFeedbackChannel);
            }
            _ = Task.Run(async () =>
            {
                try
                {
                    await locator.GameSessionService.SyncAsync(DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to sync game sessions with PlayAtlas server.");
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
            var loc_run_manual_sync = ResourceProvider.GetString("LOC_Label_MenuItem_ManualSync");
            yield return new GameMenuItem
            {
                Description = loc_run_manual_sync,
                Action = (_args) =>
                {
                    if (_args == null || _args.Games == null) return;

                    var syncItems = _args.Games
                        .Select(playniteGameMapper.Map)
                        .Select(exporterApi.LibrarySync.GameSyncItemFactory.Create)
                        .ToList();
                    GameLibrarySyncDiff diff = new GameLibrarySyncDiff(
                                added: new List<SyncGameCommandItem>(),
                                updated: syncItems,
                                deleted: new List<string>()
                            );
                    var games = syncItems
                        .Select(i => i.Game)
                        .ToList();

                    var syncGamesProgressResult = syncGameLibrary.SyncGames(diff);
                    var syncGamesOutcome = syncGameLibrary.InterpretSyncGamesResult(syncGamesProgressResult);

                    if (!syncGamesOutcome.Success)
                    {
                        PresentSyncOutcome(syncGamesOutcome, dialogFeedbackChannel);
                        return;
                    }

                    var syncMediaFilesResult = syncGameLibrary.SyncMediaFiles(games);
                    var syncMediaFilesOutcome = syncGameLibrary.InterpretSyncMediaFilesResult(syncMediaFilesResult);
                    PresentSyncOutcome(syncMediaFilesOutcome, dialogFeedbackChannel);
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

        public string GetExtensionVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "Unknown";
            return version;
        }

        public string GetExtensionId()
        {
            return Id.ToString();
        }

        public string GetSecurityDirectoryPath()
        {
            return System.IO.Path.Combine(GetExtensionDataFolderPath(), "security");
        }

        public string GetExtensionDataDirPath()
        {
            return GetPluginUserDataPath();
        }

        public string GetConfigurationDirPath()
        {
            return PlayniteApi.Paths.ConfigurationPath;
        }

        public void PresentSyncOutcome(SyncOutcome outcome, ISyncFeedbackChannelPort channel)
        {
            switch (outcome.Severity)
            {
                case SyncSeverity.Error:
                    channel.ShowError(outcome.Message, outcome.Title);
                    break;

                case SyncSeverity.Warning:
                    channel.ShowWarning(outcome.Message, outcome.Title);
                    break;

                case SyncSeverity.Success:
                    channel.ShowSuccess(outcome.Message, outcome.Title);
                    break;
            }
        }
    }
}
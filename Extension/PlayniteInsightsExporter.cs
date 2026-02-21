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
using PlayniteInsightsExporter.Src;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace PlayniteInsightsExporter
{
    public class PlayniteInsightsExporter : GenericPlugin, IExporterPluginContextPort
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private PlayniteInsightsExporterSettingsViewModel Settings { get; set; }

        private readonly ExporterApi exporterApi;
        private readonly SyncGameLibraryWorkflow syncGameLibrary;
        private readonly GameSessionWorkflow gameSession;
        private readonly ISyncFeedbackChannelPort dialogFeedbackChannel;
        private readonly ISyncFeedbackChannelPort notificationFeedbackChannel;
        private readonly IPlayniteGameMapperPort playniteGameMapper;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        public readonly string Name = "PlayAtlas Exporter";
        public override Guid Id { get; } = Guid.Parse("ccbe324c-c160-4ad5-b749-5c64f8cbc113");

        public PlayniteInsightsExporter(IPlayniteAPI api) : base(api)
        {
            var compositionRoot = new ExporterCompositionRoot(
                logger,
                PlayniteApi,
                this
            );
            exporterApi = compositionRoot.Build();
            syncGameLibrary = new SyncGameLibraryWorkflow(exporterApi, PlayniteApi);
            gameSession = new GameSessionWorkflow(exporterApi);
            dialogFeedbackChannel = new DialogFeedbackChannel(PlayniteApi);
            notificationFeedbackChannel = new NotificationFeedbackChannel(PlayniteApi);
            playniteGameMapper = new PlayniteGameMapper();

            Settings = new PlayniteInsightsExporterSettingsViewModel(
                this, 
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

        private void OnItemCollectionChanged(
            object sender, 
            ItemCollectionChangedEventArgs<Game> e
        )
        {
            if (e.AddedItems.Any() || e.RemovedItems.Any())
            {
                var sycnGamesResult = syncGameLibrary.SyncGames(
                        toAdd: e.AddedItems,
                        toDelete: e.RemovedItems
                    );
                var syncGamesOutcome = syncGameLibrary.InterpretSyncGamesResult(sycnGamesResult);

                if (!syncGamesOutcome.Success)
                {
                    PresentOperationOutcome(syncGamesOutcome, notificationFeedbackChannel);
                    return;
                }

                var syncMediaFilesResult = syncGameLibrary.SyncMediaFiles(e.AddedItems);
                var syncMediaFilesOutcome = syncGameLibrary.InterpretSyncMediaFilesResult(syncMediaFilesResult);

                if (!syncMediaFilesOutcome.Success)
                {
                    PresentOperationOutcome(syncMediaFilesOutcome, notificationFeedbackChannel);
                }
            }
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }

            var sycnGamesResult = syncGameLibrary.SyncGames(args.Game);
            var syncGamesOutcome = syncGameLibrary.InterpretSyncGamesResult(sycnGamesResult);

            if (!syncGamesOutcome.Success)
            {
                PresentOperationOutcome(syncGamesOutcome, notificationFeedbackChannel);
                return;
            }
        }

        public override async void OnGameStarted(OnGameStartedEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }

            var sessionResult = await gameSession.OpenSessionAsync(args.Game);

            if (!sessionResult.Success)
            {
                PresentOperationOutcome(sessionResult, notificationFeedbackChannel);
            }
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

            var sessionResult = await gameSession
                .CloseSessionAsync(args.Game, args.ElapsedSeconds);

            if (!sessionResult.Success)
            {
                PresentOperationOutcome(sessionResult, notificationFeedbackChannel);
            }

            var sycnGamesResult = syncGameLibrary.SyncGames(args.Game);
            var syncGamesOutcome = syncGameLibrary.InterpretSyncGamesResult(sycnGamesResult);

            if (!syncGamesOutcome.Success)
            {
                PresentOperationOutcome(syncGamesOutcome, notificationFeedbackChannel);
            }
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }

            var sycnGamesResult = syncGameLibrary.SyncGames(args.Game);
            var syncGamesOutcome = syncGameLibrary.InterpretSyncGamesResult(sycnGamesResult);

            if (!syncGamesOutcome.Success)
            {
                PresentOperationOutcome(syncGamesOutcome, notificationFeedbackChannel);
                return;
            }
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
                OperationOutcome outcome = new OperationOutcome(
                    false, 
                    OutcomeSeverity.Error,
                    $"Failed to initialize extension environment: {ex.Message}",
                    "PlayAtlas Exporter"
                );
                exporterApi.Logger.Error(outcome.Message, ex);
                PresentOperationOutcome(outcome, notificationFeedbackChannel);
            }

            try
            {
                exporterApi.PlayAtlasClient.EventStream.StartAsync(cts.Token);
            }
            catch (Exception ex)
            {
                OperationOutcome outcome = new OperationOutcome(
                    false,
                    OutcomeSeverity.Error,
                    $"Failed to create event stream with PlayAtlas server: {ex.Message}",
                    "PlayAtlas Exporter"
                );
                exporterApi.Logger.Error(outcome.Message, ex);
                PresentOperationOutcome(outcome, notificationFeedbackChannel);
            }

            var games = exporterApi.PlayniteIntegration.Query.GetAllGames.Execute();
            var corpus = exporterApi.GameCorpus.CorpusBuilder.Build(games.ToList());
            var labeledCorpus = exporterApi.GameCorpus.CorpusLabeler.ApplyLabels(corpus);

            var result = exporterApi.GameCorpus.TextCorpusMiner.Mine(labeledCorpus);
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
            PlayniteApi.Database.Games.ItemCollectionChanged -= OnItemCollectionChanged;
            cts.Cancel();
        }

        public override async void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            if (Settings?.Settings?.EnableLibrarySyncOnUpdate == true)
            {
                var syncGamesProgressResult = syncGameLibrary.SyncGames();
                var syncGamesOutcome = syncGameLibrary.InterpretSyncGamesResult(syncGamesProgressResult);

                if (!syncGamesOutcome.Success)
                {
                    PresentOperationOutcome(syncGamesOutcome, notificationFeedbackChannel);
                }
            }
            if (Settings?.Settings?.EnableMediaFilesSyncOnUpdate == true)
            {
                var syncMediaFilesResult = syncGameLibrary.SyncMediaFiles();
                var syncMediaFilesOutcome = syncGameLibrary.InterpretSyncMediaFilesResult(syncMediaFilesResult);

                if (!syncMediaFilesOutcome.Success)
                {
                    PresentOperationOutcome(syncMediaFilesOutcome, notificationFeedbackChannel);
                }
            }

            var processPendingSessionsResult = await gameSession.ProcessPendingSessionsAsync();

            if (!processPendingSessionsResult.Success)
            {
                PresentOperationOutcome(processPendingSessionsResult, notificationFeedbackChannel);
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
                        PresentOperationOutcome(syncGamesOutcome, dialogFeedbackChannel);
                        return;
                    }

                    var syncMediaFilesResult = syncGameLibrary.SyncMediaFiles(games);
                    var syncMediaFilesOutcome = syncGameLibrary.InterpretSyncMediaFilesResult(syncMediaFilesResult);

                    if (!syncMediaFilesOutcome.Success)
                    {
                        PresentOperationOutcome(syncMediaFilesOutcome, dialogFeedbackChannel);
                        return;
                    }

                    var loc_successSyncClientServer = ResourceProvider.GetString("LOC_Success_SyncClientServer");
                    PresentOperationOutcome(
                        new OperationOutcome(
                            true, 
                            OutcomeSeverity.Success, 
                            loc_successSyncClientServer, 
                            "Library Sync"
                        ), dialogFeedbackChannel);
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

        public void PresentOperationOutcome(OperationOutcome outcome, ISyncFeedbackChannelPort channel)
        {
            switch (outcome.Severity)
            {
                case OutcomeSeverity.Error:
                    channel.ShowError(outcome.Message, outcome.Title);
                    break;

                case OutcomeSeverity.Warning:
                    channel.ShowWarning(outcome.Message, outcome.Title);
                    break;

                case OutcomeSeverity.Success:
                    channel.ShowSuccess(outcome.Message, outcome.Title);
                    break;
            }
        }
    }
}
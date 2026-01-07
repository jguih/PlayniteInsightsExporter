using ExporterBootstrap.Application;
using ExporterCommon.Application;
using ExporterCommon.Domain;
using ExporterLibraryExporter.Application;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteInsightsExporter.Adapters;
using PlayniteInsightsExporter.Notifications;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter
{
    public class SyncMediaFilesWorkflowResult
    {
        public SyncMediaFilesResult SyncResult { get; set; } = null;
        public GlobalProgressResult ProgressResult { get; set; }

        public SyncMediaFilesWorkflowResult() { }
    }

    public class SyncGameLibraryWorkflow
    {
        private readonly PlayniteGameMapper GameMapper = new PlayniteGameMapper();
        private readonly ExporterApi ExporterApi;
        private readonly IPlayniteAPI PlayniteApi;

        public SyncGameLibraryWorkflow(
            ExporterApi exporterApi,
            IPlayniteAPI playniteApi
        )
        {
            this.ExporterApi = exporterApi;
            this.PlayniteApi = playniteApi;
        }

        public GlobalProgressResult SyncGames()
        {
            try
            {
                return SyncGames(overrideDiff: null);
            }
            catch (Exception ex)
            {
                return new GlobalProgressResult(false, false, ex);
            }
        }

        public GlobalProgressResult SyncGames(Game game)
        {
            try
            {
                var entity = GameMapper.Map(game);
                var syncItem = ExporterApi.LibrarySync.GameSyncItemFactory.Create(entity);
                GameLibrarySyncDiff diff = new GameLibrarySyncDiff(
                            added: new List<SyncGameCommandItem>(),
                            updated: new List<SyncGameCommandItem>() { syncItem },
                            deleted: new List<string>()
                        );
                return SyncGames(diff);
            }
            catch (Exception ex)
            {
                return new GlobalProgressResult(false, false, ex);
            }
        }

        public GlobalProgressResult SyncGames(
            List<Game> toAdd = null,
            List<Game> toUpdate = null,
            List<Game> toDelete = null
        )
        {
            try
            {
                var _added = toAdd ?? new List<Game>();
                var _updated = toUpdate ?? new List<Game>();
                var _deleted = toDelete ?? new List<Game>();
                var added = _added
                    .Select(GameMapper.Map)
                    .Select(ExporterApi.LibrarySync.GameSyncItemFactory.Create)
                    .ToList();
                var updated = _updated
                    .Select(GameMapper.Map)
                    .Select(ExporterApi.LibrarySync.GameSyncItemFactory.Create)
                    .ToList();
                var deleted = _deleted
                    .Select(g => g.Id.ToString())
                    .ToList();

                GameLibrarySyncDiff diff = new GameLibrarySyncDiff(
                            added: added,
                            updated: updated,
                            deleted: deleted
                        );

                return SyncGames(diff);
            }
            catch (Exception ex)
            {
                return new GlobalProgressResult(false, false, ex);
            }
        }

        public GlobalProgressResult SyncGames(GameLibrarySyncDiff overrideDiff = null)
        {
            var loc_progress_syncing_library = ResourceProvider.GetString("LOC_Loading_SyncClientServer");
            var loc_progress_syncing_games = ResourceProvider.GetString("LOC_Progress_SyncingGames");

            GlobalProgressOptions syncGamesProgressOptions = new GlobalProgressOptions(null)
            {
                Cancelable = true,
                IsIndeterminate = true,
                Text = loc_progress_syncing_library
            };

            var syncGamesProgressResult = PlayniteApi
                .Dialogs
                .ActivateGlobalProgress(async progress =>
                {
                    var diff = overrideDiff 
                        ?? await ExporterApi.LibrarySync
                            .LibrarySyncService
                            .ComputeGameLibraryDiff();
                    var total = diff.Total;

                    progress.Text = loc_progress_syncing_games
                        .Replace("{{total}}", total.ToString());

                    await ExporterApi.LibrarySync
                        .LibrarySyncService
                        .SyncGamesLibraryAsync(diff, progress.CancelToken);
                }, syncGamesProgressOptions);

            return syncGamesProgressResult;
        }

        public SyncMediaFilesWorkflowResult SyncMediaFiles(Game game)
        {
            try
            {
                var entity = GameMapper.Map(game);
                var games = new List<AppGame>() { entity };
                return SyncMediaFiles(games);
            }
            catch (Exception ex)
            {
                return new SyncMediaFilesWorkflowResult()
                {
                    ProgressResult = new GlobalProgressResult(false, false, ex),
                    SyncResult = new SyncMediaFilesResult(
                        ex.Message, 
                        SyncMediaFilesResultReasonCode.OneOrMoreFailed,
                        false,
                        0,
                        0,
                        0
                    )
                };
            }
        }

        public SyncMediaFilesWorkflowResult SyncMediaFiles(List<Game> game)
        {
            try
            {
                var games = game
                    .Select(GameMapper.Map)
                    .ToList();
                return SyncMediaFiles(games);
            }
            catch (Exception ex)
            {
                return new SyncMediaFilesWorkflowResult()
                {
                    ProgressResult = new GlobalProgressResult(false, false, ex),
                    SyncResult = new SyncMediaFilesResult(
                        ex.Message,
                        SyncMediaFilesResultReasonCode.OneOrMoreFailed,
                        false,
                        0,
                        0,
                        0
                    )
                };
            }
        }

        public SyncMediaFilesWorkflowResult SyncMediaFiles(List<AppGame> overrideGames = null)
        {
            var loc_progress_syncing_library = ResourceProvider.GetString("LOC_Loading_SyncClientServer");
            var loc_progress_syncing_media_files = ResourceProvider.GetString("LOC_Progress_SyncingMediaFiles");

            SyncMediaFilesResult syncResult = null;

            var progressResult = PlayniteApi
                .Dialogs
                .ActivateGlobalProgress(async progress =>
                {
                    var games = overrideGames 
                        ?? ExporterApi.PlayniteIntegration
                            .Query
                            .GetAllGames
                            .Execute();

                    progress.IsIndeterminate = false;
                    progress.CurrentProgressValue = 0;
                    progress.ProgressMaxValue = games.Count();

                    var context = new SyncMediaFilesContext
                    {
                        OnBeginProcessing = (game) =>
                        {
                            var progressText = loc_progress_syncing_media_files
                                .Replace("{{current}}", (progress.CurrentProgressValue + 1).ToString())
                                .Replace("{{total}}", (progress.ProgressMaxValue).ToString())
                                .Replace("{{gameName}}", game.Name);
                            progress.Text = progressText;
                        },
                        OnFinishProcessing = (game) =>
                        {
                            progress.CurrentProgressValue++;
                        }
                    };

                    syncResult = await ExporterApi.LibrarySync
                        .LibrarySyncService
                        .SyncMediaFilesAsync(
                            games,
                            progress.CancelToken,
                            context
                        );
                },
                new GlobalProgressOptions(loc_progress_syncing_library, true)
            );

            return new SyncMediaFilesWorkflowResult
            {
                ProgressResult = progressResult,
                SyncResult = syncResult,
            };
        }

        public OperationOutcome InterpretSyncGamesResult(GlobalProgressResult result)
        {
            if (result.Canceled)
            {
                var loc_operationCanceledByUser = ResourceProvider.GetString("LOC_SyncOperation_CanceledByUser");
                return new OperationOutcome
                {
                    Message = loc_operationCanceledByUser,
                    Title = "Sync Games",
                    Severity = OutcomeSeverity.Warning,
                    Success = false
                };
            }

            if (result.Error != null)
            {
                var loc_failedSyncGameLibrary = ResourceProvider.GetString("LOC_Failed_SyncGameLibrary");
                return new OperationOutcome
                {
                    Message = $"{loc_failedSyncGameLibrary}:\n\n{result.Error.Message}",
                    Title = "Sync Games",
                    Severity = OutcomeSeverity.Error,
                    Success = false
                };
            }

            var loc_successSyncClientServer = ResourceProvider.GetString("LOC_Success_SyncGameLibrary");
            return new OperationOutcome
            {
                Message = loc_successSyncClientServer,
                Title = "Sync Games",
                Severity = OutcomeSeverity.Success,
                Success = true
            };
        }

        public OperationOutcome InterpretSyncMediaFilesResult(SyncMediaFilesWorkflowResult result)
        {
            if (result.ProgressResult.Canceled)
            {
                var loc_operationCanceledByUser = ResourceProvider.GetString("LOC_SyncOperation_CanceledByUser");
                return new OperationOutcome
                {
                    Message = loc_operationCanceledByUser,
                    Title = "Sync Media Files",
                    Severity = OutcomeSeverity.Warning,
                    Success = false
                };
            }

            if (result.ProgressResult.Error != null)
            {
                return new OperationOutcome
                {
                    Message = $"Unexpected error while syncing media files:\n\n{result.ProgressResult.Error.Message}",
                    Title = "Sync Media Files",
                    Severity = OutcomeSeverity.Error,
                    Success = false
                };
            }

            if (result.SyncResult == null)
            {
                return new OperationOutcome
                {
                    Message = "Sync media files was not executed.",
                    Title = "Sync Media Files",
                    Severity = OutcomeSeverity.Error,
                    Success = false
                };
            }

            if (!result.SyncResult.OperationSuccess)
            {
                return new OperationOutcome
                {
                    Message = $"Media files sync finished with errors.\n\n" +
                        $"Success: {result.SyncResult.Success}\n" +
                        $"Skipped: {result.SyncResult.Skipped}\n" +
                        $"Failed: {result.SyncResult.Failed}",
                    Title = "Sync Media Files",
                    Severity = OutcomeSeverity.Error,
                    Success = false
                };
            }

            var loc_successSyncClientServer = ResourceProvider.GetString("LOC_Success_SyncMediaFiles");
            return new OperationOutcome
            {
                Message = loc_successSyncClientServer,
                Title = "Sync Media Games",
                Severity = OutcomeSeverity.Success,
                Success = true
            };
        }
    }
}

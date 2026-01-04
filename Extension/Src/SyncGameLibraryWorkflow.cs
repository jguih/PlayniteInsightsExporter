using ExporterBootstrap.Application;
using ExporterCommon.Domain;
using ExporterLibraryExporter.Application;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Src
{
    public class SyncMediaFilesWorkflowResult
    {
        public SyncMediaFilesResult SyncResult { get; set; } = null;
        public GlobalProgressResult ProgressResult { get; set; }

        public SyncMediaFilesWorkflowResult() { }
    }

    public class SyncGameLibraryWorkflow
    {
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

        public bool HandleSyncGamesResult(GlobalProgressResult result)
        {
            var loc_operationCanceledByUser = ResourceProvider.GetString("LOC_Operation_CanceledByUser");
            var loc_failedSyncGameLibrary = ResourceProvider.GetString("LOC_Failed_SyncGameLibrary");

            if (result.Canceled)
            {
                PlayniteApi.Dialogs.ShowMessage(
                    loc_operationCanceledByUser,
                    "Sync Games"
                );
                return false;
            }

            if (result.Error != null)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(
                    $"{loc_failedSyncGameLibrary}:\n\n{result.Error.Message}",
                    "Sync Games"
                );
                return false;
            }

            return true;
        }

        public bool HandleSyncMediaFilesResult(SyncMediaFilesWorkflowResult result)
        {
            var loc_operationCanceledByUser = ResourceProvider.GetString("LOC_Operation_CanceledByUser");

            if (result.ProgressResult.Canceled)
            {
                PlayniteApi.Dialogs.ShowMessage(
                    loc_operationCanceledByUser,
                    "Sync Media Files"
                );
                return false;
            }

            if (result.ProgressResult.Error != null)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(
                    $"Unexpected error while syncing media files:\n\n{result.ProgressResult.Error.Message}",
                    "Sync Media Files"
                );
                return false;
            }

            if (result.SyncResult == null)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(
                    "Sync media files was not executed.",
                    "Sync Media Files"
                );
                return false;
            }

            if (!result.SyncResult.OperationSuccess)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(
                    $"Media files sync finished with errors.\n\n" +
                    $"Success: {result.SyncResult.Success}\n" +
                    $"Skipped: {result.SyncResult.Skipped}\n" +
                    $"Failed: {result.SyncResult.Failed}",
                    "Sync Media Files"
                );
                return false;
            }

            return true;
        }
    }
}

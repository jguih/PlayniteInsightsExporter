using ExporterBootstrap.Application;
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

        public GlobalProgressResult SyncGames()
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
                    var diff = await ExporterApi.LibrarySync
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

        public SyncMediaFilesWorkflowResult SyncMediaFiles()
        {
            var loc_progress_syncing_library = ResourceProvider.GetString("LOC_Loading_SyncClientServer");
            var loc_progress_syncing_media_files = ResourceProvider.GetString("LOC_Progress_SyncingMediaFiles");

            SyncMediaFilesResult syncResult = null;

            var progressResult = PlayniteApi
                .Dialogs
                .ActivateGlobalProgress(async progress =>
                {
                    var games = ExporterApi.PlayniteIntegration
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
    }
}

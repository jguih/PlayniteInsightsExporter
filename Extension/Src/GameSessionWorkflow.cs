using ExporterBootstrap.Application;
using Playnite.SDK.Models;
using PlayniteInsightsExporter.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Src
{
    public class GameSessionWorkflow
    {
        private readonly ExporterApi ExporterApi;

        public GameSessionWorkflow(ExporterApi ExporterApi)
        {
            this.ExporterApi = ExporterApi;
        }

        public async Task<OperationOutcome> OpenSessionAsync(Game game)
        {
            var title = "Game Session";

            try
            {
                var now = DateTime.UtcNow;
                await ExporterApi.GameSession
                    .GameSessionService
                    .OpenSessionAsync(game.Id.ToString(), now);
                return new OperationOutcome()
                {
                    Success = true,
                    Severity = SyncSeverity.Success,
                    Message = $"Created game session for {game.Name}",
                    Title = title
                };
            }
            catch (Exception ex)
            {
                return new OperationOutcome()
                {
                    Success = false,
                    Severity = SyncSeverity.Error,
                    Message = ex.Message,
                    Title = title
                };
            }
        }

        public async Task<OperationOutcome> CloseSessionAsync(Game game, ulong duration)
        {
            var title = "Game Session";

            try
            {
                var now = DateTime.UtcNow;
                await ExporterApi.GameSession
                    .GameSessionService
                    .CloseSessionAsync(game.Id.ToString(), duration, now);
                return new OperationOutcome()
                {
                    Success = true,
                    Severity = SyncSeverity.Success,
                    Message = $"Closed game session for {game.Name}",
                    Title = title
                };
            }
            catch (Exception ex)
            {
                return new OperationOutcome()
                {
                    Success = false,
                    Severity = SyncSeverity.Error,
                    Message = ex.Message,
                    Title = title
                };
            }
        }
    }
}

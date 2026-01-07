using ExporterBootstrap.Application;
using Playnite.SDK.Models;
using PlayniteInsightsExporter.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PlayniteInsightsExporter.Src
{
    public class GameSessionWorkflow
    {
        private readonly ExporterApi ExporterApi;
        private readonly string title = "Game Session";

        public GameSessionWorkflow(ExporterApi ExporterApi)
        {
            this.ExporterApi = ExporterApi;
        }

        public async Task<OperationOutcome> OpenSessionAsync(Game game)
        {
            try
            {
                var now = DateTime.UtcNow;
                await ExporterApi.GameSession
                    .GameSessionService
                    .OpenSessionAsync(game.Id.ToString(), now);
                return new OperationOutcome()
                {
                    Success = true,
                    Severity = OutcomeSeverity.Success,
                    Message = $"Created game session for {game.Name}",
                    Title = title
                };
            }
            catch (Exception ex)
            {
                return new OperationOutcome()
                {
                    Success = false,
                    Severity = OutcomeSeverity.Error,
                    Message = $"Failed to open game session for {game.Name}: {ex.Message}",
                    Title = title
                };
            }
        }

        public async Task<OperationOutcome> CloseSessionAsync(Game game, ulong duration)
        {
            try
            {
                var now = DateTime.UtcNow;
                await ExporterApi.GameSession
                    .GameSessionService
                    .CloseSessionAsync(game.Id.ToString(), duration, now);
                return new OperationOutcome()
                {
                    Success = true,
                    Severity = OutcomeSeverity.Success,
                    Message = $"Closed game session for {game.Name}",
                    Title = title
                };
            }
            catch (Exception ex)
            {
                return new OperationOutcome()
                {
                    Success = false,
                    Severity = OutcomeSeverity.Error,
                    Message = $"Failed to close game session for {game.Name}: {ex.Message}",
                    Title = title
                };
            }
        }

        public async Task<OperationOutcome> ProcessPendingSessionsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                await ExporterApi.GameSession
                    .GameSessionService
                    .ProcessPendingSessionsAsync(now);
                return new OperationOutcome()
                {
                    Success = true,
                    Severity = OutcomeSeverity.Success,
                    Message = $"Processed pending sessions successfully",
                    Title = title
                };
            }
            catch (Exception ex)
            {
                return new OperationOutcome()
                {
                    Success = false,
                    Severity = OutcomeSeverity.Error,
                    Message = $"Failed to process pending sessions: {ex.Message}",
                    Title = title
                };
            }
        }
    }
}

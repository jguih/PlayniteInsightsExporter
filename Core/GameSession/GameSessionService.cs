using Core.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class GameSessionService : IGameSessionService
    {
        private IPlayniteInsightsExporterContext PluginContext { get; set; }
        private IAppLogger Logger { get; set; }
        private IHashService HashService { get; set; }
        private IPlayniteInsightsWebServerService WebAppService { get; set; }
        private IFileSystemService Fs { get; set; }
        private string SessionsFolderPath { get; set; }
        private GameSessionConfig Config { get; }
        private IPlayniteProgressService ProgressService { get; set; }

        public GameSessionService(
            IPlayniteInsightsExporterContext PluginContext,
            IAppLogger Logger,
            IHashService HashService,
            IPlayniteInsightsWebServerService WebAppService,
            IFileSystemService FileSystemService,
            GameSessionConfig Config,
            IPlayniteProgressService ProgressService
        )
        {
            this.PluginContext = PluginContext;
            this.Logger = Logger;
            this.HashService = HashService;
            this.WebAppService = WebAppService;
            this.Config = Config;
            this.ProgressService = ProgressService;
            Fs = FileSystemService;
            SessionsFolderPath = Config.SESSIONS_DIR_PATH;

            if (!Fs.DirectoryExists(SessionsFolderPath))
            {
                Fs.DirectoryCreate(SessionsFolderPath);
            }
        }

        private async Task<bool> SendOpenSessionAsync(GameSession session)
        {
            var command = OpenSessionCommand.FromSession(session);
            return await WebAppService.PostJson(
                WebAppEndpoints.OpenSession,
                command);
        }

        private async Task<bool> SendCloseSessionAsync(GameSession session)
        {
            var command = CloseSessionCommand.FromSession(session);
            return await WebAppService.PostJson(
                WebAppEndpoints.CloseSession,
                command);
        }

        private bool ShouldClose(DateTime now, GameSession session)
        {
            var sessionAge = now - session.StartTime;
            return sessionAge <= TimeSpan.FromHours(3);
        }

        private bool ShouldDelete(DateTime now, GameSession session)
        {
            var sessionAge = now - session.StartTime;
            return sessionAge.TotalDays > Config.DELETE_FILES_OLDER_THAN_DAYS;
        }

        private bool ShouldStale(DateTime now, GameSession session)
        {
            var sessionAge = now - session.StartTime;
            return sessionAge.TotalHours > Config.STALE_AFTER_HOURS;
        }

        private async Task CloseAndSendSession(GameSession session, ulong duration, DateTime now)
        {
            session.EndTime = now;
            session.Duration = duration;
            session.Status = GameSession.STATUS_CLOSED;
            var result = await SendCloseSessionAsync(session);
            if (!result)
            {
                // Mark session as completed so it can be collected later
                Fs.FileWriteAllText(
                    GetClosedSessionFilePath(session.SessionId),
                    JsonConvert.SerializeObject(session));
            }
        }

        private async Task StaleAndSendSession(GameSession session)
        {
            session.Status = GameSession.STATUS_STALE;
            var result = await SendCloseSessionAsync(session);
            if (!result)
            {
                Fs.FileWriteAllText(
                    GetStaleSessionFilePath(session.SessionId),
                    JsonConvert.SerializeObject(session));
            }
        }

        public string GetSessionId(string gameId, DateTime now)
        {
            return HashService.GetHashForGameSession(gameId, now);
        }

        public string GetSessionFilePath(string gameId)
        {
            return Fs.PathCombine(SessionsFolderPath,
                $"{gameId}{Config.IN_PROGRESS_SUFFIX}{Config.SESSION_FILE_EXTENSION}");
        }

        public string GetStaleSessionFilePath(string sessionId)
        {
            return Fs.PathCombine(SessionsFolderPath,
                $"{sessionId}{Config.STALE_SUFFIX}{Config.SESSION_FILE_EXTENSION}");
        }

        public string GetClosedSessionFilePath(string sessionId)
        {
            return Fs.PathCombine(SessionsFolderPath,
                $"{sessionId}{Config.CLOSED_SUFFIX}{Config.SESSION_FILE_EXTENSION}");
        }

        public async Task<bool> OpenSession(string gameId, DateTime now)
        {
            try
            {
                var sessionFilePath = GetSessionFilePath(gameId);
                if (Fs.FileExists(sessionFilePath))
                {
                    var existingJson = Fs.FileReadAllText(sessionFilePath);
                    var existingSession = JsonConvert.DeserializeObject<GameSession>(existingJson);
                    if (existingSession != null && existingSession.IsValidInProgressSession())
                    {
                        if (ShouldClose(now, existingSession))
                        {
                            // Close session if it is 3 hours old or less
                            var duration = (ulong)(now - existingSession.StartTime).TotalSeconds;
                            await CloseAndSendSession(existingSession, duration, now);
                        }
                        else // Mark existing session as stale
                        {
                            await StaleAndSendSession(existingSession);
                        }
                    }
                    Fs.FileDelete(sessionFilePath);
                }
                // Create new session
                var sessionId = GetSessionId(gameId, now);
                var session = new GameSession()
                {
                    GameId = gameId,
                    SessionId = sessionId,
                    StartTime = now,
                    Status = GameSession.STATUS_IN_PROGRESS
                };
                Fs.FileWriteAllText(sessionFilePath, JsonConvert.SerializeObject(session));
                return await SendOpenSessionAsync(session);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to create session for game {gameId}.");
                return false;
            }
        }

        public async Task<bool> CloseSession(string gameId, ulong duration, DateTime now)
        {
            try
            {
                var sessionFilePath = GetSessionFilePath(gameId);
                if (!Fs.FileExists(sessionFilePath))
                {
                    Logger.Warn($"No open session found for game {gameId} to close.");
                    return false;
                }
                var sessionJson = Fs.FileReadAllText(sessionFilePath);
                var session = JsonConvert.DeserializeObject<GameSession>(sessionJson);
                if (session == null || !session.IsValidInProgressSession())
                {
                    Logger.Warn($"Session data for game {gameId} is invalid and its file will be deleted");
                    Fs.FileDelete(sessionFilePath);
                    return false;
                }
                await CloseAndSendSession(session, duration, now);
                Fs.FileDelete(sessionFilePath);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to close session.");
                return false;
            }
        }

        public async Task<bool> SyncAsync(DateTime now)
        {
            Logger.Debug("Syncing remaining sessions.");
            try
            {
                var pattern = $"*{Config.SESSION_FILE_EXTENSION}";
                foreach (var file in Fs.DirectoryGetFiles(SessionsFolderPath, pattern))
                {
                    var contents = Fs.FileReadAllText(file);
                    var session = JsonConvert.DeserializeObject<GameSession>(contents);
                    if (session == null || !session.IsValid())
                    {
                        Logger.Warn($"Session data in file {file} is invalid and will be deleted");
                        Fs.FileDelete(file);
                        continue;
                    }
                    var createdTime = Fs.FileGetCreationTimeUtc(file);
                    if (session.Status == GameSession.STATUS_IN_PROGRESS)
                    {
                        if (!ShouldStale(now, session)) continue;
                        await StaleAndSendSession(session);
                        Fs.FileDelete(file);
                        continue;
                    }
                    if (session.Status == GameSession.STATUS_CLOSED)
                    {
                        var result = await SendCloseSessionAsync(session);
                        if (result == true)
                        {
                            Fs.FileDelete(file);
                            Logger.Info(file + " deleted after successful sync.");
                        }
                        else if (ShouldDelete(now, session))
                        {
                            Fs.FileDelete(file);
                            Logger.Info(file + " deleted after being stale for too long.");
                        }
                        else
                        {
                            Logger.Warn($"Failed to sync completed session {session.SessionId}. Will retry on next library sync.");
                        }
                        continue;
                    }
                    if (session.Status == GameSession.STATUS_STALE)
                    {
                        var result = await SendCloseSessionAsync(session);
                        if (result == true)
                        {
                            Fs.FileDelete(file);
                            Logger.Info(file + " deleted after successful sync.");
                        }
                        else if (ShouldDelete(now, session))
                        {
                            Fs.FileDelete(file);
                            Logger.Info(file + " deleted after being stale for too long.");
                        }
                        else
                        {
                            Logger.Warn($"Failed to sync stale session {session.SessionId}. Will retry on next library sync.");
                        }
                        continue;
                    }
                    Logger.Warn($"Session {session.SessionId} has an unknown status '{session.Status}' and will be deleted.");
                    Fs.FileDelete(file);
                }
                Logger.Info("Sessions sync completed.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to sync sessions.");
                return false;
            }
        }

        public bool Sync(DateTime now)
        {
            var message = ResourceProvider.GetString("LOC_Loading_SyncClientServer");
            return ProgressService.ActivateGlobalProgress(
                message, 
                false, 
                async (progress) =>
                {
                    progress.IsIndeterminate = true;
                    return await SyncAsync(now);
                }
            );
        }
    }
}

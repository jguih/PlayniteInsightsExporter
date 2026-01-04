using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterCommon.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameSessions.Application
{
    public class GameSessionService : IGameSessionServicePort
    {
        private readonly IAppLoggerPort appLogger;
        private readonly IHashServicePort hashService;
        private readonly IPlayAtlasHttpClientPort playAtlasClient;
        private readonly IFileSystemServicePort fileSystemService;
        private readonly GameSessionConfig config;
        private readonly ISystemConfigPort systemConfig;

        public GameSessionService(
            IAppLoggerPort appLogger, 
            IHashServicePort hashService, 
            IPlayAtlasHttpClientPort playAtlasClient, 
            IFileSystemServicePort fileSystemService, 
            GameSessionConfig config, 
            ISystemConfigPort systemConfig
        )
        {
            this.appLogger = appLogger;
            this.hashService = hashService;
            this.playAtlasClient = playAtlasClient;
            this.fileSystemService = fileSystemService;
            this.config = config;
            this.systemConfig = systemConfig;
        }

        private async Task<bool> SendOpenSessionAsync(GameSession session)
        {
            try
            {
                var command = OpenSessionCommand.FromSession(session);
                var result = await playAtlasClient.PostJson(
                    WebAppEndpoints.OpenSession,
                    command);
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                appLogger.Error("Failed to send open session command.", ex);
                return false;
            }
        }

        private async Task<bool> SendCloseSessionAsync(GameSession session)
        {
            try
            {
                var command = CloseSessionCommand.FromSession(session);
                var result = await playAtlasClient.PostJson(
                    WebAppEndpoints.CloseSession,
                    command);
                return result.IsSuccessStatusCode;
            } catch (Exception ex)
            {
                appLogger.Error("Failed to send close session command.", ex);
                return false;
            }
        }

        private bool ShouldClose(DateTime now, GameSession session)
        {
            var sessionAge = now - session.StartTime;
            return sessionAge <= TimeSpan.FromHours(3);
        }

        private bool ShouldDelete(DateTime now, GameSession session)
        {
            var sessionAge = now - session.StartTime;
            return sessionAge.TotalDays > config.DELETE_FILES_OLDER_THAN_DAYS;
        }

        private bool ShouldStale(DateTime now, GameSession session)
        {
            var sessionAge = now - session.StartTime;
            return sessionAge.TotalHours > config.STALE_AFTER_HOURS;
        }

        private async Task CloseAndSendSession(GameSession session, ulong duration, DateTime now)
        {
            session.Close(now, duration);
            var result = await SendCloseSessionAsync(session);
            if (!result)
            {
                // Mark session as completed so it can be collected later
                fileSystemService.FileWriteAllText(
                    GetClosedSessionFilePath(session.SessionId),
                    JsonConvert.SerializeObject(session));
            }
        }

        private async Task StaleAndSendSession(GameSession session)
        {
            session.Stale();
            var result = await SendCloseSessionAsync(session);
            if (!result)
            {
                fileSystemService.FileWriteAllText(
                    GetStaleSessionFilePath(session.SessionId),
                    JsonConvert.SerializeObject(session));
            }
        }

        public string GetSessionId(string gameId, DateTime now)
        {
            return hashService.GetHashForGameSession(gameId, now);
        }

        public string GetSessionFilePath(string gameId)
        {
            return fileSystemService.PathCombine(systemConfig.SessionsDirPath,
                $"{gameId}{config.IN_PROGRESS_SUFFIX}{config.SESSION_FILE_EXTENSION}");
        }

        public string GetStaleSessionFilePath(string sessionId)
        {
            return fileSystemService.PathCombine(systemConfig.SessionsDirPath,
                $"{sessionId}{config.STALE_SUFFIX}{config.SESSION_FILE_EXTENSION}");
        }

        public string GetClosedSessionFilePath(string sessionId)
        {
            return fileSystemService.PathCombine(systemConfig.SessionsDirPath,
                $"{sessionId}{config.CLOSED_SUFFIX}{config.SESSION_FILE_EXTENSION}");
        }

        public async Task<bool> OpenSessionAsync(string gameId, DateTime now)
        {
            try
            {
                var sessionFilePath = GetSessionFilePath(gameId);
                if (fileSystemService.FileExists(sessionFilePath))
                {
                    var existingJson = fileSystemService.FileReadAllText(sessionFilePath);
                    var existingSession = JsonConvert.DeserializeObject<GameSession>(existingJson);
                    if (existingSession != null)
                    {
                        existingSession.Validate();
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
                    fileSystemService.FileDelete(sessionFilePath);
                }
                // Create new session
                var sessionId = GetSessionId(gameId, now);
                var session = new GameSession()
                {
                    gameId = gameId,
                    sessionId = sessionId,
                    startTime = now,
                    status = GameSession.STATUS_IN_PROGRESS
                };
                fileSystemService.FileWriteAllText(sessionFilePath, JsonConvert.SerializeObject(session));
                return await SendOpenSessionAsync(session);
            }
            catch (Exception ex)
            {
                appLogger.Error(ex, $"Failed to create session for game {gameId}.");
                return false;
            }
        }

        public async Task<bool> CloseSessionAsync(string gameId, ulong duration, DateTime now)
        {
            try
            {
                var sessionFilePath = GetSessionFilePath(gameId);
                if (!fileSystemService.FileExists(sessionFilePath))
                {
                    appLogger.Warn($"No open session found for game {gameId} to close.");
                    return false;
                }
                var sessionJson = fileSystemService.FileReadAllText(sessionFilePath);
                var session = JsonConvert.DeserializeObject<GameSession>(sessionJson);
                if (session == null || !session.IsValidInProgressSession())
                {
                    appLogger.Warn($"Session data for game {gameId} is invalid and its file will be deleted");
                    fileSystemService.FileDelete(sessionFilePath);
                    return false;
                }
                await CloseAndSendSession(session, duration, now);
                fileSystemService.FileDelete(sessionFilePath);
                return true;
            }
            catch (Exception ex)
            {
                appLogger.Error(ex, "Failed to close session.");
                return false;
            }
        }

        public async Task<bool> SyncAsync(DateTime now)
        {
            appLogger.Debug("Syncing remaining sessions.");
            try
            {
                var pattern = $"*{config.SESSION_FILE_EXTENSION}";
                foreach (var file in fileSystemService.DirectoryGetFiles(SessionsFolderPath, pattern))
                {
                    var contents = fileSystemService.FileReadAllText(file);
                    var session = JsonConvert.DeserializeObject<GameSession>(contents);
                    if (session == null || !session.IsValid())
                    {
                        appLogger.Warn($"Session data in file {file} is invalid and will be deleted");
                        fileSystemService.FileDelete(file);
                        continue;
                    }
                    var createdTime = fileSystemService.FileGetCreationTimeUtc(file);
                    if (session.Status == GameSession.STATUS_IN_PROGRESS)
                    {
                        if (!ShouldStale(now, session)) continue;
                        await StaleAndSendSession(session);
                        fileSystemService.FileDelete(file);
                        continue;
                    }
                    if (session.Status == GameSession.STATUS_CLOSED)
                    {
                        var result = await SendCloseSessionAsync(session);
                        if (result == true)
                        {
                            fileSystemService.FileDelete(file);
                            appLogger.Info(file + " deleted after successful sync.");
                        }
                        else if (ShouldDelete(now, session))
                        {
                            fileSystemService.FileDelete(file);
                            appLogger.Info(file + " deleted after being stale for too long.");
                        }
                        else
                        {
                            appLogger.Warn($"Failed to sync completed session {session.SessionId}. Will retry on next library sync.");
                        }
                        continue;
                    }
                    if (session.Status == GameSession.STATUS_STALE)
                    {
                        var result = await SendCloseSessionAsync(session);
                        if (result == true)
                        {
                            fileSystemService.FileDelete(file);
                            appLogger.Info(file + " deleted after successful sync.");
                        }
                        else if (ShouldDelete(now, session))
                        {
                            fileSystemService.FileDelete(file);
                            appLogger.Info(file + " deleted after being stale for too long.");
                        }
                        else
                        {
                            appLogger.Warn($"Failed to sync stale session {session.SessionId}. Will retry on next library sync.");
                        }
                        continue;
                    }
                    appLogger.Warn($"Session {session.SessionId} has an unknown status '{session.Status}' and will be deleted.");
                    fileSystemService.FileDelete(file);
                }
                appLogger.Info("Sessions sync completed.");
                return true;
            }
            catch (Exception ex)
            {
                appLogger.Error(ex, "Failed to sync sessions.");
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

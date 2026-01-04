using ExporterCommon.Application;
using ExporterCommon.Domain;
using ExporterCommon.Infra;
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

        private string GetInProgressSessionFilePath(string gameId)
        {
            return fileSystemService.PathCombine(
                            systemConfig.SessionsDirPath,
                            $"{gameId}" +
                            $"{config.IN_PROGRESS_SUFFIX}" +
                            $"{config.SESSION_FILE_EXTENSION}"
                        );
        }

        private string GetSessionFilePath(GameSession session)
        {
            switch (session.Status)
            {
                case GameSessionStatus.InProgress:
                    {
                        return GetInProgressSessionFilePath(session.GameId);
                    }
                case GameSessionStatus.Stale:
                    {
                        return fileSystemService.PathCombine(
                            systemConfig.SessionsDirPath,
                            $"{session.SessionId}" +
                            $"{config.STALE_SUFFIX}" +
                            $"{config.SESSION_FILE_EXTENSION}"
                        );
                    }
                case GameSessionStatus.Closed:
                    {
                        return fileSystemService.PathCombine(
                            systemConfig.SessionsDirPath,
                            $"{session.SessionId}" +
                            $"{config.CLOSED_SUFFIX}" +
                            $"{config.SESSION_FILE_EXTENSION}"
                        );
                    }
                default:
                    throw new InvalidOperationException($"Invalid session status: {session.Status}");
            }
        }

        private string SerializeSessionToJsonString(GameSession session)
        {
            return JsonConvert.SerializeObject(session, Formatting.Indented);
        }

        private void UpdateSessionFile(GameSession session)
        {
            var path = GetSessionFilePath(session);
            var jsonString = SerializeSessionToJsonString(session);
            fileSystemService.FileWriteAllText(path, jsonString);
        }

        private async Task SendSessionToServerAsync(GameSession session)
        {
            switch (session.Status)
            {
                case GameSessionStatus.InProgress:
                    {
                        OpenGameSessionCommand command = new OpenGameSessionCommand
                        {
                            GameSession = session
                        };
                        await playAtlasClient.OpenGameSessionAsync(command);
                        break;
                    }
                case GameSessionStatus.Stale:
                    {
                        StaleGameSessionCommand command = new StaleGameSessionCommand
                        {
                            GameSession = session
                        };
                        await playAtlasClient.StaleGameSessionAsync(command);
                        break;
                    }
                case GameSessionStatus.Closed:
                    {
                        CloseGameSessionCommand command = new CloseGameSessionCommand
                        {
                            GameSession = session
                        };
                        await playAtlasClient.CloseGameSessionAsync(command);
                        break;
                    }
            }
        }

        private GameSession GetSessionFromFile(string path)
        {
            var existingJson = fileSystemService.FileReadAllText(path);
            var existingSession = JsonConvert.DeserializeObject<GameSession>(existingJson);
            return existingSession ??
                 throw new InvalidDataException(
                    $"Failed to deserialize GameSession from file '{path}'."
                 );
        }

        private bool ShouldClose(DateTime now, GameSession session)
        {
            if (session.Status != GameSessionStatus.InProgress)
            {
                return false;
            }
            var sessionAge = now - session.StartTime;
            return sessionAge <= TimeSpan.FromHours(4);
        }

        private bool ShouldDelete(DateTime now, GameSession session)
        {
            var sessionAge = now - session.StartTime;
            return sessionAge.TotalDays > config.DELETE_FILES_OLDER_THAN_DAYS;
        }

        private bool ShouldStale(DateTime now, GameSession session)
        {
            if (session.Status != GameSessionStatus.InProgress)
            {
                return false;
            }
            var sessionAge = now - session.StartTime;
            return sessionAge.TotalHours > config.STALE_AFTER_HOURS;
        }

        private async Task ProcessPendingSessionAsync(GameSession session, DateTime now, string filePath)
        {
            if (ShouldStale(now, session))
            {
                session.Stale();

                UpdateSessionFile(session);
                fileSystemService.FileDelete(filePath);
                await SendSessionToServerAsync(session);
            }

            if (ShouldDelete(now, session))
            {
                try
                {
                    await SendSessionToServerAsync(session);
                }
                finally
                {
                    fileSystemService.FileDelete(filePath);
                }
            }

            await SendSessionToServerAsync(session);
            fileSystemService.FileDelete(filePath);
        }

        public async Task OpenSessionAsync(string gameId, DateTime now)
        {
            var inProgressSessionFilePath = GetInProgressSessionFilePath(gameId);

            if (fileSystemService.FileExists(inProgressSessionFilePath))
            {
                var existingSession = GetSessionFromFile(inProgressSessionFilePath);

                if (ShouldClose(now, existingSession))
                {
                    var duration = (ulong)(now - existingSession.StartTime).TotalSeconds;
                    existingSession.Close(now, duration);
                }
                else if (ShouldStale(now, existingSession))
                {
                    existingSession.Stale();
                }

                UpdateSessionFile(existingSession);
                fileSystemService.FileDelete(inProgressSessionFilePath);
                await SendSessionToServerAsync(existingSession);
            }

            var sessionId = hashService.ComputeHashForGameSession(gameId, now);
            var session = new GameSession(
                    gameId: gameId,
                    sessionId: sessionId,
                    startTime: now
                );
            UpdateSessionFile(session);
            await SendSessionToServerAsync(session);
        }

        public async Task CloseSessionAsync(string gameId, ulong duration, DateTime now)
        {
            var inProgressSessionFilePath = GetInProgressSessionFilePath(gameId);

            if (!fileSystemService.FileExists(inProgressSessionFilePath))
            {
                appLogger.Error($"No open session found for game {gameId} to close.");
                throw new FileNotFoundException(
                    $"In-progress session file not found for game {gameId}.",
                    inProgressSessionFilePath
                );
            }

            var session = GetSessionFromFile(inProgressSessionFilePath);
            session.Close(now, duration);

            UpdateSessionFile(session);
            fileSystemService.FileDelete(inProgressSessionFilePath);
            await SendSessionToServerAsync(session);
        }

        public async Task ProcessPendingSessionsAsync(DateTime now)
        {
            appLogger.Debug("Syncing pending sessions.");

            var pattern = $"*{config.SESSION_FILE_EXTENSION}";
            var files = fileSystemService
                .DirectoryGetFiles(systemConfig.SessionsDirPath, pattern);

            foreach (var filePath in files)
            {
                GameSession session;

                try
                {
                    session = GetSessionFromFile(filePath);
                }
                catch (Exception ex)
                {
                    appLogger.Error($"Failed to parse file at {filePath} as a valid game session. The file will be deleted.", ex);
                    fileSystemService.FileDelete(filePath);
                    continue;
                }

                try
                {
                    await ProcessPendingSessionAsync(session, now, filePath);
                }
                catch (Exception ex)
                {
                    appLogger.Error($"Failed to process pending session {session.SessionId}", ex);
                }
            }
            appLogger.Debug("Pending game sessions sync completed.");
        }
    }
}

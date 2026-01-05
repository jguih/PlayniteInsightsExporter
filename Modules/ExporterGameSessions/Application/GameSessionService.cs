using ExporterCommon.Application;
using ExporterCommon.Domain;
using ExporterCommon.Infra;
using ExporterGameSessions.Infra;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameSessions.Application
{
    public class GameSessionService : IGameSessionServicePort
    {
        private enum PendingSessionAction
        {
            None,
            StaleAndSend,
            SendAndDelete,
            CloseAndSend
        }

        private readonly IAppLoggerPort appLogger;
        private readonly IHashServicePort hashService;
        private readonly IPlayAtlasHttpClientPort playAtlasClient;
        private readonly IFileSystemServicePort fileSystemService;
        private readonly GameSessionConfig config;
        private readonly ISystemConfigPort systemConfig;
        private readonly IGameSessionSerializerPort gameSessionSerializer;

        public GameSessionService(
            IAppLoggerPort appLogger,
            IHashServicePort hashService,
            IPlayAtlasHttpClientPort playAtlasClient,
            IFileSystemServicePort fileSystemService,
            GameSessionConfig config,
            ISystemConfigPort systemConfig,
            IGameSessionSerializerPort gameSessionSerializer
        )
        {
            this.appLogger = appLogger;
            this.hashService = hashService;
            this.playAtlasClient = playAtlasClient;
            this.fileSystemService = fileSystemService;
            this.config = config;
            this.systemConfig = systemConfig;
            this.gameSessionSerializer = gameSessionSerializer;
        }

        private PendingSessionAction DecidePendingSessionAction(
            DateTime now,
            GameSession session
        )
        {
            if (session.Status != GameSessionStatus.InProgress)
            {
                return PendingSessionAction.SendAndDelete;
            }

            var age = now - session.StartTime;

            if (age > TimeSpan.FromHours(3))
            {
                return PendingSessionAction.StaleAndSend;
            }

            return PendingSessionAction.CloseAndSend;
        }


        private string GetInProgressSessionFilePath(string gameId)
        {
            return fileSystemService.PathCombine(
                            systemConfig.SessionsDirPath,
                            $"{gameId}" +
                            $"{config.InProgressSuffix}" +
                            $"{config.SessionFileExtension}"
                        );
        }

        private void UpdateSessionFile(GameSession session)
        {
            var path = GetSessionFilePath(session);
            var jsonString = gameSessionSerializer.Serialize(session);
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
            var json = fileSystemService.FileReadAllText(path);
            return gameSessionSerializer.Deserialize(json);
        }

        private async Task ProcessPendingSessionAsync(
            GameSession session,
            DateTime now,
            ulong? duration = null
        )
        {
            var filePath = GetSessionFilePath(session);
            var action = DecidePendingSessionAction(now, session);

            switch (action)
            {
                case PendingSessionAction.StaleAndSend:
                    session.Stale();
                    UpdateSessionFile(session);
                    fileSystemService.FileDelete(filePath);
                    await SendSessionToServerAsync(session);
                    break;

                case PendingSessionAction.SendAndDelete:
                    try
                    {
                        await SendSessionToServerAsync(session);
                        fileSystemService.FileDelete(filePath);
                    }
                    catch
                    {
                        if (config.ShouldDeleteAfterFailure(session, now))
                        {
                            fileSystemService.FileDelete(filePath);
                        }
                        throw;
                    }
                    break;

                case PendingSessionAction.CloseAndSend:
                    if (!duration.HasValue)
                    {
                        break;
                    }
                    session.Close(now, duration.Value);
                    UpdateSessionFile(session);
                    fileSystemService.FileDelete(filePath);
                    await SendSessionToServerAsync(session);
                    break;

                case PendingSessionAction.None:
                    break;
            }
        }

        public string GetSessionFilePath(GameSession session)
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
                            $"{config.StaleSuffix}" +
                            $"{config.SessionFileExtension}"
                        );
                    }
                case GameSessionStatus.Closed:
                    {
                        return fileSystemService.PathCombine(
                            systemConfig.SessionsDirPath,
                            $"{session.SessionId}" +
                            $"{config.ClosedSuffix}" +
                            $"{config.SessionFileExtension}"
                        );
                    }
                default:
                    throw new InvalidOperationException($"Invalid session status: {session.Status}");
            }
        }

        public async Task OpenSessionAsync(string gameId, DateTime now)
        {
            var inProgressSessionFilePath = GetInProgressSessionFilePath(gameId);

            try
            {
                if (fileSystemService.FileExists(inProgressSessionFilePath))
                {
                    var existingSession = GetSessionFromFile(inProgressSessionFilePath);
                    var duration = (ulong)(now - existingSession.StartTime).TotalSeconds;
                    await ProcessPendingSessionAsync(existingSession, now, duration);
                }
            }
            catch (IOException ex)
            {
                appLogger.Error($"Failed to process pending session (IO)", ex);
            }
            catch (HttpRequestException ex)
            {
                appLogger.Error($"Failed to sync pending session (network)", ex);
            }
            catch (Exception ex)
            {
                appLogger.Error($"Failed to process pending session", ex);
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

            var pattern = $"*{config.SessionFileExtension}";
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
                    await ProcessPendingSessionAsync(session, now);
                }
                catch (IOException ex)
                {
                    appLogger.Error($"Failed to process pending session {session.SessionId} (IO)", ex);
                }
                catch (HttpRequestException ex)
                {
                    appLogger.Error($"Failed to sync pending session {session.SessionId} (network)", ex);
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

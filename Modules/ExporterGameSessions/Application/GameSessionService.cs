using ExporterCommon.Application;
using ExporterCommon.Domain;
using ExporterCommon.Infra;
using ExporterGameSessions.Error;
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
        private readonly IGameSessionSerializerPort serializer;

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
            this.serializer = gameSessionSerializer;
        }

        private string GetActiveIndexFilePath()
        {
            var path = fileSystemService.PathCombine(
                systemConfig.SessionsDirPath,
                config.ActiveIndexFileName
            );
            return path;
        }

        private Dictionary<string, string> LoadActiveSessionsIndex()
        {
            var path = GetActiveIndexFilePath();

            if (fileSystemService.FileExists(path))
            {
                var jsonString = fileSystemService.FileReadAllText(path);
                var activeIndex = serializer.DeserializeActiveSessionsIndex(jsonString);
                return activeIndex;
            }

            return new Dictionary<string, string>();
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

        private void SaveSession(GameSession session)
        {
            var path = GetSessionFilePath(session);
            var jsonString = serializer.Serialize(session);
            fileSystemService.FileWriteAllText(path, jsonString);
        }

        private void SaveActiveIndex(Dictionary<string, string> activeIndex)
        {
            var path = GetActiveIndexFilePath();
            var jsonString = serializer.Serialize(activeIndex);
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

        private GameSession LoadSession(string sessionId)
        {
            var path = GetSessionFilePath(sessionId);
            if (!fileSystemService.FileExists(path))
                throw new FileNotFoundException(nameof(path));
            var json = fileSystemService.FileReadAllText(path);
            return serializer.Deserialize(json);
        }

        private async Task ProcessPendingSessionAsync(
            GameSession session,
            DateTime now,
            ulong? duration = null
        )
        {
            var filePath = GetSessionFilePath(session);
            var action = DecidePendingSessionAction(now, session);

            void UpdateActiveIndex()
            {
                var activeIndex = LoadActiveSessionsIndex();
                if (activeIndex.TryGetValue(session.GameId, out var activeSessionId) &&
                    activeSessionId == session.SessionId)
                {
                    activeIndex.Remove(session.GameId);
                    SaveActiveIndex(activeIndex);
                }
            }

            switch (action)
            {
                case PendingSessionAction.StaleAndSend:
                    session.Stale();
                    SaveSession(session);

                    UpdateActiveIndex();

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
                    SaveSession(session);

                    UpdateActiveIndex();

                    await SendSessionToServerAsync(session);
                    break;

                case PendingSessionAction.None:
                    break;
            }
        }

        public string GetSessionFilePath(GameSession session)
        {
            return fileSystemService.PathCombine(
                            systemConfig.SessionsDirPath,
                            $"{session.SessionId}" +
                            $"{config.SessionFileExtension}"
                        );
        }

        public string GetSessionFilePath(string sessionId)
        {
            return fileSystemService.PathCombine(
                            systemConfig.SessionsDirPath,
                            $"{sessionId}" +
                            $"{config.SessionFileExtension}"
                        );
        }

        public async Task<GameSessionOperationResult> OpenSessionAsync(string gameId, DateTime now)
        {
            var activeIndex = LoadActiveSessionsIndex();

            try
            {
                if (activeIndex.TryGetValue(gameId, out var activeSessionId))
                {
                    var existingSession = LoadSession(activeSessionId);
                    var duration = (ulong)(now - existingSession.StartTime).TotalSeconds;
                    await ProcessPendingSessionAsync(existingSession, now, duration);
                }
            }
            catch (Exception ex)
            {
                appLogger.Error($"Failed to process pending session", ex);
            }

            var sessionId = hashService.ComputeSHA256Hex(gameId, now);
            var session = new GameSession(
                    gameId: gameId,
                    sessionId: sessionId,
                    startTime: now
                );

            SaveSession(session);
            activeIndex[gameId] = sessionId;
            SaveActiveIndex(activeIndex);

            await SendSessionToServerAsync(session);

            return new GameSessionOperationResult(GetSessionFilePath(sessionId), session);
        }

        public async Task<GameSessionOperationResult> CloseSessionAsync(string gameId, ulong duration, DateTime now)
        {
            var activeIndex = LoadActiveSessionsIndex();

            if (activeIndex.TryGetValue(gameId, out var activeSessionId))
            {
                var session = LoadSession(activeSessionId);
                session.Close(now, duration);

                SaveSession(session);
                activeIndex.Remove(gameId);
                SaveActiveIndex(activeIndex);

                await SendSessionToServerAsync(session);
                return new GameSessionOperationResult(GetSessionFilePath(session), session);
            }

            appLogger.Error($"No open session found for game {gameId} while closing session.");
            throw new GameSessionNotFoundException(
                $"No open session found for game {gameId} while closing session."
            );
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
                    if (!fileSystemService.FileExists(filePath))
                        throw new FileNotFoundException(nameof(filePath));
                    var json = fileSystemService.FileReadAllText(filePath);
                    session = serializer.Deserialize(json);
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
                catch (Exception ex)
                {
                    appLogger.Error($"Failed to process pending session {session.SessionId}", ex);
                }
            }
            appLogger.Debug("Pending game sessions sync completed.");
        }
    }
}

using Newtonsoft.Json;
using PlayniteInsightsExporter.Lib.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib
{
    public interface ISessionTrackingService
    {
        Task<bool> CreateSession(string gameId);
        Task<bool> CloseSession(string gameId, ulong duration);
        Task<bool> Sync();
    }

    public class SessionTrackingService : ISessionTrackingService
    {
        private IPlayniteInsightsExporterContext PluginContext { get; set; }
        private IAppLogger Logger { get; set; }
        private IHashService HashService { get; set; }
        private IPlayniteInsightsWebServerService WebAppService { get; set; }
        private IFileSystemService Fs { get; set; }

        private static readonly string IN_PROGRESS_SUFFIX = "-in-progress";
        private static readonly string COMPLETED_SUFFIX = "-complete";
        private static readonly string STALE_SUFFIX = "-stale";
        private static readonly string SESSION_FILE_EXTENSION = ".json";
        private static readonly int DELETE_FILES_OLDER_THAN_DAYS = 14;
        private static readonly int STALE_AFTER_HOURS = 48;

        public SessionTrackingService(
            IPlayniteInsightsExporterContext PluginContext,
            IAppLogger Logger,
            IHashService HashService,
            IPlayniteInsightsWebServerService WebAppService,
            IFileSystemService FileSystemService
        )
        {
            this.PluginContext = PluginContext;
            this.Logger = Logger;
            this.HashService = HashService;
            this.WebAppService = WebAppService;
            this.Fs = FileSystemService;
        }

        private string SessionsFolderPath
        {
            get
            {
                var path = Path.Combine(PluginContext.CtxGetExtensionDataFolderPath(), "sessions");
                if (!Fs.DirectoryExists(path))
                {
                    Fs.DirectoryCreate(path);
                }
                return path;
            }
        }

        private string GetSessionFilePath(string gameId)
        {
            return Path.Combine(SessionsFolderPath, 
                $"{gameId}{IN_PROGRESS_SUFFIX}{SESSION_FILE_EXTENSION}");
        }

        private string GetStaleSessionFilePath(string sessionId)
        {
            return Path.Combine(SessionsFolderPath, 
                $"{sessionId}{STALE_SUFFIX}{SESSION_FILE_EXTENSION}");
        }

        private string GetCompletedSessionFilePath(string sessionId)
        {
            return Path.Combine(SessionsFolderPath, 
                $"{sessionId}{COMPLETED_SUFFIX}{SESSION_FILE_EXTENSION}");
        }

        private async Task<bool> SendOpenSessionAsync(GameSession session)
        {
            try
            {
                var json = JsonConvert.SerializeObject(session);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                return await WebAppService.Post(WebAppEndpoints.OpenSession, content);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to send open session for game {session.GameId}.");
                return false;
            }
        }

        private async Task<bool> SendCloseSessionAsync(GameSession session)
        {
            try
            {
                var json = JsonConvert.SerializeObject(session);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                return await WebAppService.Post(WebAppEndpoints.CloseSession, content);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to send close session for game {session.GameId}.");
                return false;
            }
        }

        public async Task<bool> CreateSession(string gameId)
        {
            try
            {
                var now = DateTime.UtcNow;
                if (Fs.FileExists(GetSessionFilePath(gameId)))
                {
                    var existingJson = Fs.FileReadAllText(GetSessionFilePath(gameId));
                    var existingSession = JsonConvert.DeserializeObject<GameSession>(existingJson);
                    if (existingSession != null)
                    {
                        // Mark session as stale
                        existingSession.Status = GameSession.STATUS_STALE;
                        Fs.FileWriteAllText(
                            GetStaleSessionFilePath(existingSession.SessionId), 
                            JsonConvert.SerializeObject(existingSession));
                    }
                    Fs.FileDelete(GetSessionFilePath(gameId));
                }
                // Create new session
                var sessionId = HashService.GetHashForGameSession(gameId, now);
                var session = new GameSession(gameId, now, sessionId, GameSession.STATUS_IN_PROGRESS);
                Fs.FileWriteAllText(
                    GetSessionFilePath(gameId),
                    JsonConvert.SerializeObject(session));
                return await SendOpenSessionAsync(session);
            } catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to create session for game {gameId}.");
                return false;
            }
        }

        public async Task<bool> CloseSession(string gameId, ulong duration)
        {
            try
            {
                if (!File.Exists(GetSessionFilePath(gameId)))
                {
                    Logger.Warn($"No session found for game {gameId} to end.");
                    return false;
                }
                var sessionJson = File.ReadAllText(GetSessionFilePath(gameId));
                var session = JsonConvert.DeserializeObject<GameSession>(sessionJson);
                if (session == null)
                {
                    Logger.Warn($"Session data for game {gameId} is invalid.");
                    return false;
                }
                session.EndTime = DateTime.UtcNow;
                session.Duration = duration;
                session.Status = GameSession.STATUS_COMPLETE;
                var result = await SendCloseSessionAsync(session);
                if (result == false)
                {
                    // Mark session as completed so it can be collected later
                    File.WriteAllText(
                        GetCompletedSessionFilePath(session.SessionId),
                        JsonConvert.SerializeObject(session));
                }
                File.Delete(GetSessionFilePath(gameId));
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to close session.");
                return false;
            }
        }

        public async Task<bool> Sync()
        {
            Logger.Debug("Syncing remaining sessions with web app.");
            try
            {
                var pattern = $"*{STALE_SUFFIX}{SESSION_FILE_EXTENSION}";
                foreach (var file in Directory.GetFiles(SessionsFolderPath, pattern))
                {
                    var contents = File.ReadAllText(file);
                    var createdTime = File.GetCreationTimeUtc(file);
                    var shouldDelete = (DateTime.UtcNow - createdTime).TotalDays > DELETE_FILES_OLDER_THAN_DAYS;
                    var session = JsonConvert.DeserializeObject<GameSession>(contents);
                    if (session == null || !session.IsValidStaleSession())
                    {
                        Logger.Warn($"Session data in file {file} is invalid.");
                        File.Delete(file);
                        continue;
                    }
                    var result = await SendCloseSessionAsync(session);
                    if (result)
                    {
                        File.Delete(file);
                        Logger.Info(file + " deleted after successful sync.");
                    }
                    else if (shouldDelete)
                    {
                        File.Delete(file);
                        Logger.Info(file + " deleted after being stale for too long.");
                    }
                    else
                    {
                        Logger.Warn($"Failed to sync stale session {session.SessionId}. Will retry on next library sync.");
                    }
                }
                pattern = $"*{COMPLETED_SUFFIX}{SESSION_FILE_EXTENSION}";
                foreach (var file in Directory.GetFiles(SessionsFolderPath, pattern))
                {
                    var contents = File.ReadAllText(file);
                    var createdTime = File.GetCreationTimeUtc(file);
                    var shouldDelete = (DateTime.UtcNow - createdTime).TotalDays > DELETE_FILES_OLDER_THAN_DAYS;
                    var session = JsonConvert.DeserializeObject<GameSession>(contents);
                    if (session == null || !session.IsValidCompleteSession())
                    {
                        Logger.Warn($"Session data in file {file} is invalid and will be deleted");
                        File.Delete(file);
                        continue;
                    }
                    var result = await SendCloseSessionAsync(session);
                    if (result)
                    {
                        File.Delete(file);
                        Logger.Info(file + " deleted after successful sync.");
                    }
                    else if (shouldDelete)
                    {
                        File.Delete(file);
                        Logger.Info(file + " deleted after being stale for too long.");
                    }
                    else
                    {
                        Logger.Warn($"Failed to sync stale session {session.SessionId}. Will retry on next library sync.");
                    }
                }
                pattern = $"*{IN_PROGRESS_SUFFIX}{SESSION_FILE_EXTENSION}";
                foreach (var file in Directory.GetFiles(SessionsFolderPath, pattern))
                {
                    var contents = File.ReadAllText(file);
                    var createdTime = File.GetCreationTimeUtc(file);
                    var shouldStale = (DateTime.UtcNow - createdTime).TotalHours > STALE_AFTER_HOURS;
                    if (!shouldStale) continue;
                    var session = JsonConvert.DeserializeObject<GameSession>(contents);
                    if (session == null || !session.IsValidInProgressSession())
                    {
                        Logger.Warn($"Session data in file {file} is invalid and will be deleted");
                        File.Delete(file);
                        continue;
                    }
                    session.Status = GameSession.STATUS_STALE;
                    var result = await SendCloseSessionAsync(session);
                    if (!result)
                    {
                        Logger.Info($"Failed to sync in-progress session {session.SessionId} that became stale (older than {STALE_AFTER_HOURS} hours). Will mark it as stale and retry on next library sync.");
                        File.WriteAllText(
                            GetStaleSessionFilePath(session.SessionId),
                            JsonConvert.SerializeObject(session));
                    }
                    File.Delete(file);
                }
                Logger.Info("Sessions sync completed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to sync sessions.");
                return false;
            }
        }
    }
}

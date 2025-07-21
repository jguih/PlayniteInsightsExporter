using Newtonsoft.Json;
using Playnite.SDK;
using PlayniteInsightsExporter.Lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib
{
    public class SessionTrackingService
    {
        private PlayniteInsightsExporter Plugin { get; set; }
        private ILogger Logger { get; set; }
        private IHashService HashService { get; set; }
        private IPlayniteInsightsWebServerService WebAppService { get; set; }

        public SessionTrackingService(
            PlayniteInsightsExporter Plugin,
            ILogger Logger,
            IHashService HashService,
            IPlayniteInsightsWebServerService WebAppService
        )
        {
            this.Plugin = Plugin;
            this.Logger = Logger;
            this.HashService = HashService;
            this.WebAppService = WebAppService;
        }

        private string SessionsFolderPath
        {
            get
            {
                var path = Path.Combine(Plugin.GetPluginUserDataPath(), "sessions");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        private string GetSessionFilePath(string gameId)
        {
            return Path.Combine(SessionsFolderPath, $"{gameId}-in-progress.json");
        }

        private string GetStaleSessionFilePath(string sessionId)
        {
            return Path.Combine(SessionsFolderPath, $"{sessionId}-stale.json");
        }

        private string GetCompletedSessionFilePath(string sessionId)
        {
            return Path.Combine(SessionsFolderPath, $"{sessionId}-completed.json");
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
                var openSessionFile = GetSessionFilePath(gameId);
                var now = DateTime.UtcNow;
                var sessionId = HashService.GetHashForGameSession(gameId, now);
                if (File.Exists(openSessionFile))
                {
                    File.Move(openSessionFile, GetStaleSessionFilePath(sessionId));
                }
                var session = new GameSession(gameId, now, sessionId);
                var sessionJson = JsonConvert.SerializeObject(session);
                File.WriteAllText(openSessionFile, sessionJson);
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
                var result = await SendCloseSessionAsync(session);
                if (result == false)
                {
                    // Mark session as completed so it can be collected later
                    sessionJson = JsonConvert.SerializeObject(session);
                    File.WriteAllText(GetCompletedSessionFilePath(session.SessionId), sessionJson);
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class GameSession
    {
        public string SessionId { get; set; }
        public string GameId { get; set; }
        public string Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public ulong? Duration { get; set; }

        public static string STATUS_IN_PROGRESS = "in_progress";
        public static string STATUS_COMPLETE = "complete";
        public static string STATUS_STALE = "stale";

        public GameSession() { }

        public GameSession(
            string gameId, 
            DateTime startTime, 
            string sessionId, 
            string status,
            DateTime? endtime = null, 
            ulong? duration = null)
        {
            GameId = gameId;
            StartTime = startTime;
            EndTime = endtime;
            Status = status;
            Duration = duration;
            SessionId = sessionId;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(SessionId) &&
                   !string.IsNullOrEmpty(GameId) &&
                   StartTime != null &&
                   (Status == STATUS_IN_PROGRESS || 
                   Status == STATUS_COMPLETE || 
                   Status == STATUS_STALE);
        }

        public bool IsValidCompleteSession()
        {
            return IsValid() && 
                Status == STATUS_COMPLETE && 
                EndTime.HasValue && 
                Duration.HasValue;
        }

        public bool IsValidInProgressSession()
        {
            return IsValid() && 
                Status == STATUS_IN_PROGRESS && 
                EndTime == null && 
                Duration == null;
        }

        public bool IsValidStaleSession()
        {
            return IsValid() && Status == STATUS_STALE;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib.Models
{
    public class GameSession
    {
        public string SessionId { get; set; }
        public string GameId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public ulong? Duration { get; set; }

        public GameSession(
            string gameId, 
            DateTime startTime, 
            string sessionId, 
            DateTime? endtime = null, 
            ulong? duration = null)
        {
            GameId = gameId;
            StartTime = startTime;
            EndTime = endtime;
            Duration = duration;
            SessionId = sessionId;
        }

    }
}

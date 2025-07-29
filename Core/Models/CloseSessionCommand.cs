using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class CloseSessionCommand : BaseCommand
    {
        public string SessionId { get; set; }
        public string GameId { get; set; }
        public string Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public ulong? Duration { get; set; }

        public CloseSessionCommand() { }

        public static CloseSessionCommand FromSession(GameSession session)
        {
            if (session == null || !session.IsValidClosedSession())
            {
                return null;
            }
            return new CloseSessionCommand
            {
                SessionId = session.SessionId,
                GameId = session.GameId,
                Status = session.Status,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Duration = session.Duration
            };
        }
    }
}

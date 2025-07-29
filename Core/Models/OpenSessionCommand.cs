using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class OpenSessionCommand : BaseCommand
    {
        public string SessionId { get; set; }
        public string GameId { get; set; }
        public string Status { get; set; }
        public DateTime StartTime { get; set; }

        public OpenSessionCommand() { }

        public static OpenSessionCommand FromSession(GameSession session)
        {
            if (session == null || !session.IsValid())
            {
                return null;
            }
            return new OpenSessionCommand
            {
                SessionId = session.SessionId,
                GameId = session.GameId,
                Status = session.Status,
                StartTime = session.StartTime
            };
        }
    }
}

using ExporterCommon.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameSessions.Application
{
    public class GameSessionOperationResult
    {
        public readonly string SessionFilePath;
        public readonly GameSession Session;

        public GameSessionOperationResult(string sessionFilePath, GameSession session)
        {
            SessionFilePath = sessionFilePath;
            Session = session;
        }
    }
}

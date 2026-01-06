using ExporterCommon.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameSessions.Application
{
    public class GameSessionConfig
    {
        public string SessionFileExtension { get; } = ".session.json";
        public string ActiveIndexFileName { get; } = "active-index.json";
        public TimeSpan MaxRetention { get; } = TimeSpan.FromDays(14);

        public GameSessionConfig()
        {
        }

        public bool ShouldDeleteAfterFailure(GameSession session, DateTime now)
        {
            return now - session.EndTime > MaxRetention;
        }
    }
}

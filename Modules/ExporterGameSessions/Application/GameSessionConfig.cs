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
        public string InProgressSuffix { get; } = "-in-progress";
        public string ClosedSuffix { get; } = "-closed";
        public string StaleSuffix { get; } = "-stale";
        public string SessionFileExtension { get; } = ".json";
        public TimeSpan MaxRetention { get; } = TimeSpan.FromDays(7);

        public GameSessionConfig()
        {
        }

        public bool ShouldDeleteAfterFailure(GameSession session, DateTime now)
        {
            return now - session.EndTime > MaxRetention;
        }
    }
}

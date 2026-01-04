using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Domain
{
    public enum GameSessionStatus
    {
        InProgress,
        Closed,
        Stale
    }

    public class GameSession : BaseEntity
    {
        private readonly string sessionId;
        private readonly string gameId;
        private readonly DateTime startTime = DateTime.UtcNow;
        private GameSessionStatus status = GameSessionStatus.InProgress;
        private DateTime? endTime = null;
        private ulong? duration = null;

        public string SessionId { get { return sessionId; } }
        public string GameId { get { return gameId; } }
        public DateTime StartTime { get { return startTime; } }
        public GameSessionStatus Status { get { return status; } }
        public DateTime? EndTime { get { return endTime; } }
        public ulong? Duration { get { return duration; } }

        public GameSession(
            string gameId, 
            string sessionId, 
            DateTime? startTime = null,
            DateTime? endTime = null, 
            GameSessionStatus? status = null,
            ulong? duration = null)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentNullException(nameof(sessionId));
            if (string.IsNullOrEmpty(gameId))
                throw new ArgumentNullException(nameof(gameId));
            this.sessionId = sessionId;
            this.gameId = gameId;

            if (status != null)
                this.status = (GameSessionStatus) status;
            if (startTime != null)
                this.startTime = (DateTime) startTime;

            this.endTime = endTime;
            this.duration = duration;
        }

        public void Stale(DateTime? endTime = null, ulong? duration = null)
        {
            if (status != GameSessionStatus.InProgress)
                throw new InvalidOperationException("Cannot stale not in-progress session");
            if (endTime != null)
            {
                if (endTime <= startTime)
                    throw new ArgumentOutOfRangeException(nameof(endTime));
                this.endTime = endTime;
            }
            if (duration != null)
            {
                if (duration < 0)
                    throw new ArgumentOutOfRangeException(nameof(duration));
                this.duration = duration;
            }
            status = GameSessionStatus.Stale;
        }

        public void Close(DateTime endTime, ulong duration)
        {
            if (status != GameSessionStatus.InProgress)
                throw new InvalidOperationException("Cannot close not in-progress session");
            if (endTime <= startTime)
                throw new ArgumentOutOfRangeException(nameof(endTime));
            if (duration < 0)
                throw new ArgumentOutOfRangeException(nameof(duration));
            this.duration = duration;
            this.endTime = endTime;
            status = GameSessionStatus.Closed;
        }
    }
}

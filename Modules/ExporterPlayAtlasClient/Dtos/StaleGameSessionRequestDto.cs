using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Dtos
{
    public class StaleGameSessionRequestDto : BaseRequestDto
    {
        public static readonly string ENDPOINT = "/api/extension/session/stale";
        public string SessionId { get; }
        public string GameId { get; }
        public DateTime StartTime { get; }

        public StaleGameSessionRequestDto(
            string sessionId,
            string gameId,
            DateTime startTime
        )
        {
            SessionId = sessionId;
            GameId = gameId;
            StartTime = startTime;
        }
    }
}

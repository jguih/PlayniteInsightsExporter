using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Dtos
{
    [JsonObject]
    public class OpenGameSessionRequestDto : BaseRequestDto
    {
        public static readonly string ENDPOINT = "/api/extension/session/open";
        public DateTime ClientUtcNow { get; } = DateTime.UtcNow;
        public string SessionId { get; }
        public string GameId { get; }
        public DateTime StartTime { get; }

        public OpenGameSessionRequestDto(
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

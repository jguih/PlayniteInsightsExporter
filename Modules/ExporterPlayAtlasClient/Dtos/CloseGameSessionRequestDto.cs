using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Dtos
{
    [JsonObject]
    public class CloseGameSessionRequestDto : BaseRequestDto
    {
        public static readonly string ENDPOINT = "/api/extension/session/close";
        public string SessionId { get; }
        public string GameId { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }
        public ulong Duration { get; }

        public CloseGameSessionRequestDto(
            string sessionId, 
            string gameId, 
            DateTime startTime, 
            DateTime endTime, 
            ulong duration
        )
        {
            SessionId = sessionId;
            GameId = gameId;
            StartTime = startTime;
            EndTime = endTime;
            Duration = duration;
        }
    }
}

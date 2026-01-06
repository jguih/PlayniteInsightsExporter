using ExporterCommon.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameSessions.Infra
{
    public class GameSessionSerializer : IGameSessionSerializerPort
    {
        public GameSession Deserialize(string json)
        {
            var existingSession = JsonConvert.DeserializeObject<GameSession>(json);
            return existingSession ??
                 throw new InvalidDataException("Failed to deserialize GameSession.");
        }

        public Dictionary<string, string> DeserializeActiveSessionsIndex(string json)
        {
            var activeIndex = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            return activeIndex ?? new Dictionary<string, string>();
        }

        public string Serialize(GameSession session)
        {
            return JsonConvert.SerializeObject(session, Formatting.Indented);
        }

        public string Serialize(Dictionary<string, string> activeIndex)
        {
            return JsonConvert.SerializeObject(activeIndex, Formatting.Indented);
        }
    }
}

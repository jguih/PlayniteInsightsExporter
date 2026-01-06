using ExporterCommon.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameSessions.Infra
{
    public interface IGameSessionSerializerPort
    {
        string Serialize(GameSession session);
        string Serialize(Dictionary<string, string> activeIndex);
        GameSession Deserialize(string json);
        Dictionary<string, string> DeserializeActiveSessionsIndex(string json);
    }
}

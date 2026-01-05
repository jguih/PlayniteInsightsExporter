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
        GameSession Deserialize(string json);
    }
}

using ExporterGameSessions.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application
{
    public class ExporterGameSessionApi
    {
        public readonly IGameSessionServicePort GameSessionService;

        public ExporterGameSessionApi(IGameSessionServicePort gameSessionService)
        {
            GameSessionService = gameSessionService;
        }
    }
}

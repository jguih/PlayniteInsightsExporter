using ExporterBootstrap.Application.Modules;
using ExporterGameSessions.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Lib.Modules;
internal sealed class TestGameSessionModule : IGameSessionModulePort
{
    public IGameSessionServicePort GameSessionService { get; }

    public TestGameSessionModule(
        IGameSessionServicePort gameSessionService
    )
    {
        GameSessionService = gameSessionService;
    }
}

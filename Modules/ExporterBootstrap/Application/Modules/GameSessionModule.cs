using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterGameSessions.Application;
using ExporterGameSessions.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application.Modules
{
    public sealed class GameSessionModule : IGameSessionModulePort
    {
        public IGameSessionServicePort GameSessionService { get; }

        public GameSessionModule(
            IAppLoggerPort appLogger,
            IHashServicePort hashService,
            IPlayAtlasHttpClientPort playAtlasClient,
            IFileSystemServicePort fileSystemService,
            ISystemConfigPort systemConfig
        )
        {
            GameSessionConfig config = new GameSessionConfig();
            IGameSessionSerializerPort gameSessionSerializer = new GameSessionSerializer();
            GameSessionService = new GameSessionService(
                    appLogger,
                    hashService,
                    playAtlasClient,
                    fileSystemService,
                    config,
                    systemConfig,
                    gameSessionSerializer
                );
        }
    }
}

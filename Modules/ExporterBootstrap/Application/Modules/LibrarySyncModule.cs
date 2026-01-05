using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterLibraryExporter.Application;
using ExporterPlayAtlasClient.Application;
using ExporterSystem.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application.Modules
{
    public class LibrarySyncModule : ILibrarySyncModulePort
    {
        public ILibrarySyncServicePort LibrarySyncService { get; }
        public IGameSyncItemFactoryPort GameSyncItemFactory { get; }

        public LibrarySyncModule(
            IAppLoggerPort appLogger,
            IPlayAtlasHttpClientPort httpClient,
            IHashServicePort hashService,
            IFileSystemServicePort fileSystem,
            ISystemConfigPort systemConfig,
            IPlayniteGameRepositoryPort gameRepository
        )
        {
            LibrarySyncService = new LibrarySyncService(
                 appLogger: appLogger,
                 playAtlasHttpClient: httpClient,
                 hashService: hashService,
                 fileSystemService: fileSystem,
                 systemConfig: systemConfig,
                 gameRepository: gameRepository
            );
            GameSyncItemFactory = new GameSyncItemFactory(hashService);
        }
    }
}

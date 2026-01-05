using ExporterLibraryExporter.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application
{
    public class ExporterLibrarySyncApi
    {
        public readonly ILibrarySyncServicePort LibrarySyncService;
        public readonly IGameSyncItemFactoryPort GameSyncItemFactory;

        public ExporterLibrarySyncApi(
            ILibrarySyncServicePort librarySyncService,
            IGameSyncItemFactoryPort gameSyncItemFactory
        )
        {
            LibrarySyncService = librarySyncService;
            GameSyncItemFactory = gameSyncItemFactory;
        }
    }
}

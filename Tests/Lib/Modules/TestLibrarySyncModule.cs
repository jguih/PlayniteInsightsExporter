using ExporterBootstrap.Application.Modules;
using ExporterLibraryExporter.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Lib.Adapters;

namespace Tests.Lib.Modules;
internal sealed class TestLibrarySyncModule : ILibrarySyncModulePort
{
    public ILibrarySyncServicePort LibrarySyncService { get; }
    public IGameSyncItemFactoryPort GameSyncItemFactory { get; }

    public TestLibrarySyncModule(
        IGameSyncItemFactoryPort gameSyncItemFactory,
        ILibrarySyncServicePort? librarySyncService = null
    )
    {
        GameSyncItemFactory = gameSyncItemFactory;
        LibrarySyncService = librarySyncService ?? new NoOpLibrarySyncService();
    }
}

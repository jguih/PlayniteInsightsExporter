using ExporterLibraryExporter.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application.Modules
{
    public interface ILibrarySyncModulePort
    {
        ILibrarySyncServicePort LibrarySyncService { get; }
        IGameSyncItemFactoryPort GameSyncItemFactory { get; }
    }
}

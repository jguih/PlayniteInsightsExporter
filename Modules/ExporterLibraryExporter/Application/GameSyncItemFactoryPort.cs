using ExporterCommon.Application;
using ExporterCommon.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterLibraryExporter.Application
{
    public interface IGameSyncItemFactoryPort
    {
        SyncGameCommandItem Create(AppGame game);
    }
}

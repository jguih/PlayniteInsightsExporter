using ExporterCommon.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Application
{
    public interface ISyncGamesDtoMapperPort
    {
        SyncGamesRequestDto Map(SyncGamesCommand command);
    }
}

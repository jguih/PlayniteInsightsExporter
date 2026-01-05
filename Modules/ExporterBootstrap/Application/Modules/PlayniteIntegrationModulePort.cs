using ExporterPlayniteIntegration.Queries.GetAllGames;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application.Modules
{
    public interface IPlayniteIntegrationModulePort
    {
        IGetAllGamesQueryHandlerPort GetAllGamesQueryHandler { get; }
    }
}

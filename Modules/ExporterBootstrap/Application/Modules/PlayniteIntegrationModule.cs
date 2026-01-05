using ExporterCommon.Application;
using ExporterPlayniteIntegration.Queries.GetAllGames;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application.Modules
{
    public class PlayniteIntegrationModule : IPlayniteIntegrationModulePort
    {
        public IGetAllGamesQueryHandlerPort GetAllGamesQueryHandler { get; }

        public PlayniteIntegrationModule(
            IPlayniteGameRepositoryPort gameRepository    
        )
        {
            GetAllGamesQueryHandler = new GetAllGamesQueryHandler(
                gameRepository
            );
        }
    }
}

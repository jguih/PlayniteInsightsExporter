using ExporterPlayniteIntegration.Queries.GetAllGames;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application
{
    public class ExporterPlayniteIntegrationApiQuery
    {
        public readonly IGetAllGamesQueryHandlerPort GetAllGames;

        public ExporterPlayniteIntegrationApiQuery(IGetAllGamesQueryHandlerPort getAllGames)
        {
            GetAllGames = getAllGames;
        }
    }

    public class ExporterPlayniteIntegrationApi
    {
        public readonly ExporterPlayniteIntegrationApiQuery Query;

        public ExporterPlayniteIntegrationApi(
            ExporterPlayniteIntegrationApiQuery query
        )
        {
            Query = query;
        }
    }
}

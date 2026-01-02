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
        public readonly GetAllGamesQueryHandler GetAllGames;

        public ExporterPlayniteIntegrationApiQuery(GetAllGamesQueryHandler getAllGames)
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

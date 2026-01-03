using ExporterCommon.Application;
using ExporterCommon.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayniteIntegration.Queries.GetAllGames
{
    public interface IGetAllGamesQueryHandlerPort : IQueryHandlerPort<IReadOnlyList<AppGame>> { }

    public class GetAllGamesQueryHandler : IGetAllGamesQueryHandlerPort
    {
        private readonly IPlayniteGameRepositoryPort playniteGameRepository;

        public GetAllGamesQueryHandler(
            IPlayniteGameRepositoryPort playniteGameRepository
        )
        {
            this.playniteGameRepository = playniteGameRepository;
        }

        public IReadOnlyList<AppGame> Execute()
        {
            var games = playniteGameRepository.GetAll();

            return games;
        }
    }
}

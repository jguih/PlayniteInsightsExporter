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
    public class GetAllGamesQueryHandler : IQueryHandlerPort<IReadOnlyList<AppGame>>
    {
        private readonly IPlayniteGameRepositoryPort playniteGameRepository;
        private readonly IHashServicePort hashService;

        public GetAllGamesQueryHandler(
            IPlayniteGameRepositoryPort playniteGameRepository,
            IHashServicePort hashService)
        {
            this.playniteGameRepository = playniteGameRepository;
            this.hashService = hashService;
        }

        public IReadOnlyList<AppGame> Execute()
        {
            var games = playniteGameRepository.GetAll();

            foreach (var game in games)
            {
                game.ContentHash = hashService.ComputeHashFromGame(game);
            }

            return games;
        }
    }
}

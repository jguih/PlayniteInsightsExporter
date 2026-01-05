using ExporterBootstrap.Application;
using ExporterCommon.Application;
using ExporterCommon.Domain;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Adapters
{
    internal class PlayniteGameRepositoryAdapter : IPlayniteGameRepositoryPort
    {
        private readonly IPlayniteAPI api;
        private readonly IPlayniteGameMapperPort gameMapper;

        public PlayniteGameRepositoryAdapter(
            IPlayniteAPI api
        )
        {
            this.api = api;
            this.gameMapper = new PlayniteGameMapper();
        }

        public IReadOnlyList<AppGame> GetAll()
        {
            var playniteGames = api.Database.Games;
            var games = playniteGames
                .Select(gameMapper.Map)
                .ToList();
            return games;
        }
    }
}

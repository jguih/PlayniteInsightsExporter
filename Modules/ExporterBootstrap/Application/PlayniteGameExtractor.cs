using ExporterCommon.Application;
using ExporterCommon.Domain;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application
{
    public class PlayniteGameExtractor : IPlayniteGameExtractorPort
    {
        private readonly IHashServicePort hashService;
        private readonly IPlayniteGameMapperPort gameMapper;

        public PlayniteGameExtractor(
            IHashServicePort hashService,
            IPlayniteGameMapperPort gameMapper
        )
        {
            this.hashService = hashService;
            this.gameMapper = gameMapper;
        }

        public SyncGameCommandItem Extract(Game game)
        {
            AppGame entity = gameMapper.Map(game);
            string contentHash = hashService.ComputeHashFromGame(entity);
            return new SyncGameCommandItem(entity, contentHash);
        }
    }
}

using ExporterCommon.Application;
using ExporterCommon.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterLibraryExporter.Application
{
    public sealed class GameSyncItemFactory : IGameSyncItemFactoryPort
    {
        private readonly IHashServicePort hashService;

        public GameSyncItemFactory(IHashServicePort hashService)
        {
            this.hashService = hashService;
        }

        public SyncGameCommandItem Create(AppGame game)
        {
            string contentHash = hashService.ComputeHashFromGame(game);
            return new SyncGameCommandItem(game, contentHash);
        }
    }
}

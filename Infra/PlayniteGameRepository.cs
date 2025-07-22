using Core;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infra
{
    public class PlayniteGameRepository : IPlayniteGameRepository
    {
        private readonly IPlayniteAPI Api;
        private readonly IAppLogger Logger;

        public PlayniteGameRepository(IPlayniteAPI api, IAppLogger logger)
        {
            Api = api;
            Logger = logger;
        }

        public IItemCollection<Game> GetAll()
        {
            return Api.Database.Games;
        }

        public IEnumerable<string> GetIdList()
        {
            return Api.Database.Games.Select(g => g.Id.ToString());
        }
    }
}

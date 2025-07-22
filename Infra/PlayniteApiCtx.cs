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
    public class PlayniteApiCtx : IPlayniteApiContext
    {
        public IPlayniteAPI PlayniteApi { get; }

        public PlayniteApiCtx(IPlayniteAPI PlayniteApi)
        {
            this.PlayniteApi = PlayniteApi;
        }

        public IItemCollection<Game> DatabaseGames()
        {
            return PlayniteApi.Database.Games;
        }

        public GlobalProgressResult DialogsActivateGlobalProgress(Func<GlobalProgressActionArgs, Task> progresAction, GlobalProgressOptions progressOptions)
        {
            return PlayniteApi.Dialogs.ActivateGlobalProgress(progresAction, progressOptions);
        }
    }
}

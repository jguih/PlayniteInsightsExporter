using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public interface IPlayniteApiContext
    {
        IItemCollection<Game> DatabaseGames();
        GlobalProgressResult DialogsActivateGlobalProgress(Func<GlobalProgressActionArgs, Task> progresAction, GlobalProgressOptions progressOptions);
    }
}

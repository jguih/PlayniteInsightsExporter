using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public interface IPlayniteGameRepository
    {
        IEnumerable<string> GetIdList();
        IItemCollection<Game> GetAll();
    }
}

using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public interface IHashService
    {
        string HashFolderContents(string dir);
        string GetHashFromPlayniteGame(Game game);
        string GetHashForGameSession(string gameId, DateTime startTime);
    }
}

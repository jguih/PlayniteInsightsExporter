using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public interface IGameSessionService
    {
        /// <summary>
        /// Opens a game session.
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns></returns>
        Task<bool> OpenSession(string gameId);
        /// <summary>
        /// Closes a game session.
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        Task<bool> CloseSession(string gameId, ulong duration);
        /// <summary>
        /// Synchronizes remaining session data with the server.
        /// </summary>
        /// <returns></returns>
        Task<bool> Sync();
    }
}

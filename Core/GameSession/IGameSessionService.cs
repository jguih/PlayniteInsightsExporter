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
        Task<bool> OpenSession(string gameId, DateTime now);
        /// <summary>
        /// Closes a game session.
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        Task<bool> CloseSession(string gameId, ulong duration, DateTime now);
        /// <summary>
        /// Synchronizes remaining session data with the server.
        /// </summary>
        /// <returns></returns>
        Task<bool> SyncAsync(DateTime now);
        /// <summary>
        /// Synchronizes remaining session data with the server. No global progress dialog is shown.
        /// </summary>
        /// <returns></returns>
        bool Sync(DateTime now);
    }
}

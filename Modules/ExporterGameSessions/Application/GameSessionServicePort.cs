using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameSessions.Application
{
    public interface IGameSessionServicePort
    {
        /// <summary>
        /// Opens a game session.
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns></returns>
        Task<bool> OpenSessionAsync(string gameId, DateTime now);
        /// <summary>
        /// Closes a game session.
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        Task<bool> CloseSessionAsync(string gameId, ulong duration, DateTime now);
        /// <summary>
        /// Synchronizes remaining session data with the server.
        /// </summary>
        /// <returns></returns>
        Task<bool> SyncAsync(DateTime now);
    }
}

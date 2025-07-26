using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public interface ILibExporter
    {
        /// <summary>
        /// Syncs games with the server.
        /// </summary>
        /// <param name="showProgress">Whether to show global progress.</param>
        /// <param name="itemsToAdd">List of games to add. If null, it'll check the entire database for new games.</param>
        /// <param name="itemsToUpdate">List of games to update. If null, it'll check the entire database for updated games.</param>
        /// <param name="itemsToRemove">List of games to remove. If null, it'll check the entire database for removed games.</param>
        /// <returns></returns>
        bool RunLibrarySync(
            List<Game> itemsToAdd = null,
            List<Game> itemsToUpdate = null,
            List<Game> itemsToRemove = null
        );
        /// <summary>
        /// Syncs games with the server. Asynchronous version of <see cref="RunLibrarySync"/>. Does not show global progress.
        /// </summary>
        /// <param name="showProgress">Whether to show global progress.</param>
        /// <param name="itemsToAdd">List of games to add. If null, it'll check the entire database for new games.</param>
        /// <param name="itemsToUpdate">List of games to update. If null, it'll check the entire database for updated games.</param>
        /// <param name="itemsToRemove">List of games to remove. If null, it'll check the entire database for removed games.</param>
        /// <returns></returns>
        Task<bool> RunLibrarySyncAsync(
            List<Game> itemsToAdd = null,
            List<Game> itemsToUpdate = null,
            List<Game> itemsToRemove = null
        );
        /// <summary>
        /// Syncs a list of games with the server
        /// </summary>
        /// <param name="itemsToSync">List o games to sync. New and updated games will be derived from this list.</param>
        /// <returns></returns>
        bool RunGameListSync(List<Game> itemsToSync);
        /// <summary>
        /// Syncronizes library media files for games with the web server.
        /// </summary>
        /// <param name="games">List of games to check for media files changes. If null, media files for all games will be checked.</param>
        /// <returns></returns>
        bool RunMediaFilesSync(IEnumerable<Game> games = null);
        /// <summary>
        /// Syncronizes library media files for games with the web server. Asynchronous version of <see cref="RunMediaFilesSync"/>. No global progress is shown.
        /// </summary>
        /// <param name="games">List of games to check for media files changes. If null, media files for all games will be checked.</param>
        /// <returns></returns>
        Task<bool> RunMediaFilesSyncAsync(IEnumerable<Game> games = null);
    }
}

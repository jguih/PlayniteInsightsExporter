using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryExporter.Application
{
    public interface ILibraryExporterPort
    {
        bool ExportLibrary(
            List<Game> itemsToAdd = null,
            List<Game> itemsToUpdate = null,
            List<Game> itemsToRemove = null
        );
        Task<bool> ExportLibraryAsync(
            List<Game> itemsToAdd = null,
            List<Game> itemsToUpdate = null,
            List<Game> itemsToRemove = null
        );
        bool ExportLibrary(List<Game> itemsToSync);
        Task<bool> ExportMediaFiles(IEnumerable<Game> games = null, CancellationToken cancellationToken = default);
    }
}

using ExporterCommon.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExporterLibraryExporter.Application
{
    public interface ILibraryExporterServicePort
    {
        bool ExportLibrary(
            List<AppGame> itemsToAdd = null,
            List<AppGame> itemsToUpdate = null,
            List<AppGame> itemsToRemove = null
        );
        Task<bool> ExportLibraryAsync(
            List<AppGame> itemsToAdd = null,
            List<AppGame> itemsToUpdate = null,
            List<AppGame> itemsToRemove = null
        );
        bool ExportLibrary(List<AppGame> itemsToSync);
        Task<ExportMediaFilesResult> ExportMediaFiles(IEnumerable<AppGame> games = null, CancellationToken cancellationToken = default);
    }
}

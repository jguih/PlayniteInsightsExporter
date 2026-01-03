using ExporterCommon.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExporterLibraryExporter.Application
{
    public class LibraryExportDiff
    {
        public IReadOnlyList<AppGame> ToAdd { get; } = new List<AppGame>();
        public IReadOnlyList<AppGame> ToUpdate { get; } = new List<AppGame>();
        public IReadOnlyList<AppGame> ToRemove { get; } = new List<AppGame>();

        public bool HasChanges =>
            ToAdd.Count > 0 || ToUpdate.Count > 0 || ToRemove.Count > 0;

        public LibraryExportDiff(
            IReadOnlyList<AppGame> added, 
            IReadOnlyList<AppGame> updated, 
            IReadOnlyList<AppGame> deleted
        )
        {
            ToAdd = added ?? new List<AppGame>(); ;
            ToUpdate = updated ?? new List<AppGame>();
            ToRemove = deleted ?? new List<AppGame>();
        }
    }

    public interface ILibraryExporterServicePort
    {
        Task<bool> ExportLibraryAsync(
            LibraryExportDiff diff, 
            CancellationToken cancellationToken = default
        );
        Task<ExportMediaFilesResult> ExportMediaFilesAsync(
            IReadOnlyList<AppGame> games = null, 
            CancellationToken cancellationToken = default,
            ExportMediaFilesContext context = null
        );
        Task<LibraryExportDiff> ComputeLibraryDiff();
    }
}

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
        public IReadOnlyList<AppGame> Added { get; } = new List<AppGame>();
        public IReadOnlyList<AppGame> Updated { get; } = new List<AppGame>();
        public IReadOnlyList<AppGame> Deleted { get; } = new List<AppGame>();

        public bool HasChanges =>
            Added.Count > 0 || Updated.Count > 0 || Deleted.Count > 0;

        public LibraryExportDiff(
            IReadOnlyList<AppGame> added, 
            IReadOnlyList<AppGame> updated, 
            IReadOnlyList<AppGame> deleted
        )
        {
            Added = added ?? new List<AppGame>(); ;
            Updated = updated ?? new List<AppGame>();
            Deleted = deleted ?? new List<AppGame>();
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
            CancellationToken cancellationToken = default
        );
        Task<LibraryExportDiff> ComputeLibraryDiff();
    }
}

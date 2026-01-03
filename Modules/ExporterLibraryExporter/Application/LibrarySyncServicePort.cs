using ExporterCommon.Application;
using ExporterCommon.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExporterLibraryExporter.Application
{
    public class GameLibrarySyncDiff
    {
        public IReadOnlyList<SyncGameCommandItem> ToAdd { get; } = new List<SyncGameCommandItem>();
        public IReadOnlyList<SyncGameCommandItem> ToUpdate { get; } = new List<SyncGameCommandItem>();
        public IReadOnlyList<string> ToRemove { get; } = new List<string>();

        public bool HasChanges =>
            ToAdd.Count > 0 || ToUpdate.Count > 0 || ToRemove.Count > 0;

        public int Total =>
            ToAdd.Count + ToUpdate.Count + ToRemove.Count;

        public GameLibrarySyncDiff(
            IReadOnlyList<SyncGameCommandItem> added, 
            IReadOnlyList<SyncGameCommandItem> updated, 
            IReadOnlyList<string> deleted
        )
        {
            ToAdd = added ?? new List<SyncGameCommandItem>(); ;
            ToUpdate = updated ?? new List<SyncGameCommandItem>();
            ToRemove = deleted ?? new List<string>();
        }
    }

    public interface ILibrarySyncServicePort
    {
        Task SyncGamesLibraryAsync(
            GameLibrarySyncDiff diff, 
            CancellationToken cancellationToken = default
        );
        Task<SyncMediaFilesResult> SyncMediaFilesAsync(
            IReadOnlyList<AppGame> games = null, 
            CancellationToken cancellationToken = default,
            SyncMediaFilesContext context = null
        );
        Task<GameLibrarySyncDiff> ComputeGameLibraryDiff();
    }
}

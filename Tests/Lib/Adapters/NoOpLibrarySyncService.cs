using ExporterCommon.Domain;
using ExporterLibraryExporter.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Lib.Adapters;
internal class NoOpLibrarySyncService : ILibrarySyncServicePort
{
    public Task<GameLibrarySyncDiff> ComputeGameLibraryDiff()
    {
        return Task.FromResult(new GameLibrarySyncDiff([], [], []));
    }

    public Task SyncGamesLibraryAsync(GameLibrarySyncDiff diff, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<SyncMediaFilesResult> SyncMediaFilesAsync(
        IReadOnlyList<AppGame> games = null, 
        CancellationToken cancellationToken = default, 
        SyncMediaFilesContext context = null
    )
    {
        return Task.FromResult(new SyncMediaFilesResult(
                            reasonCode: SyncMediaFilesResultReasonCode.Success,
                            reason: "Success",
                            operationSuccess: true,
                            skipped: 0,
                            success: 0,
                            failed: 0
                        ));
    }
}

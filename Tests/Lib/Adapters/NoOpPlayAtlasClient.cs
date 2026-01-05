using ExporterCommon.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Lib.Adapters;

internal class NoOpPlayAtlasClient : IPlayAtlasHttpClientPort
{
    public Task CloseGameSessionAsync(CloseGameSessionCommand command)
    {
        return Task.CompletedTask;
    }

    public Task<PlayAtlasLibraryManifest> GetManifestAsync()
    {
        return Task.FromResult(new PlayAtlasLibraryManifest(0, [], []));
    }

    public Task OpenGameSessionAsync(OpenGameSessionCommand command)
    {
        return Task.CompletedTask;
    }

    public Task StaleGameSessionAsync(StaleGameSessionCommand command)
    {
        return Task.CompletedTask;
    }

    public Task SyncGamesAsync(SyncGamesCommand command)
    {
        return Task.CompletedTask;
    }

    public Task SyncMediaFilesAsync(SyncMediaFilesCommand command)
    {
        return Task.CompletedTask;
    }
}

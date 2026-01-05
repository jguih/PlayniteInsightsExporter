using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Application
{
    public interface IPlayAtlasHttpClientPort
    {
        Task<PlayAtlasLibraryManifest> GetManifestAsync();
        Task SyncGamesAsync(SyncGamesCommand command);
        Task SyncMediaFilesAsync(SyncMediaFilesCommand command);
        Task OpenGameSessionAsync(OpenGameSessionCommand command);
        Task CloseGameSessionAsync(CloseGameSessionCommand command);
        Task StaleGameSessionAsync(StaleGameSessionCommand command);
    }
}

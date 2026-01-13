using ExporterCommon.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExporterCommon.Application
{
    public interface IPlayAtlasHttpClientPort
    {
        Task<PlayAtlasLibraryManifest> GetManifestAsync();
        Task SyncGamesAsync(SyncGamesCommand command, CancellationToken cancellationToken = default);
        Task SyncMediaFilesAsync(SyncMediaFilesCommand command, CancellationToken cancellationToken = default);
        Task OpenGameSessionAsync(OpenGameSessionCommand command, CancellationToken cancellationToken = default);
        Task CloseGameSessionAsync(CloseGameSessionCommand command, CancellationToken cancellationToken = default);
        Task StaleGameSessionAsync(StaleGameSessionCommand command, CancellationToken cancellationToken = default);
        Task<ExtensionRegistration> RegisterExtensionAsync(RegisterExtensionCommand command, CancellationToken cancellationToken = default);
    }
}

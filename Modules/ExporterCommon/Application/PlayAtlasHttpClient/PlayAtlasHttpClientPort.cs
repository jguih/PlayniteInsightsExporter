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
        Task SyncGamesAsync(SyncGamesCommand request);
        Task SyncMediaFilesAsync(SyncMediaFilesCommand request);
    }
}

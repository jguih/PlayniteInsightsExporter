using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Application
{
    public interface IPlayAtlasHttpClientPort
    {
        Task<GetPlayAtlasManifestResponse> GetManifestAsync();
        Task<PlayAtlastHttpClientResponse> SendGamesAsync(SendGamesRequest request);
        Task<PlayAtlastHttpClientResponse> SendMediaFilesAsync(SendMediaFilesRequest request);
    }
}

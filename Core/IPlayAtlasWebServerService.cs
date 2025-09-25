using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public interface IPlayAtlasWebServerService
    {
        Task<HttpResponseMessage> Post(string endpoint, HttpContent content, string contentHash);
        Task<HttpResponseMessage> PostJson(string endpoint, object data);
        Task<PlayniteLibraryManifest> GetManifestAsync();
        Task<HttpResponseMessage> CheckHealth();
        Task<HttpResponseMessage> RegisterAsync(RegisterExtensionCommand command);
    }
}

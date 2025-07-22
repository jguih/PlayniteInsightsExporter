using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public interface IPlayniteInsightsWebServerService
    {
        Task<bool> Post(string endpoint, HttpContent content);
        Task<bool> PostJson(string endpoint, object data);
        Task<PlayniteLibraryManifest> GetManifestAsync();
    }
}

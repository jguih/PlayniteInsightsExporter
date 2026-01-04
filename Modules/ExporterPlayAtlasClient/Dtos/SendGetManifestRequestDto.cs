using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Dtos
{
    [JsonObject]
    public class SendGetManifestRequestDto : BaseRequestDto
    {
        public static readonly string ENDPOINT = "/api/extension/sync/manifest";
    }
}

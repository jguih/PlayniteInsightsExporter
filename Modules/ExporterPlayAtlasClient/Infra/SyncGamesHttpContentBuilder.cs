using ExporterCommon.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Infra
{
    public class SyncGamesHttpContentBuilder : IHttpContentBuilderPort<SyncGamesRequest>
    {
        public HttpContent Build(SyncGamesRequest request)
        {
            var jsonContent = new StringContent(
                    content: request.ToJsonString(),
                    encoding: Encoding.UTF8,
                    mediaType: "application/json"
                );
            return jsonContent;
        }
    }
}

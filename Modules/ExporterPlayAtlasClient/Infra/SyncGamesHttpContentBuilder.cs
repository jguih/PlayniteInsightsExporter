using ExporterCommon.Application;
using ExporterCommon.Dtos;
using ExporterPlayAtlasClient.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Infra
{
    public interface ISyncGamesHttpContentBuilderPort : IHttpContentBuilderPort<SyncGamesRequestDto> { }

    public class SyncGamesHttpContentBuilder : ISyncGamesHttpContentBuilderPort
    {
        public HttpContent Build(SyncGamesRequestDto request)
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

using ExporterPlayAtlasClient.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Infra
{
    public abstract class BaseHttpContentBuilder<TRequest>
    : IHttpContentBuilderPort<TRequest>
    where TRequest : BaseRequestDto
    {
        public virtual HttpContent Build(TRequest request)
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

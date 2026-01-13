using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Infra
{
    public interface IHttpRequestSignerPort
    {
        HttpRequestMessage CreateSignedRequest(
            HttpMethod method,
            string endpoint,
            HttpContent content = null,
            string bodyHash = null,
            bool includeRegistrationId = true
        );
    }
}

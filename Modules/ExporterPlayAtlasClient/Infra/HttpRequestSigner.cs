using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterPlayAtlasClient.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Infra
{
    public class HttpRequestSigner : IHttpRequestSignerPort
    {
        private readonly IExporterPluginContextPort pluginContext;
        private readonly ISystemConfigPort systemConfig;
        private readonly ISignatureServicePort signatureService;
        private readonly IExtensionRegistrationFileHandlerPort extensionRegistrationFileHandler;

        public HttpRequestSigner(
            IExporterPluginContextPort pluginContext, 
            ISystemConfigPort systemConfig, 
            ISignatureServicePort signatureService,
            IExtensionRegistrationFileHandlerPort extensionRegistrationFileHandler
        )
        {
            this.pluginContext = pluginContext;
            this.systemConfig = systemConfig;
            this.signatureService = signatureService;
            this.extensionRegistrationFileHandler = extensionRegistrationFileHandler;
        }

        private string ParseUrl(string endpoint = "")
        {
            var webAppUrl = pluginContext.GetWebServerURL();
            if (string.IsNullOrEmpty(endpoint))
            {
                return webAppUrl;
            }
            return $"{webAppUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        }

        public HttpRequestMessage CreateSignedRequest(
            HttpMethod method, 
            string endpoint, 
            HttpContent content = null, 
            string bodyHash = null,
            bool includeRegistrationId = true
        )
        {
            string serverUrl = pluginContext.GetWebServerURL();
            string requestUrl = ParseUrl(endpoint);
            string extensionId = pluginContext.GetExtensionId();

            byte[] canonicalBytes = signatureService.BuildRequestCanonicalString(
                method: method,
                endpoint: endpoint,
                bodyHash: bodyHash
            );

            string signatureBase64 = signatureService.Sign(canonicalBytes);

            var request = new HttpRequestMessage(method, requestUrl);

            if (content != null)
            {
                request.Content = content;
            }

            request.Headers.Add("Origin", serverUrl);
            request.Headers.Add("Referer", serverUrl);
            request.Headers.Add("X-Signature", signatureBase64);
            request.Headers.Add("X-ExtensionId", extensionId);

            if (includeRegistrationId == true)
            {
                string registrationId = extensionRegistrationFileHandler.GetRegistrationId();
                request.Headers.Add("X-RegistrationId", registrationId);
            }

            if (bodyHash != null)
            {
                request.Headers.Add("X-ContentHash", bodyHash);
            }

            return request;
        }
    }
}

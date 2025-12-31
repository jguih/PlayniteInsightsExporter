using ExporterCommon.Application;
using ExporterCommon.Infra;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Application
{
    public class PlayAtlasHttpClient : IPlayAtlasHttpClientPort
    {
        private readonly IAppLoggerPort appLogger;
        private readonly IExporterPluginContextPort pluginContext;
        private readonly ISystemConfigPort systemConfig;
        private readonly ISignatureServicePort signatureService;
        private readonly HttpClient httpClient;

        public PlayAtlasHttpClient(
            IAppLoggerPort appLogger,
            IExporterPluginContextPort pluginContext,
            ISystemConfigPort systemConfig,
            ISignatureServicePort signatureService
        )
        {
            this.appLogger = appLogger;
            this.pluginContext = pluginContext;
            this.systemConfig = systemConfig;
            this.signatureService = signatureService;
            httpClient = new HttpClient();
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

        public async Task<PlayAtlasLibraryManifest> GetManifestAsync()
        {
            try
            {
                string endpoint = SendGetManifestRequest.ENDPOINT;
                string serverUrl = pluginContext.GetWebServerURL();
                string requestUrl = ParseUrl(endpoint);
                string registrationId = systemConfig.GetExtensionRegistrationId();
                string extensionId = pluginContext.GetExtensionId();
                byte[] canonicalBytes = signatureService.BuildRequestCanonicalString(
                        method: HttpMethod.Get,
                        endpoint: SendGetManifestRequest.ENDPOINT,
                        bodyHash: null
                    );
                string signatureBase64 = signatureService.Sign(canonicalBytes);

                using (var request = new HttpRequestMessage(HttpMethod.Get, requestUrl))
                {
                    request.Headers.Add("Origin", serverUrl);
                    request.Headers.Add("Referer", serverUrl);
                    request.Headers.Add("X-Signature", signatureBase64);
                    request.Headers.Add("X-ExtensionId", extensionId);
                    request.Headers.Add("X-RegistrationId", registrationId);

                    var response = await httpClient.SendAsync(request);

                    response.EnsureSuccessStatusCode();

                    var body = await response.Content.ReadAsStringAsync();
                    var manifest = JsonConvert.DeserializeObject<PlayAtlasLibraryManifest>(body);

                    return manifest;
                }
            } catch (Exception ex)
            {
                appLogger.Error("PlayAtlas manifest request failed", ex);
                throw ex;
            }
        }

        public Task SendGamesAsync(SendGamesRequest request)
        {
            throw new NotImplementedException();
        }

        public Task SendMediaFilesAsync(SendMediaFilesRequest request)
        {
            throw new NotImplementedException();
        }
    }
}

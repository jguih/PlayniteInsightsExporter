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
        private readonly IHashServicePort hashService;
        private readonly HttpClient httpClient;

        public PlayAtlasHttpClient(
            IAppLoggerPort appLogger,
            IExporterPluginContextPort pluginContext,
            ISystemConfigPort systemConfig,
            ISignatureServicePort signatureService,
            IHashServicePort hashService
        )
        {
            this.appLogger = appLogger;
            this.pluginContext = pluginContext;
            this.systemConfig = systemConfig;
            this.signatureService = signatureService;
            this.hashService = hashService;
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

        private HttpRequestMessage CreateSignedRequest(
            HttpMethod method,
            string endpoint,
            HttpContent content = null,
            string bodyHash = null
        )
        {
            string serverUrl = pluginContext.GetWebServerURL();
            string requestUrl = ParseUrl(endpoint);
            string registrationId = systemConfig.GetExtensionRegistrationId();
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
            request.Headers.Add("X-RegistrationId", registrationId);

            if (bodyHash != null)
            {
                request.Headers.Add("X-ContentHash", bodyHash);
            }

            return request;
        }


        public async Task<PlayAtlasLibraryManifest> GetManifestAsync()
        {
            try
            {
                string endpoint = SendGetManifestRequest.ENDPOINT;
                using (var request = CreateSignedRequest(HttpMethod.Get, endpoint))
                using (var response = await httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();

                    var body = await response.Content.ReadAsStringAsync();
                    var manifest = JsonConvert.DeserializeObject<PlayAtlasLibraryManifest>(body);

                    return manifest;
                }
            }
            catch (Exception ex)
            {
                appLogger.Error("PlayAtlas manifest request failed", ex);
                throw;
            }
        }

        public async Task SyncGamesAsync(SyncGamesRequest request)
        {
            try
            {
                string endpoint = SyncGamesRequest.ENDPOINT;
                string requestBody = request.ToJsonString();
                string contentHash = hashService.ComputeSHA256HashFromString(requestBody);

                using (
                    var jsonContent = new StringContent(
                        requestBody,
                        Encoding.UTF8,
                        "application/json"
                    )
                )
                using (
                    var signedRequest = CreateSignedRequest(
                        HttpMethod.Post,
                        endpoint,
                        jsonContent,
                        contentHash
                    )
                )
                using (var response = await httpClient.SendAsync(signedRequest))
                {
                    response.EnsureSuccessStatusCode();
                }

            }
            catch (Exception ex)
            {
                appLogger.Error("PlayAtlas request to sync games failed", ex);
                throw;
            }
        }

        public Task SendMediaFilesAsync(SyncMediaFilesRequest request)
        {
            throw new NotImplementedException();
        }
    }
}

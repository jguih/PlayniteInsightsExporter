using Core;
using Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Infra
{
    public class PlayAtlasWebServerService : IPlayAtlasWebServerService
    {
        private readonly IPlayAtlasExporterContext plugin;
        private readonly IAppLogger logger;
        private readonly SignatureService signatureService;

        public PlayAtlasWebServerService(
            IPlayAtlasExporterContext plugin,
            IAppLogger logger,
            SignatureService signatureService
        ) {
            this.logger = logger;
            this.plugin = plugin;
            this.signatureService = signatureService;
        }

        private string GetWebAppURL(string endpoint = "")
        {
            var webAppUrl = plugin.GetWebServerURL();
            if (string.IsNullOrEmpty(webAppUrl))
            {
                throw new InvalidOperationException("Playnite Insights Web Server URL must not be empty.");
            }
            if (string.IsNullOrEmpty(endpoint))
            {
                return webAppUrl;
            }
            return $"{webAppUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        }

        public async Task<HttpResponseMessage> Post(string endpoint, HttpContent content)
        {
            var contentBytes = await content.ReadAsByteArrayAsync();
            string signatureBase64 = signatureService.Sign(contentBytes);
            string timestamp = DateTime.UtcNow.ToString("o");
            string extensionId = plugin.GetExtensionId();

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, GetWebAppURL(endpoint)))
            {
                request.Content = content;
                request.Headers.Add("Origin", GetWebAppURL());
                request.Headers.Add("Referer", GetWebAppURL());
                request.Headers.Add("X-Signature", signatureBase64);
                request.Headers.Add("X-Timestamp", timestamp);
                request.Headers.Add("X-ExtensionId", extensionId);
                var response = await client.SendAsync(request);
                return response;
            }
        }

        public async Task<HttpResponseMessage> Get(string endpoint)
        {
            string timestamp = DateTime.UtcNow.ToString("o");
            string extensionId = plugin.GetExtensionId();
            var canonicalString = $"GET|{endpoint}|{extensionId}|{timestamp}";
            var contentBytes = Encoding.UTF8.GetBytes(canonicalString);
            string signatureBase64 = signatureService.Sign(contentBytes);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, GetWebAppURL(endpoint)))
            {
                request.Headers.Add("Origin", GetWebAppURL());
                request.Headers.Add("Referer", GetWebAppURL());
                request.Headers.Add("X-Signature", signatureBase64);
                request.Headers.Add("X-Timestamp", timestamp);
                request.Headers.Add("X-ExtensionId", extensionId);
                var response = await client.SendAsync(request);
                return response;
            }
        }


        public async Task<HttpResponseMessage> PostJson(string endpoint, object data)
        {
            using (var jsonContent = new StringContent(
                JsonConvert.SerializeObject(data),
                Encoding.UTF8,
                "application/json")
            )
            {
                return await Post(endpoint, jsonContent);
            }
        }

        public async Task<PlayniteLibraryManifest> GetManifestAsync()
        {
            var response = await Get(WebAppEndpoints.SyncManifest);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            var manifest = JsonConvert.DeserializeObject<PlayniteLibraryManifest>(responseBody);
            return manifest;
        }

        public async Task<HttpResponseMessage> CheckHealth()
        {
            return await Get(WebAppEndpoints.HealthCheck);
        }
    }
}

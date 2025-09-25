using Core;
using Core.ExtensionRegistration;
using Core.Models;
using Core.Models.Error;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        private readonly IHashService hashService;
        private readonly Func<string> getRegistrationId;

        public PlayAtlasWebServerService(
            IPlayAtlasExporterContext plugin,
            IAppLogger logger,
            SignatureService signatureService,
            IHashService hashService,
            Func<string> getRegistrationId
        )
        {
            this.logger = logger;
            this.plugin = plugin;
            this.signatureService = signatureService;
            this.hashService = hashService;
            this.getRegistrationId = getRegistrationId;
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

        private string GetCanonicalString(
            string method,
            string endpoint,
            string timestamp,
            string bodyHash = null
        )
        {
            var extensionId = plugin.GetExtensionId();
            if (bodyHash != null)
            {
                return $"{method}|{endpoint}|{extensionId}|{timestamp}|{bodyHash}";
            }
            return $"{method}|{endpoint}|{extensionId}|{timestamp}";
        }

        public async Task<HttpResponseMessage> Post(
            string endpoint,
            HttpContent content,
            string contentHash
        )
        {
            HttpResponseMessage response = null;
            try
            {
                string registrationId = getRegistrationId.Invoke();
                string timestamp = DateTime.UtcNow.ToString("o");
                string canonicalString = GetCanonicalString(
                    method: HttpMethod.Post.ToString(),
                    endpoint: endpoint,
                    timestamp: timestamp,
                    bodyHash: contentHash);
                var canonicalBytes = Encoding.UTF8.GetBytes(canonicalString);
                string signatureBase64 = signatureService.Sign(canonicalBytes);
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
                    request.Headers.Add("X-ContentHash", contentHash);
                    request.Headers.Add("X-RegistrationId", registrationId);
                    response = await client.SendAsync(request);
                    return response;
                }
            }
            catch (InvalidOperationException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new ExtensionHttpException(
                    HttpMethod.Post,
                    endpoint,
                    response?.StatusCode ?? 0,
                    null,
                    ex);
            }
        }

        public async Task<HttpResponseMessage> Get(string endpoint)
        {
            HttpResponseMessage response = null;
            try
            {
                string registrationId = getRegistrationId.Invoke();
                string timestamp = DateTime.UtcNow.ToString("o");
                string extensionId = plugin.GetExtensionId();
                var canonicalString = GetCanonicalString(
                    method: HttpMethod.Get.ToString(),
                    endpoint: endpoint,
                    timestamp: timestamp);
                var canonicalBytes = Encoding.UTF8.GetBytes(canonicalString);
                string signatureBase64 = signatureService.Sign(canonicalBytes);

                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, GetWebAppURL(endpoint)))
                {
                    request.Headers.Add("Origin", GetWebAppURL());
                    request.Headers.Add("Referer", GetWebAppURL());
                    request.Headers.Add("X-Signature", signatureBase64);
                    request.Headers.Add("X-Timestamp", timestamp);
                    request.Headers.Add("X-ExtensionId", extensionId);
                    request.Headers.Add("X-RegistrationId", registrationId);
                    response = await client.SendAsync(request);
                    return response;
                }
            }
            catch (InvalidOperationException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new ExtensionHttpException(
                    HttpMethod.Get,
                    endpoint,
                    response?.StatusCode ?? 0,
                    null,
                    ex);
            }
        }

        public async Task<HttpResponseMessage> PostJson(string endpoint, object data)
        {
            var dataString = JsonConvert.SerializeObject(data);
            using (var jsonContent = new StringContent(
                dataString,
                Encoding.UTF8,
                "application/json")
            )
            {
                var contentHash = hashService.ComputeSHA256HashString(dataString);
                return await Post(endpoint, jsonContent, contentHash);
            }
        }

        public async Task<PlayniteLibraryManifest> GetManifestAsync()
        {
            try
            {
                var response = await Get(WebAppEndpoints.SyncManifest);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var manifest = JsonConvert.DeserializeObject<PlayniteLibraryManifest>(responseBody);
                return manifest;
            }
            catch (InvalidOperationException ex)
            {
                throw ex;
            }
            catch (ExtensionHttpException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new ExtensionException("Failed to get manifest from PlayAtlas Web Server.", ex);
            }
        }

        public async Task<HttpResponseMessage> CheckHealth()
        {
            return await Get(WebAppEndpoints.HealthCheck);
        }

        public async Task<HttpResponseMessage> RegisterAsync(RegisterExtensionCommand command)
        {
            var dataString = JsonConvert.SerializeObject(command);
            using (var content = new StringContent(
                dataString,
                Encoding.UTF8,
                "application/json")
            )
            {
                var contentHash = hashService.ComputeSHA256HashString(dataString);
                HttpResponseMessage response = null;
                string endpoint = WebAppEndpoints.Register;
                try
                {
                    string timestamp = DateTime.UtcNow.ToString("o");
                    string canonicalString = GetCanonicalString(
                        method: HttpMethod.Post.ToString(),
                        endpoint: endpoint,
                        timestamp: timestamp,
                        bodyHash: contentHash);
                    var canonicalBytes = Encoding.UTF8.GetBytes(canonicalString);
                    string signatureBase64 = signatureService.Sign(canonicalBytes);
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
                        request.Headers.Add("X-ContentHash", contentHash);
                        response = await client.SendAsync(request);
                        return response;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    throw new ExtensionHttpException(
                        HttpMethod.Post,
                        endpoint,
                        response?.StatusCode ?? 0,
                        null,
                        ex);
                }
            }
        }
    }
}

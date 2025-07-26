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
    public class PlayniteInsightsWebServerService : IPlayniteInsightsWebServerService
    {
        private readonly IPlayniteInsightsExporterContext PluginCtx;
        private readonly IAppLogger Logger;

        public PlayniteInsightsWebServerService(
            IPlayniteInsightsExporterContext PluginCtx,
            IAppLogger Logger)
        {
            this.Logger = Logger;
            this.PluginCtx = PluginCtx;
        }

        private string GetWebAppURL(string endpoint = "")
        {
            var webAppUrl = PluginCtx.CtxGetWebServerURL();
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

        public async Task<bool> Post(string endpoint, HttpContent content)
        {
            try
            {
                // Create request
                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Post, GetWebAppURL(endpoint)))
                {
                    request.Content = content;
                    request.Headers.Add("Origin", GetWebAppURL());
                    request.Headers.Add("Referer", GetWebAppURL());
                    var response = await client.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();
                    response.EnsureSuccessStatusCode();
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"POST request to {GetWebAppURL(endpoint)} failed");
                return false;
            }
        }

        public async Task<bool> PostJson(string endpoint, object data)
        {
            try
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
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to serialize data for POST request to {GetWebAppURL(endpoint)}");
                return false;
            }
        }

        public async Task<PlayniteLibraryManifest> GetManifestAsync()
        {
            try
            {
                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, GetWebAppURL(WebAppEndpoints.SyncManifest)))
                {
                    request.Headers.Add("Origin", GetWebAppURL());
                    request.Headers.Add("Referer", GetWebAppURL());
                    var response = await client.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var manifest = JsonConvert.DeserializeObject<PlayniteLibraryManifest>(responseBody);
                    response.EnsureSuccessStatusCode();
                    return manifest;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to get manifest file");
                return null;
            }
        }
    }
}

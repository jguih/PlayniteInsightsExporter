using Newtonsoft.Json;
using Playnite.SDK;
using PlayniteInsightsExporter.Lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib
{
    public class PlayniteInsightsWebServerService
    {
        private readonly PlayniteInsightsExporter Plugin;
        private readonly PlayniteInsightsExporterSettings Settings;
        private readonly ILogger Logger;

        public PlayniteInsightsWebServerService(
            PlayniteInsightsExporter Plugin, 
            PlayniteInsightsExporterSettings Settings,
            ILogger Logger) 
        {
            this.Plugin = Plugin;
            this.Settings = Settings;
            this.Logger = Logger;
        }

        private string GetWebAppURL(string endpoint = "")
        {
            string locServerURLNotSet = ResourceProvider.GetString("LOCServerURLNotSet");
            if (string.IsNullOrEmpty(Settings.WebAppURL))
            {
                throw new InvalidOperationException(locServerURLNotSet);
            }
            if (string.IsNullOrEmpty(endpoint))
            {
                return Settings.WebAppURL;
            }
            return $"{Settings.WebAppURL.TrimEnd('/')}/{endpoint.TrimStart('/')}";
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

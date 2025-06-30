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
        private IPlayniteAPI PlayniteApi { get; }
        private PlayniteInsightsExporterSettings Settings { get; }

        public PlayniteInsightsWebServerService(PlayniteInsightsExporter Plugin) 
        {
            this.Settings = Plugin.GetUserSettings();
            this.PlayniteApi = Plugin.PlayniteApi;
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

        public async Task<ValidationResult> Post(string endpoint, HttpContent content, string loadingText)
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
                    // var responseBody = await response.Content.ReadAsStringAsync();
                    response.EnsureSuccessStatusCode();
                    return new ValidationResult(
                            IsValid: true,
                            Message: $"POST request to {GetWebAppURL(endpoint)} succeeded",
                            HttpCode: 200
                        );
                }
            }
            catch (Exception e)
            {
                return new ValidationResult(
                        IsValid: false,
                        Message: $"POST request to {GetWebAppURL(endpoint)} failed",
                        HttpCode: 500
                    );
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
                return null;
            }
        }
    }
}

using Playnite.SDK;
using PlayniteInsightsExporter.Lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib
{
    class PlayniteInsightsWebServerService
    {
        private IPlayniteAPI PlayniteApi { get; }
        private PlayniteInsightsExporterSettings Settings { get; }

        public PlayniteInsightsWebServerService(
            PlayniteInsightsExporterSettings Settings,
            IPlayniteAPI PlayniteApi) 
        {
            this.Settings = Settings;
            this.PlayniteApi = PlayniteApi;
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

        public string Post(string endpoint, HttpContent content, string loadingText)
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
                    var result = PlayniteApi.Dialogs.ActivateGlobalProgress(
                        async (args) =>
                        {
                            var response = await client.SendAsync(request);
                            // var responseBody = await response.Content.ReadAsStringAsync();
                            response.EnsureSuccessStatusCode();
                        },
                        new GlobalProgressOptions(loadingText));
                    if (result.Error != null)
                    {
                        throw result.Error;
                    }
                    return "OK";
                }
            }
            catch (Exception e)
            {
                if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                {
                    return e.InnerException.Message;
                }
                return e.Message;
            }
        }
    }
}

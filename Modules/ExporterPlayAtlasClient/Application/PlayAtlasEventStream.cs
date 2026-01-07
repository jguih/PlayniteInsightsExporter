using ExporterCommon.Application;
using ExporterCommon.Application.PlayAtlasHttpClient;
using ExporterCommon.Infra;
using ExporterPlayAtlasClient.Infra;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Application
{
    public class PlayAtlasEventStream : IPlayAtlasEventStreamPort
    {
        private readonly IHttpRequestSignerPort requestSigner;
        private readonly IAppLoggerPort appLogger;
        private readonly HttpClient httpClient;

        public PlayAtlasEventStream(
            IHttpRequestSignerPort requestSigner,
            IAppLoggerPort appLogger,
            HttpClient httpClient
        )
        {
            this.requestSigner = requestSigner;
            this.appLogger = appLogger;
            this.httpClient = httpClient;
        }

        private HttpRequestMessage CreateSseRequest(string lastEventId = null)
        {
            var endpoint = "/api/extension/event";

            var request = requestSigner.CreateSignedRequest(HttpMethod.Get, endpoint);
            request.Headers.Accept.ParseAdd("text/event-stream");
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };

            if (!string.IsNullOrEmpty(lastEventId))
            {
                request.Headers.Add("Last-Event-ID", lastEventId);
            }

            return request;
        }

        private void DispatchEvent(string eventType, string json, string eventId)
        {
            appLogger.Info($"Received SSE event {eventType} ({eventId})");

            switch (eventType)
            {
                case "take-screenshot":
                    // var cmd = JsonConvert.DeserializeObject<TakeScreenshotCommand>(json);
                    // trigger Playnite action
                    break;

                case "update-playstate":
                    // var update = JsonConvert.DeserializeObject<UpdatePlaystateCommand>(json);
                    break;

                default:
                    appLogger.Warn($"Unknown SSE event: {eventType}");
                    break;
            }
        }

        private async Task ReadEventStream(
            StreamReader reader,
            Action<string> onEventId,
            CancellationToken cancellationToken
        )
        {
            string line;
            string eventType = null;
            string eventId = null;
            var dataBuilder = new StringBuilder();

            while (!cancellationToken.IsCancellationRequested &&
                   (line = await reader.ReadLineAsync()) != null)
            {
                if (line.Length == 0 && dataBuilder.Length > 0)
                {
                    if (string.IsNullOrEmpty(eventType))
                    {
                        eventType = "message";
                    }

                    try
                    {
                        DispatchEvent(eventType, dataBuilder.ToString(), eventId);
                    }
                    finally
                    {
                        dataBuilder.Clear();
                    }

                    eventType = null;
                    eventId = null;
                    continue;
                }

                if (line.StartsWith(":"))
                {
                    continue; // keep-alive
                }
                else if (line.StartsWith("id:"))
                {
                    eventId = line.Substring(3).Trim();
                    onEventId(eventId);
                }
                else if (line.StartsWith("event:"))
                {
                    eventType = line.Substring(6).Trim();
                }
                else if (line.StartsWith("data:"))
                {
                    dataBuilder.Append(line.Substring(5).Trim());
                }
            }
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            string lastEventId = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var request = CreateSseRequest(lastEventId))
                    using (var response = await httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken))
                    {
                        response.EnsureSuccessStatusCode();

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var reader = new StreamReader(stream))
                        {
                            await ReadEventStream(reader, id => lastEventId = id, cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    appLogger.Info("SSE connection lost, retrying...");
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }
    }
}

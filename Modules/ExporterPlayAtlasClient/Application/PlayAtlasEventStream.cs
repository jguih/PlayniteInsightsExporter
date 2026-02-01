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
        private readonly IReadOnlyDictionary<string, ISseEventHandlerPort> handlers;
        private readonly HttpClient httpClient;
        private int retryCounter = 0;

        public PlayAtlasEventStream(
            IHttpRequestSignerPort requestSigner,
            IAppLoggerPort appLogger,
            IReadOnlyDictionary<string, ISseEventHandlerPort> handlers,
            HttpClient httpClient
        )
        {
            this.requestSigner = requestSigner;
            this.appLogger = appLogger;
            this.handlers = handlers;
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
            appLogger.Info($"Received SSE from PlayAtlas server {eventType} ({eventId})");

            if (!handlers.TryGetValue(eventType, out var handler))
            {
                appLogger.Warn($"Ignored SSE {eventType} due to unavailable handler that matches the event type");
                return;
            }

            handler.Handle(json, eventId);
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
                if (line.Length == 0)
                {
                    if (dataBuilder.Length > 0 || eventType != null)
                    {
                        if (string.IsNullOrEmpty(eventType))
                            eventType = "message";

                        try
                        {
                            DispatchEvent(eventType, dataBuilder.ToString(), eventId);
                        }
                        catch (Exception ex)
                        {
                            appLogger.Error(
                                $"Failed to process SSE from PlayAtlas server {eventType} ({eventId})",
                                ex
                            );
                        }
                        finally
                        {
                            dataBuilder.Clear();
                        }

                        eventType = null;
                        eventId = null;
                    }

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
                    if (dataBuilder.Length > 0)
                        dataBuilder.Append('\n');

                    dataBuilder.Append(line.Substring(5));
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

                        retryCounter = 0;

                        appLogger.Info("Successfully stablished event stream connection with PlayAtlas server");

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
                    retryCounter++;
                    if (retryCounter == 1 || retryCounter % 15 == 0)
                        appLogger.Info($"Lost SSE connection with PlayAtlas server, retrying... (attempt {retryCounter})");
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
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

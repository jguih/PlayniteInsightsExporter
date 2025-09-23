using Core;
using Core.Models;
using Core.Screencapture;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Infra
{
    public class HttpServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly IPlayAtlasExporterContext Context;
        private readonly IAppLogger Logger;
        private readonly IScreenCaptureService ScreenCaptureService;
        private readonly List<Action> OnStartListeners = new List<Action>();
        private readonly List<Action> OnStopListeners = new List<Action>();

        public HttpServer(
            IPlayAtlasExporterContext context,
            IAppLogger logger,
            IScreenCaptureService screenCaptureService
        )
        {
            Context = context;
            Logger = logger;
            ScreenCaptureService = screenCaptureService;
        }

        private void HandleRequest(HttpListenerContext context)
        {
            string body;
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                body = reader.ReadToEnd();
            }

            // Get signature from header
            string signatureBase64 = context.Request.Headers["X-Signature"];
            if (string.IsNullOrEmpty(signatureBase64))
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            byte[] signature;
            byte[] payloadBytes;
            try
            {
                signature = Convert.FromBase64String(signatureBase64);
                payloadBytes = Encoding.UTF8.GetBytes(body);
            } catch (Exception)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            byte[] publicKeyDer = File.ReadAllBytes(Context.GetWebServerPublicKeyPath());

            if (!SignatureVerifier.Verify(payloadBytes, signature, publicKeyDer))
            {
                context.Response.StatusCode = 403;
                context.Response.Close();
                return;
            }

            // Signature verified

            HttpServerPayload payload = null;
            try
            {
                payload = JsonConvert.DeserializeObject<HttpServerPayload>(body);
            }
            catch (Exception)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            try
            {
                HandlePayload(payload, context);
                return;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error handling payload");
                context.Response.StatusCode = 500;
                context.Response.Close();
                return;
            }
        }

        private void HandlePayload(HttpServerPayload payload, HttpListenerContext context)
        {
            switch (payload.Action)
            {
                case "screenshot":
                    {
                        ScreenCaptureService.TakeScreenshot();
                        context.Response.StatusCode = 200;
                        context.Response.Close();
                        break;
                    }
                default:
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                        break;
                    }
            }
        }

        public static string GetPrefix(string port)
        {
            return $"http://+:{port}/";
        }

        public void Stop()
        {
            _listener.Stop();

            foreach (var action in OnStopListeners)
            {
                action();
            }
        }

        public void Start()
        {
            var port = Context.GetHttpServerPort();
            var prefix = GetPrefix(port);

            _listener.Prefixes.Add(prefix);
            _listener.Start();

            Task.Run(async () =>
            {
                while (_listener.IsListening)
                {
                    var context = await _listener.GetContextAsync();
                    try
                    {
                        HandleRequest(context);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error handling request");
                        context.Response.StatusCode = 500;
                        context.Response.Close();
                    }
                }
            });

            foreach (var action in OnStartListeners)
            {
                action();
            }
        }

        public void OnStart(Action action)
        {
            OnStartListeners.Add(action);
        }

        public void OnStop(Action action)
        {
            OnStopListeners.Add(action);
        }
    }
}

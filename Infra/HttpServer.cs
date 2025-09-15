using Core;
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
        private readonly IPlayniteInsightsExporterContext Context;
        private readonly List<Action> OnStartListeners = new List<Action>();
        private readonly List<Action> OnStopListeners = new List<Action>();

        public HttpServer (IPlayniteInsightsExporterContext context)
        {
            Context = context;
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
            var prefix = $"http://+:{port}/";

            _listener.Prefixes.Add(prefix);
            _listener.Start();

            Task.Run(async () =>
            {
                while (_listener.IsListening)
                {
                    var context = await _listener.GetContextAsync();
                    HandleRequest(context);
                }
            });

            foreach (var action in OnStartListeners)
            {
                action();
            }
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

            byte[] signature = Convert.FromBase64String(signatureBase64);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(body);
            byte[] publicKeyDer = File.ReadAllBytes(Context.GetWebServerPublicKeyPath());

            if (!SignatureVerifier.Verify(payloadBytes, signature, publicKeyDer))
            {
                context.Response.StatusCode = 403;
                context.Response.Close();
                return;
            }

            // Signature verified

            byte[] responseBytes = Encoding.UTF8.GetBytes("OK");
            context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
            context.Response.Close();
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

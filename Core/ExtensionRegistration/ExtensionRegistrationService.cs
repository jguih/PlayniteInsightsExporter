using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Core.ExtensionRegistration
{
    public class ExtensionRegistrationService : IExtensionRegistrationService
    {
        private IKeyManager keyManager { get; set; }
        private IPlayAtlasWebServerService webServer { get; set; }
        private IPlayAtlasExporterContext context { get; set; }

        public ExtensionRegistrationService(
            IKeyManager keyManager,
            IPlayAtlasWebServerService webServer,
            IPlayAtlasExporterContext context
        ) {
            this.keyManager = keyManager;
            this.webServer = webServer;
            this.context = context;
        }

        public async Task RegisterAsync()
        {
            string hostname = Environment.MachineName;
            string os = Environment.OSVersion.ToString();
            string id = context.GetExtensionId();
            string version = context.GetExtensionVersion();
            string key = keyManager.GetPublicKeyAsPem();

            RegisterExtensionCommand command = new RegisterExtensionCommand()
            {
                Hostname = hostname,
                Os = os,
                ExtensionId = id,
                ExtensionVersion = version,
                PublicKey = key
            };

            await webServer.PostJson(WebAppEndpoints.Register, command);
        }
    }
}

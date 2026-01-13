using ExporterCommon.Application;
using ExporterPlayAtlasClient.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Commands.RegisterExtension
{
    public interface IRegisterExtensionCommandHandlerPort : IAsyncCommandHandlerPort { }

    public class RegisterExtensionCommandHandler : IRegisterExtensionCommandHandlerPort
    {
        private readonly IExporterPluginContextPort pluginContext;
        private readonly IKeyManagerPort keyManager;
        private readonly IPlayAtlasHttpClientPort playAtlasHttpClient;
        private readonly IExtensionRegistrationFileHandlerPort extensionRegistrationFileHandler;

        public RegisterExtensionCommandHandler(
            IExporterPluginContextPort pluginContext, 
            IKeyManagerPort keyManager, 
            IPlayAtlasHttpClientPort playAtlasHttpClient,
            IExtensionRegistrationFileHandlerPort extensionRegistrationFileHandler
        )
        {
            this.pluginContext = pluginContext;
            this.keyManager = keyManager;
            this.playAtlasHttpClient = playAtlasHttpClient;
            this.extensionRegistrationFileHandler = extensionRegistrationFileHandler;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            string hostname = Environment.MachineName;
            string os = Environment.OSVersion.ToString();
            string id = pluginContext.GetExtensionId();
            string version = pluginContext.GetExtensionVersion();
            string key = keyManager.GetPublicKeyAsPem();

            RegisterExtensionCommand command = new RegisterExtensionCommand()
            {
                ExtensionId = id,
                ExtensionVersion = version,
                Hostname = hostname,
                Os = os,
                PublicKeyPem = key
            };

            var registration = await playAtlasHttpClient.RegisterExtensionAsync(command, cancellationToken);

            extensionRegistrationFileHandler.WriteRegistration(registration);
        }
    }
}

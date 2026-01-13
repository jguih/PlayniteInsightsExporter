using ExporterCommon.Application;
using ExporterCommon.Application.PlayAtlasHttpClient;
using ExporterPlayAtlasClient.Application;
using ExporterPlayAtlasClient.Commands.RegisterExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application
{
    public class ExporterPlayAtlasClientCommandsApi
    {
        public readonly IRegisterExtensionCommandHandlerPort RegisterExtensionCommandHandler;

        public ExporterPlayAtlasClientCommandsApi(
            IRegisterExtensionCommandHandlerPort registerExtensionCommandHandlerPort
        )
        {
            RegisterExtensionCommandHandler = registerExtensionCommandHandlerPort;
        }
    }

    public class ExporterPlayAtlasClientApi
    {
        public readonly IPlayAtlasHttpClientPort PlayAtlasHttpClient;
        public readonly IPlayAtlasEventStreamPort EventStream;
        public readonly ExporterPlayAtlasClientCommandsApi Command;
        public readonly IExtensionRegistrationFileHandlerPort ExtensionRegistrationHandler;

        public ExporterPlayAtlasClientApi(
            IPlayAtlasHttpClientPort playAtlasHttpClient,
            IPlayAtlasEventStreamPort eventStream,
            IRegisterExtensionCommandHandlerPort registerExtensionCommandHandler,
            IExtensionRegistrationFileHandlerPort extensionRegistrationHandler
        )
        {
            PlayAtlasHttpClient = playAtlasHttpClient;
            EventStream = eventStream;
            Command = new ExporterPlayAtlasClientCommandsApi(
                    registerExtensionCommandHandler
                );
            ExtensionRegistrationHandler = extensionRegistrationHandler;
        }
    }
}

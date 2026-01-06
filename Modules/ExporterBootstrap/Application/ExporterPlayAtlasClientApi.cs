using ExporterCommon.Application;
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
        public readonly IRegisterExtensionCommandHandlerPort RegisterExtensionCommandHandlerPort;

        public ExporterPlayAtlasClientCommandsApi(
            IRegisterExtensionCommandHandlerPort registerExtensionCommandHandlerPort
        )
        {
            RegisterExtensionCommandHandlerPort = registerExtensionCommandHandlerPort;
        }
    }

    public class ExporterPlayAtlasClientApi
    {
        public readonly IPlayAtlasHttpClientPort PlayAtlasHttpClient;
        public readonly ExporterPlayAtlasClientCommandsApi Command;

        public ExporterPlayAtlasClientApi(
            IPlayAtlasHttpClientPort playAtlasHttpClient,
            IRegisterExtensionCommandHandlerPort registerExtensionCommandHandler
        )
        {
            PlayAtlasHttpClient = playAtlasHttpClient;
            Command = new ExporterPlayAtlasClientCommandsApi(
                    registerExtensionCommandHandler
                );
        }
    }
}

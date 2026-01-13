using ExporterBootstrap.Application.Module;
using ExporterCommon.Application;
using ExporterCommon.Application.PlayAtlasHttpClient;
using ExporterPlayAtlasClient.Application;
using ExporterPlayAtlasClient.Commands.RegisterExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Lib.Adapters;

namespace Tests.Lib.Modules;
public sealed class TestPlayAtlasClientModule : IPlayAtlasClientModulePort
{
    public IPlayAtlasHttpClientPort Client { get; }
    public IRegisterExtensionCommandHandlerPort RegisterExtensionCommandHandler { get; }
    public IPlayAtlasEventStreamPort EventStream { get; }
    public IExtensionRegistrationFileHandlerPort ExtensionRegistrationFileHandler { get; }

    public TestPlayAtlasClientModule(
        IPlayAtlasHttpClientPort? client = null    
    )
    {
        Client = client ?? new NoOpPlayAtlasClient();
        RegisterExtensionCommandHandler = new NoOpRegisterExtensionCommandHandler();
        EventStream = new NoOpPlayAtlasEventStream();
        ExtensionRegistrationFileHandler = new NoOpExtensionRegistrationFileHandler();
    }
}

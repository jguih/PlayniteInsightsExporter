using ExporterCommon.Application;
using ExporterCommon.Application.PlayAtlasHttpClient;
using ExporterPlayAtlasClient.Commands.RegisterExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application.Module
{
    public interface IPlayAtlasClientModulePort
    {
        IPlayAtlasHttpClientPort Client { get; }
        IPlayAtlasEventStreamPort EventStream { get; }
        IRegisterExtensionCommandHandlerPort RegisterExtensionCommandHandler { get; }
    }
}

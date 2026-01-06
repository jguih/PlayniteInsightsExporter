using ExporterCommon.Application;
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
        IRegisterExtensionCommandHandlerPort RegisterExtensionCommandHandler { get; }
    }
}

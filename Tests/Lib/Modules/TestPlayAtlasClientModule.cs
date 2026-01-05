using ExporterBootstrap.Application.Module;
using ExporterCommon.Application;
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

    public TestPlayAtlasClientModule(
        IPlayAtlasHttpClientPort? client = null    
    )
    {
        Client = client ?? new NoOpPlayAtlasClient();
    }
}

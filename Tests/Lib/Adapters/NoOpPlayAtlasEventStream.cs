using ExporterCommon.Application.PlayAtlasHttpClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Lib.Adapters;
internal class NoOpPlayAtlasEventStream : IPlayAtlasEventStreamPort
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

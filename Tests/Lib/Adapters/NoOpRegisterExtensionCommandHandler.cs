using ExporterPlayAtlasClient.Commands.RegisterExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Lib.Adapters;
internal class NoOpRegisterExtensionCommandHandler : IRegisterExtensionCommandHandlerPort
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

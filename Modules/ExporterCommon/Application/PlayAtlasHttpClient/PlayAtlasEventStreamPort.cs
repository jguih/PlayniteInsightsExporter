using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExporterCommon.Application.PlayAtlasHttpClient
{
    public interface IPlayAtlasEventStreamPort
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}

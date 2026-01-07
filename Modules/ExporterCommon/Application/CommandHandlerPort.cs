using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExporterCommon.Application
{
    public interface ICommandHandlerPort
    {
        void Execute();
    }

    public interface IAsyncCommandHandlerPort
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }

    public interface ICommandHandlerPort<TResult>
    {
        TResult Execute();
    }

    public interface ICommandHandlerPort<TResult, TArgs>
    {
        TResult Execute(TArgs args);
    }
}

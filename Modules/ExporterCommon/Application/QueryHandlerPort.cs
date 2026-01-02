using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Application
{
    public interface IQueryHandlerPort<TResult>
    {
        TResult Execute();
    }

    public interface IQueryHandlerPort<TResult, TArgs>
    {
        TResult Execute(TArgs args);
    }
}

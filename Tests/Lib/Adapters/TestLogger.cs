using ExporterCommon.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Lib.Adapters;
internal class TestLogger : IAppLoggerPort
{
    public void Debug(string message)
    {
        return;
    }

    public void Error(string message, Exception ex = null)
    {
        return;
    }

    public void Info(string message)
    {
        return;
    }

    public void Warn(string message)
    {
        return;
    }
}

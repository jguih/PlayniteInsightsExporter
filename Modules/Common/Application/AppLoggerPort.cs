using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Application
{
    public interface IAppLoggerPort
    {
        void Info(string message);
        void Warn(string message);
        void Error(Exception ex, string message);
        void Debug(string message);
    }
}

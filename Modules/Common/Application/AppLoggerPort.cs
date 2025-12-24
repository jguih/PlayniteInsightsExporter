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
        void Error(string message, Exception ex = null);
        void Debug(string message);
    }
}

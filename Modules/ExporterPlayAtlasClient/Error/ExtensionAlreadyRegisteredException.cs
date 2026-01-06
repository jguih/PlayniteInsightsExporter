using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameSessions.Error
{
    public class ExtensionAlreadyRegisteredException : Exception
    {
        public ExtensionAlreadyRegisteredException()
        : base("Extension is already registered.")
        {
        }

        public ExtensionAlreadyRegisteredException(string message)
        : base(message)
        {
        }

        public ExtensionAlreadyRegisteredException(string message, Exception innerException)
        : base(message, innerException)
        {
        }
    }
}

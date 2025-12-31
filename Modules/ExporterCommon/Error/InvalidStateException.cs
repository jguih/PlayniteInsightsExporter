using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Error
{
    public class InvalidStateException : Exception
    {
        public InvalidStateException()
        : base("The object is in an invalid state.")
        {
        }

        public InvalidStateException(string message)
        : base(message)
        {
        }

        public InvalidStateException(string message, Exception innerException)
        : base(message, innerException)
        {
        }
    }
}

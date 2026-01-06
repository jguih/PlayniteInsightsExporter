using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameSessions.Error
{
    public class GameSessionNotFoundException : Exception
    {
        public GameSessionNotFoundException()
        : base("Game session not found.")
        {
        }

        public GameSessionNotFoundException(string message)
        : base(message)
        {
        }

        public GameSessionNotFoundException(string message, Exception innerException)
        : base(message, innerException)
        {
        }
    }
}

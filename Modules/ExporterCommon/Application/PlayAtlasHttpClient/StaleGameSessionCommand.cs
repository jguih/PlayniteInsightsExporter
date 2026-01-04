using ExporterCommon.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Application
{
    public class StaleGameSessionCommand
    {
        public GameSession GameSession { get; set; }

        public StaleGameSessionCommand() { }
    }
}

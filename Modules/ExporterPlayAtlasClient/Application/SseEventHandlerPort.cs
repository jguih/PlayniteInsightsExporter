using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Application
{
    public interface ISseEventHandlerPort
    {
        string EventType { get; }
        void Handle(string json, string eventId);
    }

}

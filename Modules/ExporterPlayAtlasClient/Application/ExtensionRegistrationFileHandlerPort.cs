using ExporterCommon.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Application
{
    public interface IExtensionRegistrationFileHandlerPort
    {
        string GetRegistrationId();
        void WriteRegistration(ExtensionRegistration registration);
    }
}

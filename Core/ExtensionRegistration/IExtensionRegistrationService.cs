using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.ExtensionRegistration
{
    public interface IExtensionRegistrationService
    {
        Task RegisterAsync();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.ExtensionRegistration
{
    public interface IExtensionRegistrationService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when a new registration request is made, false if the extension is already registered</returns>
        Task<bool> RegisterAsync();
        string GetRegistrationId();
    }
}

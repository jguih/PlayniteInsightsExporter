using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public interface IPlayniteProgressService
    {
        /// <summary>
        /// Activates Playnite's global progress dialog.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancelable"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        bool ActivateGlobalProgress(
            string message,
            bool cancelable,
            Func<GlobalProgressActionArgs, Task<bool>> action
        );
    }
}

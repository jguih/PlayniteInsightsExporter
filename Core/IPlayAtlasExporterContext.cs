using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public interface IPlayAtlasExporterContext
    {
        string GetExtensionDataFolderPath();
        string GetWebServerURL();
        /// <summary>
        /// Gets the ShareX executable path
        /// </summary>
        /// <exception cref="InvalidOperationException">When ShareX executable path is null or empty</exception>
        /// <returns>ShareX executable path</returns>
        string GetShareXExePath();
        string GetWebServerPublicKeyPath();
        string GetHttpServerPort();
        string GetExtensionVersion();
        string GetExtensionId();
        string GetSecurityDirectoryPath();
    }
}

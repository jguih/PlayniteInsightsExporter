using ExporterCommon.Application;
using ExporterCommon.Infra;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace ExporterSystem.Infra
{

    public class SystemConfig : ISystemConfigPort
    {
        private readonly IExporterPluginContextPort pluginContext;
        private readonly IFileSystemServicePort fileSystemService;

        public string LibraryFilesDirPath { get; }
        public string SecurityDirPath { get; }
        public string ExtensionRegistrationId { get; } = null;

        public SystemConfig(
            IExporterPluginContextPort pluginContext,
            IFileSystemServicePort fileSystemService
        ) 
        {
            this.fileSystemService = fileSystemService;
            this.pluginContext = pluginContext;

            string configDir = pluginContext.GetConfigurationDirPath();
            string dataDir = pluginContext.GetExtensionDataDirPath();
            LibraryFilesDirPath = fileSystemService.PathCombine(configDir, "library", "files");
            SecurityDirPath = fileSystemService.PathCombine(dataDir, "security");
            ExtensionRegistrationId = GetRegistrationIdFromFile();
        }

        public string GetRegistrationIdFromFile()
        {
            var registrationIdPath = fileSystemService.PathCombine(SecurityDirPath, "registrationId.txt");
            if (!fileSystemService.FileExists(registrationIdPath))
            {
                return null;
            }
            var jsonString = fileSystemService.FileReadAllText(registrationIdPath);
            var registration = JsonConvert.DeserializeObject<ExtensionRegistration>(jsonString);
            return registration.RegistrationId;
        }
    }
}

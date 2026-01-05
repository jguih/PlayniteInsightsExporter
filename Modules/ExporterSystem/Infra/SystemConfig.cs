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
        public string SessionsDirPath { get; }
        public string ExtensionRegistrationId { get; set; } = null;

        public SystemConfig(
            IFileSystemServicePort fileSystemService,
            string configDirPath,
            string dataDirPath
        ) 
        {
            if (string.IsNullOrEmpty(configDirPath))
                throw new ArgumentNullException(nameof(configDirPath));
            if (string.IsNullOrEmpty(dataDirPath))
                throw new ArgumentNullException(nameof(dataDirPath));

            this.fileSystemService = fileSystemService;

            if (!fileSystemService.DirectoryExists(configDirPath))
                throw new DirectoryNotFoundException(nameof(configDirPath));
            if (!fileSystemService.DirectoryExists(dataDirPath))
                throw new DirectoryNotFoundException(nameof(dataDirPath));

            LibraryFilesDirPath = fileSystemService.PathCombine(configDirPath, "library", "files");
            SecurityDirPath = fileSystemService.PathCombine(dataDirPath, "security");
            SessionsDirPath = fileSystemService.PathCombine(dataDirPath, "sessions");
        }

        public void LoadRegistrationId()
        {
            var registrationIdPath = fileSystemService.PathCombine(SecurityDirPath, "registrationId.txt");

            if (!fileSystemService.FileExists(registrationIdPath))
            {
                throw new FileNotFoundException(registrationIdPath);
            }

            var jsonString = fileSystemService.FileReadAllText(registrationIdPath);
            var registration = JsonConvert.DeserializeObject<ExtensionRegistration>(jsonString);

            if (registration == null)
            {
                throw new InvalidDataException($"Failed to parse extension registration from file '{registrationIdPath}'");
            }

            ExtensionRegistrationId = registration.RegistrationId;
        }
    }
}

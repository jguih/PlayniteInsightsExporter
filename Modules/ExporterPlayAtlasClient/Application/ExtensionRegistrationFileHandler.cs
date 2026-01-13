using ExporterCommon.Common;
using ExporterCommon.Infra;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Application
{
    public class ExtensionRegistrationFileHandler : IExtensionRegistrationFileHandlerPort
    {
        private readonly ISystemConfigPort systemConfig;
        private readonly IFileSystemServicePort fileSystemService;
        private string RegistrationId { get; set; } = null;

        public ExtensionRegistrationFileHandler(
            ISystemConfigPort systemConfig,
            IFileSystemServicePort fileSystemService
        )
        {
            this.systemConfig = systemConfig;
            this.fileSystemService = fileSystemService;
        }

        public string GetRegistrationId()
        {
            if (RegistrationId != null)
            {
                return RegistrationId;
            }

            var path = systemConfig.RegistrationIdFilePath;

            if (!fileSystemService.FileExists(path))
            {
                throw new FileNotFoundException(path);
            }

            var jsonString = fileSystemService.FileReadAllText(path);
            var registration = JsonConvert.DeserializeObject<ExtensionRegistration>(jsonString) ?? throw new InvalidDataException($"Failed to parse extension registration from file '{path}'");
            RegistrationId = registration.RegistrationId;
            return RegistrationId;
        }

        public void WriteRegistration(ExtensionRegistration registration)
        {
            var jsonString = registration.ToJsonString();
            fileSystemService.FileWriteAllText(systemConfig.RegistrationIdFilePath, jsonString);
        }
    }
}

using ExporterCommon.Application;
using ExporterCommon.Common;
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
        public string LibraryFilesDirPath { get; }
        public string SecurityDirPath { get; }
        public string SessionsDirPath { get; }
        public string RegistrationIdFilePath { get; }

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

            LibraryFilesDirPath = fileSystemService.PathCombine(configDirPath, "library", "files");
            SecurityDirPath = fileSystemService.PathCombine(dataDirPath, "security");
            SessionsDirPath = fileSystemService.PathCombine(dataDirPath, "sessions");
            RegistrationIdFilePath = fileSystemService.PathCombine(SecurityDirPath, "registrationId.txt");
        }
    }
}

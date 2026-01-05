using ExporterCommon.Application;
using ExporterCommon.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterSystem.Infra
{
    public sealed class InfraEnvironmentInitializer
    {
        private readonly IFileSystemServicePort fileSystemService;
        private readonly IKeyManagerPort keyManager;
        private readonly ISystemConfigPort systemConfig;
        private readonly IAppLoggerPort appLogger;

        public InfraEnvironmentInitializer(
            IFileSystemServicePort fileSystemService,
            IKeyManagerPort keyManager,
            ISystemConfigPort systemConfig,
            IAppLoggerPort appLogger
        )
        {
            this.fileSystemService = fileSystemService;
            this.keyManager = keyManager;
            this.systemConfig = systemConfig;
            this.appLogger = appLogger;
        }

        public void Initialize()
        {
            var extensionDirs = new List<string>()
            {
                systemConfig.SecurityDirPath,
                systemConfig.SessionsDirPath
            };

            foreach (var dirPath in extensionDirs)
            {
                if (!fileSystemService.DirectoryExists(dirPath))
                    fileSystemService.DirectoryCreate(dirPath);
            }

            if (!keyManager.KeyExistsAndIsValid())
            {
                appLogger.Warn("Assymetric key pair is missing or invalid, trying to create a new pair...");
                keyManager.WriteAsymmetricKeyPair();
            }

            systemConfig.LoadRegistrationId();
        }
    }

}

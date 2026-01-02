using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterSystem.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application
{
    public class ExporterInfraApi
    {
        private readonly IAppLoggerPort appLogger;
        private readonly ISystemConfigPort systemConfig;

        public readonly IFileSystemServicePort FileSystemService;
        public readonly IHashServicePort HashService;
        public readonly IKeyManagerPort KeyManager;
        public readonly ISignatureServicePort SignatureService;

        public ExporterInfraApi(
            IAppLoggerPort appLogger,
            ISystemConfigPort systemConfig,
            IFileSystemServicePort fileSystemService
        )
        {
            this.appLogger = appLogger;
            this.FileSystemService = fileSystemService;
            this.systemConfig = systemConfig;

            KeyManager = new KeyManager(systemConfig, fileSystemService, appLogger);
            HashService = new HashService(fileSystemService);
            SignatureService = new SignatureService(appLogger, KeyManager, systemConfig);
        }

        public void InitInfra()
        {
            var extensionDirs = new List<string>()
            {
                systemConfig.SecurityDirPath
            };

            foreach(var dirPath in extensionDirs)
            {
                if (!FileSystemService.DirectoryExists(dirPath))
                    FileSystemService.DirectoryCreate(dirPath);
            }

            if (!KeyManager.KeyExistsAndIsValid())
            {
                appLogger.Warn("Assymetric key pair is missing or invalid, trying to create a new pair...");
                KeyManager.WriteAsymmetricKeyPair();
            }
        }
    }
}

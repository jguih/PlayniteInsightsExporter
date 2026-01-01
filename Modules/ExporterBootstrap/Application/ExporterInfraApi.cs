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
        private IAppLoggerPort appLogger;
        private ExporterConfigApi configApi;

        public readonly IFileSystemServicePort fileSystemService;
        public readonly IHashServicePort hashService;
        public readonly IKeyManagerPort keyManager;
        public readonly ISignatureServicePort signatureService;

        public ExporterInfraApi(
            ExporterConfigApi configApi,
            IAppLoggerPort appLogger,
            IFileSystemServicePort fileSystemService
        )
        {
            this.appLogger = appLogger;
            this.fileSystemService = fileSystemService;
            this.configApi = configApi;

            keyManager = new KeyManager(configApi.SystemConfig, fileSystemService, appLogger);
            hashService = new HashService(fileSystemService);
            signatureService = new SignatureService(appLogger, keyManager, configApi.SystemConfig);
        }

        public void InitInfra()
        {
            var extensionDirs = new List<string>()
            {
                configApi.SystemConfig.SecurityDirPath
            };

            foreach(var dirPath in extensionDirs)
            {
                if (!fileSystemService.DirectoryExists(dirPath))
                    fileSystemService.DirectoryCreate(dirPath);
            }

            if (!keyManager.KeyExistsAndIsValid())
            {
                appLogger.Warn("Assymetric key pair is missing or invalid, trying to create a new pair...");
                keyManager.WriteAssymetricKeyPair();
            }
        }
    }
}

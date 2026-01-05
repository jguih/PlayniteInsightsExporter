using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterSystem.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application.Module
{
    public sealed class InfraModule : IInfraModulePort
    {
        public IFileSystemServicePort FileSystem { get; }
        public IHashServicePort HashService { get; }
        public IKeyManagerPort KeyManager { get; }
        public ISignatureServicePort SignatureService { get; }
        public IPlayniteGameRepositoryPort PlayniteGameRepository { get; }
        public IEnvironmentInitializerPort EnvironmentInitializer { get; }

        public InfraModule(
            IFileSystemServicePort fileSystem,
            ISystemConfigPort systemConfig,
            IExporterPluginContextPort plugin,
            IAppLoggerPort appLogger,
            IPlayniteGameRepositoryPort playniteGameRepository
        )
        {
            FileSystem = fileSystem;
            HashService = new HashService(FileSystem);
            KeyManager = new KeyManager(systemConfig, FileSystem, appLogger);
            SignatureService = new SignatureService(
                appLogger,
                KeyManager,
                plugin
            );
            PlayniteGameRepository = playniteGameRepository;
            EnvironmentInitializer = new InfraEnvironmentInitializer(
                   fileSystemService: FileSystem,
                   keyManager: KeyManager,
                   systemConfig: systemConfig,
                   appLogger: appLogger
                );
        }
    }

}

using ExporterBootstrap.Application.Module;
using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterSystem.Infra;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Lib.Modules;

public sealed class TestInfraModule : IInfraModulePort
{
    public IFileSystemServicePort FileSystem { get; }
    public IHashServicePort HashService { get; }
    public IKeyManagerPort KeyManager { get; }
    public ISignatureServicePort SignatureService { get; }
    public IPlayniteGameRepositoryPort PlayniteGameRepository { get; }
    public IEnvironmentInitializerPort EnvironmentInitializer { get; }

    public TestInfraModule(
        IFileSystemServicePort fileSystem, 
        IHashServicePort hashService, 
        IKeyManagerPort keyManager,
        ISignatureServicePort signatureService,
        IPlayniteGameRepositoryPort playniteGameRepository, 
        IEnvironmentInitializerPort environmentInitializer
    )
    {
        FileSystem = fileSystem;
        HashService = hashService;
        KeyManager = keyManager;
        SignatureService = signatureService;
        PlayniteGameRepository = playniteGameRepository;
        EnvironmentInitializer = environmentInitializer;
    }
}

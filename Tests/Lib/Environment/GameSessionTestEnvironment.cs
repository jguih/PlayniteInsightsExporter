using ExporterBootstrap.Application;
using ExporterBootstrap.Application.Module;
using ExporterBootstrap.Application.Modules;
using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterGameSessions.Application;
using ExporterGameSessions.Infra;
using ExporterLibraryExporter.Application;
using ExporterSystem.Infra;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Lib.Adapters;
using Tests.Lib.Modules;

namespace Tests.Lib.Environment;

internal sealed class GameSessionTestEnvironment : IDisposable
{
    public ExporterApi Api { get; }
    public IGameSessionSerializerPort Serializer { get; }

    public readonly string WorkDir;

    private GameSessionTestEnvironment(
        ExporterApi api,
        IGameSessionSerializerPort serializer,
        string workDir
    )
    {
        Api = api;
        Serializer = serializer;
        WorkDir = workDir;
    }

    public static GameSessionTestEnvironment Create()
    {
        var workDir = Path.Join(Path.GetTempPath(), $"{Guid.NewGuid()}-playatlas-test");
        var configDirPath = Path.Join(workDir, "config");

        IAppLoggerPort appLogger = new TestLogger();
        IFileSystemServicePort fileSystem = new FileSystemService();
        IPlayniteGameRepositoryPort gameRepo = new NoOpPlayniteGameRepository();
        IHashServicePort hashService = new HashService(fileSystem);

        fileSystem.DirectoryCreate(configDirPath);

        ISystemConfigPort config = new SystemConfig(
            fileSystem,
            configDirPath,
            workDir
        );

        IEnvironmentInitializerPort envInit =
            new InfraEnvironmentInitializer(
                fileSystem,
                new Mock<IKeyManagerPort>().Object,
                config,
                appLogger
            );

        IInfraModulePort infra = new TestInfraModule(
            fileSystem,
            hashService,
            new Mock<IKeyManagerPort>().Object,
            new Mock<ISignatureServicePort>().Object,
            gameRepo,
            envInit
        );

        var playAtlas = new TestPlayAtlasClientModule();
        var librarySync = new TestLibrarySyncModule(
            new GameSyncItemFactory(hashService)
        );

        var playniteIntegration = new PlayniteIntegrationModule(gameRepo);

        var serializer = new GameSessionSerializer();
        var gameSessionService = new GameSessionService(
            appLogger,
            hashService,
            playAtlas.Client,
            fileSystem,
            new GameSessionConfig(),
            config,
            serializer
        );

        var gameSessionModule = new TestGameSessionModule(gameSessionService);

        var bootstrapper = new ExporterBootstraper(
            appLogger,
            infra,
            playAtlas,
            librarySync,
            playniteIntegration,
            gameSessionModule
        );

        var api = bootstrapper.BootstrapExporterApi();
        api.EnvironmentInitializer.EnsureDirectories();

        return new GameSessionTestEnvironment(api, serializer, workDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(WorkDir))
            Directory.Delete(WorkDir, recursive: true);
    }
}


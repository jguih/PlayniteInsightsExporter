using Bogus;
using ExporterCommon.Application;
using ExporterCommon.Domain;
using ExporterCommon.Infra;
using ExporterCommon.Testing;
using ExporterLibraryExporter.Application;
using ExporterSystem.Infra;
using Moq;

namespace Tests.Unit;

[Trait("Category", "Unit")]
public class LibraryExporterTests
{
    private readonly Faker faker = new();
    private readonly Mock<IAppLoggerPort> appLogger;
    private readonly Mock<IPlayAtlasHttpClientPort> playAtlasHttpClient;
    private readonly Mock<IHashServicePort> hashService;
    private readonly Mock<IFileSystemServicePort> fileSystemService;
    private readonly Mock<IExporterPluginContextPort> pluginContext;
    private readonly Mock<IPlayniteGameRepositoryPort> gameRepository;
    private readonly ISystemConfigPort systemConfig;

    private readonly ILibraryExporterServicePort libraryExporter;

    private readonly GameFactory gameFactory;

    public LibraryExporterTests()
    {
        appLogger = new Mock<IAppLoggerPort>();
        playAtlasHttpClient = new Mock<IPlayAtlasHttpClientPort>();

        hashService = new Mock<IHashServicePort>();
        hashService
            .Setup(hs => hs.ComputeHashFromFolderContents(It.IsAny<string>()))
            .Returns(faker.Random.Hash());

        fileSystemService = new Mock<IFileSystemServicePort>();
        fileSystemService
            .Setup(fs => fs.PathCombine(It.IsAny<string[]>()))
            .Returns((string[] paths) => Path.Combine(paths));

        pluginContext = new Mock<IExporterPluginContextPort>();
        pluginContext
            .Setup(x => x.GetConfigurationDirPath())
            .Returns(faker.System.DirectoryPath());
        pluginContext
            .Setup(x => x.GetExtensionDataDirPath())
            .Returns(faker.System.DirectoryPath());

        gameRepository = new Mock<IPlayniteGameRepositoryPort>();

        systemConfig = new SystemConfig(
            pluginContext.Object, 
            fileSystemService.Object
        );

        libraryExporter = new LibraryExporterService(
            appLogger.Object,
            playAtlasHttpClient.Object,
            hashService.Object,
            fileSystemService.Object,
            systemConfig,
            gameRepository.Object
        );

        gameFactory = new GameFactory();
    }

    [Fact]
    public async Task ExportMediaFiles_Should_Fail_When_ManifestRequestFails()
    {
        // Arrange
        var games = gameFactory.BuildGameList(15);
        playAtlasHttpClient
            .Setup(x => x.GetManifestAsync())
            .ThrowsAsync(new Exception("Invalid request"));
        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
        {
            return libraryExporter.ExportMediaFilesAsync(games);
        });
    }

    [Fact]
    public async Task ExportMediaFiles_Should_SkipGame_When_GameNotInManifest()
    {
        // Arrange
        var manifest = new PlayAtlasLibraryManifest(
            totalGamesInLibrary: 0,
            gamesInLibrary: [],
            mediaExistsFor: []
        );
        playAtlasHttpClient
            .Setup(x => x.GetManifestAsync())
            .Returns(Task.FromResult(manifest));
        var games = gameFactory.BuildGameList(15);
        // Act
        var result = await libraryExporter.ExportMediaFilesAsync(games);
        // Assert
        Assert.True(result.OperationSuccess);
        Assert.Equal(0, result.Success);
        Assert.Equal(15, result.Skipped);
    }

    [Fact]
    public async Task ComputeLibraryDiff_Should_Return_Add_Update_And_Remove()
    {
        // Arrange
        var gameA = gameFactory.BuildGame();
        var gameB = gameFactory.BuildGame();
        var gameC = gameFactory.BuildGame();
        var gameD = gameFactory.BuildGame();

        gameA.ContentHash = "hash-a";
        gameB.ContentHash = "hash-b";
        gameC.ContentHash = "hash-c-local";

        gameRepository
            .Setup(r => r.GetAll())
            .Returns([gameA, gameB, gameC]);

        var manifest = new PlayAtlasLibraryManifest(
            totalGamesInLibrary: 3,
            gamesInLibrary:
            [
                new(gameB.Id.ToString(), "hash-b"),          // same → no-op
                new(gameC.Id.ToString(), "hash-c-remote"),   // different → update
                new(gameD.Id.ToString(), "hash-d")           // not local → remove
            ],
            mediaExistsFor: []
        );

        playAtlasHttpClient
            .Setup(x => x.GetManifestAsync())
            .ReturnsAsync(manifest);

        // Act
        var diff = await libraryExporter.ComputeLibraryDiff();

        // Assert
        Assert.Single(diff.ToAdd);
        Assert.Equal(gameA.Id, diff.ToAdd[0].Id);

        Assert.Single(diff.ToUpdate);
        Assert.Equal(gameC.Id, diff.ToUpdate[0].Id);

        Assert.Single(diff.ToRemove);
        Assert.Equal(gameD.Id, diff.ToRemove[0].Id);
    }

}

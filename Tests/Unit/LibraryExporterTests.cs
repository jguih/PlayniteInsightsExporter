using Bogus;
using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterCommon.Testing;
using ExporterLibraryExporter.Application;
using Moq;
using ExporterSystem.Infra;

namespace Tests.Unit;

[Trait("Category", "Unit")]
public class LibraryExporterTests
{
    private readonly Faker faker = new();
    private readonly Mock<IAppLoggerPort> appLogger;
    private readonly Mock<IPlayAtlasHttpClientPort> playAtlasHttpClient;
    private readonly Mock<IHashServicePort> hashService;
    private readonly Mock<IFileSystemServicePort> fileSystemService;
    private readonly ISystemConfigPort systemConfig;

    private readonly ILibraryExporterServicePort libraryExporter;

    private readonly GameFactory gameFactory;

    public LibraryExporterTests()
    {
        appLogger = new Mock<IAppLoggerPort>();
        playAtlasHttpClient = new Mock<IPlayAtlasHttpClientPort>();
        hashService = new Mock<IHashServicePort>();
        fileSystemService = new Mock<IFileSystemServicePort>();
        systemConfig = new SystemConfig()
        {
            LibraryFilesDirPath = faker.System.DirectoryPath()
        };

        fileSystemService
            .Setup(fs => fs.PathCombine(It.IsAny<string[]>()))
            .Returns((string[] paths) => Path.Combine(paths));
        hashService
            .Setup(hs => hs.ComputeHashFromFolderContents(It.IsAny<string>()))
            .Returns(faker.Random.Hash());

        libraryExporter = new LibraryExporterService(
            appLogger.Object,
            playAtlasHttpClient.Object,
            hashService.Object,
            fileSystemService.Object,
            systemConfig
        );

        gameFactory = new GameFactory();
    }

    [Fact]
    public async Task ExportMediaFiles_Should_Fail_When_ManifestRequestFails()
    {
        // Arrange
        var games = gameFactory.BuildGameList(15);
        var manifestResponse = new GetPlayAtlasManifestResponse(
            success: false,
            reason: "Failed to fetch manifest",
            reasonCode: ReasonCode.NotFound,
            manifest: null
        );
        playAtlasHttpClient
            .Setup(x => x.GetManifestAsync())
            .Returns(Task.FromResult(manifestResponse));
        // Act
        var result = await libraryExporter.ExportMediaFiles(games);
        // Assert
        playAtlasHttpClient
            .Verify(x => x.GetManifestAsync(), Times.Once);
        Assert.False(result.OperationSuccess);
        Assert.Equal(ExportMediaFilesResultReasonCode.FailedToFetchManifest, result.ReasonCode);
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
        var manifestResponse = new GetPlayAtlasManifestResponse(
            success: true,
            reason: "Success",
            reasonCode: ReasonCode.Success,
            manifest: manifest
        );
        playAtlasHttpClient
            .Setup(x => x.GetManifestAsync())
            .Returns(Task.FromResult(manifestResponse));
        var games = gameFactory.BuildGameList(15);
        // Act
        var result = await libraryExporter.ExportMediaFiles(games);
        // Assert
        Assert.True(result.OperationSuccess);
        Assert.Equal(0, result.Success);
        Assert.Equal(15, result.Skipped);
    }
}

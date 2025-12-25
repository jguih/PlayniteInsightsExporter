using Bogus;
using Common.Application;
using Common.Infra;
using LibraryExporter.Application;
using Moq;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Lib;

namespace Tests.Unit;

/// <summary>
/// Unit tests for Library Exporter.
/// </summary>
[Trait("Category", "Unit")]
public class LibraryExporterTests
{
    private readonly Faker faker = new Faker();
    private readonly Mock<IAppLoggerPort> appLogger;
    private readonly Mock<IPlayAtlasHttpClientPort> playAtlasHttpClient;
    private readonly Mock<IHashServicePort> hashService;
    private readonly Mock<IFileSystemServicePort> fileSystemService;
    private readonly Mock<ISystemConfigPort> systemConfig;

    private readonly ILibraryExporterServicePort libraryExporter;

    private readonly GameFactory gameFactory;

    public LibraryExporterTests()
    {
        appLogger = new Mock<IAppLoggerPort>();
        playAtlasHttpClient = new Mock<IPlayAtlasHttpClientPort>();
        hashService = new Mock<IHashServicePort>();
        fileSystemService = new Mock<IFileSystemServicePort>();
        systemConfig = new Mock<ISystemConfigPort>();

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
            systemConfig.Object
        );

        gameFactory = new GameFactory();
    }

    [Fact]
    public async Task ExportMediaFiles_Should_Fail_When_ManifestRequestFails()
    {
        // Arrange
        playAtlasHttpClient
            .Setup(x => x.GetManifestAsync())
            .Returns(Task.FromResult(new GetPlayAtlasManifestResponse(false, "Failed to fetch manifest", ReasonCode.NotFound, null)));
        // Act
        var result = await libraryExporter.ExportMediaFiles([]);
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
        var manifest = new PlayAtlasLibraryManifest()
        {
            gamesInLibrary = [],
            mediaExistsFor = [],
            totalGamesInLibrary = 0
        };
        var manifestResponse = new GetPlayAtlasManifestResponse(
            success: true,
            reason: "Success",
            reasonCode: ReasonCode.Success,
            manifest: manifest
        );
        playAtlasHttpClient
            .Setup(x => x.GetManifestAsync())
            .Returns(Task.FromResult(manifestResponse));
        var games = new List<Game>()
        {
            gameFactory.BuildGame()
        };
        // Act
        var result = await libraryExporter.ExportMediaFiles(games);
    }
}

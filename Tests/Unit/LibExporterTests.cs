using Core;
using Core.Models;
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
/// Unit tests for LibExporter.
/// </summary>
[Trait("Category", "Unit")]
public class LibExporterTests
{
    private Mock<IPlayniteProgressService> ProgressServiceMock { get; }
    private Mock<IPlayniteGameRepository> GameRepository { get; }
    private Mock<IPlayniteInsightsWebServerService> WebServiceMock { get; }
    private Mock<IFileSystemService> FsMock { get; }
    private Mock<IHashService> HashServiceMock { get; }
    private LibExporter LibExporter { get; set; }
    private TestFactory TestFactory { get; } = new TestFactory();

    public LibExporterTests()
    {
        ProgressServiceMock = new Mock<IPlayniteProgressService>();
        GameRepository = new Mock<IPlayniteGameRepository>();
        WebServiceMock = new Mock<IPlayniteInsightsWebServerService>();
        HashServiceMock = new Mock<IHashService>();
        FsMock = new Mock<IFileSystemService>();

        HashServiceMock
            .Setup(x => x.HashFolderContents(It.IsAny<string>()))
            .Returns(Guid.NewGuid().ToString());
        HashServiceMock
            .Setup(x => x.GetHashFromPlayniteGame(It.IsAny<Game>()))
            .Returns(Guid.NewGuid().ToString());

        LibExporter = new LibExporter(
            ProgressService: ProgressServiceMock.Object,
            PlayniteGameRepository: GameRepository.Object,
            WebServerService: WebServiceMock.Object,
            Logger: Mock.Of<IAppLogger>(),
            HashService: HashServiceMock.Object,
            LibraryFilesDir: "/testFolder/libraryFiles",
            FileSystemService: FsMock.Object
        );
    }

    [Fact]
    public async Task OnLibSync_RequestToAddGame()
    {
        // Arrange
        var game = TestFactory.GetGame();
        var gamesToAdd = new List<Game> { game };
        var gamesToUpdate = new List<Game>();
        var gamesToRemove = new List<Game>();
        // Act
        await LibExporter.RunLibrarySyncAsync(
            itemsToAdd: gamesToAdd,
            itemsToUpdate: gamesToUpdate,
            itemsToRemove: gamesToRemove);
        // Assert
        WebServiceMock.Verify(
            x => x.PostJson(
                WebAppEndpoints.SyncGames, 
                It.Is<SyncGameListCommand>(c =>
                    c.UpdatedItems.Count == 0 &&
                    c.AddedItems.Count == 1 &&
                    c.AddedItems[0].Id == game.Id &&
                    c.RemovedItems.Count == 0
                )
            ), Times.Once);
    }
}

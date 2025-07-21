using Moq;
using Newtonsoft.Json;
using PlayniteInsightsExporter;
using PlayniteInsightsExporter.Lib;
using PlayniteInsightsExporter.Lib.Models;

namespace UnitTests;

public class GameSessionServiceTests
{
    private Mock<IAppLogger> LoggerMock { get; }
    private Mock<IPlayniteInsightsExporterContext> PluginCtxMock { get; }
    private Mock<IHashService> HashServiceMock { get; }
    private Mock<IPlayniteInsightsWebServerService> WebServiceMock { get; }
    private Mock<IFileSystemService> FileSystemMock { get; }
    private SessionTrackingService SessionsService { get; set; }

    public GameSessionServiceTests()
    {
        LoggerMock = new Mock<IAppLogger>();
        PluginCtxMock = new Mock<IPlayniteInsightsExporterContext>();
        HashServiceMock = new Mock<IHashService>();
        WebServiceMock = new Mock<IPlayniteInsightsWebServerService>();
        FileSystemMock = new Mock<IFileSystemService>();
        SessionsService = new SessionTrackingService(
            PluginCtxMock.Object,
            LoggerMock.Object,
            HashServiceMock.Object,
            WebServiceMock.Object,
            FileSystemMock.Object
        );

        var fakeSessionsFolder = "/fake/sessions";
        WebServiceMock
            .Setup(ws => ws.Post(It.IsAny<string>(), It.IsAny<HttpContent>()))
            .ReturnsAsync(false);
        PluginCtxMock
            .Setup(ctx => ctx.CtxGetExtensionDataFolderPath())
            .Returns(fakeSessionsFolder);
        FileSystemMock
            .Setup(fs => fs.DirectoryExists(fakeSessionsFolder))
            .Returns(true);
        FileSystemMock
            .Setup(fs => fs.DirectoryCreate(fakeSessionsFolder));
    }

    [Fact]
    public async void OnOpen_CreatesHash()
    {
        // Arrange
        var gameId = "game123";
        HashServiceMock
            .Setup(hs => hs.GetHashForGameSession(gameId, It.IsAny<DateTime>()))
            .Returns("hashedGameId");
        FileSystemMock
            .Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(false);
        // Act
        var result = await SessionsService.CreateSession(gameId);
        // Assert
        HashServiceMock
            .Verify(fs => fs.GetHashForGameSession(gameId, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async void OnOpen_MarkExistingInProgressSessionAsStale()
    {
        // Arrange
        var gameId = "game123";
        var sessionId = "hashedGameId";
        var fakeSession = new GameSession
        {
            GameId = gameId,
            StartTime = DateTime.UtcNow,
            SessionId = sessionId,
            Status = GameSession.STATUS_IN_PROGRESS,
        };
        FileSystemMock
            .Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(true);
        FileSystemMock
            .Setup(fs => fs.FileReadAllText(It.IsAny<string>()))
            .Returns(JsonConvert.SerializeObject(fakeSession));
        // Act
        var result = await SessionsService.CreateSession(gameId);
        // Assert
        FileSystemMock
           .Verify(fs => fs.FileWriteAllText(
                It.Is<string>(s => s.Contains("-stale.json")),
                It.Is<string>(s => s.Contains(GameSession.STATUS_STALE))
                ), Times.Once);
        FileSystemMock
            .Verify(fs => fs.FileDelete(
                It.Is<string>(s => s.Contains("-in-progress.json"))
                ), Times.Once);
    }
}
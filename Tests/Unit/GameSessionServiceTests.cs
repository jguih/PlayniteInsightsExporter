using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Moq;
using Newtonsoft.Json;
using PlayniteInsightsExporter;
using PlayniteInsightsExporter.Lib;
using PlayniteInsightsExporter.Lib.Models;

namespace Tests.Unit;

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
            .Setup(ws => ws.PostJson(It.IsAny<string>(), It.IsAny<object>()))
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
    public async void OpenSession_UsesHashAsSessionId()
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
        await SessionsService.OpenSession(gameId);
        // Assert
        HashServiceMock
            .Verify(fs => fs.GetHashForGameSession(gameId, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task OpenSession_WhenExistingInProgressSessionExists_MarksItAsStale()
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
            .Setup(fs => fs.FileExists(It.Is<string>(s => s.Contains(gameId))))
            .Returns(true);
        FileSystemMock
            .Setup(fs => fs.FileReadAllText(It.IsAny<string>()))
            .Returns(JsonConvert.SerializeObject(fakeSession));
        // Act
        await SessionsService.OpenSession(gameId);
        // Assert
        FileSystemMock
           .Verify(fs => fs.FileWriteAllText(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains(GameSession.STATUS_STALE))
                ), Times.Once);
        FileSystemMock
            .Verify(fs => fs.FileDelete(
                It.Is<string>(s => s.Contains(gameId))
                ), Times.Once);
    }

    [Fact]
    public async Task OpenSession_WhenExistingInProgressSessionExists_CreatesNewSession()
    {
        // Arrange
        var gameId = "game123";
        var fakeSession = new GameSession
        {
            GameId = gameId,
            StartTime = DateTime.UtcNow,
            SessionId = null,
            Status = null,
        };
        FileSystemMock
            .Setup(fs => fs.FileExists(It.Is<string>(s => s.Contains(gameId))))
            .Returns(true);
        FileSystemMock
            .Setup(fs => fs.FileReadAllText(It.IsAny<string>()))
            .Returns(JsonConvert.SerializeObject(fakeSession));
        WebServiceMock
            .Setup(ws => ws.PostJson(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(true);
        // Act
        var result = await SessionsService.OpenSession(gameId);
        // Assert
        Assert.True(result);
        FileSystemMock
           .Verify(fs => fs.FileWriteAllText(
                It.Is<string>(s => s.Contains(gameId)),
                It.Is<string>(s => s.Contains(GameSession.STATUS_IN_PROGRESS) && s.Contains(gameId))
                ), Times.Once);
        WebServiceMock
            .Verify(ws => ws.PostJson(
                WebAppEndpoints.OpenSession,
                It.Is<GameSession>(gs => gs.GameId == gameId && gs.Status == GameSession.STATUS_IN_PROGRESS)
            ), Times.Once);
    }

    // Created sessions should use gameId in file name
    // so the file can be found later when closing the session.
    [Fact]
    public async Task OpenSession_WhenNoExistingSession_CreatesNewSession()
    {
        // Arrange
        var gameId = "game123";
        var sessionHash = "hashedGameId";
        FileSystemMock
            .Setup(fs => fs.FileExists(It.Is<string>(s => s.Contains(gameId))))
            .Returns(false);
        HashServiceMock
            .Setup(hs => hs.GetHashForGameSession(gameId, It.IsAny<DateTime>()))
            .Returns(sessionHash);
        WebServiceMock
            .Setup(ws => ws.PostJson(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(true);
        // Act
        var result = await SessionsService.OpenSession(gameId);
        // Assert
        Assert.True(result);
        FileSystemMock
            .Verify(fs => fs.FileWriteAllText(
                It.Is<string>(s => s.Contains(gameId)),
                It.Is<string>(s => s.Contains(gameId) && s.Contains(GameSession.STATUS_IN_PROGRESS))
            ), Times.Once);
        WebServiceMock
            .Verify(ws => ws.PostJson(
                WebAppEndpoints.OpenSession,
                It.Is<GameSession>(gs => gs.GameId == gameId && gs.SessionId == sessionHash)
            ), Times.Once);
    }

    [Fact]
    public async Task CloseSession_WhenNoOpenedSessionExists_ReturnsFalse()
    {
        // Arrange
        var gameId = "game123";
        ulong duration = 2000;
        FileSystemMock
            .Setup(fs => fs.FileExists(It.Is<string>(s => s.Contains(gameId))))
            .Returns(false);
        // Act
        var result = await SessionsService.CloseSession(gameId, duration);
        // Assert
        Assert.False(result);
        WebServiceMock.Verify(ws => ws.PostJson(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }
}
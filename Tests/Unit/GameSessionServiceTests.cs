using Core;
using Core.Models;
using Moq;
using Newtonsoft.Json;

namespace Tests.Unit;

/// <summary>
/// Unit tests for GameSessionService.
/// </summary>
[Trait("Category", "Unit")]
public class GameSessionServiceTests
{
    private Mock<IAppLogger> LoggerMock { get; }
    private Mock<IPlayniteInsightsExporterContext> PluginCtxMock { get; }
    private Mock<IHashService> HashServiceMock { get; }
    private Mock<IPlayniteInsightsWebServerService> WebServiceMock { get; }
    private Mock<IFileSystemService> FileSystemMock { get; }
    private GameSessionService SessionsService { get; set; }

    public GameSessionServiceTests()
    {
        LoggerMock = new Mock<IAppLogger>();
        PluginCtxMock = new Mock<IPlayniteInsightsExporterContext>();
        HashServiceMock = new Mock<IHashService>();
        WebServiceMock = new Mock<IPlayniteInsightsWebServerService>();
        FileSystemMock = new Mock<IFileSystemService>();
        var gameSessionConfig = new GameSessionConfig
        {
            SESSIONS_DIR_PATH = "/testFolder/sessions"
        };

        FileSystemMock
            .Setup(fs => fs.PathCombine(It.IsAny<string[]>()))
            .Returns((string[] paths) => Path.Combine(paths));
        FileSystemMock
            .Setup(fs => fs.DirectoryExists(gameSessionConfig.SESSIONS_DIR_PATH))
            .Returns(true);
        HashServiceMock
            .Setup(hs => hs.GetHashForGameSession(It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns((string gameId, DateTime startTime) =>
            {
                return $"{gameId}-{startTime:yyyyMMddHHmmss}";
            });

        SessionsService = new GameSessionService(
            PluginCtxMock.Object,
            LoggerMock.Object,
            HashServiceMock.Object,
            WebServiceMock.Object,
            FileSystemMock.Object,
            gameSessionConfig
        );
    }

    // Existing in progress session should be marked as complete
    // if it was started less than or equals to 3 hour ago.
    [Theory]
    [InlineData(3)]
    [InlineData(2)]
    [InlineData(1)]
    public async Task OpenSession_WhenExistingInProgressSessionExists_CompleteSession(int hoursAgo)
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var sessionId = SessionsService.GetSessionId(gameId, now);
        var sessionFilePath = SessionsService.GetSessionFilePath(gameId);
        var startTime = now - TimeSpan.FromHours(hoursAgo);
        var fakeSession = new GameSession
        {
            GameId = gameId,
            StartTime = startTime,
            SessionId = sessionId,
            Status = GameSession.STATUS_IN_PROGRESS,
        };
        FileSystemMock
            .Setup(fs => fs.FileExists(sessionFilePath))
            .Returns(true);
        FileSystemMock
            .Setup(fs => fs.FileReadAllText(It.IsAny<string>()))
            .Returns(JsonConvert.SerializeObject(fakeSession));
        // Act
        await SessionsService.OpenSession(gameId, now);
        // Assert
        FileSystemMock
           .Verify(fs => fs.FileWriteAllText(
                It.Is<string>(s => s.Contains(sessionId)),
                It.Is<string>(s => s.Contains(GameSession.STATUS_CLOSED))
                ), Times.Once);
        FileSystemMock
            .Verify(fs => fs.FileDelete(
                sessionFilePath
                ), Times.Once);
        FileSystemMock
            .Verify(fs => fs.FileWriteAllText(
                sessionFilePath,
                It.Is<string>(s => s.Contains(GameSession.STATUS_IN_PROGRESS) && s.Contains(gameId))
            ), Times.Once);
    }

    [Theory]
    [InlineData(3.1)]
    [InlineData(4)]
    [InlineData(24)]
    public async Task OpenSession_WhenExistingInProgressSessionExists_StaleSession(double hoursAgo)
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var sessionId = SessionsService.GetSessionId(gameId, now);
        var sessionFilePath = SessionsService.GetSessionFilePath(gameId);
        var startTime = now - TimeSpan.FromHours(hoursAgo);
        var fakeSession = new GameSession
        {
            GameId = gameId,
            StartTime = startTime,
            SessionId = sessionId,
            Status = GameSession.STATUS_IN_PROGRESS,
        };
        FileSystemMock
            .Setup(fs => fs.FileExists(sessionFilePath))
            .Returns(true);
        FileSystemMock
            .Setup(fs => fs.FileReadAllText(It.IsAny<string>()))
            .Returns(JsonConvert.SerializeObject(fakeSession));
        // Act
        await SessionsService.OpenSession(gameId, now);
        // Assert
        FileSystemMock
           .Verify(fs => fs.FileWriteAllText(
                SessionsService.GetStaleSessionFilePath(sessionId),
                It.Is<string>(s => s.Contains(GameSession.STATUS_STALE))
                ), Times.Once);
        FileSystemMock
            .Verify(fs => fs.FileDelete(
                sessionFilePath
                ), Times.Once);
        FileSystemMock
            .Verify(fs => fs.FileWriteAllText(
                sessionFilePath,
                It.Is<string>(s => s.Contains(GameSession.STATUS_IN_PROGRESS) && s.Contains(gameId))
            ), Times.Once);
    }

    [Fact]
    public async Task OpenSession_WhenNoExistingSession_CreatesNewSession()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var gameId = "game123";
        var sessionFilePath = SessionsService.GetSessionFilePath(gameId);
        FileSystemMock
            .Setup(fs => fs.FileExists(sessionFilePath))
            .Returns(false);
        WebServiceMock
            .Setup(ws => ws.PostJson(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(true);
        // Act
        var result = await SessionsService.OpenSession(gameId, now);
        // Assert
        Assert.True(result);
        FileSystemMock
            .Verify(fs => fs.FileWriteAllText(
                sessionFilePath,
                It.Is<string>(s => s.Contains(gameId) && s.Contains(GameSession.STATUS_IN_PROGRESS))
            ), Times.Once);
        WebServiceMock
            .Verify(ws => ws.PostJson(
                WebAppEndpoints.OpenSession,
                It.Is<GameSession>(gs => gs.GameId == gameId)
            ), Times.Once);
    }

    [Fact]
    public async Task CloseSession_WhenNoOpenedSessionExists_ReturnsFalse()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var gameId = Guid.NewGuid().ToString();
        var sessionFilePath = SessionsService.GetSessionFilePath(gameId);
        ulong duration = 2000;
        FileSystemMock
            .Setup(fs => fs.FileExists(sessionFilePath))
            .Returns(false);
        // Act
        var result = await SessionsService.CloseSession(gameId, duration, now);
        // Assert
        Assert.False(result);
        WebServiceMock.Verify(ws => ws.PostJson(WebAppEndpoints.CloseSession, It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task OnClose_WhenInProgressSessionIsInvalid_DeleteFile()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var gameId = Guid.NewGuid().ToString();
        var sessionId = SessionsService.GetSessionId(gameId, now);
        GameSession session = new()
        {
            GameId = null,
            StartTime = now,
            SessionId = sessionId,
            Status = "Invalid Status",
        };
        var sessionFilePath = SessionsService.GetSessionFilePath(gameId);
        ulong duration = 3000;
        FileSystemMock
            .Setup(fs => fs.FileExists(sessionFilePath))
            .Returns(true);
        FileSystemMock
            .Setup(fs => fs.FileReadAllText(sessionFilePath))
            .Returns(JsonConvert.SerializeObject(session));
        var result = await SessionsService.CloseSession(gameId, duration, now);
        Assert.False(result, "CloseSession should return false for invalid session.");
        FileSystemMock.Verify(fs => fs.FileDelete(sessionFilePath), Times.Once);
        WebServiceMock
            .Verify(ws => ws.PostJson(WebAppEndpoints.CloseSession, It.IsAny<object>()), Times.Never);
    }
}
using Core;
using Core.Models;
using Moq;
using Newtonsoft.Json;

namespace Tests.Unit;

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
            IN_PROGRESS_SUFFIX = "-in-progress",
            COMPLETED_SUFFIX = "-completed",
            STALE_SUFFIX = "-stale",
            SESSION_FILE_EXTENSION = ".json",
            DELETE_FILES_OLDER_THAN_DAYS = 14,
            STALE_AFTER_HOURS = 48,
            SESSIONS_DIR_PATH = "/testFolder/sessions"
        };

        FileSystemMock
            .Setup(fs => fs.PathCombine(It.IsAny<string[]>()))
            .Returns((string[] paths) => Path.Combine(paths));
        FileSystemMock
            .Setup(fs => fs.DirectoryExists(gameSessionConfig.SESSIONS_DIR_PATH))
            .Returns(true);

        SessionsService = new GameSessionService(
            PluginCtxMock.Object,
            LoggerMock.Object,
            HashServiceMock.Object,
            WebServiceMock.Object,
            FileSystemMock.Object,
            gameSessionConfig
        );
    }

    [Fact]
    public async void OpenSession_UsesHashAsSessionId()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var gameId = "game123";
        HashServiceMock
            .Setup(hs => hs.GetHashForGameSession(gameId, It.IsAny<DateTime>()))
            .Returns("hashedGameId");
        FileSystemMock
            .Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(false);
        // Act
        await SessionsService.OpenSession(gameId, now);
        // Assert
        HashServiceMock
            .Verify(fs => fs.GetHashForGameSession(gameId, It.IsAny<DateTime>()), Times.Once);
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
        var gameId = "game123";
        var sessionId = "hashedGameId";
        var now = DateTime.UtcNow;
        var startTime = now - TimeSpan.FromHours(hoursAgo);
        var fakeSession = new GameSession
        {
            GameId = gameId,
            StartTime = startTime,
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
        await SessionsService.OpenSession(gameId, now);
        // Assert
        FileSystemMock
           .Verify(fs => fs.FileWriteAllText(
                It.Is<string>(s => s.Contains(sessionId)),
                It.Is<string>(s => s.Contains(GameSession.STATUS_COMPLETE))
                ), Times.Once);
        FileSystemMock
            .Verify(fs => fs.FileDelete(
                It.Is<string>(s => s.Contains(gameId))
                ), Times.Once);
    }

    [Theory]
    [InlineData(3.1)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(24)]
    public async Task OpenSession_WhenExistingInProgressSessionExists_MarksItAsStale(double hoursAgo)
    {
        // Arrange
        var gameId = "game123";
        var sessionId = "hashedGameId";
        var now = DateTime.UtcNow;
        var startTime = now - TimeSpan.FromHours(hoursAgo);
        var fakeSession = new GameSession
        {
            GameId = gameId,
            StartTime = startTime,
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
        await SessionsService.OpenSession(gameId, now);
        // Assert
        FileSystemMock
           .Verify(fs => fs.FileWriteAllText(
                It.Is<string>(s => s.Contains(sessionId)),
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
        var now = DateTime.UtcNow;
        var fakeSession = new GameSession
        {
            GameId = gameId,
            StartTime = now,
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
        var result = await SessionsService.OpenSession(gameId, now);
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
        var now = DateTime.UtcNow;
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
        var result = await SessionsService.OpenSession(gameId, now);
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
        var now = DateTime.UtcNow;
        var gameId = "game123";
        ulong duration = 2000;
        FileSystemMock
            .Setup(fs => fs.FileExists(It.Is<string>(s => s.Contains(gameId))))
            .Returns(false);
        // Act
        var result = await SessionsService.CloseSession(gameId, duration, now);
        // Assert
        Assert.False(result);
        WebServiceMock.Verify(ws => ws.PostJson(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }
}
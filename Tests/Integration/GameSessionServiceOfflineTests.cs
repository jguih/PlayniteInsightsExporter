using Core;
using Core.Models;
using Infra;
using Moq;
using Newtonsoft.Json;

namespace Tests.Integration;

/// <summary>
/// Integration tests for GameSessionService in offline mode (no connection with Playnite Insights Web Server).
/// </summary>
[Trait("Category", "Integration")]
public class GameSessionServiceOfflineTests : IDisposable
{
    private Mock<IAppLogger> LoggerMock { get; }
    private Mock<IPlayniteInsightsExporterContext> PluginCtxMock { get; }
    private Mock<IPlayniteInsightsWebServerService> WebServiceMock { get; }
    private GameSessionService SessionsService { get; set; }
    private string SessionsDirPath { get; set; }

    public GameSessionServiceOfflineTests()
    {
        LoggerMock = new Mock<IAppLogger>();
        PluginCtxMock = new Mock<IPlayniteInsightsExporterContext>();
        var hashService = new HashService(LoggerMock.Object);
        WebServiceMock = new Mock<IPlayniteInsightsWebServerService>();
        var fileSystem = new FileSystemService();
        SessionsDirPath = Path.GetTempPath() + $"{Guid.NewGuid()}-playnite-insights-sessions";
        var config = new GameSessionConfig
        {
            SESSIONS_DIR_PATH = SessionsDirPath
        };

        WebServiceMock
            .Setup(ws => ws.PostJson(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(false);

        SessionsService = new GameSessionService(
            PluginCtxMock.Object,
            LoggerMock.Object,
            hashService,
            WebServiceMock.Object,
            fileSystem,
            config
        );
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(SessionsDirPath))
        {
            Directory.Delete(path: SessionsDirPath, recursive: true);
        }
    }

    [Fact]
    public async Task OnOpen_CreateSessionFile()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var sessionId = SessionsService.GetSessionId(gameId, now);
        var sessionFilePath = SessionsService.GetSessionFilePath(gameId);
        // Act
        await SessionsService.OpenSession(gameId, now);
        // Assert
        Assert.True(File.Exists(sessionFilePath), "Session file should exist after opening session.");
        var sessionContent = File.ReadAllText(sessionFilePath);
        var session = JsonConvert.DeserializeObject<GameSession>(sessionContent);
        Assert.NotNull(session);
        Assert.Equal(gameId, session.GameId);
        Assert.Equal(sessionId, session.SessionId);
        Assert.Equal(GameSession.STATUS_IN_PROGRESS, session.Status);
        Assert.Equal(now, session.StartTime);
        Assert.Null(session.EndTime);
        Assert.Null(session.Duration);
    }

    [Fact]
    public async Task OnClose_WhenInProgressSessionExists_CloseSession()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var endTime = now.AddHours(2);
        var sessionId = SessionsService.GetSessionId(gameId, now);
        ulong duration = (ulong)(endTime - now).TotalSeconds;
        var sessionFilePath = SessionsService.GetClosedSessionFilePath(sessionId);
        // Act
        await SessionsService.OpenSession(gameId, now);
        await SessionsService.CloseSession(gameId, duration, endTime);
        // Assert
        Assert.True(File.Exists(sessionFilePath), "Session file should exist after closing session.");
        var sessionContent = File.ReadAllText(sessionFilePath);
        var session = JsonConvert.DeserializeObject<GameSession>(sessionContent);
        Assert.NotNull(session);
        Assert.Equal(gameId, session.GameId);
        Assert.Equal(sessionId, session.SessionId);
        Assert.Equal(GameSession.STATUS_COMPLETE, session.Status);
        Assert.Equal(now, session.StartTime);
        Assert.Equal(endTime, session.EndTime);
        Assert.Equal(duration, session.Duration);
    }

    /// <summary>
    /// Sessions should be marked as Stale if a new session is opened while an in-progress session exists, and the existing session is older than the configured stale threshold (default 3 hours).
    /// </summary>
    [Theory]
    [InlineData(3.1)]
    [InlineData(4)]
    [InlineData(24)]
    public async Task OnOpen_WhenInProgressSessionExists_StaleSession(double hoursAfter)
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var sessionId = SessionsService.GetSessionId(gameId, now.AddHours(hoursAfter));
        var inProgressFilePath = SessionsService.GetSessionFilePath(gameId);
        var staleSessionId = SessionsService.GetSessionId(gameId, now);
        var staleFilePath = SessionsService.GetStaleSessionFilePath(staleSessionId);
        // Act
        await SessionsService.OpenSession(gameId, now);
        await SessionsService.OpenSession(gameId, now.AddHours(hoursAfter));
        // Assert
        Assert.True(File.Exists(staleFilePath), "Stale session file should exist.");
        var staleSessionContent = File.ReadAllText(staleFilePath);
        var staleSession = JsonConvert.DeserializeObject<GameSession>(staleSessionContent);
        Assert.NotNull(staleSession);
        Assert.Equal(gameId, staleSession.GameId);
        Assert.Equal(now, staleSession.StartTime);
        Assert.Equal(staleSessionId, staleSession.SessionId);
        Assert.Equal(GameSession.STATUS_STALE, staleSession.Status);
        Assert.True(File.Exists(inProgressFilePath), "In-progress session file should be created.");
        var inProgressSessionContent = File.ReadAllText(inProgressFilePath);
        var inProgressSession = JsonConvert.DeserializeObject<GameSession>(inProgressSessionContent);
        Assert.NotNull(inProgressSession);
        Assert.Equal(gameId, inProgressSession.GameId);
        Assert.Equal(sessionId, inProgressSession.SessionId);
        Assert.Equal(GameSession.STATUS_IN_PROGRESS, inProgressSession.Status);
        Assert.Equal(now.AddHours(hoursAfter), inProgressSession.StartTime);
    }

    /// <summary>
    /// In progress session should be closed when a new session is opened, if the existing session is not older than or has an age equivalent to the configured stale threshold (default 3 hours).
    /// </summary>
    /// <param name="hoursAfter"></param>
    /// <returns></returns>
    [Theory]
    [InlineData(3)]
    [InlineData(2)]
    [InlineData(0.5)]
    public async Task OnOpen_WhenInProgressSessionExists_CloseSession(double hoursAfter)
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var sessionId = SessionsService.GetSessionId(gameId, now.AddHours(hoursAfter));
        var inProgressFilePath = SessionsService.GetSessionFilePath(gameId);
        var closedSessionId = SessionsService.GetSessionId(gameId, now);
        var closedFilePath = SessionsService.GetClosedSessionFilePath(closedSessionId);
        var duration = (ulong)(now.AddHours(hoursAfter) - now).TotalSeconds;
        // Act
        await SessionsService.OpenSession(gameId, now);
        await SessionsService.OpenSession(gameId, now.AddHours(hoursAfter));
        // Assert
        Assert.True(File.Exists(closedFilePath), "Closed session file should exist.");
        var closedSessionContent = File.ReadAllText(closedFilePath);
        var closedSession = JsonConvert.DeserializeObject<GameSession>(closedSessionContent);
        Assert.NotNull(closedSession);
        Assert.Equal(gameId, closedSession.GameId);
        Assert.Equal(closedSessionId, closedSession.SessionId);
        Assert.Equal(now, closedSession.StartTime);
        Assert.Equal(GameSession.STATUS_COMPLETE, closedSession.Status);
        Assert.Equal(now.AddHours(hoursAfter), closedSession.EndTime);
        Assert.Equal(duration, closedSession.Duration);
        Assert.True(File.Exists(inProgressFilePath), "In-progress session file should exist.");
        var inProgressSessionContent = File.ReadAllText(inProgressFilePath);
        var inProgressSession = JsonConvert.DeserializeObject<GameSession>(inProgressSessionContent);
        Assert.NotNull(inProgressSession);
        Assert.Equal(gameId, inProgressSession.GameId);
        Assert.Equal(sessionId, inProgressSession.SessionId);
        Assert.Equal(now.AddHours(hoursAfter), inProgressSession.StartTime);
        Assert.Equal(GameSession.STATUS_IN_PROGRESS, inProgressSession.Status);
    }

    [Theory]
    [InlineData(48.1)]
    [InlineData(49)]
    [InlineData(500)]
    public async Task OnSync_WhenInProgressSessionExists_StaleSession(double hoursOld)
    {
        // Arrange
        var now = DateTime.UtcNow;
        GameSession session = new()
        {
            GameId = Guid.NewGuid().ToString(),
            SessionId = Guid.NewGuid().ToString(),
            StartTime = DateTime.UtcNow.AddHours(-hoursOld),
            Status = GameSession.STATUS_IN_PROGRESS,
        };
        var sessionFilePath = SessionsService.GetSessionFilePath(session.GameId);
        File.WriteAllText(sessionFilePath, JsonConvert.SerializeObject(session));
        var staleFilePath = SessionsService.GetStaleSessionFilePath(session.SessionId);
        // Act
        await SessionsService.Sync(now);
        // Assert
        Assert.False(File.Exists(sessionFilePath));
        Assert.True(File.Exists(staleFilePath));
        var staleSessionContent = File.ReadAllText(staleFilePath);
        var staleSession = JsonConvert.DeserializeObject<GameSession>(staleSessionContent);
        Assert.NotNull(staleSession);
        Assert.Equal(session.GameId, staleSession.GameId);
        Assert.Equal(session.SessionId, staleSession.SessionId);
        Assert.Equal(session.StartTime, staleSession.StartTime);
        Assert.Equal(GameSession.STATUS_STALE, staleSession.Status);
    }

    [Fact]
    public async Task OnSync_WhenCloseSessionIsTooOld_DeleteFile()
    {

    }
}

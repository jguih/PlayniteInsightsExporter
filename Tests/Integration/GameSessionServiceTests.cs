using Core;
using Core.Models;
using Infra;
using Moq;
using Newtonsoft.Json;

namespace Tests.Integration;
public class GameSessionServiceTests : IDisposable
{
    private Mock<IAppLogger> LoggerMock { get; }
    private Mock<IPlayniteInsightsExporterContext> PluginCtxMock { get; }
    private HashService HashService { get; }
    private Mock<IPlayniteInsightsWebServerService> WebServiceMock { get; }
    private FileSystemService FileSystem { get; }
    private GameSessionService SessionsService { get; set; }
    private string SessionsDirPath { get; set; }
    private GameSessionConfig Config { get; set; }

    public GameSessionServiceTests()
    {
        LoggerMock = new Mock<IAppLogger>();
        PluginCtxMock = new Mock<IPlayniteInsightsExporterContext>();
        HashService = new HashService(LoggerMock.Object);
        WebServiceMock = new Mock<IPlayniteInsightsWebServerService>();
        FileSystem = new FileSystemService();
        SessionsDirPath = Path.GetTempPath() + $"{Guid.NewGuid()}-playnite-insights-sessions";
        Config = new GameSessionConfig
        {
            IN_PROGRESS_SUFFIX = "-in-progress",
            COMPLETED_SUFFIX = "-completed",
            STALE_SUFFIX = "-stale",
            SESSION_FILE_EXTENSION = ".json",
            DELETE_FILES_OLDER_THAN_DAYS = 14,
            STALE_AFTER_HOURS = 48,
            SESSIONS_DIR_PATH = SessionsDirPath
        };

        WebServiceMock
            .Setup(ws => ws.PostJson(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(false);

        SessionsService = new GameSessionService(
            PluginCtxMock.Object,
            LoggerMock.Object,
            HashService,
            WebServiceMock.Object,
            FileSystem,
            Config
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
        var sessionId = HashService.GetHashForGameSession(gameId, now);
        // Act
        await SessionsService.OpenSession(gameId, now);
        // Assert
        var sessionFilePath = Path.Combine(
            SessionsDirPath, 
            $"{gameId}{Config.IN_PROGRESS_SUFFIX}{Config.SESSION_FILE_EXTENSION}");
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
        var sessionId = HashService.GetHashForGameSession(gameId, now);
        ulong duration = (ulong)(endTime - now).TotalSeconds;
        var sessionFilePath = Path.Combine(
            SessionsDirPath, 
            $"{sessionId}{Config.COMPLETED_SUFFIX}{Config.SESSION_FILE_EXTENSION}");
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
}

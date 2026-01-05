using ExporterCommon.Application;
using ExporterCommon.Domain;
using ExporterCommon.Infra;
using ExporterGameSessions.Application;
using ExporterGameSessions.Infra;
using ExporterSystem.Infra;
using Moq;

namespace Tests.Unit;

/// <summary>
/// Unit tests for GameSessionService.
/// </summary>
[Trait("Category", "Unit")]
public class GameSessionServiceTests
{
    private readonly Mock<IAppLoggerPort> appLogger;
    private readonly Mock<IExporterPluginContextPort> pluginContext;
    private readonly Mock<IHashServicePort> hashService;
    private readonly Mock<IPlayAtlasHttpClientPort> playAtlasClient;
    private readonly Mock<IFileSystemServicePort> fileSystemService;
    private readonly ISystemConfigPort systemConfig;
    private readonly IGameSessionSerializerPort gameSessionSerializer;
    private readonly IGameSessionServicePort sessionsService;

    public GameSessionServiceTests()
    {
        var gameSessionConfig = new GameSessionConfig();
        appLogger = new Mock<IAppLoggerPort>();
        playAtlasClient = new Mock<IPlayAtlasHttpClientPort>();
        gameSessionSerializer = new GameSessionSerializer();

        pluginContext = new Mock<IExporterPluginContextPort>();
        pluginContext
            .Setup(x => x.GetConfigurationDirPath())
            .Returns("/config");
        pluginContext
            .Setup(x => x.GetExtensionDataDirPath())
            .Returns("/data");

        hashService = new Mock<IHashServicePort>();
        hashService
            .Setup(hs => hs.ComputeHashForGameSession(It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns((string gameId, DateTime startTime) =>
            {
                return $"{gameId}-{startTime:yyyyMMddHHmmss}";
            });


        fileSystemService = new Mock<IFileSystemServicePort>();
        fileSystemService
            .Setup(fs => fs.PathCombine(It.IsAny<string[]>()))
            .Returns((string[] paths) => Path.Combine(paths));

        systemConfig = new SystemConfig(pluginContext.Object, fileSystemService.Object);

        fileSystemService
            .Setup(fs => fs.DirectoryExists(systemConfig.SessionsDirPath))
            .Returns(true);

        sessionsService = new GameSessionService(
            appLogger.Object,
            hashService.Object,
            playAtlasClient.Object,
            fileSystemService.Object,
            gameSessionConfig,
            systemConfig,
            gameSessionSerializer
        );
    }

    private bool IsClosedSessionJson(string json)
    {
        var session = gameSessionSerializer.Deserialize(json);
        return session.Status == GameSessionStatus.Closed;
    }

    private bool IsInProgressSessionJson(string json)
    {
        var session = gameSessionSerializer.Deserialize(json);
        return session?.Status == GameSessionStatus.InProgress;
    }

    private bool IsStaleSessionJson(string json)
    {
        var session = gameSessionSerializer.Deserialize(json);
        return session?.Status == GameSessionStatus.Stale;
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
        var sessionId = hashService.Object.ComputeHashForGameSession(gameId, now);
        var startTime = now - TimeSpan.FromHours(hoursAgo);
        var existingSession = new GameSession(
                gameId: gameId,
                sessionId: sessionId,
                startTime: startTime
            );
        var sessionFilePath = sessionsService.GetSessionFilePath(existingSession);
        fileSystemService
            .Setup(fs => fs.FileExists(sessionFilePath))
            .Returns(true);
        fileSystemService
            .Setup(fs => fs.FileReadAllText(It.IsAny<string>()))
            .Returns(gameSessionSerializer.Serialize(existingSession));
        // Act
        await sessionsService.OpenSessionAsync(gameId, now);
        // Assert
        fileSystemService.Verify(fs => fs.FileWriteAllText(
            It.Is<string>(p => p.Contains(sessionId)),
            It.Is<string>(json => IsClosedSessionJson(json))
        ), Times.Once);
        fileSystemService
            .Verify(fs => fs.FileDelete(sessionFilePath), Times.Once);
        fileSystemService
            .Verify(fs => fs.FileWriteAllText(
                sessionFilePath,
                It.Is<string>(json => IsInProgressSessionJson(json))
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
        var sessionId = hashService.Object.ComputeHashForGameSession(gameId, now);
        var startTime = now - TimeSpan.FromHours(hoursAgo);
        var existingSession = new GameSession(
                gameId: gameId,
                sessionId: sessionId,
                startTime: startTime
            );
        var sessionFilePath = sessionsService.GetSessionFilePath(existingSession);
        fileSystemService
            .Setup(fs => fs.FileExists(sessionFilePath))
            .Returns(true);
        fileSystemService
            .Setup(fs => fs.FileReadAllText(It.IsAny<string>()))
            .Returns(gameSessionSerializer.Serialize(existingSession));
        // Act
        await sessionsService.OpenSessionAsync(gameId, now);
        // Assert
        fileSystemService.Verify(fs => fs.FileWriteAllText(
            It.Is<string>(p => p.Contains(sessionId)),
            It.Is<string>(json => IsStaleSessionJson(json))
        ), Times.Once);
        fileSystemService
            .Verify(fs => fs.FileDelete(sessionFilePath), Times.Once);
        fileSystemService
            .Verify(fs => fs.FileWriteAllText(
                sessionFilePath,
                It.Is<string>(json => IsInProgressSessionJson(json))
            ), Times.Once);
    }

    [Fact]
    public async Task OpenSession_WhenNoExistingSession_CreatesNewSession()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var gameId = "game123";
        fileSystemService
            .Setup(fs => fs.FileExists(It.Is<string>(s => s.Contains(gameId))))
            .Returns(false);
        // Act
        await sessionsService.OpenSessionAsync(gameId, now);
        // Assert
        fileSystemService
            .Verify(fs => fs.FileWriteAllText(
                It.Is<string>(s => s.Contains(gameId)),
                It.Is<string>(json => IsInProgressSessionJson(json))
            ), Times.Once);
        playAtlasClient
            .Verify(x => x.OpenGameSessionAsync(
                It.IsAny<OpenGameSessionCommand>()
            ), Times.Once);
    }

    //[Fact]
    //public async Task CloseSession_WhenNoOpenedSessionExists_ReturnsFalse()
    //{
    //    // Arrange
    //    var now = DateTime.UtcNow;
    //    var gameId = Guid.NewGuid().ToString();
    //    var sessionFilePath = sessionsService.GetSessionFilePath(gameId);
    //    ulong duration = 2000;
    //    fileSystemService
    //        .Setup(fs => fs.FileExists(sessionFilePath))
    //        .Returns(false);
    //    // Act
    //    var result = await sessionsService.CloseSession(gameId, duration, now);
    //    // Assert
    //    Assert.False(result);
    //    WebServiceMock.Verify(ws => ws.PostJson(WebAppEndpoints.CloseSession, It.Is<CloseSessionCommand>(s => s.GameId == gameId)), Times.Never);
    //}

    //[Fact]
    //public async Task OnClose_WhenInProgressSessionIsInvalid_DeleteFile()
    //{
    //    // Arrange
    //    var now = DateTime.UtcNow;
    //    var gameId = Guid.NewGuid().ToString();
    //    var sessionId = sessionsService.GetSessionId(gameId, now);
    //    GameSession session = new()
    //    {
    //        GameId = null,
    //        StartTime = now,
    //        SessionId = sessionId,
    //        Status = "Invalid Status",
    //    };
    //    var sessionFilePath = sessionsService.GetSessionFilePath(gameId);
    //    ulong duration = 3000;
    //    fileSystemService
    //        .Setup(fs => fs.FileExists(sessionFilePath))
    //        .Returns(true);
    //    fileSystemService
    //        .Setup(fs => fs.FileReadAllText(sessionFilePath))
    //        .Returns(JsonConvert.SerializeObject(session));
    //    var result = await sessionsService.CloseSession(gameId, duration, now);
    //    Assert.False(result, "CloseSession should return false for invalid session.");
    //    fileSystemService.Verify(fs => fs.FileDelete(sessionFilePath), Times.Once);
    //    WebServiceMock
    //        .Verify(ws => ws.PostJson(WebAppEndpoints.CloseSession, It.IsAny<object>()), Times.Never);
    //}
}
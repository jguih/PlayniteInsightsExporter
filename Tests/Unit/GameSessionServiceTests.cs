using ExporterCommon.Application;
using ExporterCommon.Domain;
using ExporterCommon.Infra;
using ExporterGameSessions.Application;
using ExporterGameSessions.Infra;
using ExporterSystem.Infra;
using Moq;
using Tests.Lib.Builders;

namespace Tests.Unit;

/// <summary>
/// Unit tests for GameSessionService.
/// </summary>
[Trait("Category", "Unit")]
public class GameSessionServiceTests
{
    private bool IsClosedSessionJson(string json, IGameSessionSerializerPort serializer)
    {
        var session = serializer.Deserialize(json);
        return session.Status == GameSessionStatus.Closed;
    }

    private bool IsInProgressSessionJson(string json, IGameSessionSerializerPort serializer)
    {
        var session = serializer.Deserialize(json);
        return session?.Status == GameSessionStatus.InProgress;
    }

    private bool IsStaleSessionJson(string json, IGameSessionSerializerPort serializer)
    {
        var session = serializer.Deserialize(json);
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
        var builder = new GameSessionServiceTestBuilder();
        GameSessionService sut = builder.Build();

        var gameId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var startTime = now - TimeSpan.FromHours(hoursAgo);

        var sessionId = builder.HashService.Object.ComputeHashForGameSession(gameId, now);

        var existingSession = new GameSession(
                gameId: gameId,
                sessionId: sessionId,
                startTime: startTime
            );

        var sessionFilePath = sut.GetSessionFilePath(existingSession);

        builder.FileSystem
            .Setup(fs => fs.FileExists(sessionFilePath))
            .Returns(true);
        builder.FileSystem
            .Setup(fs => fs.FileReadAllText(It.IsAny<string>()))
            .Returns(builder.Serializer.Serialize(existingSession));

        // Act
        await sut.OpenSessionAsync(gameId, now);

        // Assert
        builder.FileSystem
            .Verify(fs => fs.FileWriteAllText(
                It.Is<string>(p => p.Contains(sessionId)),
                It.Is<string>(json => IsClosedSessionJson(json, builder.Serializer))
            ), Times.Once);
        builder.FileSystem
            .Verify(fs => fs.FileDelete(sessionFilePath), Times.Once);
        builder.FileSystem
            .Verify(fs => fs.FileWriteAllText(
                sessionFilePath,
                It.Is<string>(json => IsInProgressSessionJson(json, builder.Serializer))
            ), Times.Once);
    }

    [Theory]
    [InlineData(3.1)]
    [InlineData(4)]
    [InlineData(24)]
    public async Task OpenSession_WhenExistingInProgressSessionExists_StaleSession(double hoursAgo)
    {
        // Arrange
        var builder = new GameSessionServiceTestBuilder();
        GameSessionService sut = builder.Build();

        var gameId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var startTime = now - TimeSpan.FromHours(hoursAgo);

        var sessionId = builder.HashService.Object.ComputeHashForGameSession(gameId, now);

        var existingSession = new GameSession(
                gameId: gameId,
                sessionId: sessionId,
                startTime: startTime
            );

        var sessionFilePath = sut.GetSessionFilePath(existingSession);

        builder.FileSystem
            .Setup(fs => fs.FileExists(sessionFilePath))
            .Returns(true);
        builder.FileSystem
            .Setup(fs => fs.FileReadAllText(It.IsAny<string>()))
            .Returns(builder.Serializer.Serialize(existingSession));

        // Act
        await sut.OpenSessionAsync(gameId, now);

        // Assert
        builder.FileSystem
            .Verify(fs => fs.FileWriteAllText(
                It.Is<string>(p => p.Contains(sessionId)),
                It.Is<string>(json => IsStaleSessionJson(json, builder.Serializer))
            ), Times.Once);
        builder.FileSystem
            .Verify(fs => fs.FileDelete(sessionFilePath), Times.Once);
        builder.FileSystem
            .Verify(fs => fs.FileWriteAllText(
                sessionFilePath,
                It.Is<string>(json => IsInProgressSessionJson(json, builder.Serializer))
            ), Times.Once);
    }

    [Fact]
    public async Task OpenSession_WhenNoExistingSession_CreatesNewSession()
    {
        // Arrange
        var builder = new GameSessionServiceTestBuilder();
        GameSessionService sut = builder.Build();

        var now = DateTime.UtcNow;
        var gameId = "game123";

        builder.FileSystem
            .Setup(fs => fs.FileExists(It.Is<string>(s => s.Contains(gameId))))
            .Returns(false);

        // Act
        await sut.OpenSessionAsync(gameId, now);

        // Assert
        builder.FileSystem
            .Verify(fs => fs.FileWriteAllText(
                It.Is<string>(s => s.Contains(gameId)),
                It.Is<string>(json => IsInProgressSessionJson(json, builder.Serializer))
            ), Times.Once);
        builder.Client
            .Verify(x => x.OpenGameSessionAsync(
                It.IsAny<OpenGameSessionCommand>()
            ), Times.Once);
    }

    [Fact]
    public async Task CloseSession_ShouldFail_WhenNoOpenedSessionExists()
    {
        // Arrange
        var builder = new GameSessionServiceTestBuilder();
        GameSessionService sut = builder.Build();

        var now = DateTime.UtcNow;
        var gameId = Guid.NewGuid().ToString();
        ulong duration = 2000;

        var sessionId = builder.HashService.Object.ComputeHashForGameSession(gameId, now);

        builder.FileSystem
            .Setup(fs => fs.FileExists(It.Is<string>(s => s.Contains(sessionId))))
            .Returns(false);

        // Assert
        await Assert.ThrowsAsync<FileNotFoundException>( 
            // Act
            async () => await sut.CloseSessionAsync(gameId, duration, now)
        );
    }
}
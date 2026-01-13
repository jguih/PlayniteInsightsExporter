using ExporterCommon.Application;
using ExporterCommon.Domain;
using ExporterCommon.Infra;
using ExporterGameSessions.Application;
using ExporterGameSessions.Error;
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
    private static bool IsClosedSessionJson(string json, IGameSessionSerializerPort serializer)
    {
        var session = serializer.Deserialize(json);
        return session.Status == GameSessionStatus.Closed;
    }

    private static bool IsInProgressSessionJson(string json, IGameSessionSerializerPort serializer)
    {
        var session = serializer.Deserialize(json);
        return session?.Status == GameSessionStatus.InProgress;
    }

    private static bool IsStaleSessionJson(string json, IGameSessionSerializerPort serializer)
    {
        var session = serializer.Deserialize(json);
        return session?.Status == GameSessionStatus.Stale;
    }

    [Fact]
    public async Task OpenSession_WhenNoExistingSession_CreatesNewSession()
    {
        // Arrange
        var builder = new GameSessionServiceTestBuilder();
        GameSessionService sut = builder.Build();

        var now = DateTime.UtcNow;
        var gameId = "game123";

        // Act
        var result = await sut.OpenSessionAsync(gameId, now);

        // Assert
        builder.FileSystem
            .Verify(fs => fs.FileWriteAllText(
                result.SessionFilePath,
                It.Is<string>(json => IsInProgressSessionJson(json, builder.Serializer))
            ), Times.Once);
        builder.Client
            .Verify(x => x.OpenGameSessionAsync(
                It.IsAny<OpenGameSessionCommand>(),
                default
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

        // Assert
        await Assert.ThrowsAsync<GameSessionNotFoundException>( 
            // Act
            async () => await sut.CloseSessionAsync(gameId, duration, now)
        );
    }
}
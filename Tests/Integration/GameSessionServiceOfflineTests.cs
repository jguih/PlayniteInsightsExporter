using ExporterBootstrap.Application;
using ExporterBootstrap.Application.Module;
using ExporterBootstrap.Application.Modules;
using ExporterCommon.Application;
using ExporterCommon.Domain;
using ExporterCommon.Infra;
using ExporterGameSessions.Application;
using ExporterGameSessions.Infra;
using ExporterLibraryExporter.Application;
using ExporterSystem.Infra;
using Moq;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using Tests.Lib.Adapters;
using Tests.Lib.Environment;
using Tests.Lib.Modules;

namespace Tests.Integration;

/// <summary>
/// Integration tests for GameSessionService in offline mode (no connection with Playnite Insights Web Server).
/// </summary>
[Trait("Category", "Integration")]
public class GameSessionServiceOfflineTests
{
    [Fact]
    public async Task OnOpen_CreateSessionFile()
    {
        // Arrange
        using var env = GameSessionTestEnvironment.Create();

        var gameId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        // Act
        var result = await env.Api.GameSession.GameSessionService.OpenSessionAsync(gameId, now);
        var sessionContent = File.ReadAllText(result.SessionFilePath);
        var session = env.Serializer.Deserialize(sessionContent);

        // Assert
        Assert.True(File.Exists(result.SessionFilePath), "Session file should exist after opening session.");
        Assert.NotNull(session);
        Assert.Equal(gameId, session.GameId);
        Assert.Equal(result.Session.SessionId, session.SessionId);
        Assert.Equal(GameSessionStatus.InProgress, session.Status);
        Assert.Equal(now, session.StartTime);
        Assert.Null(session.EndTime);
        Assert.Null(session.Duration);
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
        using var env = GameSessionTestEnvironment.Create();

        var gameId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        // Act
        var staleResult = await env.Api.GameSession.GameSessionService.OpenSessionAsync(gameId, now);
        var openResult = await env.Api.GameSession.GameSessionService.OpenSessionAsync(gameId, now.AddHours(hoursAfter));

        // Assert
        Assert.True(File.Exists(staleResult.SessionFilePath), "Stale session file should exist.");
        var staleSessionContent = File.ReadAllText(staleResult.SessionFilePath);
        var staleSession = env.Serializer.Deserialize(staleSessionContent);
        Assert.NotNull(staleSession);
        Assert.Equal(gameId, staleSession.GameId);
        Assert.Equal(now, staleSession.StartTime);
        Assert.Equal(staleResult.Session.SessionId, staleSession.SessionId);
        Assert.Equal(GameSessionStatus.Stale, staleSession.Status);
        Assert.True(File.Exists(openResult.SessionFilePath), "In-progress session file should be created.");
        var inProgressSessionContent = File.ReadAllText(openResult.SessionFilePath);
        var inProgressSession = env.Serializer.Deserialize(inProgressSessionContent);
        Assert.NotNull(inProgressSession);
        Assert.Equal(gameId, inProgressSession.GameId);
        Assert.Equal(openResult.Session.SessionId, inProgressSession.SessionId);
        Assert.Equal(GameSessionStatus.InProgress, inProgressSession.Status);
        Assert.Equal(now.AddHours(hoursAfter), inProgressSession.StartTime);
    }

    [Fact]
    public async Task SessionFilePath_IsStable_AcrossStateTransitions()
    {
        // Arrange
        using var env = GameSessionTestEnvironment.Create();

        var gameId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var endTime = now.AddHours(2);
        ulong duration = (ulong)(endTime - now).TotalSeconds;

        // Act
        var openResult = await env.Api.GameSession.GameSessionService.OpenSessionAsync(gameId, now);
        var closeResult = await env.Api.GameSession.GameSessionService.CloseSessionAsync(gameId, duration, endTime);
        // Assert
        Assert.True(File.Exists(openResult.SessionFilePath));
        Assert.True(File.Exists(closeResult.SessionFilePath));
        Assert.Equal(openResult.SessionFilePath, closeResult.SessionFilePath);
        var sessionContent = File.ReadAllText(openResult.SessionFilePath);
        var session = env.Serializer.Deserialize(sessionContent);
        Assert.Equal(GameSessionStatus.Closed, session.Status);
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
        using var env = GameSessionTestEnvironment.Create();

        var gameId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var duration = (ulong)(now.AddHours(hoursAfter) - now).TotalSeconds;

        // Act
        var closedResult = await env.Api.GameSession.GameSessionService.OpenSessionAsync(gameId, now);
        var openResult = await env.Api.GameSession.GameSessionService.OpenSessionAsync(gameId, now.AddHours(hoursAfter));
        // Assert
        Assert.True(File.Exists(closedResult.SessionFilePath), "Closed session file should exist.");
        var closedSessionContent = File.ReadAllText(closedResult.SessionFilePath);
        var closedSession = env.Serializer.Deserialize(closedSessionContent);
        Assert.NotNull(closedSession);
        Assert.Equal(gameId, closedSession.GameId);
        Assert.Equal(closedResult.Session.SessionId, closedSession.SessionId);
        Assert.Equal(now, closedSession.StartTime);
        Assert.Equal(GameSessionStatus.Closed, closedSession.Status);
        Assert.Equal(now.AddHours(hoursAfter), closedSession.EndTime);
        Assert.Equal(duration, closedSession.Duration);

        Assert.True(File.Exists(openResult.SessionFilePath), "In-progress session file should exist.");
        var inProgressSessionContent = File.ReadAllText(openResult.SessionFilePath);
        var inProgressSession = env.Serializer.Deserialize(inProgressSessionContent);
        Assert.NotNull(inProgressSession);
        Assert.Equal(gameId, inProgressSession.GameId);
        Assert.Equal(openResult.Session.SessionId, inProgressSession.SessionId);
        Assert.Equal(now.AddHours(hoursAfter), inProgressSession.StartTime);
        Assert.Equal(GameSessionStatus.InProgress, inProgressSession.Status);
    }

    //[Theory]
    //[InlineData(48.1)]
    //[InlineData(49)]
    //[InlineData(500)]
    //public async Task OnSync_WhenInProgressSessionExists_StaleSession(double hoursOld)
    //{
    //    // Arrange
    //    var now = DateTime.UtcNow;
    //    GameSession session = new()
    //    {
    //        GameId = Guid.NewGuid().ToString(),
    //        SessionId = Guid.NewGuid().ToString(),
    //        StartTime = DateTime.UtcNow.AddHours(-hoursOld),
    //        Status = GameSession.STATUS_IN_PROGRESS,
    //    };
    //    var sessionFilePath = SessionsService.GetSessionFilePath(session.GameId);
    //    File.WriteAllText(sessionFilePath, JsonConvert.SerializeObject(session));
    //    var staleFilePath = SessionsService.GetStaleSessionFilePath(session.SessionId);
    //    // Act
    //    await SessionsService.SyncAsync(now);
    //    // Assert
    //    Assert.False(File.Exists(sessionFilePath));
    //    Assert.True(File.Exists(staleFilePath));
    //    var staleSessionContent = File.ReadAllText(staleFilePath);
    //    var staleSession = JsonConvert.DeserializeObject<GameSession>(staleSessionContent);
    //    Assert.NotNull(staleSession);
    //    Assert.Equal(session.GameId, staleSession.GameId);
    //    Assert.Equal(session.SessionId, staleSession.SessionId);
    //    Assert.Equal(session.StartTime, staleSession.StartTime);
    //    Assert.Equal(GameSession.STATUS_STALE, staleSession.Status);
    //}

    ///// <summary>
    ///// Stale and closed sessions older than 14 days (default value) or invalid should be deleted during sync.
    ///// </summary>
    //[Fact]
    //public async Task OnSync_WhenSessionIsTooOldOrInvalid_DeleteFile()
    //{
    //    // Arrange
    //    var now = DateTime.UtcNow;
    //    var startTime = DateTime.UtcNow.AddDays(-15);
    //    GameSession staleSession = new()
    //    {
    //        GameId = Guid.NewGuid().ToString(),
    //        SessionId = Guid.NewGuid().ToString(),
    //        StartTime = startTime,
    //        Status = GameSession.STATUS_STALE,
    //    };
    //    var staleFilePath = SessionsService.GetStaleSessionFilePath(staleSession.SessionId);
    //    File.WriteAllText(staleFilePath, JsonConvert.SerializeObject(staleSession));
    //    GameSession closedSession = new()
    //    {
    //        GameId = Guid.NewGuid().ToString(),
    //        SessionId = Guid.NewGuid().ToString(),
    //        StartTime = startTime,
    //        EndTime = startTime.AddHours(1),
    //        Duration = 3600,
    //        Status = GameSession.STATUS_CLOSED,
    //    };
    //    var closedFilePath = SessionsService.GetClosedSessionFilePath(closedSession.SessionId);
    //    File.WriteAllText(closedFilePath, JsonConvert.SerializeObject(closedSession));
    //    var invalidSession = new GameSession
    //    {
    //        GameId = string.Empty, // Invalid game ID
    //        SessionId = string.Empty, // Invalid session ID
    //        StartTime = startTime,
    //        Status = "invalid_status" // Invalid status
    //    };
    //    var invalidFilePath = SessionsService.GetSessionFilePath(invalidSession.GameId);
    //    File.WriteAllText(invalidFilePath, JsonConvert.SerializeObject(invalidSession));
    //    // Act
    //    await SessionsService.SyncAsync(now);
    //    // Assert
    //    Assert.False(File.Exists(staleFilePath), "Old stale session file should be deleted after sync.");
    //    Assert.False(File.Exists(closedFilePath), "Old closed session file should be deleted after sync.");
    //    Assert.False(File.Exists(invalidFilePath), "Invalid session file should be deleted after sync.");
    //}
}

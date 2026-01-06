using Bogus;
using ExporterCommon.Application;
using ExporterCommon.Domain;
using ExporterCommon.Infra;
using ExporterCommon.Testing;
using ExporterLibraryExporter.Application;
using ExporterSystem.Infra;
using Moq;
using Tests.Lib.Builders;

namespace Tests.Unit;

[Trait("Category", "Unit")]
public class LibraryExporterTests
{
    [Fact]
    public async Task ExportMediaFiles_Should_Fail_When_ManifestRequestFails()
    {
        // Arrange
        var builder = new LibraryExporterTestBuilder();
        LibrarySyncService sut = builder.Build();

        var games = builder.GameFactory.BuildGameList(15);
        builder.PlayAtlasHttpClient
            .Setup(x => x.GetManifestAsync())
            .ThrowsAsync(new Exception("Invalid request"));

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
        {
            return sut.SyncMediaFilesAsync(games);
        });
    }

    [Fact]
    public async Task ExportMediaFiles_Should_SkipGame_When_GameNotInManifest()
    {
        // Arrange
        var builder = new LibraryExporterTestBuilder();
        LibrarySyncService sut = builder.Build();

        var manifest = new PlayAtlasLibraryManifest(
            totalGamesInLibrary: 0,
            gamesInLibrary: [],
            mediaExistsFor: []
        );

        builder.PlayAtlasHttpClient
            .Setup(x => x.GetManifestAsync())
            .Returns(Task.FromResult(manifest));
        var games = builder.GameFactory.BuildGameList(15);

        // Act
        var result = await sut.SyncMediaFilesAsync(games);

        // Assert
        Assert.True(result.OperationSuccess);
        Assert.Equal(0, result.Success);
        Assert.Equal(15, result.Skipped);
    }

    [Fact]
    public async Task ComputeLibraryDiff_Should_Return_Add_Update_And_Remove()
    {
        // Arrange
        var builder = new LibraryExporterTestBuilder();
        LibrarySyncService sut = builder.Build();

        var gameA = builder.GameFactory.BuildGame();
        var gameB = builder.GameFactory.BuildGame();
        var gameC = builder.GameFactory.BuildGame();
        var gameD = builder.GameFactory.BuildGame();

        builder.GameRepository
            .Setup(r => r.GetAll())
            .Returns([gameA, gameB, gameC]);

        var manifest = new PlayAtlasLibraryManifest(
            totalGamesInLibrary: 3,
            gamesInLibrary:
            [
                new(gameB.Id.ToString(), "hash-b"),          // same → no-op
                new(gameC.Id.ToString(), "hash-c-remote"),   // different → update
                new(gameD.Id.ToString(), "hash-d")           // not local → remove
            ],
            mediaExistsFor: []
        );

        builder.PlayAtlasHttpClient
            .Setup(x => x.GetManifestAsync())
            .ReturnsAsync(manifest);
        builder.HashService
            .Setup(x => x.ComputeSHA256Base64(It.Is<AppGame>(g => g.Id.Equals(gameA.Id))))
            .Returns("hash-a");
        builder.HashService
            .Setup(x => x.ComputeSHA256Base64(It.Is<AppGame>(g => g.Id.Equals(gameB.Id))))
            .Returns("hash-b");
        builder.HashService
            .Setup(x => x.ComputeSHA256Base64(It.Is<AppGame>(g => g.Id.Equals(gameC.Id))))
            .Returns("hash-c-local");

        // Act
        var diff = await sut.ComputeGameLibraryDiff();

        // Assert
        Assert.Single(diff.ToAdd);
        Assert.Equal(gameA.Id, diff.ToAdd[0].Game.Id);

        Assert.Single(diff.ToUpdate);
        Assert.Equal(gameC.Id, diff.ToUpdate[0].Game.Id);

        Assert.Single(diff.ToRemove);
        Assert.Equal(gameD.Id.ToString(), diff.ToRemove[0]);
    }

}

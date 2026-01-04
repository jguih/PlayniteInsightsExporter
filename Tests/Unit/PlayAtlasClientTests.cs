using Bogus;
using ExporterCommon.Application;
using ExporterCommon.Domain;
using ExporterCommon.Testing;
using ExporterPlayAtlasClient.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Unit;

[Trait("Category", "Unit")]
public class PlayAtlasClientTests
{
    private readonly ISyncGamesDtoMapperPort syncGamesDtoMapper;
    private readonly GameFactory gameFactory;
    private readonly Faker faker;

    public PlayAtlasClientTests()
    {
        syncGamesDtoMapper = new SyncGamesDtoMapper();
        gameFactory = new GameFactory();
        faker = new Faker();
    }

    [Fact]
    public void Map_ShouldNot_Throw_WhenGameOptionalFieldsAreNull()
    {
        var game = gameFactory.BuildGame();
        game.ReleaseDate = null;
        var command = new SyncGamesCommand
        {
            AddedItems =
            [
                new SyncGameCommandItem(
                    game: game,
                    contentHash: faker.Random.Hash()
                )
            ]
        };

        var requestDto = syncGamesDtoMapper.Map(command);

        Assert.NotNull(requestDto);
    }

}

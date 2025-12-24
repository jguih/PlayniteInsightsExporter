using Bogus;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Lib;
public class GameFactory
{
    private readonly Faker faker = new Faker();

    private Game _BuildGame()
    {
        return new Game
        {
            Id = faker.Random.Guid(),
            Name = faker.Lorem.Word(),
            IsInstalled = faker.Random.Bool()
        };
    }

    public Game BuildGame()
    {
        return _BuildGame();
    }

    public List<Game> BuildGameList(int n)
    {
        var games = new List<Game>(n);

        for (int i = 0; i < n; i++)
        {
            games.Add(_BuildGame());
        }

        return games;
    }
}

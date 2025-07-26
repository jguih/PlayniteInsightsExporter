using Bogus;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Lib;
internal class TestFactory
{
    private Faker Faker { get; } = new Faker();

    public Game GetGame()
    {
        return new Game
        {
            Id = Faker.Random.Guid(),
            Name = Faker.Lorem.Word(),
            IsInstalled = Faker.Random.Bool()
        };
    }
}

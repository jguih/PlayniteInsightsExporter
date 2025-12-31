using Bogus;
using ExporterCommon.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Testing
{
    public class GameFactory
    {
        private readonly Faker faker = new Faker();

        private AppGame _BuildGame()
        {
            return new AppGame
            {
                Id = faker.Random.Guid(),
                Name = faker.Lorem.Word(),
                IsInstalled = faker.Random.Bool()
            };
        }

        public AppGame BuildGame()
        {
            return _BuildGame();
        }

        public List<AppGame> BuildGameList(int n)
        {
            var games = new List<AppGame>(n);

            for (int i = 0; i < n; i++)
            {
                games.Add(_BuildGame());
            }

            return games;
        }
    }
}

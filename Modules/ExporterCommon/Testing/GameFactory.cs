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
            bool isInstalled = faker.Random.Bool();

            return new AppGame
            {
                Id = faker.Random.Guid(),
                Name = faker.Lorem.Word(),
                IsInstalled = isInstalled,
                Added = faker.Date.Recent(),
                BackgroundImage = faker.System.DirectoryPath(),
                CoverImage = faker.System.DirectoryPath(),
                Icon = faker.System.DirectoryPath(),
                CompletionStatus = null,
                Description = faker.Lorem.Paragraphs(5),
                InstallDirectory = isInstalled ? faker.System.DirectoryPath() : null,
                IsHidden = faker.Random.Bool(),
                LastActivity = faker.Date.Recent(),
                Playtime = checked((ulong)faker.Random.Number(min: 0)),
                ReleaseDate = faker.Date.Recent()
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

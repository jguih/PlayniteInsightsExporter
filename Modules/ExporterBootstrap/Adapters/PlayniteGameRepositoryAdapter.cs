using ExporterCommon.Application;
using ExporterCommon.Domain;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Adapters
{
    internal class PlayniteGameRepositoryAdapter : IPlayniteGameRepositoryPort
    {
        private readonly IPlayniteAPI api;

        public PlayniteGameRepositoryAdapter(
            IPlayniteAPI api
        )
        {
            this.api = api;
        }

        private AppGame MapToExtensionGame(Game game)
        {
            var appGame = new AppGame()
            {
                Id = game.Id,
                Name = game.Name,
                Description = game.Description,
                Platforms = game.Platforms?
                    .Select(p => new AppPlatform()
                    {
                        Id = p.Id,
                        Name = p.Name,
                        SpecificationId = p.SpecificationId,
                        Background = p.Background,
                        Cover = p.Cover,
                        Icon = p.Icon
                    }).ToList() ?? Enumerable.Empty<AppPlatform>().ToList(),
                Developers = game.Developers?
                    .Select(d => new AppCompany()
                    {
                        Id = d.Id,
                        Name = d.Name
                    }).ToList() ?? Enumerable.Empty<AppCompany>().ToList(),
                Publishers = game.Publishers?
                    .Select(d => new AppCompany()
                    {
                        Id = d.Id,
                        Name = d.Name
                    }).ToList() ?? Enumerable.Empty<AppCompany>().ToList(),
                Genres = game.Genres?
                    .Select(g => new AppGenre()
                    {
                        Id = g.Id,
                        Name = g.Name,
                    }).ToList() ?? Enumerable.Empty<AppGenre>().ToList(),
                Added = game.Added,
                LastActivity = game.LastActivity,
                ReleaseDate = game.ReleaseDate?.Date,
                BackgroundImage = game.BackgroundImage,
                CoverImage = game.CoverImage,
                Icon = game.Icon,
                CompletionStatus = game.CompletionStatus is null
                    ? null
                    : new AppCompletionStatus
                    {
                        Id = game.CompletionStatus.Id,
                        Name = game.CompletionStatus.Name,
                    },
                InstallDirectory = game.InstallDirectory,
                IsHidden = game.Hidden,
                IsInstalled = game.IsInstalled,
                Playtime = game.Playtime,
            };
            return appGame;
        }

        public IReadOnlyList<AppGame> GetAll()
        {
            var playniteGames = api.Database.Games;
            var games = playniteGames
                .Select(MapToExtensionGame)
                .ToList();
            return games;
        }
    }
}

using Common.Application;
using Common.Infra;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryExporter.Application
{
    public class LibraryExporter : ILibraryExporterPort
    {
        private readonly IAppLoggerPort appLogger;
        private readonly IPlayAtlasHttpClientPort playAtlasHttpClient;
        private readonly IHashServicePort hashService;
        private readonly IFileSystemServicePort fileSystemService;
        private readonly ISystemConfigPort systemConfig;

        public LibraryExporter(
          IAppLoggerPort appLogger,
          IPlayAtlasHttpClientPort playAtlasHttpClient,
          IHashServicePort hashService,
          IFileSystemServicePort fileSystemService,
          ISystemConfigPort systemConfig
        )
        {
            this.appLogger = appLogger;
            this.playAtlasHttpClient = playAtlasHttpClient;
            this.hashService = hashService;
            this.fileSystemService = fileSystemService;
            this.systemConfig = systemConfig;
        }

        private bool IsGameInServerLibrary(PlayAtlasLibraryManifest manifest, string gameId)
        {
            if (manifest is null)
            {
                return false;
            }

            var gameInLibrary = manifest?.gamesInLibrary?
                    .Where((gil) => gil.gameId == gameId)
                    .FirstOrDefault() ?? null;

            return gameInLibrary != null;
        }

        private bool ShouldSendMediaFiles(PlayAtlasLibraryManifest manifest, string gameId, string contentHash)
        {
            if (manifest is null)
            {
                return false;
            }

            var mediaExistsForEntry = manifest?.mediaExistsFor?
                        .Where(m => m.gameId == gameId)
                        .FirstOrDefault() ?? null;

            if (mediaExistsForEntry != null)
            {
                if (mediaExistsForEntry.contentHash == contentHash)
                {
                    return false;
                }
            }

            return true;
        }

        private IReadOnlyCollection<MediaFileDescriptor> GetMediaFileDescriptors(
            Game game,
            string mediaFolderPath
        )
        {
            if (!fileSystemService.DirectoryExists(mediaFolderPath))
            {
                appLogger.Debug(
                    $"Media folder not found at {mediaFolderPath}. Assuming the game has no media files."
                );
                return Array.Empty<MediaFileDescriptor>();
            }

            var files = fileSystemService.DirectoryGetFiles(mediaFolderPath);
            if (!files.Any())
            {
                appLogger.Debug(
                    $"No media files found at {mediaFolderPath}. Assuming the game has no media files."
                );
                return Array.Empty<MediaFileDescriptor>();
            }

            var normalizedFiles = new HashSet<string>(
                    files.Select(fileSystemService.PathGetFullPath),
                    StringComparer.OrdinalIgnoreCase
                );
            var libraryFilesDir = systemConfig.GetLibraryFilesDirPath();
            var descriptors = new List<MediaFileDescriptor>();

            void TryAdd(string relativePath, MediaRole role)
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                    return;

                var fullPath = fileSystemService.PathCombine(libraryFilesDir, relativePath);
                fullPath = fileSystemService.PathGetFullPath(fullPath);

                if (normalizedFiles.Contains(fullPath))
                {
                    descriptors.Add(new MediaFileDescriptor(
                        role: role,
                        fullPath: fullPath
                    ));
                }
            }

            TryAdd(game.BackgroundImage, MediaRole.Background);
            TryAdd(game.CoverImage, MediaRole.Cover);
            TryAdd(game.Icon, MediaRole.Icon);

            return descriptors;
        }

        public bool ExportLibrary(List<Game> itemsToAdd = null, List<Game> itemsToUpdate = null, List<Game> itemsToRemove = null)
        {
            throw new NotImplementedException();
        }

        public bool ExportLibrary(List<Game> itemsToSync)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExportLibraryAsync(List<Game> itemsToAdd = null, List<Game> itemsToUpdate = null, List<Game> itemsToRemove = null)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ExportMediaFiles(IEnumerable<Game> games = null, CancellationToken cancellationToken = default)
        {
            appLogger.Debug($"Starting library media files sync for {games.Count()} games.");
            int skipped = 0;
            int success = 0;
            int failed = 0;
            var manifest = await playAtlasHttpClient.GetManifestAsync();

            foreach (var game in games)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    appLogger.Info("Library media files sync cancelled by user.");
                    return true;
                }

                string gameId = game.Id.ToString();
                string mediaFolderPath = fileSystemService.PathCombine(systemConfig.GetLibraryFilesDirPath(), gameId);
                string contentHash = hashService.ComputeHashFromFolderContents(mediaFolderPath);

                if (string.IsNullOrEmpty(contentHash))
                {
                    skipped++;
                    continue;
                }

                if (!IsGameInServerLibrary(manifest, gameId))
                {
                    skipped++;
                    continue;
                }

                if (!ShouldSendMediaFiles(manifest, gameId, contentHash))
                {
                    skipped++;
                    continue;
                }

                string canonicalHash = hashService.ComputeCanonicalHashForGameMediaFiles(
                        gameId: gameId,
                        contentHash: contentHash,
                        mediaFolderPath: mediaFolderPath
                    );
                var descriptors = GetMediaFileDescriptors(game, mediaFolderPath);
                var request = new SendMediaFilesRequest(
                        gameId: gameId,
                        contentHash: contentHash,
                        canonicalHash: canonicalHash,
                        mediaFiles: descriptors
                    );

                var result = await playAtlasHttpClient.SendMediaFilesAsync(request);
                if (result.Success)
                {
                    success++;
                }
                else
                {
                    failed++;
                }
            }

            return true;
        }
    }
}

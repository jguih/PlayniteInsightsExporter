using ExporterCommon.Application;
using ExporterCommon.Domain;
using ExporterCommon.Infra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExporterLibraryExporter.Application
{
    enum MediaProcessResult
    {
        Skipped,    
        SentSuccessfully,
        SentFailed,
        Failed
    }


    public class LibraryExporterService : ILibraryExporterServicePort
    {
        private readonly IAppLoggerPort appLogger;
        private readonly IPlayAtlasHttpClientPort playAtlasHttpClient;
        private readonly IHashServicePort hashService;
        private readonly IFileSystemServicePort fileSystemService;
        private readonly ISystemConfigPort systemConfig;
        private readonly IPlayniteGameRepositoryPort gameRepository;

        public LibraryExporterService(
          IAppLoggerPort appLogger,
          IPlayAtlasHttpClientPort playAtlasHttpClient,
          IHashServicePort hashService,
          IFileSystemServicePort fileSystemService,
          ISystemConfigPort systemConfig,
          IPlayniteGameRepositoryPort gameRepository
        )
        {
            this.appLogger = appLogger;
            this.playAtlasHttpClient = playAtlasHttpClient;
            this.hashService = hashService;
            this.fileSystemService = fileSystemService;
            this.systemConfig = systemConfig;
            this.gameRepository = gameRepository;
        }

        private bool IsGameInServerLibrary(PlayAtlasLibraryManifest manifest, string gameId)
        {
            if (manifest is null)
            {
                return false;
            }

            var gameInLibrary = manifest?.GamesInLibrary?
                    .Where(item => item.GameId == gameId)
                    .FirstOrDefault() ?? null;

            return gameInLibrary != null;
        }

        private bool ShouldSendMediaFiles(PlayAtlasLibraryManifest manifest, string gameId, string contentHash)
        {
            if (manifest is null)
            {
                return false;
            }

            var mediaExistsForEntry = manifest?.MediaExistsFor?
                        .Where(item => item.GameId == gameId)
                        .FirstOrDefault() ?? null;

            if (mediaExistsForEntry != null)
            {
                if (mediaExistsForEntry.ContentHash == contentHash)
                {
                    return false;
                }
            }

            return true;
        }

        private IReadOnlyCollection<MediaFileDescriptor> GetMediaFileDescriptors(
            AppGame game,
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
            var libraryFilesDir = systemConfig.LibraryFilesDirPath;
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

        private async Task<MediaProcessResult> ProcessGameMediaAsync(
            AppGame game,
            PlayAtlasLibraryManifest manifest
        )
        {
            string gameId = game.Id.ToString();
            string mediaFolderPath = fileSystemService
                .PathCombine(systemConfig.LibraryFilesDirPath, gameId);
            string contentHash;
            string canonicalHash;

            if (!fileSystemService.DirectoryExists(mediaFolderPath))
            {
                return MediaProcessResult.Skipped;
            }

            try
            {
                contentHash = hashService
                    .ComputeHashFromFolderContents(mediaFolderPath);
            }
            catch (Exception ex)
            {
                appLogger.Error($"Failed to compute content hash for game (Id: {game.Id}, Name: {game.Name})", ex);
                return MediaProcessResult.Failed;
            }

            if (string.IsNullOrEmpty(contentHash))
            {
                return MediaProcessResult.Skipped;
            }

            if (!IsGameInServerLibrary(manifest, gameId))
            {
                return MediaProcessResult.Skipped;
            }

            if (!ShouldSendMediaFiles(manifest, gameId, contentHash))
            {
                return MediaProcessResult.Skipped;
            }

            try
            {
                canonicalHash = hashService.ComputeCanonicalHashForGameMediaFiles(
                        gameId: gameId,
                        contentHash: contentHash,
                        mediaFolderPath: mediaFolderPath
                    );
            }
            catch (Exception ex)
            {
                appLogger.Error($"Failed to compute canonical hash for game (Id: {game.Id}, Name: {game.Name})", ex);
                return MediaProcessResult.Failed;
            }

            try
            {
                var descriptors = GetMediaFileDescriptors(game, mediaFolderPath);
                var request = new SyncMediaFilesRequest(
                        gameId: gameId,
                        contentHash: contentHash,
                        canonicalHash: canonicalHash,
                        mediaFiles: descriptors
                    );
                await playAtlasHttpClient.SyncMediaFilesAsync(request);
                return MediaProcessResult.SentSuccessfully;
            }
            catch (Exception ex)
            {
                appLogger.Error($"Failed to send media files for game (Id: {game.Id}, Name: {game.Name}) to PlayAtlas server", ex);
                return MediaProcessResult.SentFailed;
            }
        }

        public Task<bool> ExportLibraryAsync(
           LibraryExportDiff diff,
           CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public async Task<ExportMediaFilesResult> ExportMediaFilesAsync(
            IReadOnlyList<AppGame> games,
            CancellationToken cancellationToken = default,
            ExportMediaFilesContext context = null
        )
        {
            appLogger.Debug($"Exporting media files for {games.Count()} games...");

            if (games == null || !games.Any())
            {
                appLogger.Debug($"No game media files to export");
                return new ExportMediaFilesResult(
                        reasonCode: ExportMediaFilesResultReasonCode.Success,
                        reason: "Success",
                        operationSuccess: true,
                        skipped: 0,
                        success: 0,
                        failed: 0
                    );
            }

            int skipped = 0;
            int success = 0;
            int failed = 0;
            int requestsInBatch = 0;
            var manifest = await playAtlasHttpClient.GetManifestAsync();
            const int batchSize = 5;
            const int delayBetweenBatchesMs = 2000;

            for (int i = 0; i < games.Count; i++)
            {
                var game = games[i];

                context?.OnBeginProcessing(game);

                if (cancellationToken.IsCancellationRequested)
                {
                    appLogger.Info("Library media files sync cancelled by user.");
                    return new ExportMediaFilesResult(
                            reasonCode: ExportMediaFilesResultReasonCode.OperationCanceledByUser,
                            reason: "Canceled by user",
                            operationSuccess: true,
                            skipped: skipped,
                            success: success,
                            failed: failed
                        );
                }

                var result = await ProcessGameMediaAsync(
                    game, 
                    manifest
                );

                context?.OnFinishProcessing(game);

                switch (result)
                {
                    case MediaProcessResult.SentSuccessfully:
                        {
                            success++;
                            requestsInBatch++;
                            break;
                        }
                    case MediaProcessResult.SentFailed:
                        {
                            failed++;
                            requestsInBatch++;
                            break;
                        }
                    case MediaProcessResult.Failed: failed++; break;
                    case MediaProcessResult.Skipped: skipped++; break;
                }

                if (requestsInBatch >= batchSize)
                {
                    requestsInBatch = 0;
                    await Task.Delay(delayBetweenBatchesMs, cancellationToken);
                }
            }

            if (failed == 0)
            {
                return new ExportMediaFilesResult(
                            reasonCode: ExportMediaFilesResultReasonCode.Success,
                            reason: "Success",
                            operationSuccess: true,
                            skipped: skipped,
                            success: success,
                            failed: failed
                        );
            }

            return new ExportMediaFilesResult(
                    reasonCode: ExportMediaFilesResultReasonCode.OneOrMoreFailed,
                    reason: "One or more operations failed",
                    operationSuccess: false,
                    skipped: skipped,
                    success: success,
                    failed: failed
                );
        }

        public async Task<LibraryExportDiff> ComputeLibraryDiff()
        {
            var manifest = await playAtlasHttpClient.GetManifestAsync();
            var localGames = gameRepository.GetAll();

            var toAdd = new List<AppGame>();
            var toUpdate = new List<AppGame>();
            var toRemove = new List<AppGame>();

            var localById = localGames.ToDictionary(g => g.Id.ToString());
            var manifestById = manifest?.GamesInLibrary?
                .ToDictionary(g => g.GameId)
                ?? new Dictionary<string, PlayAtlasLibraryManifestItem>();

            // Add & Update
            foreach (var localGame in localById.Values)
            {
                if (!manifestById.TryGetValue(localGame.Id.ToString(), out var manifestGame))
                {
                    toAdd.Add(localGame);
                    continue;
                }

                if (!string.Equals(manifestGame.ContentHash, localGame.ContentHash, StringComparison.Ordinal))
                {
                    toUpdate.Add(localGame);
                }
            }

            // Remove (exists on server but not locally)
            foreach (var manifestGame in manifestById.Values)
            {
                if (!localById.ContainsKey(manifestGame.GameId))
                {
                    appLogger.Warn(
                        $"Server manifest contains game {manifestGame.GameId} not present locally. It will be removed.");

                    toRemove.Add(new AppGame
                    {
                        Id = Guid.Parse(manifestGame.GameId)
                    });
                }
            }

            return new LibraryExportDiff(toAdd, toUpdate, toRemove);
        }

    }
}

using ExporterCommon.Application;
using ExporterCommon.Domain;
using ExporterCommon.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExporterLibraryExporter.Application
{
    public class LibraryExporterService : ILibraryExporterServicePort
    {
        private readonly IAppLoggerPort appLogger;
        private readonly IPlayAtlasHttpClientPort playAtlasHttpClient;
        private readonly IHashServicePort hashService;
        private readonly IFileSystemServicePort fileSystemService;
        private readonly ISystemConfigPort systemConfig;

        public LibraryExporterService(
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

        public bool ExportLibrary(
            List<AppGame> itemsToAdd = null,
            List<AppGame> itemsToUpdate = null,
            List<AppGame> itemsToRemove = null
        )
        {
            throw new NotImplementedException();
        }

        public bool ExportLibrary(List<AppGame> itemsToSync)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExportLibraryAsync(
            List<AppGame> itemsToAdd = null,
            List<AppGame> itemsToUpdate = null,
            List<AppGame> itemsToRemove = null
        )
        {
            throw new NotImplementedException();
        }

        public async Task<ExportMediaFilesResult> ExportMediaFiles(
            IEnumerable<AppGame> games = null,
            CancellationToken cancellationToken = default
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
            var manifestResponse = await playAtlasHttpClient.GetManifestAsync();

            if (!manifestResponse.Success)
            {
                appLogger.Warn($"Export game media files failed. Could not fetch library manifest from server");
                return new ExportMediaFilesResult(
                        reasonCode: ExportMediaFilesResultReasonCode.FailedToFetchManifest,
                        reason: $"Failed to fetch manifest: {manifestResponse.Reason}",
                        operationSuccess: false,
                        skipped: skipped,
                        success: success,
                        failed: failed
                    );
            }

            var manifest = manifestResponse.Manifest;

            foreach (var game in games)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    appLogger.Info("Library media files sync cancelled by user.");
                    return new ExportMediaFilesResult(
                            reasonCode: ExportMediaFilesResultReasonCode.OperationCanceledByUser,
                            reason: "Operation canceled by user",
                            operationSuccess: true,
                            skipped: skipped,
                            success: success,
                            failed: failed
                        );
                }

                string gameId = game.Id.ToString();
                string mediaFolderPath = fileSystemService.PathCombine(systemConfig.LibraryFilesDirPath, gameId);
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
                    appLogger.Error($"Failed to send media files to PlayAtlas server: {result.Reason}");
                    failed++;
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
    }
}

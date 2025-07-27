using Core;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infra
{
    public class HashService : IHashService
    {
        private readonly IAppLogger Logger;

        public HashService(IAppLogger logger)
        {
            Logger = logger;
        }

        public string HashFolderContents(string dir)
        {
            if(string.IsNullOrEmpty(dir))
            {
                Logger.Warn("Attempted to create hash for null or empty directory path.");
                return string.Empty;
            }

            try
            {
                if (!Directory.Exists(dir))
                {
                    Logger.Warn($"Attempted to create hash for non-existent game media folder: {dir}. Ignore this warning if this game does not contain any media files");
                    return string.Empty;
                }

                var files = Directory.GetFiles(dir)
                    .Select(Path.GetFileName)
                    .OrderBy(n => n, StringComparer.Ordinal)
                    .ToList();

                if (!files.Any())
                {
                    Logger.Warn($"No files found in game media directory: {dir}. Returning empty hash.");
                    return string.Empty;
                }

                using (var sha256 = SHA256.Create())
                {
                    StringBuilder fileRecord = new StringBuilder();
                    foreach (var file in files)
                    {
                        var filePath = Path.Combine(dir, file);
                        var info = new FileInfo(filePath);
                        var mtimeMs = ((DateTimeOffset)info.LastWriteTimeUtc)
                            .ToUnixTimeMilliseconds();
                        fileRecord.Append($"{file}|{info.Length}|{mtimeMs}");
                    }
                    var stringRecord = fileRecord.ToString();
                    var bytes = Encoding.UTF8.GetBytes(stringRecord);
                    return BitConverter
                        .ToString(sha256.ComputeHash(bytes))
                        .Replace("-", "")
                        .ToLowerInvariant();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to create hash for {dir}");
                return string.Empty;
            }
        }

        public string GetHashFromPlayniteGame(Game game)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                {
                    var developers = game.Developers != null ?
                        string.Join(",", game.Developers?.Select(d => d.Name)) : "";
                    var genres = game.Genres != null ?
                        string.Join(",", game.Genres?.Select(g => g.Name)) : "";
                    var tags = game.Tags != null ?
                        string.Join(",", game.Tags?.Select(t => t.Name)) : "";
                    var categories = game.Categories != null ?
                        string.Join(",", game.Categories?.Select(c => c.Name)) : "";
                    var features = game.Features != null ?
                        string.Join(",", game.Features?.Select(f => f.Name)) : "";
                    var publishers = game.Publishers != null ?
                        string.Join(",", game.Publishers?.Select(p => p.Name)) : "";
                    var platforms = game.Platforms != null ?
                        string.Join(",", game.Platforms?.Select(p => p.Name)) : "";
                    var playtime = game.Playtime.ToString();
                    var playcount = game.PlayCount.ToString();
                    var lastActivity = game.LastActivity?.ToString() ?? "";
                    var description = game.Description != null ?
                        game.Description.Length > 20 ?
                        game.Description?.Substring(0, 20) :
                        game.Description : "";
                    var completionStatus = game.CompletionStatus?.Name ?? "";
                    var metadata = $"{lastActivity}|" +
                        $"{game.Name}|" +
                        $"{description}|" +
                        $"{game.Added}|" +
                        $"{game.IsInstalled}|" +
                        $"{game.InstallDirectory}|" +
                        $"{game.CoverImage}|" +
                        $"{game.BackgroundImage}|" +
                        $"{game.Icon}|" +
                        $"{completionStatus}|" +
                        $"{tags}|" +
                        $"{genres}|" +
                        $"{categories}|" +
                        $"{features}|" +
                        $"{developers}|" +
                        $"{publishers}|" +
                        $"{platforms}|" +
                        $"{playcount}|" +
                        $"{playtime}|" +
                        $"{game.Hidden}|" +
                        $"{game.Version}";
                    var bytes = Encoding.UTF8.GetBytes(metadata);
                    var hash = sha256.ComputeHash(bytes);
                    return BitConverter
                        .ToString(hash)
                        .Replace("-", "")
                        .ToLowerInvariant();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to create hash for game {game.Name}");
                return string.Empty;
            }
        }

        public string GetHashForGameSession(string gameId, DateTime startTime)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                {
                    var metadata = $"{gameId}|" +
                        $"{startTime.ToString(CultureInfo.InvariantCulture)}";
                    var bytes = Encoding.UTF8.GetBytes(metadata);
                    var hash = sha256.ComputeHash(bytes);
                    return BitConverter
                        .ToString(hash)
                        .Replace("-", "")
                        .ToLowerInvariant();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to create hash for game session with game Id: {gameId}");
                return string.Empty;
            }
        }
    }
}

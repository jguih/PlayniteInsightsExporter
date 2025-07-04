using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PlayniteInsightsExporter.Lib
{
    class HashService
    {
        private readonly ILogger Logger;

        public HashService(ILogger logger)
        {
            Logger = logger;
        }

        public string HashFolderContents(string dir)
        {
            try
            {
                var files = Directory.GetFiles(dir)
                    .Select(Path.GetFileName)
                    .OrderBy(n => n, StringComparer.Ordinal)
                    .ToList();

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
                        .Replace("-","")
                        .ToLowerInvariant();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to create hash for {dir}");
                return string.Empty;
            }
        }

        public string HashGameMetadata(Game game)
        {
            try
            {
                using(var sha256 = SHA256.Create())
                {
                    var metadata = $"{game.Id}|" +
                        $"{game.Name}|" +
                        $"{game.Description}|" +
                        $"{game.Added}|" +
                        $"{game.IsInstalled}|" +
                        $"{game.InstallDirectory}|" +
                        $"{game.CoverImage}|" +
                        $"{game.BackgroundImage}|" +
                        $"{game.Icon}|" +
                        $"{game.CompletionStatus.Id}|" +
                        $"{game.TagIds}|" +
                        $"{game.GenreIds}|" +
                        $"{game.CategoryIds}|" +
                        $"{game.FeatureIds}|";
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
    }
}

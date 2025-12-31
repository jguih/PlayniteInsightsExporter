using ExporterCommon.Application;
using ExporterCommon.Domain;
using ExporterCommon.Infra;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ExporterSystem.Infra
{
    public class HashService : IHashServicePort
    {
        private static readonly byte[] SEP = { 0 };

        private readonly IFileSystemServicePort fileSystemService;

        public HashService(
            IFileSystemServicePort fileSystemService
        )
        {
            this.fileSystemService = fileSystemService;
        }

        private byte[] Utf8(string s) => Encoding.UTF8.GetBytes(s);

        public string ComputeCanonicalHashForGameMediaFiles(string gameId, string contentHash, string mediaFolderPath)
        {
            using (var sha256 = SHA256.Create())
            {
                void AppendInfo(string data)
                {
                    if (data is null) return;

                    var dataBytes = Utf8(data);
                    sha256.TransformBlock(dataBytes, 0, dataBytes.Length, null, 0);
                    sha256.TransformBlock(SEP, 0, SEP.Length, null, 0);
                }

                AppendInfo(gameId);
                AppendInfo(contentHash);

                var files = fileSystemService.DirectoryGetFiles(mediaFolderPath)
                    .Select(path => new
                    {
                        Path = path,
                        Name = fileSystemService.PathGetFileName(path)
                    })
                    .OrderBy(f => f.Name, StringComparer.Ordinal);

                foreach (var file in files)
                {
                    AppendInfo(file.Name);

                    using (var stream = fileSystemService.FileOpenRead(file.Path))
                    {
                        var buffer = new byte[8192];
                        int read;

                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            sha256.TransformBlock(buffer, 0, read, null, 0);
                        }
                    }

                    sha256.TransformBlock(SEP, 0, SEP.Length, null, 0);
                }

                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return Convert.ToBase64String(sha256.Hash);
            }
        }

        public string ComputeHashFromFolderContents(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if (!fileSystemService.DirectoryExists(path))
                throw new DirectoryNotFoundException(path);

            var fileNames = fileSystemService.DirectoryGetFiles(path)
                .Select(fileSystemService.PathGetFileName)
                .OrderBy(n => n, StringComparer.Ordinal);

            using (var sha256 = SHA256.Create())
            {
                void AppendInfo(string data)
                {
                    if (data is null) return;

                    var dataBytes = Utf8(data);
                    sha256.TransformBlock(dataBytes, 0, dataBytes.Length, null, 0);
                    sha256.TransformBlock(SEP, 0, SEP.Length, null, 0);
                }

                foreach (var fileName in fileNames)
                {
                    var filePath = fileSystemService.PathCombine(path, fileName);

                    AppendInfo(fileName);

                    using (var stream = fileSystemService.FileOpenRead(filePath))
                    {
                        var buffer = new byte[8192];
                        int read;

                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            sha256.TransformBlock(buffer, 0, read, null, 0);
                        }
                    }

                    sha256.TransformBlock(SEP, 0, SEP.Length, null, 0);
                }

                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return Convert.ToBase64String(sha256.Hash);
            }
        }

        public string ComputeHashFromGame(AppGame game)
        {
            using (var sha256 = SHA256.Create())
            {
                void AppendInfo(string data)
                {
                    if (data is null) return;

                    var dataBytes = Utf8(data);
                    sha256.TransformBlock(dataBytes, 0, dataBytes.Length, null, 0);
                    sha256.TransformBlock(SEP, 0, SEP.Length, null, 0);
                }

                void AppendInfoArr(IEnumerable<string> dataArr)
                {
                    foreach (var data in dataArr.OrderBy(n => n, StringComparer.Ordinal))
                    {
                        AppendInfo(data);
                    }
                }

                AppendInfoArr(game.Developers.Select(d => d.Name));
                AppendInfoArr(game.Genres.Select(g => g.Name));
                AppendInfoArr(game.Platforms.Select(p => p.Name));
                AppendInfoArr(game.Publishers.Select(p => p.Name));

                AppendInfo(game.Playtime.ToString(CultureInfo.InvariantCulture));
                AppendInfo(
                    game.LastActivity.HasValue
                        ? game.LastActivity.Value.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture)
                        : ""
                );
                AppendInfo(game.Description?.Substring(0, Math.Min(100, game.Description.Length)) ?? "");
                AppendInfo(game.IsHidden ? "1" : "0");
                AppendInfo(game.IsInstalled ? "1" : "0");
                AppendInfo(game.CompletionStatus.Name);
                AppendInfo(game.BackgroundImage);
                AppendInfo(game.CoverImage);
                AppendInfo(game.Icon);

                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return Convert.ToBase64String(sha256.Hash);
            }
        }
    }
}

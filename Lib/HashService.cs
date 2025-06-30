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
                return "";
            }
        }
    }
}

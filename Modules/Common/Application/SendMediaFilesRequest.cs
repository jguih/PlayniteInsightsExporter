using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Application
{
    public enum MediaRole
    {
        Background,
        Cover,
        Icon
    }

    public class MediaFileDescriptor
    {
        public readonly MediaRole Role;
        public readonly string FullPath;

        public MediaFileDescriptor(MediaRole role, string fullPath)
        {
            Role = role;
            FullPath = fullPath;
        }
    }


    public class SendMediaFilesRequest
    {
        public readonly static string ENDPOINT = "/api/extension/sync/files";
        public readonly string GameId;
        public readonly string ContentHash;
        public readonly string CanonicalHash;
        public readonly IReadOnlyCollection<MediaFileDescriptor> MediaFiles;

        public SendMediaFilesRequest(string gameId, string contentHash, string canonicalHash, IReadOnlyCollection<MediaFileDescriptor> mediaFiles)
        {
            GameId = gameId;
            ContentHash = contentHash;
            CanonicalHash = canonicalHash;
            MediaFiles = mediaFiles;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Application
{
    public enum MediaRole
    {
        Background,
        Cover,
        Icon
    }

    public class MediaFileDescriptor
    {
        public MediaRole Role { get; set; }
        public string FullPath { get; set; }

        public MediaFileDescriptor() { }

        public MediaFileDescriptor(MediaRole role, string fullPath)
        {
            Role = role;
            FullPath = fullPath;
        }
    }

    public class SyncMediaFilesCommand
    {
        public string GameId { get; set; }
        public string ContentHash { get; set; }
        public string MediaFolderPath { get; set; }
        public IEnumerable<MediaFileDescriptor> MediaFiles { get; set; }

        public SyncMediaFilesCommand() { }

        public SyncMediaFilesCommand(
            string gameId,
            string contentHash,
            string mediaFolderPath,
            IEnumerable<MediaFileDescriptor> mediaFiles
        )
        {
            GameId = gameId;
            ContentHash = contentHash;
            MediaFolderPath = mediaFolderPath;
            MediaFiles = mediaFiles;
        }
    }
}

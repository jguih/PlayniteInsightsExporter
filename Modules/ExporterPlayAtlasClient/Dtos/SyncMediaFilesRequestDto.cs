using ExporterCommon.Application;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Dtos
{
    [JsonObject]
    public class SyncMediaFilesRequestDto : BaseRequestDto
    {
        public readonly static string ENDPOINT = "/api/extension/sync/files";
        public string GameId { get; set; }
        public string ContentHash { get; set; }
        public string CanonicalHash { get; set; }
        [JsonIgnore]
        public IEnumerable<MediaFileDescriptor> MediaFiles { get; set; }

        public SyncMediaFilesRequestDto() { }

        public SyncMediaFilesRequestDto(
            string gameId,
            string contentHash,
            string canonicalHash,
            IEnumerable<MediaFileDescriptor> mediaFiles
        )
        {
            GameId = gameId;
            ContentHash = contentHash;
            CanonicalHash = canonicalHash;
            MediaFiles = mediaFiles;
        }
    }
}

using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterPlayAtlasClient.Application;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Infra
{
    public interface ISyncMediaFilesHttpContentBuilder : IHttpContentBuilderPort<SyncMediaFilesRequestDto> { }

    public class SyncMediaFilesHttpContentBuilder : ISyncMediaFilesHttpContentBuilder
    {
        private readonly IFileSystemServicePort fileSystemService;

        public SyncMediaFilesHttpContentBuilder(IFileSystemServicePort fileSystemService)
        {
            this.fileSystemService = fileSystemService;
        }

        private static string MapRole(MediaRole role)
        {
            switch (role)
            {
                case MediaRole.Background:
                    return "background";

                case MediaRole.Cover:
                    return "cover";

                case MediaRole.Icon:
                    return "icon";

                default:
                    throw new InvalidOperationException(
                        "Unsupported media role: " + role);
            }
        }

        public HttpContent Build(SyncMediaFilesRequestDto requestDto)
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent(requestDto.GameId), "gameId" },
                { new StringContent(requestDto.ContentHash), "contentHash" }
            };
            foreach (var descriptor in requestDto.MediaFiles)
            {
                var extension = Path.GetExtension(descriptor.FullPath);

                if (string.Equals(extension, ".ico", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fileName = fileSystemService.PathGetFileName(descriptor.FullPath);
                var name = MapRole(descriptor.Role);

                var fileContent = new StreamContent(fileSystemService.FileOpenRead(descriptor.FullPath));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                content.Add(fileContent, name, fileName);
            }
            return content;
        }
    }
}

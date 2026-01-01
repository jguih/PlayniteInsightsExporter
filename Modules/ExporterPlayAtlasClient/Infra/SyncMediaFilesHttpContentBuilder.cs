using ExporterCommon.Application;
using ExporterCommon.Infra;
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
    public class SyncMediaFilesHttpContentBuilder : IHttpContentBuilderPort<SyncMediaFilesRequest>
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

        public HttpContent Build(SyncMediaFilesRequest request)
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent(request.GameId), "gameId" },
                { new StringContent(request.ContentHash), "contentHash" }
            };
            foreach (var descriptor in request.MediaFiles)
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

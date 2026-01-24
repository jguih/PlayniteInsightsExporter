using ExporterCommon.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Application
{
    public interface IHashServicePort
    {
        string ComputeSHA256Base64FromFolderContents(string path);
        string ComputeSHA256Base64(AppGame game);
        string ComputeCanonicalSHA256ForGameMediaFiles(
            string gameId,
            string contentHash,
            IEnumerable<MediaFileDescriptor> descriptors
        );
        string ComputeSHA256Base64(string input);
        string ComputeSHA256Hex(string gameId, DateTime startTime);
    }
}

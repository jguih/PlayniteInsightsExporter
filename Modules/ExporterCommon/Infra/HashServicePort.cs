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
        string ComputeHashFromFolderContents(string path);
        string ComputeHashFromGame(AppGame game);
        string ComputeCanonicalHashForGameMediaFiles(
            string gameId,
            string contentHash,
            string mediaFolderPath
        );
        string ComputeSHA256HashFromString(string input);
        string ComputeHashForGameSession(string gameId, DateTime startTime);
    }
}

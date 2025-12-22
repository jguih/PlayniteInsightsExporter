using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Application
{
    public interface IHashServicePort
    {
        string ComputeHashFromFolderContents(string path);
        string ComputeHashFromGame(Game game);
        string ComputeCanonicalHashForGameMediaFiles(
            string gameId,
            string contentHash,
            string mediaFolderPath
        );
    }
}

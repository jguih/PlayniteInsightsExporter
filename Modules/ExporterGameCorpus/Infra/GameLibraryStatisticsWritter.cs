using ExporterCommon.Infra;
using ExporterGameCorpus.Application;
using ExporterGameCorpus.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Infra
{
    public class GameLibraryStatisticsWritter : IGameLibraryStatisticsWritter
    {
        private readonly IGameLibraryStatisticsParser Parser;
        private readonly IFileSystemServicePort Fs;

        public GameLibraryStatisticsWritter(
            IGameLibraryStatisticsParser parser,
            IFileSystemServicePort fileSystemService
        )
        {
            Parser = parser;
            Fs = fileSystemService;
        }

        public void Write(GameLibraryStatistics stats, string path)
        {
            var statsJson = Parser.Serialize(stats);

            if (!string.IsNullOrWhiteSpace(path))
            {
                Fs.FileWriteAllText(path, statsJson);
            }
        }
    }
}

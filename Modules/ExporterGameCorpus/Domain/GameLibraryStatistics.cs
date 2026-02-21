using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Domain
{
    public class GameLibraryStatistics
    {
        public List<MiningExport> LibraryData { get; set; } = new List<MiningExport>();
    }
}

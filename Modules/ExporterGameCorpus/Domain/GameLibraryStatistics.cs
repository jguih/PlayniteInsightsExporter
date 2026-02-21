using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Domain
{
    public class GameLibraryStatistics
    {
        public string ClassificationId { get; set; }
        public int TotalPositives { get; set; }
        public int TotalNegatives { get; set; }
        public List<TokenStat> TopTextTokens { get; set; } = new List<TokenStat>();
        public List<TokenStat> TopTaxonomyTokens { get; set; } = new List<TokenStat>();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Domain
{
    public class LabeledCorpusDocument
    {
        public Guid PlayniteGameId { get; set; }
        public List<string> TextTokens { get; set; }
        public List<string> TaxonomyTokens { get; set; }
        public Dictionary<string, bool> Labels { get; set; }
    }
}

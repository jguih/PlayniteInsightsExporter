using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Domain
{
    public class PhraseStat
    {
        public string Phrase { get; set; }
        public int PositiveDocs { get; set; }
        public int NegativeDocs { get; set; }
        public double LogOdds { get; set; }
    }
}

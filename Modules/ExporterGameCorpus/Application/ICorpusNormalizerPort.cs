using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Application
{
    public interface ICorpusNormalizerPort
    {
        string NormalizeText(string input);
        List<string> NormalizeTaxonomy(IEnumerable<string> terms);
    }
}

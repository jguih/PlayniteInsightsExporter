using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Domain
{
    public class CorpusNormalizerOptions
    {
        public bool NormalizeAccents { get; set; } = true;
        public bool RemoveHtml { get; set; } = true;
        public bool DecodeHtmlEntities { get; set; } = true;
        public bool Lowercase { get; set; } = true;
    }
}

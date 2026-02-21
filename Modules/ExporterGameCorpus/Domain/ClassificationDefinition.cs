using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Domain
{
    public class ClassificationDefinition
    {
        public string Id { get; set; }
        public List<string> RequiredTags { get; set; } = new List<string>();
        public List<string> RequiredGenres { get; set; } = new List<string>();
        public List<string> ExcludedTags { get; set; } = new List<string>();
    }
}

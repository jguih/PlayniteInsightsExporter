using ExporterGameCorpus.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Application
{
    public interface ITextCorpusMinerPort
    {
        MiningExport Mine(List<LabeledCorpusDocument> labeledCorpus);
    }
}

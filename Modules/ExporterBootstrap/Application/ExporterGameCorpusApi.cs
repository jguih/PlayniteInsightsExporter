using ExporterGameCorpus.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application
{
    public class ExporterGameCorpusApi
    {
        public readonly ICorpusBuilderPort CorpusBuilder;
        public readonly ICorpusLabelerPort CorpusLabeler;
        public readonly ITextCorpusMinerPort TextCorpusMiner;

        public ExporterGameCorpusApi(
            ICorpusBuilderPort corpusBuilder,
            ICorpusLabelerPort corpusLabeler,
            ITextCorpusMinerPort textCorpusMiner
        )
        {
            CorpusBuilder = corpusBuilder;
            CorpusLabeler = corpusLabeler;
            TextCorpusMiner = textCorpusMiner;
        }
    }
}

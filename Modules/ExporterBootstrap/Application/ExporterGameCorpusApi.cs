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
        public readonly ICorpusMinerPort CorpusMiner;
        public readonly IGameLibraryStatisticsWritter StatisticsWritter;

        public ExporterGameCorpusApi(
            ICorpusBuilderPort corpusBuilder,
            ICorpusLabelerPort corpusLabeler,
            ICorpusMinerPort textCorpusMiner,
            IGameLibraryStatisticsWritter statisticsWritter
        )
        {
            CorpusBuilder = corpusBuilder;
            CorpusLabeler = corpusLabeler;
            CorpusMiner = textCorpusMiner;
            StatisticsWritter = statisticsWritter;
        }
    }
}

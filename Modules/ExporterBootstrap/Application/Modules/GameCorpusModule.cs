using ExporterCommon.Infra;
using ExporterGameCorpus.Application;
using ExporterGameCorpus.Domain;
using ExporterGameCorpus.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application.Modules
{
    public class GameCorpusModule : IGameCorpusModulePort
    {
        public ICorpusBuilderPort CorpusBuilder { get; }
        public ICorpusLabelerPort CorpusLabeler { get; }
        public ICorpusMinerPort TextCorpusMiner { get; }
        public IGameLibraryStatisticsParser StatisticsParser { get; }
        public IGameLibraryStatisticsWritter StatisticsWritter { get; }

        public GameCorpusModule(
            IFileSystemServicePort fileSystemService    
        ) 
        {
            var corpusNormalizer = new CorpusNormalizer(new CorpusNormalizerOptions());

            CorpusBuilder = new CorpusBuilder(corpusNormalizer);
            CorpusLabeler = new CorpusLabeler(ClassificationDefinitions.All);
            TextCorpusMiner = new CorpusMiner();
            StatisticsParser = new GameLibraryStatisticsParser();
            StatisticsWritter = new GameLibraryStatisticsWritter(StatisticsParser, fileSystemService);
        }
    }
}

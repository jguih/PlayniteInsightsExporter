using ExporterGameCorpus.Application;
using ExporterGameCorpus.Domain;
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
        public ITextCorpusMinerPort TextCorpusMiner { get; }

        public GameCorpusModule() 
        {
            var corpusNormalizer = new CorpusNormalizer(new CorpusNormalizerOptions());

            CorpusBuilder = new CorpusBuilder(corpusNormalizer);
            CorpusLabeler = new CorpusLabeler(ClassificationDefinitions.All);
            TextCorpusMiner = new TextCorpusMiner();
        }
    }
}

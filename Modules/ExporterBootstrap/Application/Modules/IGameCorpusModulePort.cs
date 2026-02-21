using ExporterGameCorpus.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application.Modules
{
    public interface IGameCorpusModulePort
    {
        ICorpusBuilderPort CorpusBuilder { get; }
        ICorpusLabelerPort CorpusLabeler { get; }
        ICorpusMinerPort TextCorpusMiner { get; }
        IGameLibraryStatisticsParser StatisticsParser { get; }
        IGameLibraryStatisticsWritter StatisticsWritter { get; }
    }
}

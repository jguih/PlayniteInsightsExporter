using ExporterGameCorpus.Application;
using ExporterGameCorpus.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Infra
{
    public class GameLibraryStatisticsParser : IGameLibraryStatisticsParser
    {
        public string Serialize(GameLibraryStatistics stats)
        {
            var content = JsonConvert.SerializeObject(stats, Formatting.Indented);
            return content;
        }
    }
}

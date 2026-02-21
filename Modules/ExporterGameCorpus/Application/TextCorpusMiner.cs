using ExporterGameCorpus.Domain;
using ExporterGameCorpus.Domain.ValueObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Application
{
    public class TextCorpusMiner : ITextCorpusMinerPort
    {
        public TextCorpusMiner() { }

        private Dictionary<string, int> CountDocumentFrequency(
            IEnumerable<LabeledCorpusDocument> items)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                var phrases = item.TextTokens.Distinct();

                foreach (var phrase in phrases)
                {
                    if (map.TryGetValue(phrase, out _))
                    {
                        map[phrase]++;
                    }
                    else
                    {
                        map[phrase] = 0;
                    }
                }
            }

            return map;
        }

        public MiningExport Mine(List<LabeledCorpusDocument> labeledCorpus)
        {
            var classificationId = ClassificationId.HORROR;

            var positives = labeledCorpus
                .Where(x => x.Labels.TryGetValue(classificationId, out var v) && v)
                .ToList();

            var negatives = labeledCorpus
                .Where(x => !x.Labels.TryGetValue(classificationId, out var v) || !v)
                .ToList();

            int totalPositives = positives.Count;
            int totalNegatives = negatives.Count;

            var rng = new Random(40);

            var usedNegatives = negatives
                .OrderBy(_ => rng.Next())
                .Take(totalPositives)
                .ToList();

            int usedNegativeCount = usedNegatives.Count;

            var posMap = CountDocumentFrequency(positives);
            var negMap = CountDocumentFrequency(usedNegatives);

            var allKeys = posMap.Keys.Union(negMap.Keys);

            var stats = new List<TokenStat>();

            foreach (var key in allKeys)
            {
                posMap.TryGetValue(key, out var pos);
                negMap.TryGetValue(key, out var neg);

                if (key.Length < 3) continue;

                stats.Add(new TokenStat
                {
                    Value = key,
                    Pos = pos,
                    Neg = neg
                });
            }

            stats = stats
                .Where(s => s.Pos >= 3)
                .ToList();

            double posTotal = totalPositives;
            double negTotal = usedNegativeCount;

            var ranked = stats
                .Select(s =>
                {
                    double posRate = s.Pos / posTotal;
                    double negRate = s.Neg / negTotal;

                    double lift = negRate == 0
                        ? double.MaxValue
                        : posRate / negRate;

                    return new
                    {
                        s.Value,
                        s.Pos,
                        s.Neg,
                        Lift = lift
                    };
                })
                .OrderByDescending(x => x.Lift)
                .Take(200)
                .ToList();

            return new MiningExport
            {
                ClassificationId = classificationId,
                TotalNegatives = totalNegatives,
                TotalPositives = totalPositives,
                UsedNegatives = usedNegativeCount,
                TopTextTokens = ranked
                    .Select(x => new TokenStat
                    {
                        Value = x.Value,
                        Pos = x.Pos,
                        Neg = x.Neg
                    })
                    .ToList()
            };
        }
    }
}

using ExporterGameCorpus.Domain;
using ExporterGameCorpus.Domain.ValueObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Application
{
    public class CorpusMiner : ICorpusMinerPort
    {
        private HashSet<string> stopwords = new HashSet<string> { "and", "the", "with", "for", "of", "in", "on", "a", "to", "is", "was" };

        public CorpusMiner() { }

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

        bool IsValidToken(string token)
        {
            if (token.All(char.IsDigit)) return false;

            if (token.Length < 3) return false;

            return true;
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

            double posTotal = totalPositives;
            double negTotal = usedNegativeCount;
            double minPosRate = 0.05; // 5%

            foreach (var key in allKeys)
            {
                if (!IsValidToken(key)) continue;
                if (stopwords.Contains(key)) continue;

                posMap.TryGetValue(key, out var pos);
                negMap.TryGetValue(key, out var neg);

                if (pos < 5) continue;

                double posRate = pos / posTotal;
                double negRate = neg / negTotal;
                double lift = negRate == 0 ? double.MaxValue : posRate / negRate;
                int nGramLength = key.Split(' ').Length;
                double lengthBoost = Math.Pow(nGramLength, 1.5);
                double weightedScore = lift * pos * lengthBoost;

                if (posRate < minPosRate) continue;
                if (key.Length < 3) continue;

                stats.Add(new TokenStat
                {
                    Value = key,
                    Pos = pos,
                    Neg = neg,
                    Score = weightedScore
                });
            }

            var ranked = stats
                .OrderByDescending(s => s.Score)
                .Take(200)
                .ToList();

            return new MiningExport
            {
                ClassificationId = classificationId,
                TotalNegatives = totalNegatives,
                TotalPositives = totalPositives,
                UsedNegatives = usedNegativeCount,
                TopTextTokens = ranked
            };
        }
    }
}

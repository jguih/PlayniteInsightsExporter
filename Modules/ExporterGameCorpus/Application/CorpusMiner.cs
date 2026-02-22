using ExporterGameCorpus.Domain;
using ExporterGameCorpus.Domain.ValueObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Application
{
    public class CorpusMiner : ICorpusMinerPort
    {
        public CorpusMiner() { }

        private enum DocumentType
        {
            TEXT,
            TAXONOMY
        }

        private Dictionary<string, int> CountDocumentFrequency(
            IEnumerable<LabeledCorpusDocument> items,
            DocumentType type = DocumentType.TEXT
        )
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                IEnumerable<string> tokens;

                switch (type)
                {
                    case DocumentType.TEXT:
                        tokens = item.TextTokens.Distinct();
                        break;
                    case DocumentType.TAXONOMY:
                        tokens = item.TaxonomyTokens.Distinct();
                        break;
                    default:
                        throw new ArgumentException("Unknown document type");
                }

                foreach (var token in tokens)
                {
                    if (map.TryGetValue(token, out _))
                    {
                        map[token]++;
                    }
                    else
                    {
                        map[token] = 1;
                    }
                }
            }

            return map;
        }

        private bool IsValidToken(string token)
        {
            if (token.All(char.IsDigit)) return false;

            if (token.Length < 3) return false;

            if (ExceptionsDictionary.STOP_WORDS.Contains(token)) return false;

            if (ExceptionsDictionary.FILLERS.Contains(token)) return false;

            var words = token.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            if (words.Any(w => ExceptionsDictionary.FILLERS.Contains(w))) return false;

            return true;
        }

        private List<TokenStat> BuildTokenStats(
            IEnumerable<string> tokens,
            Dictionary<string, int> docFrequencyMap,
            Dictionary<string, int> posMap,
            Dictionary<string, int> negMap,
            int totalPositives,
            int totalNegatives,
            int N,
            int take
        )
        {
            var stats = new List<TokenStat>();
            var tokenStats = new List<TokenStat>();

            foreach (var key in tokens)
            {
                if (!IsValidToken(key)) continue;

                posMap.TryGetValue(key, out var posFrequency);
                negMap.TryGetValue(key, out var negFrequency);
                docFrequencyMap.TryGetValue(key, out var docFrequency);

                if (posFrequency < 5) continue;
                if (docFrequency < 3) continue;

                double pPos = (posFrequency + 1.0) / (totalPositives + 2.0);
                double pNeg = (negFrequency + 1.0) / (totalNegatives + 2.0);

                double dp = Math.Log(pPos / pNeg);
                double idf = Math.Log((N + 1.0) / (docFrequency + 1.0)) + 1.0;
                double weightedScore = idf * dp;

                stats.Add(new TokenStat
                {
                    Value = key,
                    Pos = posFrequency,
                    Neg = negFrequency,
                    Score = weightedScore
                });
            }

            var ranked = stats
                .OrderByDescending(s => s.Score)
                .Take(take)
                .ToList();

            tokenStats.AddRange(ranked);

            return tokenStats;
        }

        private MiningExport BuildTokenStats(
            List<LabeledCorpusDocument> labeledCorpus,
            string classificationId,
            Dictionary<string, int> textTokenDocFrequencyMap,
            Dictionary<string, int> taxonomyTokenDocFrequencyMap
        )
        {
            int N = labeledCorpus.Count;

            var positives = labeledCorpus
                .Where(x => x.Labels.TryGetValue(classificationId, out var v) && v)
                .ToList();

            var negatives = labeledCorpus
                .Where(x => !x.Labels.TryGetValue(classificationId, out var v) || !v)
                .ToList();

            int totalPositives = positives.Count;
            int totalNegatives = negatives.Count;

            var rng = new Random(40);

            var textTokensPosMap = CountDocumentFrequency(positives);
            var textTokensNegMap = CountDocumentFrequency(negatives);

            var textTokenKeys = textTokensPosMap.Keys.Union(textTokensNegMap.Keys);

            var textTokens = BuildTokenStats(
                    textTokenKeys,
                    textTokenDocFrequencyMap,
                    textTokensPosMap,
                    textTokensNegMap,
                    totalPositives,
                    totalNegatives,
                    N,
                    80
                );

            var taxonomyTokensPosMap = CountDocumentFrequency(positives, DocumentType.TAXONOMY);
            var taxonomyTokensNegMap = CountDocumentFrequency(negatives, DocumentType.TAXONOMY);

            var taxonomyTokenKeys = taxonomyTokensPosMap.Keys.Union(taxonomyTokensNegMap.Keys);

            var taxonomyTokens = BuildTokenStats(
                    taxonomyTokenKeys,
                    taxonomyTokenDocFrequencyMap,
                    taxonomyTokensPosMap,
                    taxonomyTokensNegMap,
                    totalPositives,
                    totalNegatives,
                    N,
                    30
                );

            return new MiningExport
            {
                ClassificationId = classificationId,
                TotalNegatives = totalNegatives,
                TotalPositives = totalPositives,
                UsedNegatives = totalNegatives,
                TopTextTokens = textTokens,
                TopTaxonomyTokens = taxonomyTokens
            };
        }

        public GameLibraryStatistics Mine(List<LabeledCorpusDocument> labeledCorpus)
        {
            var classifications = new List<string>()
            {
                ClassificationId.RUN_BASED,
                ClassificationId.HORROR
            };

            var textTokenDocFrequencyMap = CountDocumentFrequency(labeledCorpus);
            var taxonomyTokenDocFrequencyMap = CountDocumentFrequency(labeledCorpus, DocumentType.TAXONOMY);
            var stats = new GameLibraryStatistics();

            foreach (var classification in classifications)
            {
                var miningExport = BuildTokenStats(
                        labeledCorpus,
                        classification,
                        textTokenDocFrequencyMap,
                        taxonomyTokenDocFrequencyMap
                    );
                stats.LibraryData.Add(miningExport);
            }

            return stats;
        }
    }
}

using ExporterCommon.Domain;
using ExporterGameCorpus.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Application
{
    public class CorpusBuilder : ICorpusBuilderPort
    {
        private readonly ICorpusNormalizerPort CorpusNormalizer;

        public CorpusBuilder(ICorpusNormalizerPort corpusNormalizer)
        {
            CorpusNormalizer = corpusNormalizer;
        }

        private List<string> BuildTaxonomyTokens(List<AppGenre> genres, List<AppTag> tags)
        {
            var list = new List<string>(genres.Count + tags.Count);

            genres.ForEach(gen => list.Add(gen.Name));
            tags.ForEach(tag => list.Add(tag.Name));

            var distinct = list.Distinct().ToList();
            return CorpusNormalizer.NormalizeTaxonomy(distinct);
        }

        private List<string> BuildTextTokens(string text)
        {
            var normalized = CorpusNormalizer.NormalizeText(text);

            var matches = Regex.Matches(normalized, @"\b[\w'-]+\b");
            var tokens = new List<string>();

            foreach (Match match in matches)
            {
                tokens.Add(match.Value);
            };

            var result = new List<string>(tokens);

            for (int i = 0; i < tokens.Count - 1; i++)
            {
                result.Add($"{tokens[i]} {tokens[i + 1]}");
            }

            for (int i = 0; i < tokens.Count - 2; i++)
            {
                result.Add($"{tokens[i]} {tokens[i + 1]} {tokens[i + 2]}");
            }

            for (int i = 0; i < tokens.Count - 3; i++)
            {
                result.Add($"{tokens[i]} {tokens[i + 1]} {tokens[i + 2]} {tokens[i + 3]}");
            }

            return result;
        }

        public List<CorpusDocument> Build(List<AppGame> rawGames)
        {
            return rawGames
                .Where(g => !string.IsNullOrWhiteSpace(g.Description))
                .Where(g => !g.IsHidden)
                .Select(g => new CorpusDocument
                {
                    PlayniteGameId = g.Id,
                    TextTokens = BuildTextTokens($"{g.Name}\n{g.Description}"),
                    TaxonomyTokens = BuildTaxonomyTokens(g.Genres, g.Tags)
                })
                .ToList();
        }
    }
}

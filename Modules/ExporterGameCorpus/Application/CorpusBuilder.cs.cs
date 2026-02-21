using ExporterCommon.Domain;
using ExporterGameCorpus.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Application
{
    public class CorpusBuilder : ICorpusBuilderPort
    {
        private readonly ICorpusNormalizer CorpusNormalizer;

        public CorpusBuilder(ICorpusNormalizer corpusNormalizer)
        {
            CorpusNormalizer = corpusNormalizer;
        }

        private List<string> MergeAndNormalizeTaxonomy(List<AppGenre> genres, List<AppTag> tags)
        {
            var list = new List<string>(genres.Count + tags.Count);

            genres.ForEach(gen => list.Add(gen.Name));
            tags.ForEach(tag => list.Add(tag.Name));

            var distinct = list.Distinct().ToList();
            return CorpusNormalizer.NormalizeTaxonomy(distinct);
        }

        public List<CorpusDocument> Build(List<AppGame> rawGames)
        {
            return rawGames
                .Where(g => !string.IsNullOrWhiteSpace(g.Description))
                .Select(g => new CorpusDocument
                {
                    PlayniteGameId = g.Id,
                    TextContent = CorpusNormalizer.NormalizeText($"{g.Name}\n{g.Description}"),
                    TaxonomyTerms = MergeAndNormalizeTaxonomy(g.Genres, g.Tags)
                })
                .ToList();
        }
    }
}

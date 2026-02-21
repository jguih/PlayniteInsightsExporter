using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExporterGameCorpus.Domain;

namespace ExporterGameCorpus.Application
{
    public class CorpusLabeler : ICorpusLabelerPort
    {
        private readonly List<ClassificationDefinition> Definitions;

        public CorpusLabeler(List<ClassificationDefinition> definitions)
        {
            Definitions = definitions;
        }

        public List<LabeledCorpusDocument> ApplyLabels
        (
            List<CorpusDocument> corpus
        )
        {
            var labeled = corpus.Select(doc =>
            {
                var labels = new Dictionary<string, bool>();

                foreach (var def in Definitions)
                {
                    labels[def.Id] = Matches(doc, def);
                }

                return new LabeledCorpusDocument
                {
                    PlayniteGameId = doc.PlayniteGameId,
                    TextTokens = doc.TextTokens,
                    TaxonomyTokens = doc.TaxonomyTokens,
                    Labels = labels
                };
            }).ToList();
            return labeled;
        }

        private bool Matches(
            CorpusDocument doc,
            ClassificationDefinition def)
        {
            bool tagMatch = def.RequiredTags.Any(tag =>
                doc.TaxonomyTokens.Contains(tag));

            bool genreMatch = def.RequiredGenres.Any(genre =>
                doc.TaxonomyTokens.Contains(genre));

            return tagMatch || genreMatch;
        }
    }
}

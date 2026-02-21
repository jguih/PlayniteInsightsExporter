using ExporterGameCorpus.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Application
{
    public class CorpusNormalizer : ICorpusNormalizer
    {
        private readonly CorpusNormalizerOptions Options;

        public CorpusNormalizer(CorpusNormalizerOptions options)
        {
            Options = options;
        }

        public string NormalizeText(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var text = input;

            if (Options.RemoveHtml)
                text = RemoveHtml(text);

            if (Options.DecodeHtmlEntities)
                text = WebUtility.HtmlDecode(text);

            if (Options.NormalizeAccents)
                text = RemoveDiacritics(text);

            if (Options.Lowercase)
                text = text.ToLowerInvariant();

            text = NormalizePunctuation(text);
            text = CollapseWhitespace(text);

            return text.Trim();
        }

        public List<string> NormalizeTaxonomy(IEnumerable<string> terms)
        {
            return terms
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t =>
                {
                    var term = t.Trim();

                    if (Options.NormalizeAccents)
                        term = RemoveDiacritics(term);

                    if (Options.Lowercase)
                        term = term.ToLowerInvariant();

                    return term;
                })
                .Distinct()
                .ToList();
        }

        private string RemoveHtml(string input)
        {
            return Regex.Replace(input, "<.*?>", " ");
        }

        private string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private string NormalizePunctuation(string text)
        {
            return Regex.Replace(text, @"[^\p{L}\p{Nd}]+", " ");
        }

        private string CollapseWhitespace(string text)
        {
            return Regex.Replace(text, @"\s+", " ");
        }
    }
}

using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.ProductExtraction.Sections;
using HighConvertAI.Server.Services.ProductExtraction.Abstractions;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace HighConvertAI.Server.Services.ProductExtraction.Extractors
{
    public sealed class FaqExtractor : IProductSectionExtractor
    {
        public int Order => 90;

        public void Extract(ProductExtractionContext context)
        {
            var doc = context.Document;

            var faqItems = new List<FaqItem>();

            var detailsNodes = doc.DocumentNode.SelectNodes("//details")?.ToList()
                               ?? new List<HtmlNode>();

            foreach (var detail in detailsNodes)
            {
                var question = CleanText(detail.SelectSingleNode(".//summary")?.InnerText);
                var answer = CleanText(detail.InnerText);

                if (!string.IsNullOrWhiteSpace(question))
                {
                    faqItems.Add(new FaqItem
                    {
                        Question = question,
                        Answer = answer
                    });
                }
            }

            var jsonLdFaqs = TryExtractJsonLdFaqs(doc);
            faqItems.AddRange(jsonLdFaqs);

            context.Faq = new FaqSection
            {
                HasFaq = faqItems.Count > 0,
                Items = faqItems
                    .DistinctBy(x => x.Question)
                    .ToList()
            };
        }

        private static List<FaqItem> TryExtractJsonLdFaqs(HtmlDocument doc)
        {
            var result = new List<FaqItem>();

            var scripts = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
            if (scripts == null)
                return result;

            foreach (var script in scripts)
            {
                var json = script.InnerText;

                if (!json.Contains("FAQPage", StringComparison.OrdinalIgnoreCase))
                    continue;

                var questionMatches = Regex.Matches(json, @"""name""\s*:\s*""(?<q>[^""]+)""");
                var answerMatches = Regex.Matches(json, @"""text""\s*:\s*""(?<a>[^""]+)""");

                for (var i = 0; i < questionMatches.Count; i++)
                {
                    result.Add(new FaqItem
                    {
                        Question = questionMatches[i].Groups["q"].Value,
                        Answer = i < answerMatches.Count
                            ? answerMatches[i].Groups["a"].Value
                            : null
                    });
                }
            }

            return result;
        }

        private static string? CleanText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Regex.Replace(value, @"\s+", " ").Trim();
        }
    }
}

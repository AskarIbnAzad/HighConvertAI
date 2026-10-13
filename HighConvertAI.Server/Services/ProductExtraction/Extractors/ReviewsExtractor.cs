using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.ProductExtraction.Sections;
using HighConvertAI.Server.Services.ProductExtraction.Abstractions;
using HtmlAgilityPack;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HighConvertAI.Server.Services.ProductExtraction.Extractors
{
    public sealed class ReviewsExtractor : IProductSectionExtractor
    {
        public int Order => 80;

        public void Extract(ProductExtractionContext context)
        {
            var doc = context.Document;

            var jsonLdRating = TryExtractJsonLdRating(doc, out var reviewCount);

            var bodyText = CleanText(doc.DocumentNode.SelectSingleNode("//body")?.InnerText);

            var domRating = jsonLdRating ?? TryParseRating(bodyText);
            var domReviewCount = reviewCount ?? TryParseReviewCount(bodyText);

            context.Reviews = new ReviewsSection
            {
                HasReviews = domRating.HasValue || domReviewCount.HasValue,
                Rating = domRating,
                ReviewCount = domReviewCount,
                HasStarRating = bodyText?.Contains("star", StringComparison.OrdinalIgnoreCase) == true
            };
        }

        private static decimal? TryExtractJsonLdRating(HtmlDocument doc, out int? reviewCount)
        {
            reviewCount = null;

            var scripts = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
            if (scripts == null)
                return null;

            foreach (var script in scripts)
            {
                var json = script.InnerText;

                var ratingMatch = Regex.Match(json, @"""ratingValue""\s*:\s*""?(?<rating>\d+(\.\d+)?)""?");
                var countMatch = Regex.Match(json, @"""reviewCount""\s*:\s*""?(?<count>\d+)""?");

                if (countMatch.Success && int.TryParse(countMatch.Groups["count"].Value, out var parsedCount))
                    reviewCount = parsedCount;

                if (ratingMatch.Success &&
                    decimal.TryParse(ratingMatch.Groups["rating"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rating))
                    return rating;
            }

            return null;
        }

        private static decimal? TryParseRating(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var match = Regex.Match(text, @"(?<rating>[1-5](\.\d)?)\s*(out of|/)\s*5", RegexOptions.IgnoreCase);

            if (match.Success &&
                decimal.TryParse(match.Groups["rating"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rating))
                return rating;

            return null;
        }

        private static int? TryParseReviewCount(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var match = Regex.Match(text, @"(?<count>\d+)\s*(reviews|review)", RegexOptions.IgnoreCase);

            if (match.Success && int.TryParse(match.Groups["count"].Value, out var count))
                return count;

            return null;
        }

        private static string? CleanText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Regex.Replace(value, @"\s+", " ").Trim();
        }
    }
}

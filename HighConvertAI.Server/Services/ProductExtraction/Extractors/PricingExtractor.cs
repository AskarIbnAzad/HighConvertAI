using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.ProductExtraction.Sections;
using HighConvertAI.Server.Services.ProductExtraction.Abstractions;
using HtmlAgilityPack;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HighConvertAI.Server.Services.ProductExtraction.Extractors
{
    public sealed class PricingExtractor : IProductSectionExtractor
    {
        public int Order => 20;

        public void Extract(ProductExtractionContext context)
        {
            var document = context.Document;

            var jsonLdPrice = TryGetJsonLdPrice(document, out var jsonLdCurrency);
            var metaPrice = TryGetMetaPrice(document, out var metaCurrency);
            var domPriceText = ExtractDomPriceText(document);

            decimal? currentPrice =
                jsonLdPrice
                ?? metaPrice
                ?? ParsePrice(domPriceText);

            decimal? compareAtPrice = TryExtractCompareAtPrice(document);

            var currency =
                jsonLdCurrency
                ?? metaCurrency
                ?? DetectCurrency(domPriceText);

            var isOnSale =
                compareAtPrice.HasValue
                && currentPrice.HasValue
                && compareAtPrice.Value > currentPrice.Value;

            context.Pricing = new PricingSection
            {
                CurrentPrice = currentPrice,
                CompareAtPrice = compareAtPrice,
                Currency = currency,
                IsOnSale = isOnSale,
                PriceTextRaw = domPriceText
            };

            if (currentPrice.HasValue)
            {
                context.Evidence.Add(new ExtractionEvidence
                {
                    Section = "Pricing",
                    SourceType = jsonLdPrice.HasValue ? "json-ld" : metaPrice.HasValue ? "meta" : "dom",
                    SelectorOrPath = jsonLdPrice.HasValue
                        ? "jsonld.offers.price"
                        : metaPrice.HasValue
                            ? "meta[property='product:price:amount']"
                            : "price dom candidate",
                    ExtractedValue = currentPrice.Value.ToString(CultureInfo.InvariantCulture)
                });
            }
            else
            {
                context.Issues.Add(new ExtractionIssue
                {
                    Code = "PRICE_NOT_FOUND",
                    Message = "Could not extract product price.",
                    Severity = "Warning"
                });
            }

            if (isOnSale)
            {
                context.Evidence.Add(new ExtractionEvidence
                {
                    Section = "Pricing",
                    SourceType = "dom",
                    SelectorOrPath = "compare-at / sale price candidate",
                    ExtractedValue = $"Sale detected. Current: {currentPrice}, CompareAt: {compareAtPrice}"
                });
            }
        }

        private static string? ExtractDomPriceText(HtmlDocument document)
        {
            var candidates = document.DocumentNode
                .SelectNodes("//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'price') " +
                             "or contains(translate(@id,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'price') " +
                             "or contains(translate(@data-testid,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'price')]")
                ?.Select(x => CleanText(x.InnerText))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (candidates == null || candidates.Count == 0)
                return null;

            var best = candidates
                .OrderByDescending(GetPriceTextScore)
                .FirstOrDefault();

            return best;
        }

        private static int GetPriceTextScore(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            var score = 0;

            if (Regex.IsMatch(text, @"[$৳€£₹]\s*\d", RegexOptions.IgnoreCase))
                score += 50;

            if (Regex.IsMatch(text, @"\d+([.,]\d{1,2})?"))
                score += 30;

            if (text.Contains("sale", StringComparison.OrdinalIgnoreCase))
                score += 10;

            if (text.Contains("regular", StringComparison.OrdinalIgnoreCase))
                score += 5;

            if (text.Length > 120)
                score -= 30;

            return score;
        }

        private static decimal? TryGetJsonLdPrice(HtmlDocument document, out string? currency)
        {
            currency = null;

            var scripts = document.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
            if (scripts == null)
                return null;

            foreach (var script in scripts)
            {
                var json = script.InnerText;
                if (string.IsNullOrWhiteSpace(json))
                    continue;

                var currencyMatch = Regex.Match(
                    json,
                    @"""priceCurrency""\s*:\s*""(?<currency>[A-Z]{3})""",
                    RegexOptions.IgnoreCase);

                if (currencyMatch.Success)
                    currency = currencyMatch.Groups["currency"].Value;

                var priceMatch = Regex.Match(
                    json,
                    @"""price""\s*:\s*""?(?<price>\d+([.,]\d+)?)""?",
                    RegexOptions.IgnoreCase);

                if (priceMatch.Success)
                {
                    var raw = priceMatch.Groups["price"].Value.Replace(",", ".");

                    if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                        return price;
                }
            }

            return null;
        }

        private static decimal? TryGetMetaPrice(HtmlDocument document, out string? currency)
        {
            currency = null;

            var amountNode = document.DocumentNode.SelectSingleNode(
                "//meta[@property='product:price:amount'] | " +
                "//meta[@property='og:price:amount'] | " +
                "//meta[@name='twitter:data1']");

            var currencyNode = document.DocumentNode.SelectSingleNode(
                "//meta[@property='product:price:currency'] | " +
                "//meta[@property='og:price:currency']");

            currency = currencyNode?.GetAttributeValue("content", null);

            var raw = amountNode?.GetAttributeValue("content", null);

            return ParsePrice(raw);
        }

        private static decimal? TryExtractCompareAtPrice(HtmlDocument document)
        {
            var compareNode = document.DocumentNode.SelectSingleNode(
                "//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'compare') " +
                "or contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'was-price') " +
                "or contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'regular-price') " +
                "or contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'old-price') " +
                "or contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'original-price') " +
                "or contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'price--compare') " +
                "or contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'price__compare') " +
                "or contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'money--last')]");

            var compareText = CleanText(compareNode?.InnerText);

            return ParsePrice(compareText);
        }

        private static string? DetectCurrency(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (text.Contains("৳"))
                return "BDT";

            if (text.Contains("$"))
                return "USD";

            if (text.Contains("€"))
                return "EUR";

            if (text.Contains("£"))
                return "GBP";

            if (text.Contains("₹"))
                return "INR";

            var codeMatch = Regex.Match(text, @"\b(USD|BDT|EUR|GBP|INR|CAD|AUD|AED|SAR)\b", RegexOptions.IgnoreCase);

            return codeMatch.Success
                ? codeMatch.Groups[1].Value.ToUpperInvariant()
                : null;
        }

        private static decimal? ParsePrice(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            text = text.Replace("&nbsp;", " ");

            var matches = Regex.Matches(text, @"\d[\d,]*(\.\d{1,2})?");

            if (matches.Count == 0)
                return null;

            foreach (Match match in matches)
            {
                var cleaned = match.Value.Replace(",", "");

                if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                {
                    if (value > 0)
                        return value;
                }
            }

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
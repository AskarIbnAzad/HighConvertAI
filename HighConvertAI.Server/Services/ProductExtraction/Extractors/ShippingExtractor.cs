using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.ProductExtraction.Sections;
using HighConvertAI.Server.Services.ProductExtraction.Abstractions;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace HighConvertAI.Server.Services.ProductExtraction.Extractors
{
    public sealed class ShippingExtractor : IProductSectionExtractor
    {
        public int Order => 60;

        private static readonly string[] ShippingKeywords =
        {
        "shipping",
        "delivery",
        "free shipping",
        "returns",
        "return policy",
        "refund",
        "cash on delivery",
        "cod",
        "estimated delivery"
    };

        public void Extract(ProductExtractionContext context)
        {
            var doc = context.Document;

            var nodes = doc.DocumentNode
                .SelectNodes("//*[self::div or self::section or self::p or self::span or self::li]")
                ?.Where(x => ContainsAny(CleanText(x.InnerText), ShippingKeywords))
                .Take(10)
                .ToList() ?? new List<HtmlNode>();

            var texts = nodes
                .Select(x => CleanText(x.InnerText))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            context.Shipping = new ShippingSection
            {
                HasShippingInfo = texts.Any(x => ContainsAny(x, "shipping", "delivery")),
                HasFreeShipping = texts.Any(x => ContainsAny(x, "free shipping")),
                HasReturnPolicy = texts.Any(x => ContainsAny(x, "return", "refund")),
                HasCashOnDelivery = texts.Any(x => ContainsAny(x, "cash on delivery", "cod")),
                ShippingTexts = texts
            };

            if (texts.Count == 0)
            {
                context.Issues.Add(new ExtractionIssue
                {
                    Code = "SHIPPING_INFO_NOT_FOUND",
                    Message = "Shipping or delivery information could not be detected.",
                    Severity = "Info"
                });
            }
        }

        private static bool ContainsAny(string? text, params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        private static string? CleanText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Regex.Replace(value, @"\s+", " ").Trim();
        }
    }
}

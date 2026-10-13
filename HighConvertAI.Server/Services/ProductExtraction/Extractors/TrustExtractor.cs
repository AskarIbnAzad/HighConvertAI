using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.ProductExtraction.Sections;
using HighConvertAI.Server.Services.ProductExtraction.Abstractions;
using System.Text.RegularExpressions;

namespace HighConvertAI.Server.Services.ProductExtraction.Extractors
{
    public sealed class TrustExtractor : IProductSectionExtractor
    {
        public int Order => 70;

        private static readonly string[] TrustKeywords =
        {
        "secure checkout",
        "money back",
        "guarantee",
        "warranty",
        "authentic",
        "trusted",
        "safe payment",
        "ssl",
        "visa",
        "mastercard",
        "paypal",
        "shop pay"
    };

        public void Extract(ProductExtractionContext context)
        {
            var doc = context.Document;

            var bodyText = CleanText(doc.DocumentNode.SelectSingleNode("//body")?.InnerText);

            context.Trust = new TrustSection
            {
                HasTrustBadge = ContainsAny(bodyText, TrustKeywords),
                HasGuarantee = ContainsAny(bodyText, "guarantee", "money back"),
                HasWarranty = ContainsAny(bodyText, "warranty"),
                HasSecurePayment = ContainsAny(bodyText, "secure checkout", "safe payment", "ssl"),
                PaymentMethods = ExtractPaymentMethods(bodyText)
            };
        }

        private static List<string> ExtractPaymentMethods(string? text)
        {
            var methods = new List<string>();

            if (ContainsAny(text, "visa")) methods.Add("Visa");
            if (ContainsAny(text, "mastercard")) methods.Add("Mastercard");
            if (ContainsAny(text, "paypal")) methods.Add("PayPal");
            if (ContainsAny(text, "shop pay")) methods.Add("Shop Pay");
            if (ContainsAny(text, "apple pay")) methods.Add("Apple Pay");
            if (ContainsAny(text, "google pay")) methods.Add("Google Pay");

            return methods.Distinct().ToList();
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

using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.ProductExtraction.Sections;
using HighConvertAI.Server.Services.ProductExtraction.Abstractions;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace HighConvertAI.Server.Services.ProductExtraction.Extractors
{
    public sealed class DescriptionExtractor : IProductSectionExtractor
    {
        public int Order => 40;

        public void Extract(ProductExtractionContext context)
        {
            var document = context.Document;

            var descriptionNode = FindDescriptionNode(document);

            var descriptionText = CleanText(descriptionNode?.InnerText);

            var bulletPoints = ExtractBulletPoints(descriptionNode);

            context.Description = new DescriptionSection
            {
                HasDescription = !string.IsNullOrWhiteSpace(descriptionText),
                DescriptionText = descriptionText,
                DescriptionWordCount = CountWords(descriptionText),

                BulletPoints = bulletPoints,

                HasTabsOrAccordion = PageHasTabsOrAccordion(document),

                HasIngredients = ContainsAny(
                    descriptionText,
                    "ingredient",
                    "ingredients",
                    "composition"
                ),

                HasSpecifications = ContainsAny(
                    descriptionText,
                    "specification",
                    "specifications",
                    "details",
                    "features",
                    "material",
                    "size",
                    "weight",
                    "dimension",
                    "dimensions"
                ),

                HasHowToUse = ContainsAny(
                    descriptionText,
                    "how to use",
                    "usage",
                    "directions",
                    "instruction",
                    "instructions",
                    "apply",
                    "application"
                )
            };

            if (!string.IsNullOrWhiteSpace(descriptionText))
            {
                context.Evidence.Add(new ExtractionEvidence
                {
                    Section = "Description",
                    SourceType = "dom",
                    SelectorOrPath = descriptionNode?.XPath,
                    ExtractedValue = descriptionText.Length > 300
                        ? descriptionText[..300]
                        : descriptionText
                });
            }
            else
            {
                context.Issues.Add(new ExtractionIssue
                {
                    Code = "DESCRIPTION_NOT_FOUND",
                    Message = "Product description could not be detected.",
                    Severity = "Warning"
                });
            }
        }

        private static HtmlNode? FindDescriptionNode(HtmlDocument document)
        {
            return document.DocumentNode.SelectSingleNode(
                "//*[@itemprop='description']" +

                " | //*[@id='ProductDescription']" +
                " | //*[@id='product-description']" +
                " | //*[@id='description']" +

                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product__description')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-description')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product_description')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'description')]" +

                " | //section[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product') " +
                "and contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'description')]" +

                " | //div[contains(translate(@data-section-type,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-description')]"
            );
        }

        private static List<string> ExtractBulletPoints(HtmlNode? descriptionNode)
        {
            if (descriptionNode == null)
                return new List<string>();

            return descriptionNode
                .SelectNodes(".//li")
                ?.Select(x => CleanText(x.InnerText))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();
        }

        private static bool PageHasTabsOrAccordion(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode(
                "//details" +

                " | //*[@aria-expanded]" +

                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'accordion')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'collapsible')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'collapse')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'tab')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'drawer')]"
            );

            return node != null;
        }

        private static int CountWords(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return Regex
                .Split(text.Trim(), @"\s+")
                .Count(x => !string.IsNullOrWhiteSpace(x));
        }

        private static bool ContainsAny(string? text, params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return keywords.Any(keyword =>
                text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private static string? CleanText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Regex.Replace(value, @"\s+", " ").Trim();
        }
    }
}
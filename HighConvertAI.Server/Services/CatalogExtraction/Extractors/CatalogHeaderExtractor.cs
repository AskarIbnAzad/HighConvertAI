using HighConvertAI.Server.Models.CatalogExtraction;
using HighConvertAI.Server.Models.CatalogExtraction.Sections;
using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Services.CatalogExtraction.Abstractions;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace HighConvertAI.Server.Services.CatalogExtraction.Extractors
{
    
    public sealed class CatalogHeaderExtractor : ICatalogSectionExtractor
    {
        public int Order => 10;

        public void Extract(CatalogExtractionContext context)
        {
            var doc = context.Document;

            var title =
                GetNodeText(doc.DocumentNode.SelectSingleNode("//main//h1")) ??
                GetNodeText(doc.DocumentNode.SelectSingleNode("//h1")) ??
                GetMetaContent(doc, "property", "og:title");

            var description =
                GetNodeText(doc.DocumentNode.SelectSingleNode("//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'collection__description')]")) ??
                GetNodeText(doc.DocumentNode.SelectSingleNode("//*[@data-collection-description]")) ??
                GetNodeText(doc.DocumentNode.SelectSingleNode("//main//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'description')]"));

            var breadcrumbText =
                GetNodeText(doc.DocumentNode.SelectSingleNode("//nav[contains(translate(@aria-label,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'breadcrumb')]")) ??
                GetNodeText(doc.DocumentNode.SelectSingleNode("//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'breadcrumb')]"));

            var resultCountText =
                GetNodeText(doc.DocumentNode.SelectSingleNode("//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'result-count')]")) ??
                GetNodeText(doc.DocumentNode.SelectSingleNode("//*[@data-result-count]"));

            context.Header = new CatalogHeaderSection
            {
                Title = title,
                Description = description,
                BreadcrumbText = breadcrumbText,
                ResultCountText = resultCountText
            };

            if (!string.IsNullOrWhiteSpace(title))
            {
                context.Evidence.Add(new ExtractionEvidence
                {
                    Section = "CatalogHeader",
                    SourceType = "dom/meta",
                    SelectorOrPath = "h1 / og:title",
                    ExtractedValue = title
                });
            }
            else
            {
                context.Issues.Add(new ExtractionIssue
                {
                    Code = "CATALOG_TITLE_NOT_FOUND",
                    Message = "Catalog title could not be extracted.",
                    Severity = "Warning"
                });
            }
        }

        private static string? GetMetaContent(HtmlDocument doc, string attributeName, string attributeValue)
        {
            var xpath =
                $"//meta[translate(@{attributeName}, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{attributeValue.ToLowerInvariant()}']";

            var node = doc.DocumentNode.SelectSingleNode(xpath);
            return CleanText(node?.GetAttributeValue("content", null));
        }

        private static string? GetNodeText(HtmlNode? node)
        {
            return CleanText(node?.InnerText);
        }

        private static string? CleanText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var cleaned = HtmlEntity.DeEntitize(value);
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
        }
        
    }
}

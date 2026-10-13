using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.ProductExtraction.Sections;
using HighConvertAI.Server.Services.ProductExtraction.Abstractions;
using HtmlAgilityPack;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HighConvertAI.Server.Services.ProductExtraction.Extractors
{

    public sealed class BasicInfoExtractor : IProductSectionExtractor
    {
        public int Order => 10;

        public void Extract(ProductExtractionContext context)
        {
            var doc = context.Document;

            var jsonLdProduct = GetProductJsonLd(doc);

            var title =
                GetJsonLdString(jsonLdProduct, "name") ??
                GetMetaContent(doc, "property", "og:title") ??
                GetMetaContent(doc, "name", "twitter:title") ??
                GetH1Text(doc);

            var brand =
                GetJsonLdBrand(jsonLdProduct) ??
                GetMetaContent(doc, "property", "product:brand") ??
                GetTextByClassContains(doc, "vendor") ??
                GetTextByClassContains(doc, "brand");

            var productType =
                GetJsonLdString(jsonLdProduct, "category") ??
                GetTextByClassContains(doc, "type") ??
                GetTextByClassContains(doc, "product-type");

            var subtitle =
                GetTextBySelectors(doc,
                    "//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'subtitle')]",
                    "//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'sub-title')]",
                    "//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product__subtitle')]",
                    "//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-subtitle')]",
                    "//*[contains(translate(@data-testid,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'subtitle')]"
                );

            context.BasicInfo = new BasicInfoSection
            {
                ProductTitle = CleanText(title),
                Subtitle = CleanText(subtitle),
                Brand = CleanText(brand),
                ProductType = CleanText(productType)
            };

            if (!string.IsNullOrWhiteSpace(title))
            {
                context.Evidence.Add(new ExtractionEvidence
                {
                    Section = "BasicInfo",
                    SourceType = jsonLdProduct is not null ? "json-ld/dom" : "dom/meta",
                    SelectorOrPath = "name / og:title / h1",
                    ExtractedValue = CleanText(title)
                });
            }
            else
            {
                context.Issues.Add(new ExtractionIssue
                {
                    Code = "TITLE_NOT_FOUND",
                    Message = "Product title could not be extracted.",
                    Severity = "Warning"
                });
            }

            if (!string.IsNullOrWhiteSpace(brand))
            {
                context.Evidence.Add(new ExtractionEvidence
                {
                    Section = "BasicInfo",
                    SourceType = jsonLdProduct is not null ? "json-ld" : "dom/meta",
                    SelectorOrPath = "brand / vendor / product:brand",
                    ExtractedValue = CleanText(brand)
                });
            }

            if (!string.IsNullOrWhiteSpace(productType))
            {
                context.Evidence.Add(new ExtractionEvidence
                {
                    Section = "BasicInfo",
                    SourceType = jsonLdProduct is not null ? "json-ld" : "dom",
                    SelectorOrPath = "category / type",
                    ExtractedValue = CleanText(productType)
                });
            }

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                context.Evidence.Add(new ExtractionEvidence
                {
                    Section = "BasicInfo",
                    SourceType = "dom",
                    SelectorOrPath = "subtitle selectors",
                    ExtractedValue = CleanText(subtitle)
                });
            }
        }

        private static JsonElement? GetProductJsonLd(HtmlDocument doc)
        {
            var scriptNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
            if (scriptNodes == null || scriptNodes.Count == 0)
                return null;

            foreach (var scriptNode in scriptNodes)
            {
                var json = scriptNode.InnerText?.Trim();
                if (string.IsNullOrWhiteSpace(json))
                    continue;

                try
                {
                    using var document = JsonDocument.Parse(json);

                    var root = document.RootElement;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (IsProductNode(root))
                            return root.Clone();

                        if (root.TryGetProperty("@graph", out var graph) && graph.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in graph.EnumerateArray())
                            {
                                if (IsProductNode(item))
                                    return item.Clone();
                            }
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in root.EnumerateArray())
                        {
                            if (IsProductNode(item))
                                return item.Clone();
                        }
                    }
                }
                catch
                {
                    // ignore invalid json-ld block
                }
            }

            return null;
        }

        private static bool IsProductNode(JsonElement element)
        {
            if (!element.TryGetProperty("@type", out var typeProperty))
                return false;

            if (typeProperty.ValueKind == JsonValueKind.String)
            {
                return string.Equals(typeProperty.GetString(), "Product", StringComparison.OrdinalIgnoreCase);
            }

            if (typeProperty.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in typeProperty.EnumerateArray())
                {
                    if (string.Equals(item.GetString(), "Product", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        private static string? GetJsonLdString(JsonElement? productNode, string propertyName)
        {
            if (productNode is null)
                return null;

            if (!productNode.Value.TryGetProperty(propertyName, out var property))
                return null;

            return property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetRawText(),
                _ => null
            };
        }

        private static string? GetJsonLdBrand(JsonElement? productNode)
        {
            if (productNode is null)
                return null;

            if (!productNode.Value.TryGetProperty("brand", out var brandNode))
                return null;

            if (brandNode.ValueKind == JsonValueKind.String)
                return brandNode.GetString();

            if (brandNode.ValueKind == JsonValueKind.Object)
            {
                if (brandNode.TryGetProperty("name", out var nameNode) && nameNode.ValueKind == JsonValueKind.String)
                    return nameNode.GetString();
            }

            return null;
        }

        private static string? GetMetaContent(HtmlDocument doc, string attributeName, string attributeValue)
        {
            var xpath =
                $"//meta[translate(@{attributeName}, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{attributeValue.ToLowerInvariant()}']";

            var node = doc.DocumentNode.SelectSingleNode(xpath);
            return CleanText(node?.GetAttributeValue("content", null));
        }

        private static string? GetH1Text(HtmlDocument doc)
        {
            var node = doc.DocumentNode.SelectSingleNode("//main//h1") ??
                       doc.DocumentNode.SelectSingleNode("//h1");

            return CleanText(node?.InnerText);
        }

        private static string? GetTextByClassContains(HtmlDocument doc, string classPart)
        {
            var xpath =
                $"//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'{classPart.ToLowerInvariant()}')]";

            var node = doc.DocumentNode.SelectSingleNode(xpath);
            return CleanText(node?.InnerText);
        }

        private static string? GetTextBySelectors(HtmlDocument doc, params string[] xpaths)
        {
            foreach (var xpath in xpaths)
            {
                var node = doc.DocumentNode.SelectSingleNode(xpath);
                var text = CleanText(node?.InnerText);

                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }

            return null;
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

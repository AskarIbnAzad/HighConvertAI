using HighConvertAI.Server.Models.CatalogExtraction;
using HighConvertAI.Server.Models.CatalogExtraction.Sections;
using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Services.CatalogExtraction.Abstractions;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace HighConvertAI.Server.Services.CatalogExtraction.Extractors
{
    public sealed class CatalogProductGridExtractor : ICatalogSectionExtractor
    {
        public int Order => 40;

        public void Extract(CatalogExtractionContext context)
        {
            var doc = context.Document;

            var gridRoot = FindBestGridRoot(doc);
            var products = new List<CatalogProductCard>();
            var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var candidateCards = GetCandidateCardNodes(gridRoot ?? doc.DocumentNode);

            foreach (var cardNode in candidateCards)
            {
                var productLink = GetPrimaryProductLink(cardNode);
                if (productLink == null)
                    continue;

                var rawUrl = productLink.GetAttributeValue("href", null);
                var normalizedUrl = NormalizeProductUrl(rawUrl);

                if (string.IsNullOrWhiteSpace(normalizedUrl))
                    continue;

                if (!seenUrls.Add(normalizedUrl))
                    continue;

                var title = ExtractTitle(cardNode, productLink);
                var priceText = ExtractPrice(cardNode);
                var compareAtPriceText = ExtractCompareAtPrice(cardNode);
                var imageUrl = ExtractImageUrl(cardNode);

                var hasImage = !string.IsNullOrWhiteSpace(imageUrl);
                var isSoldOut = IsSoldOut(cardNode);
                var hasQuickAdd = HasQuickAdd(cardNode);
                var hasRating = HasRating(cardNode);

                // Ignore weak false positives
                if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(priceText) && !hasImage)
                    continue;

                products.Add(new CatalogProductCard
                {
                    Title = title,
                    Url = normalizedUrl,
                    PriceText = priceText,
                    CompareAtPriceText = compareAtPriceText,
                    HasImage = hasImage,
                    ImageUrl = imageUrl,
                    IsSoldOut = isSoldOut,
                    HasQuickAdd = hasQuickAdd,
                    HasRating = hasRating
                });
            }

            context.ProductGrid = new CatalogProductGridSection
            {
                HasProductGrid = gridRoot != null || products.Count > 0,
                Products = products
            };

            if (products.Count == 0)
            {
                context.Issues.Add(new ExtractionIssue
                {
                    Code = "CATALOG_PRODUCTS_NOT_FOUND",
                    Message = "No catalog product cards could be extracted.",
                    Severity = "Warning"
                });
            }
            else
            {
                context.Evidence.Add(new ExtractionEvidence
                {
                    Section = "CatalogProductGrid",
                    SourceType = "dom",
                    SelectorOrPath = "catalog product card candidates",
                    ExtractedValue = $"Extracted {products.Count} product cards"
                });
            }
        }

        private static HtmlNode? FindBestGridRoot(HtmlDocument doc)
        {
            var selectors = new[]
            {
                "//*[@data-product-grid]",
                "//*[@data-testid='product-grid']",
                "//*[@id='product-grid']",
                "//*[contains(translate(@id,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-grid')]",
                "//*[contains(translate(@id,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'productgrid')]",
                "//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-grid-container')]",
                "//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-grid')]",
                "//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'main-collection-product-grid')]",
                "//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'collection-product-grid')]",
                "//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'collection__products')]",
                "//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'grid--product')]"
            };

            foreach (var xpath in selectors)
            {
                var node = doc.DocumentNode.SelectSingleNode(xpath);
                if (node != null)
                    return node;
            }

            return doc.DocumentNode.SelectSingleNode("//main");
        }

        private static List<HtmlNode> GetCandidateCardNodes(HtmlNode root)
        {
            var results = new List<HtmlNode>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var xpaths = new[]
            {
                ".//*[@data-product-card]",
                ".//*[@data-product-handle]",
                ".//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-card')]",
                ".//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'card-wrapper')]",
                ".//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-item')]",
                ".//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'grid__item')]",
                ".//article[.//a[contains(@href,'/products/')]]",
                ".//li[.//a[contains(@href,'/products/')]]",
                ".//div[.//a[contains(@href,'/products/')]]"
            };

            foreach (var xpath in xpaths)
            {
                var nodes = root.SelectNodes(xpath);
                if (nodes == null)
                    continue;

                foreach (var node in nodes)
                {
                    if (!IsLikelyProductCard(node))
                        continue;

                    var key = node.XPath;
                    if (seen.Add(key))
                        results.Add(node);
                }
            }

            return results;
        }

        private static bool IsLikelyProductCard(HtmlNode node)
        {
            var productLink = GetPrimaryProductLink(node);
            if (productLink == null)
                return false;

            var classValue = node.GetAttributeValue("class", string.Empty).ToLowerInvariant();
            var hasImage = node.SelectSingleNode(".//img") != null;
            var hasPrice =
                node.SelectSingleNode(".//*[@data-price]") != null ||
                node.SelectSingleNode(".//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'price')]") != null;

            var looksLikeCard =
                node.Attributes["data-product-card"] != null ||
                node.Attributes["data-product-handle"] != null ||
                classValue.Contains("product-card") ||
                classValue.Contains("card-wrapper") ||
                classValue.Contains("product-item") ||
                classValue.Contains("grid__item") ||
                node.Name.Equals("article", StringComparison.OrdinalIgnoreCase) ||
                node.Name.Equals("li", StringComparison.OrdinalIgnoreCase);

            return looksLikeCard && (hasImage || hasPrice || productLink != null);
        }

        private static HtmlNode? GetPrimaryProductLink(HtmlNode cardNode)
        {
            var links = cardNode.SelectNodes(".//a[contains(@href,'/products/')]");
            if (links == null || links.Count == 0)
                return null;

            // Prefer title-style links first
            foreach (var link in links)
            {
                var classValue = link.GetAttributeValue("class", string.Empty).ToLowerInvariant();
                if (classValue.Contains("full-unstyled-link")
                    || classValue.Contains("card__heading")
                    || classValue.Contains("product-card")
                    || classValue.Contains("title"))
                {
                    return link;
                }
            }

            return links.FirstOrDefault();
        }

        private static string? ExtractTitle(HtmlNode cardNode, HtmlNode productLink)
        {
            return FirstNonEmpty(
                GetNodeText(cardNode.SelectSingleNode(".//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'card__heading')]")),
                GetNodeText(cardNode.SelectSingleNode(".//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-card__title')]")),
                GetNodeText(cardNode.SelectSingleNode(".//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'full-unstyled-link')]")),
                GetNodeText(cardNode.SelectSingleNode(".//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'title')]")),
                GetNodeText(productLink)
            );
        }

        private static string? ExtractPrice(HtmlNode cardNode)
        {
            return FirstNonEmpty(
                GetNodeText(cardNode.SelectSingleNode(".//*[@data-price]")),
                GetNodeText(cardNode.SelectSingleNode(".//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'price-item--sale')]")),
                GetNodeText(cardNode.SelectSingleNode(".//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'price-item--regular')]")),
                GetNodeText(cardNode.SelectSingleNode(".//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'price__regular')]")),
                GetNodeText(cardNode.SelectSingleNode(".//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'price')]"))
            );
        }

        private static string? ExtractCompareAtPrice(HtmlNode cardNode)
        {
            return FirstNonEmpty(
                GetNodeText(cardNode.SelectSingleNode(".//*[@data-compare-at-price]")),
                GetNodeText(cardNode.SelectSingleNode(".//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'price-item--regular')]//s")),
                GetNodeText(cardNode.SelectSingleNode(".//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'compare')]")),
                GetNodeText(cardNode.SelectSingleNode(".//s"))
            );
        }

        private static string? ExtractImageUrl(HtmlNode cardNode)
        {
            var imageNode = cardNode.SelectSingleNode(".//img");
            if (imageNode == null)
                return null;

            return FirstNonEmpty(
                imageNode.GetAttributeValue("src", null),
                imageNode.GetAttributeValue("data-src", null),
                ExtractFirstSrcFromSrcSet(imageNode.GetAttributeValue("srcset", null)),
                ExtractFirstSrcFromSrcSet(imageNode.GetAttributeValue("data-srcset", null))
            );
        }

        private static bool IsSoldOut(HtmlNode cardNode)
        {
            return ContainsText(cardNode, "sold out")
                   || ContainsText(cardNode, "out of stock")
                   || ContainsClass(cardNode, "sold-out")
                   || ContainsClass(cardNode, "soldout");
        }

        private static bool HasQuickAdd(HtmlNode cardNode)
        {
            return cardNode.SelectSingleNode(".//*[@data-quick-add]") != null
                   || cardNode.SelectSingleNode(".//button[contains(translate(text(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'quick add')]") != null
                   || cardNode.SelectSingleNode(".//button[contains(translate(text(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'add to cart')]") != null
                   || ContainsClass(cardNode, "quick-add");
        }

        private static bool HasRating(HtmlNode cardNode)
        {
            return cardNode.SelectSingleNode(".//*[@data-rating]") != null
                   || cardNode.SelectSingleNode(".//*[contains(translate(@aria-label,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'star')]") != null
                   || ContainsClass(cardNode, "rating")
                   || ContainsClass(cardNode, "stars")
                   || ContainsClass(cardNode, "jdgm")
                   || ContainsClass(cardNode, "yotpo");
        }

        private static string? NormalizeProductUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var clean = HtmlEntity.DeEntitize(url).Trim();

            var hashIndex = clean.IndexOf('#');
            if (hashIndex >= 0)
                clean = clean[..hashIndex];

            var queryIndex = clean.IndexOf('?');
            if (queryIndex >= 0)
                clean = clean[..queryIndex];

            return string.IsNullOrWhiteSpace(clean) ? null : clean;
        }

        private static string? ExtractFirstSrcFromSrcSet(string? srcSet)
        {
            if (string.IsNullOrWhiteSpace(srcSet))
                return null;

            var first = srcSet.Split(',').FirstOrDefault()?.Trim();
            if (string.IsNullOrWhiteSpace(first))
                return null;

            var parts = first.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : null;
        }

        private static bool ContainsText(HtmlNode? node, string text)
        {
            var value = GetNodeText(node);
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Contains(text, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsClass(HtmlNode? node, string classPart)
        {
            if (node == null)
                return false;

            var classValue = node.GetAttributeValue("class", string.Empty);
            return !string.IsNullOrWhiteSpace(classValue) &&
                   classValue.Contains(classPart, StringComparison.OrdinalIgnoreCase);
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }

        private static string? GetNodeText(HtmlNode? node)
        {
            if (node == null)
                return null;

            var raw = HtmlEntity.DeEntitize(node.InnerText);
            raw = Regex.Replace(raw, @"\s+", " ").Trim();

            return string.IsNullOrWhiteSpace(raw) ? null : raw;
        }
    }
}
using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.ProductExtraction.Sections;
using HighConvertAI.Server.Services.ProductExtraction.Abstractions;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace HighConvertAI.Server.Services.ProductExtraction.Extractors
{
    public sealed class CtaExtractor : IProductSectionExtractor
    {
        public int Order => 30;

        private static readonly string[] AddToCartKeywords =
        {
            "add to cart",
            "add to bag",
            "add to basket",
            "add item",
            "cart"
        };

        private static readonly string[] BuyNowKeywords =
        {
            "buy now",
            "buy it now",
            "shop now",
            "checkout now",
            "pay now"
        };

        private static readonly string[] SoldOutKeywords =
        {
            "sold out",
            "out of stock",
            "unavailable",
            "not available",
            "coming soon"
        };

        public void Extract(ProductExtractionContext context)
        {
            var document = context.Document;

            var interactiveNodes = document.DocumentNode
                .SelectNodes("//button|//input|//a")
                ?.Where(IsVisibleInteractiveNode)
                .ToList()
                ?? new List<HtmlNode>();

            var ctaCandidates = interactiveNodes
                .Select(node => new
                {
                    Node = node,
                    Text = GetBestNodeText(node),
                    Score = GetCtaScore(node)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ToList();

            var addToCartNode = ctaCandidates
                .FirstOrDefault(x => IsAddToCartNode(x.Node))
                ?.Node;

            var buyNowNode = ctaCandidates
                .FirstOrDefault(x => IsBuyNowNode(x.Node))
                ?.Node;

            var quantityNode = FindQuantityNode(document);

            var stickyAddToCartNode = interactiveNodes.FirstOrDefault(x =>
                LooksSticky(x) && IsAddToCartNode(x));

            var paymentButton = interactiveNodes.FirstOrDefault(IsPaymentButton);

            var allCtaTexts = ctaCandidates
                .Select(x => x.Text)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var hasSoldOutState =
                interactiveNodes.Any(IsSoldOutNode) ||
                PageContainsAnyText(document, SoldOutKeywords);

            var cta = new CtaSection
            {
                HasAddToCartButton = addToCartNode != null,
                HasBuyNowButton = buyNowNode != null,
                HasQuantitySelector = quantityNode != null,
                HasStickyAddToCart = stickyAddToCartNode != null,

                IsAddToCartDisabled = IsDisabled(addToCartNode),

                PrimaryCtaText = GetBestNodeText(addToCartNode),
                SecondaryCtaText = GetBestNodeText(buyNowNode),

                PrimaryCtaSelector = addToCartNode?.XPath,
                SecondaryCtaSelector = buyNowNode?.XPath,

                QuantitySelectorType = quantityNode?.Name,

                HasPaymentButton = paymentButton != null,
                HasSoldOutState = hasSoldOutState,
                HasUnavailableState = hasSoldOutState && addToCartNode == null,

                HasClearPrimaryCta = addToCartNode != null && !IsDisabled(addToCartNode),
                HasMultipleCtas = ctaCandidates.Count >= 2,

                CtaTextsFound = allCtaTexts
            };

            if (addToCartNode == null)
            {
                cta.CtaWarnings.Add("Primary Add to Cart CTA was not found.");

                context.Issues.Add(new ExtractionIssue
                {
                    Code = "ADD_TO_CART_NOT_FOUND",
                    Message = "Add to cart button could not be detected.",
                    Severity = "Warning"
                });
            }

            if (addToCartNode != null && IsDisabled(addToCartNode))
            {
                cta.CtaWarnings.Add("Primary Add to Cart CTA appears disabled.");

                context.Issues.Add(new ExtractionIssue
                {
                    Code = "ADD_TO_CART_DISABLED",
                    Message = "Add to cart button exists but appears disabled.",
                    Severity = "Warning"
                });
            }

            if (quantityNode == null)
            {
                cta.CtaWarnings.Add("Quantity selector was not detected.");
            }

            if (buyNowNode == null && paymentButton == null)
            {
                cta.CtaWarnings.Add("Express checkout / Buy Now CTA was not detected.");
            }

            context.Cta = cta;

            AddEvidence(context, "Primary CTA", addToCartNode);
            AddEvidence(context, "Secondary CTA", buyNowNode);
            AddEvidence(context, "Quantity Selector", quantityNode);
            AddEvidence(context, "Sticky CTA", stickyAddToCartNode);
            AddEvidence(context, "Payment Button", paymentButton);
        }

        private static HtmlNode? FindQuantityNode(HtmlDocument document)
        {
            return document.DocumentNode.SelectSingleNode(
                "//input[@name='quantity']" +
                " | //select[@name='quantity']" +
                " | //input[contains(@id,'quantity')]" +
                " | //select[contains(@id,'quantity')]" +
                " | //input[contains(@class,'quantity')]" +
                " | //select[contains(@class,'quantity')]" +
                " | //input[@type='number' and (@min='1' or @value='1')]"
            );
        }

        private static bool IsAddToCartNode(HtmlNode node)
        {
            if (!IsInteractiveNode(node))
                return false;

            return ContainsAny(GetSearchableText(node), AddToCartKeywords)
                   || AttributeContainsAny(node, "name", "add")
                   || AttributeContainsAny(node, "id", "add-to-cart", "addtocart")
                   || AttributeContainsAny(node, "class", "add-to-cart", "addtocart")
                   || AttributeContainsAny(node, "data-action", "add-to-cart", "add");
        }

        private static bool IsBuyNowNode(HtmlNode node)
        {
            if (!IsInteractiveNode(node))
                return false;

            return ContainsAny(GetSearchableText(node), BuyNowKeywords)
                   || AttributeContainsAny(node, "class", "shopify-payment-button")
                   || AttributeContainsAny(node, "data-testid", "buy-now", "payment-button");
        }

        private static bool IsPaymentButton(HtmlNode node)
        {
            return AttributeContainsAny(node, "class", "shopify-payment-button")
                   || AttributeContainsAny(node, "data-shopify", "payment-button")
                   || AttributeContainsAny(node, "name", "checkout")
                   || ContainsAny(GetSearchableText(node), "paypal", "apple pay", "google pay", "shop pay");
        }

        private static bool IsSoldOutNode(HtmlNode node)
        {
            return ContainsAny(GetSearchableText(node), SoldOutKeywords);
        }

        private static int GetCtaScore(HtmlNode node)
        {
            var score = 0;
            var text = GetSearchableText(node);

            if (ContainsAny(text, AddToCartKeywords))
                score += 100;

            if (ContainsAny(text, BuyNowKeywords))
                score += 80;

            if (AttributeContainsAny(node, "type", "submit"))
                score += 25;

            if (AttributeContainsAny(node, "name", "add", "checkout"))
                score += 30;

            if (AttributeContainsAny(node, "class", "product-form", "add-to-cart", "shopify-payment-button"))
                score += 25;

            if (AttributeContainsAny(node, "data-action", "add-to-cart", "buy-now"))
                score += 25;

            if (IsDisabled(node))
                score -= 20;

            if (ContainsAny(text, SoldOutKeywords))
                score -= 30;

            return score;
        }

        private static bool PageContainsAnyText(HtmlDocument document, IEnumerable<string> keywords)
        {
            var bodyText = CleanText(document.DocumentNode.SelectSingleNode("//body")?.InnerText);
            return ContainsAny(bodyText, keywords.ToArray());
        }

        private static bool LooksSticky(HtmlNode node)
        {
            return AttributeContainsAny(node, "class", "sticky", "fixed", "floating")
                   || AttributeContainsAny(node, "id", "sticky", "fixed", "floating")
                   || AttributeContainsAny(node, "style", "position: sticky", "position:fixed", "position: fixed");
        }

        private static bool IsVisibleInteractiveNode(HtmlNode node)
        {
            if (!IsInteractiveNode(node))
                return false;

            var style = node.GetAttributeValue("style", string.Empty);

            if (style.Contains("display:none", StringComparison.OrdinalIgnoreCase)
                || style.Contains("display: none", StringComparison.OrdinalIgnoreCase)
                || style.Contains("visibility:hidden", StringComparison.OrdinalIgnoreCase)
                || style.Contains("visibility: hidden", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(node.GetAttributeValue("aria-hidden", ""), "true", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private static bool IsInteractiveNode(HtmlNode node)
        {
            var name = node.Name.ToLowerInvariant();
            return name == "button" || name == "input" || name == "a";
        }

        private static bool IsDisabled(HtmlNode? node)
        {
            if (node == null)
                return false;

            return node.Attributes["disabled"] != null
                   || string.Equals(node.GetAttributeValue("aria-disabled", ""), "true", StringComparison.OrdinalIgnoreCase)
                   || AttributeContainsAny(node, "class", "disabled")
                   || AttributeContainsAny(node, "style", "pointer-events: none");
        }

        private static string? GetBestNodeText(HtmlNode? node)
        {
            if (node == null)
                return null;

            var text = CleanText(node.InnerText);

            if (!string.IsNullOrWhiteSpace(text))
                return text;

            var value = node.GetAttributeValue("value", null);
            if (!string.IsNullOrWhiteSpace(value))
                return CleanText(value);

            var ariaLabel = node.GetAttributeValue("aria-label", null);
            if (!string.IsNullOrWhiteSpace(ariaLabel))
                return CleanText(ariaLabel);

            var title = node.GetAttributeValue("title", null);
            if (!string.IsNullOrWhiteSpace(title))
                return CleanText(title);

            return null;
        }

        private static string GetSearchableText(HtmlNode node)
        {
            var parts = new[]
            {
                node.InnerText,
                node.GetAttributeValue("value", ""),
                node.GetAttributeValue("aria-label", ""),
                node.GetAttributeValue("title", ""),
                node.GetAttributeValue("name", ""),
                node.GetAttributeValue("id", ""),
                node.GetAttributeValue("class", ""),
                node.GetAttributeValue("data-action", ""),
                node.GetAttributeValue("data-testid", "")
            };

            return CleanText(string.Join(" ", parts)) ?? string.Empty;
        }

        private static bool AttributeContainsAny(HtmlNode? node, string attributeName, params string[] values)
        {
            if (node == null)
                return false;

            var attributeValue = node.GetAttributeValue(attributeName, string.Empty);

            if (string.IsNullOrWhiteSpace(attributeValue))
                return false;

            return values.Any(value =>
                attributeValue.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        private static bool ContainsAny(string? text, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return values.Any(value =>
                text.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        private static string? CleanText(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            return Regex.Replace(raw, @"\s+", " ").Trim();
        }

        private static void AddEvidence(ProductExtractionContext context, string label, HtmlNode? node)
        {
            if (node == null)
                return;

            context.Evidence.Add(new ExtractionEvidence
            {
                Section = "Cta",
                SourceType = "dom",
                SelectorOrPath = node.XPath,
                ExtractedValue = $"{label}: {GetBestNodeText(node)}"
            });
        }
    }
}
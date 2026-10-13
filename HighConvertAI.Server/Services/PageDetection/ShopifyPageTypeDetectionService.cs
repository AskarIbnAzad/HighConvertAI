using HighConvertAI.Constants;
using HighConvertAI.Server.Services.Abstractions;
using System.Text.RegularExpressions;

namespace HighConvertAI.Server.Services.PageDetection
{
    public class ShopifyPageTypeDetectionService : IPageTypeDetectionService
    {
        public string Detect(string html, string url)
        {
            if (string.IsNullOrWhiteSpace(html))
                return PageTypes.Unknown;

            var normalizedHtml = html.ToLowerInvariant();
            var normalizedUrl = (url ?? string.Empty).ToLowerInvariant();

            var productScore = GetProductScore(normalizedHtml, normalizedUrl);
            var categoryScore = GetCategoryScore(normalizedHtml, normalizedUrl);
            var homeScore = GetHomeScore(normalizedHtml, normalizedUrl);
            var cartScore = GetCartScore(normalizedHtml, normalizedUrl);
            var checkoutScore = GetCheckoutScore(normalizedHtml, normalizedUrl);
            var thankYouScore = GetThankYouScore(normalizedHtml, normalizedUrl);
            var landingScore = GetLandingScore(normalizedHtml, normalizedUrl);

            var scores = new Dictionary<string, int>
        {
            { PageTypes.Product, productScore },
            { PageTypes.Category, categoryScore },
            { PageTypes.Home, homeScore },
            { PageTypes.Cart, cartScore },
            { PageTypes.Checkout, checkoutScore },
            { PageTypes.ThankYou, thankYouScore },
            { PageTypes.Landing, landingScore }
        };

            var bestMatch = scores
                .OrderByDescending(x => x.Value)
                .First();

            return bestMatch.Value <= 0 ? PageTypes.Unknown : bestMatch.Key;
        }

        private static int GetProductScore(string html, string url)
        {
            var score = 0;

            // URL hints
            if (url.Contains("/products/"))
                score += 5;

            if (Regex.IsMatch(url, @"/collections/[^/]+/products/[^/?#]+"))
                score += 3;

            // Shopify / schema / template signals
            if (ContainsAny(html,
                    "template-product",
                    "template--product",
                    "\"@type\":\"product\"",
                    "\"@type\": \"product\"",
                    "product:price:amount",
                    "og:type\" content=\"product\""))
                score += 6;

            // Strong product DOM signals
            if (ContainsAny(html,
                    "/cart/add",
                    "add to cart",
                    "buy it now",
                    "product-form",
                    "variant-picker",
                    "product__title",
                    "product-form__submit"))
                score += 4;

            // Product-specific inputs/meta
            if (ContainsAny(html,
                    "name=\"id\"",
                    "name='id'",
                    "data-product-id",
                    "data-product-handle",
                    "price__regular",
                    "price__sale"))
                score += 3;

            // Penalize if it clearly looks like collection/listing
            if (ContainsAny(html,
                    "collection__title",
                    "facets",
                    "filter by",
                    "sort by",
                    "product-grid-container"))
                score -= 3;

            return score;
        }

        private static int GetCategoryScore(string html, string url)
        {
            var score = 0;

            if (url.Contains("/collections/"))
                score += 5;

            if (ContainsAny(html,
                    "template-collection",
                    "template--collection",
                    "\"@type\":\"collectionpage\"",
                    "\"@type\": \"collectionpage\"",
                    "collection__title"))
                score += 5;

            if (ContainsAny(html,
                    "filter by",
                    "sort by",
                    "facets",
                    "collection-filters",
                    "product-grid",
                    "product-grid-container"))
                score += 4;

            if (ContainsAny(html,
                    "/cart/add",
                    "product-form__submit",
                    "variant-picker"))
                score -= 2;

            return score;
        }

        private static int GetHomeScore(string html, string url)
        {
            var score = 0;

            if (IsRootUrl(url))
                score += 5;

            if (ContainsAny(html,
                    "template-index",
                    "template--index",
                    "\"@type\":\"website\"",
                    "\"@type\": \"website\""))
                score += 4;

            if (ContainsAny(html,
                    "featured collection",
                    "featured products",
                    "slideshow",
                    "image banner",
                    "shop the look"))
                score += 3;

            return score;
        }

        private static int GetCartScore(string html, string url)
        {
            var score = 0;

            if (url.Contains("/cart"))
                score += 6;

            if (ContainsAny(html,
                    "template-cart",
                    "template--cart",
                    "cart__items",
                    "cart-item",
                    "subtotal",
                    "update cart"))
                score += 5;

            return score;
        }

        private static int GetCheckoutScore(string html, string url)
        {
            var score = 0;

            if (url.Contains("/checkout") || url.Contains("/checkouts/"))
                score += 6;

            if (ContainsAny(html,
                    "shopify-checkout",
                    "template-checkout",
                    "payment method",
                    "billing address",
                    "delivery method"))
                score += 5;

            return score;
        }

        private static int GetThankYouScore(string html, string url)
        {
            var score = 0;

            if (url.Contains("thank") || url.Contains("order-confirmation"))
                score += 4;

            if (ContainsAny(html,
                    "thank you for your order",
                    "order confirmed",
                    "order number",
                    "thank_you"))
                score += 6;

            return score;
        }

        private static int GetLandingScore(string html, string url)
        {
            var score = 0;

            if (url.Contains("/pages/"))
                score += 4;

            if (ContainsAny(html,
                    "template-page",
                    "template--page"))
                score += 4;

            if (!url.Contains("/products/") &&
                !url.Contains("/collections/") &&
                !url.Contains("/cart") &&
                !url.Contains("/checkout") &&
                !IsRootUrl(url))
                score += 1;

            return score;
        }

        private static bool ContainsAny(string source, params string[] values)
        {
            foreach (var value in values)
            {
                if (source.Contains(value))
                    return true;
            }

            return false;
        }

        private static bool IsRootUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            var path = uri.AbsolutePath?.Trim();
            return string.IsNullOrEmpty(path) || path == "/";
        }
    }

}

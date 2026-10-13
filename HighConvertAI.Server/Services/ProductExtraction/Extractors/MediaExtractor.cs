using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.ProductExtraction.Sections;
using HighConvertAI.Server.Services.ProductExtraction.Abstractions;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace HighConvertAI.Server.Services.ProductExtraction.Extractors
{
    public sealed class MediaExtractor : IProductSectionExtractor
    {
        public int Order => 50;

        public void Extract(ProductExtractionContext context)
        {
            var document = context.Document;

            var mediaContainer = FindProductMediaContainer(document);

            if (mediaContainer == null)
            {
                context.Issues.Add(new ExtractionIssue
                {
                    Code = "PRODUCT_MEDIA_CONTAINER_NOT_FOUND",
                    Message = "Product media/gallery container could not be detected. Fallback scan was used.",
                    Severity = "Info"
                });
            }

            var searchRoot = mediaContainer ?? document.DocumentNode;

            var imageNodes = searchRoot
                .SelectNodes(".//img")
                ?.Where(IsLikelyProductImage)
                .ToList()
                ?? new List<HtmlNode>();

            var images = imageNodes
                .Select(CreateImageInfo)
                .Where(x => !string.IsNullOrWhiteSpace(x.Src))
                .GroupBy(x => NormalizeImageUrl(x.Src))
                .Select(x => x.First())
                .ToList();

            var videoNodes = searchRoot
                .SelectNodes(".//video|.//iframe")
                ?.Where(IsLikelyProductVideo)
                .ToList()
                ?? new List<HtmlNode>();

            var videos = videoNodes
                .Select(CreateVideoInfo)
                .Where(x => !string.IsNullOrWhiteSpace(x.Src))
                .GroupBy(x => NormalizeImageUrl(x.Src))
                .Select(x => x.First())
                .ToList();

            context.Media = new MediaSection
            {
                ImageCount = images.Count,
                VideoCount = videos.Count,
                MissingAltTextCount = images.Count(x => string.IsNullOrWhiteSpace(x.Alt)),
                Images = images,
                Videos = videos
            };

            if (images.Count > 0 || videos.Count > 0)
            {
                context.Evidence.Add(new ExtractionEvidence
                {
                    Section = "Media",
                    SourceType = "dom",
                    SelectorOrPath = mediaContainer?.XPath ?? "fallback: document product-image scan",
                    ExtractedValue = $"Product images: {images.Count}, Product videos: {videos.Count}"
                });
            }
            else
            {
                context.Issues.Add(new ExtractionIssue
                {
                    Code = "PRODUCT_MEDIA_NOT_FOUND",
                    Message = "Product images/videos could not be detected.",
                    Severity = "Warning"
                });
            }
        }

        private static HtmlNode? FindProductMediaContainer(HtmlDocument document)
        {
            return document.DocumentNode.SelectSingleNode(
                "//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product__media')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-media')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product__gallery')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-gallery')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product__photos')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-photos')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product__images')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-images')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-single__media')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-single__photos')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'media-gallery')]" +
                " | //*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-gallery__media')]" +
                " | //*[contains(translate(@id,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-media')]" +
                " | //*[contains(translate(@id,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-gallery')]" +
                " | //*[contains(translate(@id,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'product-photos')]"
            );
        }

        private static ProductImageInfo CreateImageInfo(HtmlNode img)
        {
            return new ProductImageInfo
            {
                Src = GetImageSrc(img),
                Alt = CleanText(img.GetAttributeValue("alt", null)),
                Width = img.GetAttributeValue("width", null),
                Height = img.GetAttributeValue("height", null)
            };
        }

        private static ProductVideoInfo CreateVideoInfo(HtmlNode node)
        {
            var src =
                node.GetAttributeValue("src", null)
                ?? node.SelectSingleNode(".//source")?.GetAttributeValue("src", null);

            return new ProductVideoInfo
            {
                Src = src,
                Type = node.Name
            };
        }

        private static string? GetImageSrc(HtmlNode img)
        {
            return img.GetAttributeValue("src", null)
                   ?? img.GetAttributeValue("data-src", null)
                   ?? img.GetAttributeValue("data-original", null)
                   ?? img.GetAttributeValue("data-lazy", null)
                   ?? img.GetAttributeValue("data-image", null)
                   ?? ExtractFromSrcSet(img.GetAttributeValue("srcset", null))
                   ?? ExtractFromSrcSet(img.GetAttributeValue("data-srcset", null));
        }

        private static string? ExtractFromSrcSet(string? srcset)
        {
            if (string.IsNullOrWhiteSpace(srcset))
                return null;

            return srcset
                .Split(',')
                .Select(x => x.Trim().Split(' ')[0])
                .LastOrDefault(x => !string.IsNullOrWhiteSpace(x));
        }

        private static bool IsLikelyProductImage(HtmlNode img)
        {
            var combined = string.Join(" ",
                img.GetAttributeValue("src", ""),
                img.GetAttributeValue("data-src", ""),
                img.GetAttributeValue("data-original", ""),
                img.GetAttributeValue("data-lazy", ""),
                img.GetAttributeValue("data-image", ""),
                img.GetAttributeValue("srcset", ""),
                img.GetAttributeValue("data-srcset", ""),
                img.GetAttributeValue("alt", ""),
                img.GetAttributeValue("class", ""),
                img.GetAttributeValue("id", "")
            );

            if (string.IsNullOrWhiteSpace(combined))
                return false;

            if (ContainsAny(combined,
                    "logo",
                    "icon",
                    "avatar",
                    "banner",
                    "payment",
                    "badge",
                    "sprite",
                    "placeholder",
                    "loading"))
            {
                return false;
            }

            if (ContainsAny(combined,
                    "related",
                    "recommend",
                    "recommendation",
                    "upsell",
                    "cross-sell",
                    "collection",
                    "recently-viewed",
                    "thumbnail-list"))
            {
                return false;
            }

            return ContainsAny(combined,
                "product",
                "products",
                "cdn/shop",
                "/files/",
                "media",
                "image"
            );
        }

        private static bool IsLikelyProductVideo(HtmlNode node)
        {
            var combined = string.Join(" ",
                node.GetAttributeValue("src", ""),
                node.GetAttributeValue("class", ""),
                node.GetAttributeValue("id", ""),
                node.GetAttributeValue("title", "")
            );

            if (string.IsNullOrWhiteSpace(combined))
                return node.Name.Equals("video", StringComparison.OrdinalIgnoreCase);

            if (ContainsAny(combined, "youtube", "vimeo", "product", "media"))
                return true;

            return node.Name.Equals("video", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeImageUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            var clean = url.Trim();

            if (clean.StartsWith("//"))
                clean = "https:" + clean;

            var queryIndex = clean.IndexOf('?');
            if (queryIndex >= 0)
                clean = clean[..queryIndex];

            return clean.ToLowerInvariant();
        }

        private static bool ContainsAny(string? text, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return values.Any(x =>
                text.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        private static string? CleanText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Regex.Replace(value, @"\s+", " ").Trim();
        }
    }
}
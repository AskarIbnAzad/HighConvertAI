namespace HighConvertAI.Server.Models.ProductExtraction.Sections
{
    public sealed class ProductPageAiPayload
    {
        public string Url { get; set; } = string.Empty;

        public string? Title { get; set; }
        public string? Subtitle { get; set; }

        public decimal? CurrentPrice { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public bool HasSalePricing { get; set; }

        public bool HasAddToCart { get; set; }
        public bool HasBuyNow { get; set; }
        public bool HasStickyAtc { get; set; }

        public int DescriptionWordCount { get; set; }
        public int BulletPointCount { get; set; }

        public bool HasShippingInfo { get; set; }
        public bool HasReturnPolicy { get; set; }

        public bool HasTrustBadges { get; set; }
        public bool HasSecureCheckoutMessage { get; set; }

        public bool HasReviews { get; set; }
        public decimal? AverageRating { get; set; }
        public int? ReviewCount { get; set; }

        public int FaqCount { get; set; }
        public int ImageCount { get; set; }
        public int VideoCount { get; set; }

        public string? ShortDescriptionSnippet { get; set; }
        public IReadOnlyList<string> KeyTrustMessages { get; set; } = [];
    }
}

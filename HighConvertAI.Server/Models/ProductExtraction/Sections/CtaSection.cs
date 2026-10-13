namespace HighConvertAI.Server.Models.ProductExtraction.Sections
{
    //public sealed class CtaSection
    //{
    //    public bool HasAddToCartButton { get; set; }
    //    public bool HasBuyNowButton { get; set; }
    //    public bool HasStickyAddToCart { get; set; }
    //    public bool HasQuantitySelector { get; set; }
    //    public bool IsAddToCartDisabled { get; set; }
    //    public string? PrimaryCtaText { get; set; }
    //    public string? SecondaryCtaText { get; set; }
    //}
    public sealed class CtaSection
    {
        public bool HasAddToCartButton { get; set; }
        public bool HasBuyNowButton { get; set; }
        public bool HasQuantitySelector { get; set; }
        public bool HasStickyAddToCart { get; set; }
        public bool IsAddToCartDisabled { get; set; }

        public string? PrimaryCtaText { get; set; }
        public string? SecondaryCtaText { get; set; }

        // New enriched fields
        public string? PrimaryCtaSelector { get; set; }
        public string? SecondaryCtaSelector { get; set; }
        public string? QuantitySelectorType { get; set; }

        public bool HasPaymentButton { get; set; }
        public bool HasSoldOutState { get; set; }
        public bool HasUnavailableState { get; set; }

        public bool HasClearPrimaryCta { get; set; }
        public bool HasMultipleCtas { get; set; }

        public List<string> CtaTextsFound { get; set; } = new();
        public List<string> CtaWarnings { get; set; } = new();
    }
}

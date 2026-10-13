using HighConvertAI.Server.Models.ProductExtraction.Sections;

namespace HighConvertAI.Server.Models.ProductExtraction
{
    public sealed class ProductPageExtractionResult
    {
        public required string Url { get; set; }
        public required string PageType { get; set; }

        public BasicInfoSection? BasicInfo { get; set; }
        public PricingSection? Pricing { get; set; }
        public CtaSection? Cta { get; set; }
        public DescriptionSection? Description { get; set; }
        public ShippingSection? Shipping { get; set; }
        public TrustSection? Trust { get; set; }
        public ReviewsSection? Reviews { get; set; }
        public FaqSection? Faq { get; set; }
        public MediaSection? Media { get; set; }

        public IReadOnlyList<ExtractionIssue> Issues { get; set; } = [];
        public IReadOnlyList<ExtractionEvidence> Evidence { get; set; } = [];
        public ExtractionMeta Meta { get; set; } = new();
    }
}

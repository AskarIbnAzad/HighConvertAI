using HighConvertAI.Server.Models.ProductExtraction.Sections;
using HighConvertAI.Server.Models.Scanning;
using HtmlAgilityPack;
namespace HighConvertAI.Server.Models.ProductExtraction
{
    public sealed class ProductExtractionContext
    {
        public PageScanContext Input { get; }
        public HtmlDocument Document { get; }

        public BasicInfoSection? BasicInfo { get; set; }
        public PricingSection? Pricing { get; set; }
        public CtaSection? Cta { get; set; }
        public DescriptionSection? Description { get; set; }
        public ShippingSection? Shipping { get; set; }
        public TrustSection? Trust { get; set; }
        public ReviewsSection? Reviews { get; set; }
        public FaqSection? Faq { get; set; }
        public MediaSection? Media { get; set; }

        public List<ExtractionIssue> Issues { get; } = new();
        public List<ExtractionEvidence> Evidence { get; } = new();

        private ProductExtractionContext(PageScanContext input, HtmlDocument document)
        {
            Input = input;
            Document = document;
        }

        public static ProductExtractionContext Create(PageScanContext input)
        {
            var document = new HtmlDocument();
            document.LoadHtml(input.Html);

            return new ProductExtractionContext(input, document);
        }

        public ProductPageExtractionResult BuildResult()
        {
            return new ProductPageExtractionResult
            {
                Url = Input.Url,
                PageType = Input.PageType,
                BasicInfo = BasicInfo,
                Pricing = Pricing,
                Cta = Cta,
                Description = Description,
                Shipping = Shipping,
                Trust = Trust,
                Reviews = Reviews,
                Faq = Faq,
                Media = Media,
                Issues = Issues,
                Evidence = Evidence,
                Meta = new ExtractionMeta
                {
                    ExtractedAtUtc = DateTime.UtcNow,
                    Version = "1.0",
                    Strategy = "ShopifyStructuredExtraction",
                    ConfidenceScore = CalculateConfidenceScore()
                }
            };
        }

        private double CalculateConfidenceScore()
        {
            double score = 0;

            if (!string.IsNullOrWhiteSpace(BasicInfo?.ProductTitle))
                score += 0.20;

            if (Pricing?.CurrentPrice is not null || !string.IsNullOrWhiteSpace(Pricing?.PriceTextRaw))
                score += 0.20;

            if (Cta?.HasAddToCartButton == true)
                score += 0.20;

            if (!string.IsNullOrWhiteSpace(Description?.DescriptionText))
                score += 0.15;

            if (Reviews?.HasReviews == true)
                score += 0.10;

            if (Faq?.HasFaq == true)
                score += 0.05;

            if ((Media?.ImageCount ?? 0) > 0)
                score += 0.10;

            return Math.Min(score, 1.0);
        }
    }
}

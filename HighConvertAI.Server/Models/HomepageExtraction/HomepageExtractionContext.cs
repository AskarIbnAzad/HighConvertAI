using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.HomepageExtraction.Sections;
using HighConvertAI.Server.Models.Scanning;
using HtmlAgilityPack;

namespace HighConvertAI.Server.Models.HomepageExtraction
{
    public sealed class HomepageExtractionContext
    {
        public PageScanContext Input { get; }
        public HtmlDocument Document { get; }

        public HeroSection? Hero { get; set; }
        public NavSection? Nav { get; set; }
        public FeatureSection? Features { get; set; }
        public TestimonialsSection? Testimonials { get; set; }
        public HomepageCroSignalsSection? CroSignals { get; set; }

        public List<ExtractionIssue> Issues { get; } = new();
        public List<ExtractionEvidence> Evidence { get; } = new();

        private HomepageExtractionContext(PageScanContext input, HtmlDocument document)
        {
            Input = input;
            Document = document;
        }

        public static HomepageExtractionContext Create(PageScanContext input)
        {
            var document = new HtmlDocument();
            document.LoadHtml(input.Html);

            return new HomepageExtractionContext(input, document);
        }

        public HomepageExtractionResult BuildResult()
        {
            return new HomepageExtractionResult
            {
                Url = Input.Url,
                PageType = Input.PageType,
                Hero = Hero,
                Nav = Nav,
                Features = Features,
                Testimonials = Testimonials,
                CroSignals = CroSignals,
                Issues = Issues,
                Evidence = Evidence,
                Meta = new ExtractionMeta
                {
                    ExtractedAtUtc = DateTime.UtcNow,
                    Version = "1.0",
                    Strategy = "BasicHomepageExtraction",
                    ConfidenceScore = CalculateConfidenceScore()
                }
            };
        }

        private double CalculateConfidenceScore()
        {
            double score = 0;

            if (!string.IsNullOrWhiteSpace(Hero?.Headline))
                score += 0.4;

            if (Nav is not null && Nav.LinkCount > 0)
                score += 0.2;

            if (Features is not null && Features.Features.Count > 0)
                score += 0.2;

            if (Testimonials?.HasTestimonials == true)
                score += 0.2;

            return Math.Min(score, 1.0);
        }
    }
}

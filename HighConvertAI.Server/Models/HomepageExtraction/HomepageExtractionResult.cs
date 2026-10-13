using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.HomepageExtraction.Sections;

namespace HighConvertAI.Server.Models.HomepageExtraction
{
    public sealed class HomepageExtractionResult
    {
        public string Url { get; set; } = string.Empty;
        public string PageType { get; set; } = string.Empty;

        public HeroSection? Hero { get; set; }
        public NavSection? Nav { get; set; }
        public FeatureSection? Features { get; set; }
        public TestimonialsSection? Testimonials { get; set; }
        public HomepageCroSignalsSection? CroSignals { get; set; }

        public List<ExtractionIssue> Issues { get; set; } = new();
        public List<ExtractionEvidence> Evidence { get; set; } = new();
        public ExtractionMeta? Meta { get; set; }
    }
}

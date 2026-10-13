using HighConvertAI.Server.Models.HomepageExtraction;
using HighConvertAI.Server.Models.HomepageExtraction.Sections;
using HighConvertAI.Server.Services.HomepageExtraction.Abstractions;

namespace HighConvertAI.Server.Services.HomepageExtraction.Extractors
{
    public sealed class TestimonialsExtractor : IHomepageSectionExtractor
    {
        public int Order => 30;

        public void Extract(HomepageExtractionContext context)
        {
            var doc = context.Document;

            var testimonialNodes = doc.DocumentNode.SelectNodes("//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'testimonial') or contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'reviews')]");
            var count = testimonialNodes?.Count ?? 0;

            context.Testimonials = new TestimonialsSection
            {
                HasTestimonials = count > 0,
                TestimonialCount = count
            };

            if (count > 0)
            {
                context.Evidence.Add(new HighConvertAI.Server.Models.ProductExtraction.ExtractionEvidence
                {
                    Section = "Testimonials",
                    SourceType = "dom",
                    SelectorOrPath = "class contains testimonial|reviews",
                    ExtractedValue = count.ToString()
                });
            }
        }
    }
}

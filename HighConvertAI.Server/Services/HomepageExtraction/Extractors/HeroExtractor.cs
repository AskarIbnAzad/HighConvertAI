using HighConvertAI.Server.Models.HomepageExtraction;
using HighConvertAI.Server.Models.HomepageExtraction.Sections;
using HighConvertAI.Server.Services.HomepageExtraction.Abstractions;
using HighConvertAI.Server.Services.HomepageExtraction.Helpers;

namespace HighConvertAI.Server.Services.HomepageExtraction.Extractors
{
    public sealed class HeroExtractor : IHomepageSectionExtractor
    {
        public int Order => 10;

        public void Extract(HomepageExtractionContext context)
        {
            var doc = context.Document;

            var headline = HomepageHtmlHelper.GetH1Text(doc);
            var sub = HomepageHtmlHelper.GetTextByClassContains(doc, "subtitle") ?? HomepageHtmlHelper.GetTextByClassContains(doc, "sub-title");

            string? ctaText = null;
            string? ctaHref = null;

            var ctaNode = doc.DocumentNode.SelectSingleNode("//a[contains(@class,'cta') or contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'cta')]")
                         ?? doc.DocumentNode.SelectSingleNode("//button[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'cta')]");

            if (ctaNode is not null)
            {
                ctaText = HomepageHtmlHelper.CleanText(ctaNode.InnerText);
                ctaHref = ctaNode.GetAttributeValue("href", null);
            }

            context.Hero = new HeroSection
            {
                Headline = headline,
                Subheadline = sub,
                PrimaryCtaText = ctaText,
                PrimaryCtaHref = ctaHref
            };

            if (!string.IsNullOrWhiteSpace(headline))
            {
                context.Evidence.Add(new HighConvertAI.Server.Models.ProductExtraction.ExtractionEvidence
                {
                    Section = "Hero",
                    SourceType = "dom",
                    SelectorOrPath = "h1 / .hero",
                    ExtractedValue = headline
                });
            }
        }
    }
}

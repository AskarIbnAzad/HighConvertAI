using HighConvertAI.Server.Models.HomepageExtraction;
using HighConvertAI.Server.Models.HomepageExtraction.Sections;
using HighConvertAI.Server.Services.HomepageExtraction.Abstractions;

namespace HighConvertAI.Server.Services.HomepageExtraction.Extractors
{
    public sealed class NavExtractor : IHomepageSectionExtractor
    {
        public int Order => 20;

        public void Extract(HomepageExtractionContext context)
        {
            var doc = context.Document;

            var navNodes = doc.DocumentNode.SelectNodes("//nav//a") ?? doc.DocumentNode.SelectNodes("//header//a");
            var linkCount = navNodes?.Count ?? 0;

            var hasSearch = doc.DocumentNode.SelectSingleNode("//input[contains(@type,'search') or contains(@class,'search')]") is not null;

            context.Nav = new NavSection
            {
                LinkCount = linkCount,
                HasSearch = hasSearch
            };

            context.Evidence.Add(new HighConvertAI.Server.Models.ProductExtraction.ExtractionEvidence
            {
                Section = "Nav",
                SourceType = "dom",
                SelectorOrPath = "//nav//a or //header//a",
                ExtractedValue = linkCount.ToString()
            });
        }
    }
}

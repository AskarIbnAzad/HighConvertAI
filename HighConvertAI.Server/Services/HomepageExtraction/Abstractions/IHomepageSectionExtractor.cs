using HighConvertAI.Server.Models.HomepageExtraction;

namespace HighConvertAI.Server.Services.HomepageExtraction.Abstractions
{
    public interface IHomepageSectionExtractor
    {
        int Order { get; }
        void Extract(HomepageExtractionContext context);
    }
}

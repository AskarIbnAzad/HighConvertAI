using HighConvertAI.Server.Models.HomepageExtraction;
using HighConvertAI.Server.Services.Abstractions;
using HighConvertAI.Server.Services.HomepageExtraction.Abstractions;

namespace HighConvertAI.Server.Services.HomepageExtraction;

public sealed class HomepageExtractionService(IEnumerable<IHomepageSectionExtractor> extractors) : IHomepageExtractionService
{
    private readonly IEnumerable<IHomepageSectionExtractor> _extractors = [.. extractors.OrderBy(e => e.Order)];

    public HomepageExtractionResult Extract(Models.Scanning.PageScanContext input)
    {
        var context = HomepageExtractionContext.Create(input);

        foreach (var extractor in _extractors)
        {
            try
            {
                extractor.Extract(context);
            }
            catch (Exception ex)
            {
                context.Issues.Add(new Models.ProductExtraction.ExtractionIssue
                {
                    Code = "EXTRACTOR_ERROR",
                    Message = ex.Message,
                    Severity = "Error"
                });
            }
        }

        return context.BuildResult();
    }
}

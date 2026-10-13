using HighConvertAI.Constants;
using HighConvertAI.Server.Models.CatalogExtraction;
using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.Scanning;
using HighConvertAI.Server.Services.Abstractions;
using HighConvertAI.Server.Services.CatalogExtraction.Abstractions;

namespace HighConvertAI.Server.Services.CatalogExtraction;

public sealed class CatalogPageExtractionService : ICatalogPageExtractionService
{
    private readonly IEnumerable<ICatalogSectionExtractor> _sectionExtractors;

    public CatalogPageExtractionService(IEnumerable<ICatalogSectionExtractor> sectionExtractors)
    {
        _sectionExtractors = sectionExtractors;
    }

    public CatalogPageExtractionResult Extract(PageScanContext context)
    {
        if (!string.Equals(context.PageType, PageTypes.Category, StringComparison.OrdinalIgnoreCase))
        {
            return new CatalogPageExtractionResult
            {
                Url = context.Url,
                PageType = context.PageType,
                Issues =
                [
                    new ExtractionIssue
                    {
                        Code = "NOT_CATALOG_PAGE",
                        Message = "Extraction skipped because page type is not Category.",
                        Severity = "Warning"
                    }
                ]
            };
        }

        var extractionContext = CatalogExtractionContext.Create(context);

        foreach (var extractor in _sectionExtractors.OrderBy(x => x.Order))
        {
            extractor.Extract(extractionContext);
        }

        return extractionContext.BuildResult();
    }
}

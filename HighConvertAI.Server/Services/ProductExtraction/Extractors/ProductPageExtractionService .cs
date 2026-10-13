using HighConvertAI.Constants;
using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.ProductExtraction.Sections;
using HighConvertAI.Server.Models.Scanning;
using HighConvertAI.Server.Services.Abstractions;
using HighConvertAI.Server.Services.ProductExtraction.Abstractions;

namespace HighConvertAI.Server.Services.ProductExtraction.Extractors
{
    public sealed class ProductPageExtractionService : IProductPageExtractionService
    {
        private readonly IEnumerable<IProductSectionExtractor> _sectionExtractors;

        public ProductPageExtractionService(IEnumerable<IProductSectionExtractor> sectionExtractors)
        {
            _sectionExtractors = sectionExtractors;
        }

        public ProductPageExtractionResult Extract(PageScanContext context)
        {
            if (context.PageType != PageTypes.Product)
            {
                return new ProductPageExtractionResult
                {
                    Url = context.Url,
                    PageType = context.PageType,
                    Issues = new[]
                    {
                    new ExtractionIssue
                    {
                        Code = "NOT_PRODUCT_PAGE",
                        Message = "Extraction skipped because page type is not Product.",
                        Severity = "Warning"
                    }
                }
                };
            }

            var extractionContext = ProductExtractionContext.Create(context);

            foreach (var extractor in _sectionExtractors.OrderBy(x => x.Order))
            {
                extractor.Extract(extractionContext);
            }

            return extractionContext.BuildResult();
        }
    }
}

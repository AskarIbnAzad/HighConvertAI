using HighConvertAI.Server.Models.CatalogExtraction.Sections;
using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.Scanning;
using HtmlAgilityPack;

namespace HighConvertAI.Server.Models.CatalogExtraction
{
    public sealed class CatalogExtractionContext
    {
        public PageScanContext Input { get; }
        public HtmlDocument Document { get; }

        public CatalogHeaderSection? Header { get; set; }
        public CatalogToolbarSection? Toolbar { get; set; }
        public CatalogFiltersSection? Filters { get; set; }
        public CatalogProductGridSection? ProductGrid { get; set; }
        public CatalogPaginationSection? Pagination { get; set; }

        public List<ExtractionIssue> Issues { get; } = new();
        public List<ExtractionEvidence> Evidence { get; } = new();

        private CatalogExtractionContext(PageScanContext input, HtmlDocument document)
        {
            Input = input;
            Document = document;
        }

        public static CatalogExtractionContext Create(PageScanContext input)
        {
            var document = new HtmlDocument();
            document.LoadHtml(input.Html);

            return new CatalogExtractionContext(input, document);
        }

        public CatalogPageExtractionResult BuildResult()
        {
            return new CatalogPageExtractionResult
            {
                Url = Input.Url,
                PageType = Input.PageType,
                Header = Header,
                Toolbar = Toolbar,
                Filters = Filters,
                ProductGrid = ProductGrid,
                Pagination = Pagination,
                Issues = Issues,
                Evidence = Evidence,
                Meta = new ExtractionMeta
                {
                    ExtractedAtUtc = DateTime.UtcNow,
                    Version = "1.0",
                    Strategy = "ShopifyCatalogStructuredExtraction",
                    ConfidenceScore = CalculateConfidenceScore()
                }
            };
        }

        private double CalculateConfidenceScore()
        {
            double score = 0;

            if (!string.IsNullOrWhiteSpace(Header?.Title))
                score += 0.25;

            if (ProductGrid?.HasProductGrid == true)
                score += 0.25;

            if ((ProductGrid?.Products.Count ?? 0) > 0)
                score += 0.25;

            if (Toolbar?.HasSortControl == true)
                score += 0.10;

            if (Filters?.HasFilters == true)
                score += 0.10;

            if (Pagination?.HasPagination == true || Pagination?.HasLoadMore == true)
                score += 0.05;

            return Math.Min(score, 1.0);
        }
    }
}

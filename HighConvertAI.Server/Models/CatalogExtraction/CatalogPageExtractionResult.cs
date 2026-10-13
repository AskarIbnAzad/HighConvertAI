using HighConvertAI.Server.Models.CatalogExtraction.Sections;
using HighConvertAI.Server.Models.ProductExtraction;

namespace HighConvertAI.Server.Models.CatalogExtraction
{
    public sealed class CatalogPageExtractionResult
    {
        public required string Url { get; set; }
        public required string PageType { get; set; }

        public CatalogHeaderSection? Header { get; set; }
        public CatalogToolbarSection? Toolbar { get; set; }
        public CatalogFiltersSection? Filters { get; set; }
        public CatalogProductGridSection? ProductGrid { get; set; }
        public CatalogPaginationSection? Pagination { get; set; }

        public IReadOnlyList<ExtractionIssue> Issues { get; set; } = [];
        public IReadOnlyList<ExtractionEvidence> Evidence { get; set; } = [];
        public ExtractionMeta Meta { get; set; } = new();
    }
}

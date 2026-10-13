namespace HighConvertAI.Server.Models.CatalogExtraction.Sections
{
    public sealed class CatalogPaginationSection
    {
        public bool HasPagination { get; set; }
        public bool HasLoadMore { get; set; }
        public bool HasInfiniteScrollHint { get; set; }

        public string? CurrentPageText { get; set; }
    }
}

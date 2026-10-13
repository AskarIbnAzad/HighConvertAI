namespace HighConvertAI.Server.Models.CatalogExtraction.Sections
{
    public sealed class CatalogToolbarSection
    {
        public bool HasSortControl { get; set; }
        public string? SelectedSortText { get; set; }

        public bool HasResultCount { get; set; }
        public string? ResultCountText { get; set; }

        public bool HasSearchWithinCollection { get; set; }
    }
}

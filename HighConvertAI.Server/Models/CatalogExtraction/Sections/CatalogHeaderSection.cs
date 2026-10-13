namespace HighConvertAI.Server.Models.CatalogExtraction.Sections
{
    public sealed class CatalogHeaderSection
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? BreadcrumbText { get; set; }
        public string? ResultCountText { get; set; }

        public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
        public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    }
}

namespace HighConvertAI.Server.Models.CatalogExtraction.Sections
{
    public sealed class CatalogProductCard
    {
        public string? Title { get; set; }
        public string? Url { get; set; }

        public string? PriceText { get; set; }
        public string? CompareAtPriceText { get; set; }

        public bool HasImage { get; set; }
        public string? ImageUrl { get; set; }

        public bool IsSoldOut { get; set; }
        public bool HasQuickAdd { get; set; }
        public bool HasRating { get; set; }
    }
}

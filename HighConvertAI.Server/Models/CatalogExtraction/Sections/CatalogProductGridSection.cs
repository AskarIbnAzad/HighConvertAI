namespace HighConvertAI.Server.Models.CatalogExtraction.Sections
{
    public sealed class CatalogProductGridSection
    {
        public bool HasProductGrid { get; set; }
        public IReadOnlyList<CatalogProductCard> Products { get; set; } = [];

        public int ProductCardCount => Products.Count;
    }
}

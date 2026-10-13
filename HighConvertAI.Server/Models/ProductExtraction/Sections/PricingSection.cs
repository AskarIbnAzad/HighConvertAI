namespace HighConvertAI.Server.Models.ProductExtraction.Sections
{
    public sealed class PricingSection
    {
        public decimal? CurrentPrice { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public string? Currency { get; set; }
        public bool IsOnSale { get; set; }
        public string? PriceTextRaw { get; set; }
    }
}

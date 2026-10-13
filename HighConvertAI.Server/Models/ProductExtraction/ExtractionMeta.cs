namespace HighConvertAI.Server.Models.ProductExtraction
{

    public sealed class ExtractionMeta
    {
        public DateTime ExtractedAtUtc { get; set; } = DateTime.UtcNow;
        public string Version { get; set; } = "1.0";
        public string Strategy { get; set; } = "ShopifyStructuredExtraction";
        public double ConfidenceScore { get; set; }
    }
}

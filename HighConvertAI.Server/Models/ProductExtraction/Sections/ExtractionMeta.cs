namespace HighConvertAI.Server.Models.ProductExtraction.Sections
{
    public sealed class ExtractionMeta
    {
        public DateTime ExtractedAtUtc { get; set; } = DateTime.UtcNow;
        public string Version { get; set; } = "1.0";
        public string Strategy { get; set; } = "shopify-structured-html";
        public double ConfidenceScore { get; set; }
    }
}

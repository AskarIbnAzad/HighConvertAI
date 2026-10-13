namespace HighConvertAI.Server.Models.ProductExtraction
{
    public sealed class ExtractionEvidence
    {
        public string Section { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty; // json-ld, dom, text, meta
        public string SelectorOrPath { get; set; } = string.Empty;
        public string? ExtractedValue { get; set; }
    }
}

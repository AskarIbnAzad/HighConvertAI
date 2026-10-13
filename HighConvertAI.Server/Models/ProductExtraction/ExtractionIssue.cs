namespace HighConvertAI.Server.Models.ProductExtraction
{
 
    public sealed class ExtractionIssue
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "Info"; // Info, Warning, Error
    }
}

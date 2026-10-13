

namespace HighConvertAI.Server.Models.Scanning
{
    public sealed class PageScanContext
    {
        public required string Url { get; set; }
        public required string Html { get; set; }
        public required string PageType { get; set; }

        public string? TitleTag { get; set; }
        public string? MetaDescription { get; set; }
    }
}
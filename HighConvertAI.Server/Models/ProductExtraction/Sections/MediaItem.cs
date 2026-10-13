namespace HighConvertAI.Server.Models.ProductExtraction.Sections
{
    public sealed class MediaItem
    {
        public string? Url { get; set; }
        public string? Type { get; set; } // image, video
        public string? AltText { get; set; }
        public bool IsPrimary { get; set; }
    }
}

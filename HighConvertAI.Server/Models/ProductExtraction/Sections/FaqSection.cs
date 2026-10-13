namespace HighConvertAI.Server.Models.ProductExtraction.Sections
{
    public sealed class FaqSection
    {
        public bool HasFaq { get; set; }
        public List<FaqItem> Items { get; set; } = new();
    }
}

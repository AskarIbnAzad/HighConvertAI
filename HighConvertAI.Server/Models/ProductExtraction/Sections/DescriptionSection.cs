namespace HighConvertAI.Server.Models.ProductExtraction.Sections
{
    public sealed class DescriptionSection
    {
        public bool HasDescription { get; set; }

        public string? DescriptionText { get; set; }

        public int DescriptionWordCount { get; set; }

        public IReadOnlyList<string> BulletPoints { get; set; } = [];

        public int BulletPointCount => BulletPoints.Count;

        public bool HasBullets => BulletPointCount > 0;

        public bool HasTabsOrAccordion { get; set; }

        public bool HasIngredients { get; set; }

        public bool HasSpecifications { get; set; }

        public bool HasHowToUse { get; set; }
    }
}

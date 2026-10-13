namespace HighConvertAI.Server.Models.ProductExtraction.Sections
{


    public sealed class MediaSection
    {
        public int ImageCount { get; set; }

        public int VideoCount { get; set; }

        public bool HasImages => ImageCount > 0;

        public bool HasMultipleImages => ImageCount > 1;

        public bool HasVideo => VideoCount > 0;

        public int MissingAltTextCount { get; set; }

        public IReadOnlyList<ProductImageInfo> Images { get; set; } = [];

        public IReadOnlyList<ProductVideoInfo> Videos { get; set; } = [];
    }

    public sealed class ProductImageInfo
    {
        public string? Src { get; set; }

        public string? Alt { get; set; }

        public string? Width { get; set; }

        public string? Height { get; set; }

        public bool HasAltText => !string.IsNullOrWhiteSpace(Alt);
    }

    public sealed class ProductVideoInfo
    {
        public string? Src { get; set; }

        public string? Type { get; set; }
    }
}

namespace HighConvertAI.Server.Models.ProductExtraction.Sections
{
    public sealed class ReviewsSection
    {
        public bool HasReviews { get; set; }
        public decimal? Rating { get; set; }
        public int? ReviewCount { get; set; }
        public bool HasStarRating { get; set; }
    }
}

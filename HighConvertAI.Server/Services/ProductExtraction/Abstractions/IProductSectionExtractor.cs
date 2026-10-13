using HighConvertAI.Server.Models.ProductExtraction;

namespace HighConvertAI.Server.Services.ProductExtraction.Abstractions
{
    public interface IProductSectionExtractor
    {
        int Order { get; }
        void Extract(ProductExtractionContext context);
    }
}

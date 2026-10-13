using HighConvertAI.Server.Models.CatalogExtraction;

namespace HighConvertAI.Server.Services.CatalogExtraction.Abstractions
{
    public interface ICatalogSectionExtractor
    {
        int Order { get; }
        void Extract(CatalogExtractionContext context);
    }
}

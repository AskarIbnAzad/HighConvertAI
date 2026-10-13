using HighConvertAI.Server.Models.CatalogExtraction;
using HighConvertAI.Server.Models.Scanning;

namespace HighConvertAI.Server.Services.Abstractions
{
    public interface ICatalogPageExtractionService
    {
        CatalogPageExtractionResult Extract(PageScanContext context);
    }
}

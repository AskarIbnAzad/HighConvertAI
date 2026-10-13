using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.ProductExtraction.Sections;
using HighConvertAI.Server.Models.Scanning;

namespace HighConvertAI.Server.Services.Abstractions
{

    public interface IProductPageExtractionService
    {
        ProductPageExtractionResult Extract(PageScanContext context);
    }

}

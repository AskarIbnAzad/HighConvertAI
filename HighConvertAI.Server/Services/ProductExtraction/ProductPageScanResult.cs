using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.Scanning;

namespace HighConvertAI.Server.Services.ProductExtraction;

public class ProductPageScanResult(ProductPageExtractionResult productPageExtractionResult, PageScanResult pageScanResult) : PageScanResult(pageScanResult.Url, pageScanResult.PageType)
{
    public ProductPageExtractionResult ProductExtraction { get; set; } = productPageExtractionResult;
}

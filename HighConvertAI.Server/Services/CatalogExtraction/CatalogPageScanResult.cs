using HighConvertAI.Server.Models.CatalogExtraction;
using HighConvertAI.Server.Models.Scanning;

namespace HighConvertAI.Server.Services.CatalogExtraction;

public class CatalogPageScanResult(CatalogPageExtractionResult catalogExtraction, PageScanResult pageScanResult) : PageScanResult(pageScanResult)
{
    public CatalogPageExtractionResult CatalogExtraction { get; set; } = catalogExtraction;
}

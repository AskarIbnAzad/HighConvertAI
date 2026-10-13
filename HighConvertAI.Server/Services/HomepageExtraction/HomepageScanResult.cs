using HighConvertAI.Server.Models.HomepageExtraction;
using HighConvertAI.Server.Models.Scanning;

namespace HighConvertAI.Server.Services.HomepageExtraction;

public class HomepageScanResult(HomepageExtractionResult homepageExtractionResult, PageScanResult pageScanResult) : PageScanResult(pageScanResult)
{
    public HomepageExtractionResult HomepageExtraction { get; set; } = homepageExtractionResult;
}

using HighConvertAI.Server.Models.Scanning;

namespace HighConvertAI.Server.Models.AiAnalysis;

public sealed class PageScanAiAnalysisResponse
{
    public required PageScanResult ScanResult { get; set; }
    public required AiAnalysisResponse Analysis { get; set; }
}

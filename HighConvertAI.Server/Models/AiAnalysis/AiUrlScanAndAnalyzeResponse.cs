namespace HighConvertAI.Server.Models.AiAnalysis;

public sealed class AiUrlScanAndAnalyzeResponse
{
    public required AiUrlScanResult ScanResult { get; set; }
    public required AiAnalysisResponse Analysis { get; set; }
}

public sealed class AiUrlScanResult
{
    public string Url { get; set; } = string.Empty;
    public string PageType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PrimaryHeading { get; set; } = string.Empty;
    public List<string> SecondaryHeadings { get; set; } = [];
    public List<string> PrimaryCtas { get; set; } = [];
    public List<string> KeySignals { get; set; } = [];
    public string TextSample { get; set; } = string.Empty;
}
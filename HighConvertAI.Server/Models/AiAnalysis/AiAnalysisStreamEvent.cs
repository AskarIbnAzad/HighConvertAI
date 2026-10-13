namespace HighConvertAI.Server.Models.AiAnalysis;

public sealed class AiAnalysisStreamEvent
{
    public string EventType { get; set; } = "chunk";
    public string? Message { get; set; }
    public AiInsightItem? Item { get; set; }
    public AiAnalysisStats? Stats { get; set; }
    public AiAnalysisResponse? Final { get; set; }
}

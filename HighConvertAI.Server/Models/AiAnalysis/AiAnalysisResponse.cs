namespace HighConvertAI.Server.Models.AiAnalysis;

public sealed class AiAnalysisResponse
{
    public string Summary { get; set; } = string.Empty;
    public List<AiInsightItem> Items { get; set; } = [];
    public List<AiInsightGroup> Groups { get; set; } = [];
    public AiAnalysisStats Stats { get; set; } = new();
}

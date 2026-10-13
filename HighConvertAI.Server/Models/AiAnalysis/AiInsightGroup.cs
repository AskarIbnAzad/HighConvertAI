namespace HighConvertAI.Server.Models.AiAnalysis;

public sealed class AiInsightGroup
{
    public string GroupType { get; set; } = "General";
    public int IssueCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public decimal EstimatedRevenueIncreasePercent { get; set; }
    public string Condition { get; set; } = "Average";
    public List<AiInsightItem> Items { get; set; } = [];
}
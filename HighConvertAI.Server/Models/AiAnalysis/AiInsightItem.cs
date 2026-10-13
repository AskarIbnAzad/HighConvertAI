namespace HighConvertAI.Server.Models.AiAnalysis;

public sealed class AiInsightItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string GroupType { get; set; } = "General";
    public string Severity { get; set; } = "Low";
    public string Title { get; set; } = string.Empty;
    public string WhatWasRight { get; set; } = string.Empty;
    public string WhatToImprove { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

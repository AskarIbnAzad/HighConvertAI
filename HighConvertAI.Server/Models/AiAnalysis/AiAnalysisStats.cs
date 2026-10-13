namespace HighConvertAI.Server.Models.AiAnalysis;

public sealed class AiAnalysisStats
{
    public int PassedTests { get; set; }
    public int IssueCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public int TotalChecks { get; set; }
    public decimal Score { get; set; }                          // was int
    public decimal EstimatedRevenueIncreasePercent { get; set; } // was int
}

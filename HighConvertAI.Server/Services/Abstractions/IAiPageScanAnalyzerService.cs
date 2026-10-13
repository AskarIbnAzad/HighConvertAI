using HighConvertAI.Server.Models.AiAnalysis;
using HighConvertAI.Server.Models.DatabaseModels;
using HighConvertAI.Server.Models.Scanning;

namespace HighConvertAI.Server.Services.Abstractions;

public interface IAiPageScanAnalyzerService
{
    Task<List<Issue>> AnalyzeAsync(PageScanResult pageScanResult, List<IssueGroup> IssueGroups, List<IssueSeverity> IssueSaverities, CancellationToken cancellationToken = default);

    IAsyncEnumerable<AiAnalysisStreamEvent> StreamAnalysisAsync(PageScanResult pageScanResult, CancellationToken cancellationToken = default);
}

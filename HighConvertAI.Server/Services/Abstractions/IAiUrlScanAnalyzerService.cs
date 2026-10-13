using HighConvertAI.Server.Models.AiAnalysis;

namespace HighConvertAI.Server.Services.Abstractions;

public interface IAiUrlScanAnalyzerService
{
    Task<AiUrlScanAndAnalyzeResponse> AnalyzeUrlAsync(string url, CancellationToken cancellationToken = default);
}
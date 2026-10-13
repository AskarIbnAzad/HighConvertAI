using HighConvertAI.Server.Models.ApiResponse;
using HighConvertAI.Server.Models.DatabaseModels;
using HighConvertAI.Server.Models.DTO;
using HighConvertAI.Server.Models.Scanning;

namespace HighConvertAI.Server.Services.Abstractions
{
    public interface IPageScanService
    {
        Task<PageScanResult> ScanAsync(string url, CancellationToken cancellationToken = default);

        /// <summary>
        /// Extracts structural CRO data from HTML and computes a SHA-256 hash of the extracted result.
        /// </summary>
        Task<(PageScanResult ScanResult, string Hash)> ProcessHtmlAndHashAsync(string url, string html, CancellationToken cancellationToken = default);

        Task<(bool HasBeenScanned, int? PageScanId)> GetExistingScanAsync(string url, string extractionHash, CancellationToken cancellationToken = default);

        Task<PageScan?> GetPageScanByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<PageScan> ClonePageScanAsync(int existingPageScanId, Guid? userId, HttpContext httpContext, int processingTimeMs, CancellationToken cancellationToken = default);

        Task<List<IssueGroup>?> GetIssueGroupAsync();

        Task<List<IssueSeverity>?> GetIssueSeverityAsync();

        Task<ApiResponse> ValidateScanLimitAsync(Guid? userId, string? ipAddress);

        Task<PageScan> GeneratePageScanModel(Guid? userId, HttpContext httpContext, string url, string pageType, int processingTimeMs, List<Issue> issues, string? htmlHash = null);

        Task<PageScan?> GetCachedScanResultAsync(string url, string currentHtmlHash);
    }
}

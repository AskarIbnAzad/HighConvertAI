using HighConvertAI.Server.Extensions;
using HighConvertAI.Server.Models.AiAnalysis;
using HighConvertAI.Server.Models.ApiResponse;
using HighConvertAI.Server.Models.DTO;
using HighConvertAI.Server.Models.Scanning;
using HighConvertAI.Server.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace HighConvertAI.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PageScansController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IPageScanService _pageScanService;
        private readonly IPageCrawlerService _pageCrawlerService;
        private readonly IAiPageScanAnalyzerService _aiPageScanAnalyzerService;
        private readonly IAiUrlScanAnalyzerService _aiUrlScanAnalyzerService;

        public PageScansController(
            IAuthService authService,
            IPageScanService pageScanService,
            IPageCrawlerService pageCrawlerService,
            IAiPageScanAnalyzerService aiPageScanAnalyzerService,
            IAiUrlScanAnalyzerService aiUrlScanAnalyzerService)
        {
            _authService = authService;
            _pageScanService = pageScanService;
            _pageCrawlerService = pageCrawlerService;
            _aiPageScanAnalyzerService = aiPageScanAnalyzerService;
            _aiUrlScanAnalyzerService = aiUrlScanAnalyzerService;
        }

        [HttpPost("scan")]
        public async Task<ActionResult<PageScanResult>> ScanAsync([FromQuery] string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest("URL is required.");

            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                return BadRequest("A valid absolute URL is required.");

            var result = await _pageScanService.ScanAsync(url, cancellationToken);
            return Ok(result);
        }

        [HttpPost("scan-and-analyze")]
        public async Task<ActionResult> ScanAndAnalyzeAsync(
            [FromQuery] string url,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Title = "Invalid Website",
                    Message = "Please enter a website URL."
                });
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Title = "Invalid Website",
                    Message = "Please enter a valid website URL."
                });
            }

            Guid? userId = _authService.GetCurrentUserId();
            string ipAddress = HttpContext.GetClientIpAddress();

            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                //var validation = await _pageScanService.ValidateScanLimitAsync(userId, ipAddress);

                //if (!validation.Success)
                //{
                //    return StatusCode(StatusCodes.Status403Forbidden, validation);
                //}

                // 1. Crawl raw HTML
                var rawHtml = await _pageCrawlerService.CrawlHtmlAsync(url, cancellationToken);

                // 2. Extract structured CRO data and derive deterministic model hash
                  (PageScanResult scanResult, string hash) = await _pageScanService.ProcessHtmlAndHashAsync(url, rawHtml, cancellationToken);
                // 3. Query DB for matching URL + Extraction Hash
                (bool hasBeenScanned, int? existingScanId) = await _pageScanService.GetExistingScanAsync(url, hash, cancellationToken);

                if (hasBeenScanned && existingScanId.HasValue)
                {
                    stopwatch.Stop();

                    // CACHE HIT: Content layout hasn't changed.
                    // Clone data into a new record with current request/geo details.
                    var clonedScan = await _pageScanService.ClonePageScanAsync(
                        existingScanId.Value,
                        userId,
                        HttpContext,
                        (int)stopwatch.ElapsedMilliseconds,
                        cancellationToken);

                    return Ok(new ScanAndAnalyzeResponseDto
                    {
                        AuditId = clonedScan.Id,
                        Url = clonedScan.Url,
                        PageType = clonedScan.PageType,
                        Score = clonedScan.Score
                    });
                }

                // CACHE MISS: Run full AI analysis
                var issueGroups = await _pageScanService.GetIssueGroupAsync();
                var issueSeverities = await _pageScanService.GetIssueSeverityAsync();

                var analysis = await _aiPageScanAnalyzerService.AnalyzeAsync(
                    scanResult,
                    issueGroups,
                    issueSeverities,
                    cancellationToken);

                if (analysis == null || analysis.Count == 0)
                {
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new ApiResponse
                        {
                            Success = false,
                            Title = "Audit Failed",
                            Message = "We were unable to complete the audit. Please try again later."
                        });
                }

                stopwatch.Stop();

                var pageScan = await _pageScanService.GeneratePageScanModel(
                    userId,
                    HttpContext,
                    url,
                    scanResult.PageType,
                    (int)stopwatch.ElapsedMilliseconds,
                    analysis,
                    hash);

                return Ok(new ScanAndAnalyzeResponseDto
                {
                    AuditId = pageScan.Id,
                    Url = scanResult.Url,
                    PageType = pageScan.PageType,
                    Score = pageScan.Score
                });
            }
            catch (Exception)
            {
                stopwatch.Stop();

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ApiResponse
                    {
                        Success = false,
                        Title = "Audit Failed",
                        Message = "Something went wrong while processing the audit. Please try again later."
                    });
            }
        }

        [HttpPost("scan-and-analyze-ai")]
        public async Task<ActionResult<AiUrlScanAndAnalyzeResponse>> ScanAndAnalyzeAiAsync([FromQuery] string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest("URL is required.");

            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                return BadRequest("A valid absolute URL is required.");

            try
            {
                var result = await _aiUrlScanAnalyzerService.AnalyzeUrlAsync(url, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to AI scan and analyze the page. {ex.Message}");
            }
        }

        [HttpPost("scan-and-analyze/stream")]
        public async Task ScanAndAnalyzeStreamAsync([FromQuery] string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                await Response.WriteAsync("URL is required.", cancellationToken);
                return;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                await Response.WriteAsync("A valid absolute URL is required.", cancellationToken);
                return;
            }

            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";

            try
            {
                var scanResult = await _pageScanService.ScanAsync(url, cancellationToken);

                await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { eventType = "scanComplete", scanResult })}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                await foreach (var streamEvent in _aiPageScanAnalyzerService.StreamAnalysisAsync(scanResult, cancellationToken))
                {
                    var json = JsonSerializer.Serialize(streamEvent);
                    await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                var json = JsonSerializer.Serialize(new AiAnalysisStreamEvent
                {
                    EventType = "error",
                    Message = ex.Message
                });
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
    }
}
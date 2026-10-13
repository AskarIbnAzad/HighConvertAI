using HighConvertAI.Constants;
using HighConvertAI.Server.Data;
using HighConvertAI.Server.Extensions;
using HighConvertAI.Server.Models.ApiResponse;
using HighConvertAI.Server.Models.CatalogExtraction;
using HighConvertAI.Server.Models.DatabaseModels;
using HighConvertAI.Server.Models.DTO;
using HighConvertAI.Server.Models.ProductExtraction;
using HighConvertAI.Server.Models.Scanning;
using HighConvertAI.Server.Services.Abstractions;
using HighConvertAI.Server.Services.CatalogExtraction;
using HighConvertAI.Server.Services.HomepageExtraction;
using HighConvertAI.Server.Services.ProductExtraction;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using UAParser;

namespace HighConvertAI.Server.Services
{
    public sealed class PageScanService : IPageScanService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPageCrawlerService _pageCrawlerService;
        private readonly IPageTypeDetectionService _pageTypeDetectionService;
        private readonly IProductPageExtractionService _productPageExtractionService;
        private readonly ICatalogPageExtractionService _catalogPageExtractionService;
        private readonly IHomepageExtractionService _homepageExtractionService;
        private readonly IHttpClientFactory _httpClientFactory;

        public PageScanService(
            ApplicationDbContext db,
            IHttpClientFactory httpClientFactory,
            IPageCrawlerService pageCrawlerService,
            IPageTypeDetectionService pageTypeDetectionService,
            IProductPageExtractionService productPageExtractionService,
            ICatalogPageExtractionService catalogPageExtractionService,
            IHomepageExtractionService homepageExtractionService)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _pageCrawlerService = pageCrawlerService;
            _pageTypeDetectionService = pageTypeDetectionService;
            _productPageExtractionService = productPageExtractionService;
            _catalogPageExtractionService = catalogPageExtractionService;
            _homepageExtractionService = homepageExtractionService;
        }

        public async Task<PageScanResult> ScanAsync(string url, CancellationToken cancellationToken = default)
        {
            var html = await _pageCrawlerService.CrawlHtmlAsync(url, cancellationToken);
            var pageType = _pageTypeDetectionService.Detect(html, url);

            var context = new PageScanContext
            {
                Url = url,
                Html = html,
                PageType = pageType
            };

            var pageScanResult = new PageScanResult
            {
                Url = url,
                PageType = pageType
            };

            return pageType switch
            {
                PageTypes.Product => new ProductPageScanResult(_productPageExtractionService.Extract(context), pageScanResult),
                PageTypes.Home => new HomepageScanResult(_homepageExtractionService.Extract(context), pageScanResult),
                PageTypes.Category => new CatalogPageScanResult(_catalogPageExtractionService.Extract(context), pageScanResult),
                _ => pageScanResult
            };
        }

        public async Task<(PageScanResult ScanResult, string Hash)> ProcessHtmlAndHashAsync(string url, string html, CancellationToken cancellationToken = default)
        {
            var pageType = _pageTypeDetectionService.Detect(html, url);

            var context = new PageScanContext
            {
                Url = url,
                Html = html,
                PageType = pageType
            };

            var baseResult = new PageScanResult
            {
                Url = url,
                PageType = pageType
            };

            PageScanResult scanResult = pageType switch
            {
                PageTypes.Product => new ProductPageScanResult(_productPageExtractionService.Extract(context), baseResult),
                PageTypes.Home => new HomepageScanResult(_homepageExtractionService.Extract(context), baseResult),
                PageTypes.Category => new CatalogPageScanResult(_catalogPageExtractionService.Extract(context), baseResult),
                _ => baseResult
            };

            string hash = HtmlHashUtility.ComputeObjectHash(scanResult);

            return (scanResult, hash);
        }

        public async Task<(bool HasBeenScanned, int? PageScanId)> GetExistingScanAsync(string url, string htmlHash, CancellationToken cancellationToken = default)
        {
            var existingScanId = await _db.PageScans
                .Where(x => x.Url == url && x.HtmlHash == htmlHash)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return (existingScanId.HasValue, existingScanId);
        }

        public async Task<PageScan?> GetPageScanByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _db.PageScans
                .Include(p => p.PageScanDetail)
                .Include(p => p.IssueGroup)
                    .ThenInclude(ig => ig.Issues)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<PageScan> GeneratePageScanModel(
            Guid? userId,
            HttpContext httpContext,
            string url,
            string pageType,
            int processingTimeMs,
            List<Issue> issues,
            string? htmlHash = null)
        {
            decimal score = 0;
            foreach (Issue obj in issues)
            {
                obj.IssueSeverity = await _db.IssueSeveritys.FindAsync(obj.IssueSeverityId);
            }

            List<IssueGroup> issueGroups = await _db.IssueGroups.ToListAsync();
            foreach (IssueGroup obj in issueGroups)
            {
                obj.Issues = issues.Where(p => p.IssueGroupId == obj.Id).ToList();
                obj.IssueCount = obj.Issues.Count(p => p.IssueSeverityId > 0);
                obj.LowCount = obj.Issues.Count(p => p.IssueSeverityId == 1);
                obj.MediumCount = obj.Issues.Count(p => p.IssueSeverityId == 2);
                obj.HighCount = obj.Issues.Count(p => p.IssueSeverityId == 3);
                obj.IssueSeverityId = obj.Issues.Any() ? obj.Issues.Max(p => p.IssueSeverityId) : 0;
                obj.IssueSeverity = await _db.IssueSeveritys.FindAsync(obj.IssueSeverityId);
                obj.AffectedRevenueOpportunity = obj.Issues.Sum(p => p.IssueSeverity?.AffectedRevenueOpportunity ?? 0);
                obj.AffectedScore = obj.Issues.Sum(p => p.IssueSeverity?.AffectedScore ?? 0);
            }

            PageScan pageScanObject = await SavePageScan(userId, httpContext, url, pageType, score, processingTimeMs, htmlHash);
            pageScanObject.IssueGroup = issueGroups.Where(p => p.Issues?.Count > 0).ToList();
            pageScanObject.IssueCount = pageScanObject.IssueGroup.Sum(p => p.IssueCount);
            pageScanObject.HighCount = pageScanObject.IssueGroup.Sum(p => p.HighCount);
            pageScanObject.MediumCount = pageScanObject.IssueGroup.Sum(p => p.MediumCount);
            pageScanObject.LowCount = pageScanObject.IssueGroup.Sum(p => p.LowCount);
            pageScanObject.IssueSeverityId = pageScanObject.IssueGroup.Any() ? pageScanObject.IssueGroup.Max(p => p.IssueSeverityId) : 0;
            pageScanObject.IssueSeverity = await _db.IssueSeveritys.FindAsync(pageScanObject.IssueSeverityId);
            pageScanObject.AffectedRevenueOpportunity = pageScanObject.IssueGroup.Sum(p => p.AffectedRevenueOpportunity);
            pageScanObject.Score = 100 - pageScanObject.IssueGroup.Sum(p => p.AffectedScore);

            foreach (Issue obj in issues)
            {
                obj.PageScanId = pageScanObject.Id;
            }
            await _db.Issues.AddRangeAsync(issues);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };

            var json = JsonSerializer.Serialize(pageScanObject, jsonOptions);

            if (userId == null)
            {
                foreach (var grp in pageScanObject.IssueGroup)
                {
                    foreach (var issue in grp.Issues)
                    {
                        issue.Recommendation = string.Empty;
                    }
                }
                var jsonUnAuthorised = JsonSerializer.Serialize(pageScanObject, jsonOptions);
                pageScanObject.ResultJsonUnAuthorised = jsonUnAuthorised;
            }

            pageScanObject.ResultJson = json;
            _db.PageScans.Update(pageScanObject);
            await _db.SaveChangesAsync();

            return pageScanObject;
        }

        public async Task<PageScan> ClonePageScanAsync(
            int existingPageScanId,
            Guid? userId,
            HttpContext httpContext,
            int processingTimeMs,
            CancellationToken cancellationToken = default)
        {
            var existingScan = await _db.PageScans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == existingPageScanId, cancellationToken);

            if (existingScan == null)
                throw new InvalidOperationException($"PageScan with ID {existingPageScanId} was not found.");

            var existingIssues = await _db.Issues
                .AsNoTracking()
                .Where(i => i.PageScanId == existingPageScanId)
                .ToListAsync(cancellationToken);

            var ua = httpContext.Request.Headers.UserAgent.ToString();
            var client = Parser.GetDefault().Parse(ua);

            string browser = $"{client.UA.Family} {client.UA.Major}";
            string os = $"{client.OS.Family} {client.OS.Major}";
            string device = client.Device.Family == "Other" ? "Desktop" : client.Device.Family;

            var ip = httpContext.GetClientIpAddress();
            string? country = null;
            string? city = null;

            // Resolve Geolocation (Handles local loopback by querying public IP if needed for testing)
            (country, city) = await GetGeolocationAsync(ip, cancellationToken);

            var newPageScan = new PageScan
            {
                UserId = userId,
                IpAddress = ip,
                Url = existingScan.Url,
                PageType = existingScan.PageType,
                HtmlHash = existingScan.HtmlHash,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.PageScans.Add(newPageScan);
            await _db.SaveChangesAsync(cancellationToken);

            var detail = new PageScanDetail
            {
                PageScanId = newPageScan.Id,
                Browser = browser,
                OperatingSystem = os,
                DeviceType = device,
                Country = country,
                City = city,
                ProcessingTimeMs = processingTimeMs
            };

            _db.PageScanDetails.Add(detail);

            var newIssues = new List<Issue>();
            foreach (var issue in existingIssues)
            {
                var clonedIssue = new Issue
                {
                    PageScanId = newPageScan.Id,
                    IssueGroupId = issue.IssueGroupId,
                    IssueSeverityId = issue.IssueSeverityId,
                    IssueSeverity = await _db.IssueSeveritys.FindAsync(issue.IssueSeverityId),
                    Title = issue.Title,
                    WhatWasRight = issue.WhatWasRight,
                    WhatToImprove = issue.WhatToImprove,
                    Recommendation = issue.Recommendation
                };
                newIssues.Add(clonedIssue);
            }

            await _db.Issues.AddRangeAsync(newIssues, cancellationToken);

            List<IssueGroup> issueGroups = await _db.IssueGroups.AsNoTracking().ToListAsync(cancellationToken);
            foreach (IssueGroup group in issueGroups)
            {
                group.Issues = newIssues.Where(p => p.IssueGroupId == group.Id).ToList();
                group.IssueCount = group.Issues.Count(p => p.IssueSeverityId > 0);
                group.LowCount = group.Issues.Count(p => p.IssueSeverityId == 1);
                group.MediumCount = group.Issues.Count(p => p.IssueSeverityId == 2);
                group.HighCount = group.Issues.Count(p => p.IssueSeverityId == 3);
                group.IssueSeverityId = group.Issues.Any() ? group.Issues.Max(p => p.IssueSeverityId) : null;
                group.IssueSeverity = group.IssueSeverityId.HasValue
                    ? await _db.IssueSeveritys.FindAsync(group.IssueSeverityId.Value)
                    : null;
                group.AffectedRevenueOpportunity = group.Issues.Sum(p => p.IssueSeverity?.AffectedRevenueOpportunity ?? 0);
                group.AffectedScore = group.Issues.Sum(p => p.IssueSeverity?.AffectedScore ?? 0);
            }

            newPageScan.IssueGroup = issueGroups.Where(p => p.Issues?.Count > 0).ToList();
            newPageScan.IssueCount = newPageScan.IssueGroup.Sum(p => p.IssueCount);
            newPageScan.HighCount = newPageScan.IssueGroup.Sum(p => p.HighCount);
            newPageScan.MediumCount = newPageScan.IssueGroup.Sum(p => p.MediumCount);
            newPageScan.LowCount = newPageScan.IssueGroup.Sum(p => p.LowCount);

            newPageScan.IssueSeverityId = newPageScan.IssueGroup.Any()
                ? newPageScan.IssueGroup.Max(p => p.IssueSeverityId)
                : null;

            newPageScan.IssueSeverity = newPageScan.IssueSeverityId.HasValue
                ? await _db.IssueSeveritys.FindAsync(newPageScan.IssueSeverityId.Value)
                : null;

            newPageScan.AffectedRevenueOpportunity = newPageScan.IssueGroup.Sum(p => p.AffectedRevenueOpportunity);
            newPageScan.Score = 100 - newPageScan.IssueGroup.Sum(p => p.AffectedScore);
            newPageScan.PageScanDetail = detail;

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };

            var json = JsonSerializer.Serialize(newPageScan, jsonOptions);

            if (userId == null)
            {
                foreach (var grp in newPageScan.IssueGroup)
                {
                    foreach (var issue in grp.Issues)
                    {
                        issue.Recommendation = string.Empty;
                    }
                }

                var jsonUnAuthorised = JsonSerializer.Serialize(newPageScan, jsonOptions);
                newPageScan.ResultJsonUnAuthorised = jsonUnAuthorised;
            }

            newPageScan.ResultJson = json;

            _db.PageScans.Update(newPageScan);
            await _db.SaveChangesAsync(cancellationToken);

            return newPageScan;
        }

        private async Task<PageScan> SavePageScan(
            Guid? userId,
            HttpContext httpContext,
            string url,
            string pageType,
            decimal score,
            int processingTimeMs,
            string? htmlHash = null,
            CancellationToken cancellationToken = default)
        {
            var ua = httpContext.Request.Headers.UserAgent.ToString();
            var client = Parser.GetDefault().Parse(ua);

            string browser = $"{client.UA.Family} {client.UA.Major}";
            string os = $"{client.OS.Family} {client.OS.Major}";
            string device = client.Device.Family == "Other" ? "Desktop" : client.Device.Family;

            var ip = httpContext.GetClientIpAddress();
            string? country = null;
            string? city = null;

            // Resolve Geolocation
            (country, city) = await GetGeolocationAsync(ip, cancellationToken);

            var pageScan = new PageScan
            {
                UserId = userId,
                IpAddress = ip,
                Url = url,
                PageType = pageType,
                Score = score,
                HtmlHash = htmlHash,
                ResultJson = null,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.PageScans.Add(pageScan);
            await _db.SaveChangesAsync(cancellationToken);

            var detail = new PageScanDetail
            {
                PageScanId = pageScan.Id,
                Browser = browser,
                OperatingSystem = os,
                DeviceType = device,
                Country = country,
                City = city,
                ProcessingTimeMs = processingTimeMs
            };

            _db.PageScanDetails.Add(detail);
            await _db.SaveChangesAsync(cancellationToken);

            pageScan.PageScanDetail = detail;
            return pageScan;
        }

        private async Task<(string? Country, string? City)> GetGeolocationAsync(string? ip, CancellationToken cancellationToken)
        {
            string? country = null;
            string? city = null;

            // Determine query target. If local loopback, you can substitute a test IP or leave blank 
            // so ip-api evaluates your machine's actual public IP endpoint.
            string queryIp = ip;
            if (string.IsNullOrWhiteSpace(ip) || ip == "::1" || ip == "127.0.0.1")
            {
                queryIp = ""; // Leaving blank tells ip-api to check the caller's public IP address
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var geo = await httpClient.GetFromJsonAsync<IpApiResponse>(
                    $"http://ip-api.com/json/{queryIp}?fields=country,city",
                    cancellationToken);

                country = geo?.Country;
                city = geo?.City;
            }
            catch
            {
                // Fallback or ignore network errors
            }

            return (country, city);
        }

        public async Task<List<IssueGroup>?> GetIssueGroupAsync()
        {
            return await _db.IssueGroups
                .OrderBy(x => x.SortingOrder)
                .ToListAsync();
        }

        public async Task<List<IssueSeverity>?> GetIssueSeverityAsync()
        {
            return await _db.IssueSeveritys
                .OrderBy(x => x.SortingOrder)
                .ToListAsync();
        }

        public async Task<ApiResponse> ValidateScanLimitAsync(Guid? userId, string? ipAddress)
        {
            if (userId.HasValue)
            {
                var oneWeekAgo = DateTime.UtcNow.AddDays(-7);

                var totalScans = await _db.PageScans.CountAsync(x =>
                    x.UserId == userId &&
                    x.CreatedAtUtc >= oneWeekAgo);

                if (totalScans >= 2)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "You have reached your weekly limit of 2 scans."
                    };
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(ipAddress))
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Unable to determine your IP address."
                    };
                }

                var anonymousScans = await _db.PageScans.CountAsync(x =>
                    x.UserId == null &&
                    x.IpAddress == ipAddress);

                if (anonymousScans >= 2)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "You have already used your 2 free anonymous scans. Please create a free account to continue."
                    };
                }
            }

            return new ApiResponse
            {
                Success = true
            };
        }

        public async Task<PageScan?> GetCachedScanResultAsync(string url, string currentHtmlHash)
        {
            return await _db.PageScans
                .Include(p => p.IssueGroup)
                    .ThenInclude(ig => ig.Issues)
                .Where(p => p.Url == url && p.HtmlHash == currentHtmlHash)
                .OrderByDescending(p => p.CreatedAtUtc)
                .FirstOrDefaultAsync();
        }
    }
}
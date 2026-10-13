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
using HighConvertAI.Server.Services.Otp;
using HighConvertAI.Server.Services.ProductExtraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using UAParser;



namespace HighConvertAI.Server.Services
{
    public sealed class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPageCrawlerService _pageCrawlerService;
        private readonly IPageTypeDetectionService _pageTypeDetectionService;
        private readonly IProductPageExtractionService _productPageExtractionService;
        private readonly ICatalogPageExtractionService _catalogPageExtractionService;
        private readonly IHomepageExtractionService _homepageExtractionService;
        private readonly IHttpClientFactory _httpClientFactory;

        public DashboardService(
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

        public async Task<DashboardStatisticDto> GetAdminOverview()
        {
            var obj = new DashboardStatisticDto();
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
            var eightWeeksAgo = DateTime.UtcNow.AddDays(-56);

            // Run simple count & aggregation queries directly in SQL
            obj.TotalUser = await _db.Users.CountAsync(u => u.IsEmailVerified == true);
            obj.TotalAudits = await _db.PageScans.CountAsync();
            obj.TotalAnonymousScans = await _db.PageScans.CountAsync(s => s.UserId == null);

            // Safe average calculation (ignores null PageScanDetail and null ProcessingTimeMs)
            obj.AvgProcessingTimeMs = await _db.PageScans
                .Where(p => p.PageScanDetail != null && p.PageScanDetail.ProcessingTimeMs != null)
                .AverageAsync(p => (double?)p.PageScanDetail.ProcessingTimeMs) ?? 0;

            // Current week single counts
            obj.ScansThisWeek = await _db.PageScans.CountAsync(s => s.CreatedAtUtc >= oneWeekAgo);
            obj.NewUsersThisWeek = await _db.Users.CountAsync(u => u.CreatedAt >= oneWeekAgo);

            // Filter to last 8 weeks in DB first to avoid loading full history into memory
            var rawScansFor8Weeks = await _db.PageScans
                .Where(s => s.CreatedAtUtc >= eightWeeksAgo)
                .Select(s => s.CreatedAtUtc)
                .ToListAsync();

            obj.ScansPerWeek = rawScansFor8Weeks
                .GroupBy(date => StartOfWeek(date))
                .OrderBy(g => g.Key)
                .TakeLast(8)
                .Select(g => new WeeklyStat { WeekStart = g.Key, Count = g.Count() })
                .ToList();

            var rawUsersFor8Weeks = await _db.Users
                .Where(u => u.CreatedAt >= eightWeeksAgo)
                .Select(u => u.CreatedAt)
                .ToListAsync();

            obj.NewUsersPerWeek = rawUsersFor8Weeks
                .GroupBy(date => StartOfWeek(date))
                .OrderBy(g => g.Key)
                .TakeLast(8)
                .Select(g => new WeeklyStat { WeekStart = g.Key, Count = g.Count() })
                .ToList();

            // Location breakdown executed directly on SQL Server
            obj.LocationData = await _db.PageScans
                .Where(s => s.PageScanDetail != null
                         && !string.IsNullOrEmpty(s.PageScanDetail.Country)
                         && !string.IsNullOrEmpty(s.PageScanDetail.City))
                .GroupBy(s => new { s.PageScanDetail.Country, s.PageScanDetail.City })
                .Select(g => new LocationStatistic
                {
                    Country = g.Key.Country,
                    City = g.Key.City,
                    Count = g.Count()
                })
                .OrderByDescending(l => l.Count)
                .ToListAsync();

            return obj;
        }
        private static DateTime StartOfWeek(DateTime dt)
        {
            // Monday as first day of week
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
            return dt.Date.AddDays(-diff);
        }
    }
}
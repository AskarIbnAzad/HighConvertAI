using Google.Apis.PagespeedInsights.v5;
using Google.Apis.PagespeedInsights.v5.Data;
using static Google.Apis.PagespeedInsights.v5.PagespeedapiResource.RunpagespeedRequest;
using Google;
using System.Text;
using System.Text.Json;

namespace HighConvertAI.Server.Services;

public interface IPageSpeedService
{
    Task<IReadOnlyList<string>> AnalyzeUrlAsync(string url, bool fastMode = false);
}

public class PageSpeedService(HttpClient httpClient, IConfiguration configuration, ILogger<PageSpeedService> logger) : IPageSpeedService
{
    public async Task<IReadOnlyList<string>> AnalyzeUrlAsync(string url, bool fastMode = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                logger.LogWarning("URL is null or empty");
                return new List<string> { "URL is required." };
            }

            var apiKey = configuration["PageSpeedInsights:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogError("PageSpeed Insights API key is not configured");
                return new List<string> { "PageSpeed Insights API key is not configured." };
            }

            if (fastMode)
            {
                return await GetCruxMessagesAsync(httpClient, url, apiKey);
            }

            var service = new PagespeedInsightsService(new()
            {
                ApiKey = apiKey,
                ApplicationName = "HighConvertAI",
            });

            if (service?.Pagespeedapi == null)
            {
                logger.LogError("Failed to initialize PageSpeed Insights API resource");
                return new List<string> { "Failed to initialize PageSpeed Insights API." };
            }

            var request = service.Pagespeedapi.Runpagespeed(url);

            request.Strategy = StrategyEnum.DESKTOP;
            // Request only performance audits relevant for CRO to reduce payload
            request.Category = CategoryEnum.PERFORMANCE;
            // Include root `id` at top-level; cannot select individual audit keys via partial-response,
            // so request the full `audits` object and filter in code.
            request.Fields = "id,lighthouseResult(finalUrl,audits)";

            PagespeedApiPagespeedResponseV5? response;
            try
            {
                response = await request.ExecuteAsync();
            }
            catch (Google.GoogleApiException ex)
            {
                logger.LogError(ex, "PageSpeed Insights API error: {Message}", ex.Message);
                return new List<string> { $"PageSpeed Insights API error: {ex.Message}" };
            }

            if (response == null)
            {
                logger.LogWarning("PageSpeed Insights API returned null response for URL: {Url}", url);
                return new List<string> { "PageSpeed Insights API returned no data." };
            }

            return BuildMessages(response);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error while calling PageSpeed Insights API");
            return new List<string> { $"HTTP error while calling PageSpeed Insights API: {ex.Message}" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error analyzing URL with PageSpeed Insights");
            return new List<string> { $"Error analyzing URL: {ex.Message}" };
        }
    }

    private static async Task<IReadOnlyList<string>> GetCruxMessagesAsync(HttpClient httpClient, string url, string apiKey)
    {
        var requestUrl = $"https://chromeuxreport.googleapis.com/v1/records:queryRecord?key={apiKey}";
        var payload = new
        {
            url,
            formFactor = "DESKTOP",
            metrics = new[]
            {
                "first_contentful_paint",
                "largest_contentful_paint",
                "cumulative_layout_shift",
                "round_trip_time"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        using var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            return new List<string>
            {
                "Fast mode failed to retrieve CrUX data.",
                $"CrUX API status: {(int)response.StatusCode} {response.ReasonPhrase}",
                string.IsNullOrWhiteSpace(errorBody) ? "" : $"CrUX API error: {errorBody}"
            }.Where(message => !string.IsNullOrWhiteSpace(message)).ToList();
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        if (!document.RootElement.TryGetProperty("record", out var record) ||
            !record.TryGetProperty("metrics", out var metrics))
        {
            return new List<string> { "CrUX field data is unavailable for this URL." };
        }

        var recordUrlValue = GetCruxRecordUrl(record);
        return BuildCruxReport(metrics, recordUrlValue);
    }

    private static string? GetCruxRecordUrl(JsonElement record)
    {
        if (record.TryGetProperty("key", out var key) && key.TryGetProperty("url", out var recordUrl))
        {
            var recordUrlValue = recordUrl.GetString();
            if (!string.IsNullOrWhiteSpace(recordUrlValue))
            {
                return recordUrlValue;
            }
        }

        return null;
    }

    private static List<string> BuildCruxReport(JsonElement metrics, string? finalUrl)
    {
        var messages = new List<string> { "CRO report (CrUX field data p75)" };

        if (!string.IsNullOrWhiteSpace(finalUrl))
        {
            messages.Add($"Final URL: {finalUrl}");
        }

        var issues = new List<string>();
        var overallSeverity = 0;

        overallSeverity = Math.Max(overallSeverity, AddCruxReportLine(messages, issues, metrics, "first_contentful_paint", "First Contentful Paint"));
        overallSeverity = Math.Max(overallSeverity, AddCruxReportLine(messages, issues, metrics, "largest_contentful_paint", "Largest Contentful Paint"));
        overallSeverity = Math.Max(overallSeverity, AddCruxReportLine(messages, issues, metrics, "cumulative_layout_shift", "Cumulative Layout Shift"));
        overallSeverity = Math.Max(overallSeverity, AddCruxReportLine(messages, issues, metrics, "round_trip_time", "Round Trip Time"));

        var summary = BuildCruxSummary(overallSeverity, issues);
        var insertIndex = !string.IsNullOrWhiteSpace(finalUrl) ? 2 : 1;
        messages.Insert(insertIndex, summary);

        return messages;
    }

    private static int AddCruxReportLine(
        List<string> messages,
        List<string> issues,
        JsonElement metrics,
        string key,
        string label)
    {
        var p75 = GetCruxP75(metrics, key);
        if (p75 is null)
        {
            messages.Add($"{label} (p75): unavailable");
            return 0;
        }

        var (severity, rating, advice) = EvaluateCruxMetric(key, p75.Value);
        var formatted = FormatCruxValue(key, p75.Value);

        if (severity > 0)
        {
            issues.Add(label);
        }

        var suffix = string.IsNullOrWhiteSpace(advice) ? string.Empty : $" — {advice}";
        messages.Add($"{label} (p75): {formatted} — {rating}{suffix}");

        return severity;
    }

    private static double? GetCruxP75(JsonElement metrics, string key)
    {
        if (!metrics.TryGetProperty(key, out var metric) ||
            !metric.TryGetProperty("percentiles", out var percentiles) ||
            !percentiles.TryGetProperty("p75", out var p75Element))
        {
            return null;
        }

        if (p75Element.ValueKind == JsonValueKind.Number && p75Element.TryGetDouble(out var numericValue))
        {
            return numericValue;
        }

        return null;
    }

    private static (int severity, string rating, string advice) EvaluateCruxMetric(string key, double value)
    {
        return key switch
        {
            "first_contentful_paint" => RateMetric(value, 1800, 3000, "Reduce render blocking resources and ship less JS."),
            "largest_contentful_paint" => RateMetric(value, 2500, 4000, "Optimize hero media, server response, and critical CSS."),
            "cumulative_layout_shift" => RateMetric(value, 0.1, 0.25, "Reserve space for images, ads, and fonts to avoid shifts."),
            "round_trip_time" => RateMetric(value, 200, 500, "Lower server latency and use a CDN where possible."),
            _ => (0, "unknown", string.Empty)
        };
    }

    private static (int severity, string rating, string advice) RateMetric(double value, double goodThreshold, double needsThreshold, string advice)
    {
        if (value <= goodThreshold)
        {
            return (0, "good", string.Empty);
        }

        if (value <= needsThreshold)
        {
            return (1, "needs improvement", advice);
        }

        return (2, "poor", advice);
    }

    private static string BuildCruxSummary(int severity, List<string> issues)
    {
        var rating = severity switch
        {
            0 => "good",
            1 => "needs improvement",
            _ => "poor"
        };

        if (issues.Count == 0)
        {
            return $"Overall: {rating}";
        }

        return $"Overall: {rating} ({string.Join(", ", issues)})";
    }

    private static string FormatCruxValue(string key, double numericValue)
    {
        if (key == "cumulative_layout_shift")
        {
            return numericValue.ToString("0.###");
        }

        if (numericValue >= 1000)
        {
            return $"{numericValue / 1000:0.###} s";
        }

        return $"{numericValue:0.###} ms";
    }

    private static List<string> BuildMessages(PagespeedApiPagespeedResponseV5 response)
    {
        var lighthouseResult = response.LighthouseResult;
        var url = lighthouseResult?.FinalUrl ?? response.Id ?? string.Empty;
        var messages = new List<string>();

        if (!string.IsNullOrWhiteSpace(url))
        {
            messages.Add($"Final URL: {url}");
        }

        AddAuditMessage(messages, lighthouseResult?.Audits, "first-contentful-paint", "First Contentful Paint");
        AddAuditMessage(messages, lighthouseResult?.Audits, "largest-contentful-paint", "Largest Contentful Paint");
        AddAuditMessage(messages, lighthouseResult?.Audits, "cumulative-layout-shift", "Cumulative Layout Shift");
        AddAuditMessage(messages, lighthouseResult?.Audits, "server-response-time", "Time to First Byte");

        return messages;
    }

    private static void AddAuditMessage(
        List<string> messages,
        IDictionary<string, LighthouseAuditResultV5>? audits,
        string auditKey,
        string label)
    {
        if (audits == null || !audits.TryGetValue(auditKey, out var audit) || audit == null)
        {
            messages.Add($"{label}: unavailable");
            return;
        }

        var value = audit.DisplayValue;
        if (string.IsNullOrWhiteSpace(value) && audit.NumericValue is double numericValue)
        {
            value = numericValue.ToString("0.###");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            value = "unavailable";
        }

        var rating = GetRating(audit.Score);
        var suffix = string.IsNullOrWhiteSpace(rating) ? string.Empty : $" ({rating})";

        messages.Add($"{label}: {value}{suffix}");
    }

    private static string? GetRating(object score)
    {
        if (score is null)
        {
            return null;
        }

        if (!double.TryParse(score.ToString(), out var numericScore))
        {
            return null;
        }

        if (numericScore >= 0.9)
        {
            return "good";
        }

        if (numericScore >= 0.5)
        {
            return "needs improvement";
        }

        return "poor";
    }
}

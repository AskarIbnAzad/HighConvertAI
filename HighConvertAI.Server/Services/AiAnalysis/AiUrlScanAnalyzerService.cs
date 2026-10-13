using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HighConvertAI.Server.Models.AiAnalysis;
using HighConvertAI.Server.Services.Abstractions;
using HtmlAgilityPack;

namespace HighConvertAI.Server.Services.AiAnalysis;

public sealed class AiUrlScanAnalyzerService : IAiUrlScanAnalyzerService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IPageCrawlerService _pageCrawlerService;
    private readonly IAiProviderClient _aiProviderClient;

    public AiUrlScanAnalyzerService(IPageCrawlerService pageCrawlerService, IAiProviderClient aiProviderClient)
    {
        _pageCrawlerService = pageCrawlerService;
        _aiProviderClient = aiProviderClient;
    }

    public async Task<AiUrlScanAndAnalyzeResponse> AnalyzeUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var html = await _pageCrawlerService.CrawlHtmlAsync(url, cancellationToken);
        var snapshot = BuildSnapshot(url, html);

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(snapshot);

        var raw = await _aiProviderClient.CompleteAsync(systemPrompt, userPrompt, cancellationToken);
        var normalized = NormalizeJsonPayload(raw);

        var parsed = TryDeserializeResponse(normalized) ?? BuildFallbackResponse(snapshot);
        return parsed;
    }

    private static string BuildSystemPrompt()
    {
        return """
You are an e-commerce CRO and UX auditor.
Analyze the provided compact page snapshot and return ONLY valid JSON.
Do not include markdown fences or commentary.
Keep fields concise and evidence-driven.
Response schema:
{
  "scanResult": {
    "url": "string",
    "pageType": "string",
    "title": "string",
    "description": "string",
    "primaryHeading": "string",
    "secondaryHeadings": ["string"],
    "primaryCtas": ["string"],
    "keySignals": ["string"],
    "textSample": "string"
  },
  "analysis": {
    "summary": "string",
    "items": [
      {
        "id": "string",
        "type": "string",
        "severity": "Info|Warning|Critical",
        "title": "string",
        "whatWasRight": "string",
        "whatToImprove": "string",
        "recommendation": "string"
      }
    ],
    "groups": [
      {
        "type": "string",
        "items": [same item schema]
      }
    ],
    "stats": {
      "passedTests": 0,
      "issueCount": 0,
      "warningCount": 0,
      "criticalCount": 0,
      "totalChecks": 0,
      "score": 0
    }
  }
}
Use short evidence snippets and keep the score between 0 and 100.
""";
    }

    private static string BuildUserPrompt(CompactPageSnapshot snapshot)
    {
        var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);

        return $"""
Analyze this compact page snapshot and produce a scan summary plus CRO findings.
Focus on conversion clarity, trust signals, CTA quality, friction, and offer clarity.
Keep the response compact and grounded in the snapshot.
SnapshotJson:
{snapshotJson}
""";
    }

    private static CompactPageSnapshot BuildSnapshot(string url, string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var title = CleanText(doc.DocumentNode.SelectSingleNode("//title")?.InnerText);
        var description = CleanText(doc.DocumentNode.SelectSingleNode("//meta[translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='description']")?.GetAttributeValue("content", null));
        var h1 = CleanText(doc.DocumentNode.SelectSingleNode("//h1")?.InnerText);

        var secondaryHeadings = doc.DocumentNode.SelectNodes("//h2 | //h3")
            ?.Cast<HtmlNode>()
            .Select(node => CleanText(node.InnerText))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(value => value!)
            .ToList() ?? [];

        var primaryCtas = doc.DocumentNode.SelectNodes("//a[contains(@class,'cta') or contains(@class,'btn') or contains(@class,'primary')] | //button[contains(@class,'cta') or contains(@class,'btn') or contains(@class,'primary')]")
            ?.Cast<HtmlNode>()
            .Select(node => CleanText(node.InnerText))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .Select(value => value!)
            .ToList() ?? [];

        var keySignals = new List<string>();
        if (!string.IsNullOrWhiteSpace(title)) keySignals.Add($"title:{title}");
        if (!string.IsNullOrWhiteSpace(description)) keySignals.Add($"meta:{Truncate(description, 180)}");
        if (!string.IsNullOrWhiteSpace(h1)) keySignals.Add($"h1:{h1}");
        if (primaryCtas.Count > 0) keySignals.Add($"ctaCount:{primaryCtas.Count}");

        return new CompactPageSnapshot
        {
            Url = url,
            Title = title ?? string.Empty,
            Description = description ?? string.Empty,
            PrimaryHeading = h1 ?? string.Empty,
            SecondaryHeadings = secondaryHeadings,
            PrimaryCtas = primaryCtas,
            KeySignals = keySignals,
            TextSample = BuildTextSample(doc.DocumentNode)
        };
    }

    private static string BuildTextSample(HtmlNode rootNode)
    {
        var textBuilder = new StringBuilder();

        foreach (var node in rootNode.DescendantsAndSelf())
        {
            if (node.NodeType != HtmlNodeType.Element)
                continue;

            var name = node.Name.ToLowerInvariant();
            if (name is "script" or "style" or "noscript" or "svg")
                continue;

            if (name is "h1" or "h2" or "h3" or "p" or "li" or "button" or "a")
            {
                var cleaned = CleanText(node.InnerText);
                if (!string.IsNullOrWhiteSpace(cleaned))
                {
                    textBuilder.AppendLine(cleaned);
                    if (textBuilder.Length >= 1800)
                        break;
                }
            }
        }

        return Truncate(textBuilder.ToString(), 1800);
    }

    private static AiUrlScanAndAnalyzeResponse? TryDeserializeResponse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<AiUrlScanAndAnalyzeResponse>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static AiUrlScanAndAnalyzeResponse BuildFallbackResponse(CompactPageSnapshot snapshot)
    {
        return new AiUrlScanAndAnalyzeResponse
        {
            ScanResult = new AiUrlScanResult
            {
                Url = snapshot.Url,

                PageType = "Unknown",

                Title = snapshot.Title,

                Description = snapshot.Description,

                PrimaryHeading = snapshot.PrimaryHeading,

                SecondaryHeadings = snapshot.SecondaryHeadings,

                PrimaryCtas = snapshot.PrimaryCtas,

                KeySignals = snapshot.KeySignals,

                TextSample = snapshot.TextSample
            },

            Analysis = new AiAnalysisResponse
            {
                Summary =
                    "AI analysis unavailable. Returning compact page snapshot only.",

                Items =
                [
                    new AiInsightItem
                {
                    GroupType = "System",

                    Severity = "Low",

                    Title = "Fallback response",

                    WhatWasRight =
                        "The page snapshot was collected successfully.",

                    WhatToImprove =
                        "AI analysis could not be generated or parsed.",

                    Recommendation =
                        "Retry the AI scan or verify AI provider configuration."
                }
                ],

                Groups =
                [
                    new AiInsightGroup
                {
                    GroupType = "System",

                    IssueCount = 1,

                    HighCount = 0,

                    MediumCount = 0,

                    LowCount = 1,

                    EstimatedRevenueIncreasePercent = 0,

                    Condition = "Average",

                    Items =
                    [
                        new AiInsightItem
                        {
                            GroupType = "System",

                            Severity = "Low",

                            Title = "Fallback response",

                            WhatWasRight =
                                "The page snapshot was collected successfully.",

                            WhatToImprove =
                                "AI analysis could not be generated.",

                            Recommendation =
                                "Retry the AI scan or inspect the snapshot input."
                        }
                    ]
                }
                ],

                Stats = new AiAnalysisStats
                {
                    PassedTests = 0,

                    IssueCount = 1,

                    HighCount = 0,

                    MediumCount = 0,

                    LowCount = 1,

                    TotalChecks = 1,

                    Score = 0,

                    EstimatedRevenueIncreasePercent = 0
                }
            }
        };
    }

    private static string NormalizeJsonPayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return string.Empty;

        var trimmed = payload.Trim();

        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = trimmed.IndexOf('\n');
            if (firstNewLine >= 0)
            {
                trimmed = trimmed[(firstNewLine + 1)..];
            }

            if (trimmed.EndsWith("```", StringComparison.Ordinal))
            {
                trimmed = trimmed[..^3];
            }
        }

        return trimmed.Trim();
    }

    private static string CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var text = HtmlEntity.DeEntitize(value);
        text = Regex.Replace(text, "\\s+", " ").Trim();
        return text;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;

        return value[..maxLength].Trim();
    }

    private sealed class CompactPageSnapshot
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PrimaryHeading { get; set; } = string.Empty;
        public List<string> SecondaryHeadings { get; set; } = [];
        public List<string> PrimaryCtas { get; set; } = [];
        public List<string> KeySignals { get; set; } = [];
        public string TextSample { get; set; } = string.Empty;
    }
}
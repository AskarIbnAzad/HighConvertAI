using HighConvertAI.Server.Models.AiAnalysis;
using HighConvertAI.Server.Models.DatabaseModels;
using HighConvertAI.Server.Models.Scanning;
using HighConvertAI.Server.Services.Abstractions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace HighConvertAI.Server.Services.AiAnalysis;

public sealed class AiPageScanAnalyzerService : IAiPageScanAnalyzerService
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IAiProviderClient _aiProviderClient;

    public AiPageScanAnalyzerService(IAiProviderClient aiProviderClient)
    {
        _aiProviderClient = aiProviderClient;
    }

    public async Task<List<Issue>?> AnalyzeAsync(PageScanResult pageScanResult,List<IssueGroup> IssueGroups, List<IssueSeverity> IssueSaverities, CancellationToken cancellationToken = default)
    {
        try
        {
            var systemPrompt = BuildSystemPromptForJson(IssueGroups, IssueSaverities);
            var userPrompt = BuildUserPrompt(pageScanResult, streaming: false);

            var raw = await _aiProviderClient.CompleteAsync(systemPrompt, userPrompt, cancellationToken);
            var normalized = NormalizeJsonPayload(raw);

            var parsed = TryDeserializeResponse(normalized) ?? null;   
            return parsed;
        }
        catch (Exception ex)
        {
            throw;
        }
        
    }

    public async IAsyncEnumerable<AiAnalysisStreamEvent> StreamAnalysisAsync(PageScanResult pageScanResult, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPromptForNdjson();
        var userPrompt = BuildUserPrompt(pageScanResult, streaming: true);

        var allText = new StringBuilder();
        var lineBuffer = new StringBuilder();
        AiAnalysisResponse? receivedFinal = null;

        await foreach (var chunk in _aiProviderClient.StreamAsync(systemPrompt, userPrompt, cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(chunk))
                continue;

            allText.Append(chunk);
            lineBuffer.Append(chunk);

            yield return new AiAnalysisStreamEvent
            {
                EventType = "chunk",
                Message = chunk
            };

            while (TryReadLine(lineBuffer, out var line))
            {
                var streamEvent = ParseNdjsonLine(line);
                if (streamEvent is not null)
                {
                    if (streamEvent.EventType == "final" && streamEvent.Final is not null)
                        receivedFinal = streamEvent.Final;

                    yield return streamEvent;
                }
            }
        }

        if (receivedFinal is not null)
        {
            yield return new AiAnalysisStreamEvent
            {
                EventType = "complete",
                Message = "Analysis completed"
            };

            yield break;
        }

        var finalResult = TryDeserializeResponse(NormalizeJsonPayload(allText.ToString()));
        if (finalResult is not null)
        {
            yield return new AiAnalysisStreamEvent
            {
                EventType = "final",
                Final = EnsureGrouped(null)
                //Final = EnsureGrouped(finalResult)
            };

            yield return new AiAnalysisStreamEvent
            {
                EventType = "complete",
                Message = "Analysis completed"
            };

            yield break;
        }

        var fallback = BuildFallbackResponse(pageScanResult);
        yield return new AiAnalysisStreamEvent
        {
            EventType = "final",
            Final = fallback
        };

        yield return new AiAnalysisStreamEvent
        {
            EventType = "complete",
            Message = "Analysis completed with fallback"
        };
    }

    //    private static string BuildSystemPromptForJson()
    //    {
    //        return """
    //You are an e-commerce CRO and UX auditor.
    //Return ONLY valid JSON, with no markdown fences and no additional commentary.
    //Output schema:
    //{
    //  "summary": "string",
    //  "items": [
    //    {
    //      "id": "string",
    //      "type": "",
    //      "severity": "Info|Warning|Critical",
    //      "title": "string",
    //      "whatWasRight": "string",
    //      "whatToImprove": "string",
    //      "recommendation": "string"
    //    }
    //  ],
    //  "groups": [
    //    {
    //      "type": "string",
    //      "items": [same item schema]
    //    }
    //  ],
    //  "stats": {
    //    "passedTests": 0,
    //    "issueCount": 0,
    //    "warningCount": 0,
    //    "criticalCount": 0,
    //    "totalChecks": 0,
    //    "score": 0
    //  }
    //}
    //Ensure stats are internally consistent and score is between 0 and 100.
    //""";
    //    }
    //    private static string BuildSystemPromptForJson()
    //    {
    //        return """
    //You are an e-commerce CRO auditor.

    //Return ONLY valid JSON.
    //No markdown.
    //No explanation text.

    //{
    //  "summary": "string",

    //  "items": [
    //    {
    //      "id": "string",
    //      "groupType": "string",
    //      "severity": "Low|Medium|High",
    //      "title": "string",
    //      "whatWasRight": "string",
    //      "whatToImprove": "string",
    //      "recommendation": "string"
    //    }
    //  ],

    //  "groups": [
    //    {
    //      "groupType": "string",

    //      "issueCount": 0,

    //      "highCount": 0,

    //      "mediumCount": 0,

    //      "lowCount": 0,

    //      "estimatedRevenueIncreasePercent": 0,

    //      "condition": "Good|Average|Poor",

    //      "items": []
    //    }
    //  ],

    //  "stats": {
    //    "passedTests": 0,

    //    "issueCount": 0,

    //    "highCount": 0,

    //    "mediumCount": 0,

    //    "lowCount": 0,

    //    "totalChecks": 0,

    //    "score": 0,

    //    "estimatedRevenueIncreasePercent": 0
    //  }
    //}
    //""";
    //    }


    //    private static string BuildSystemPromptForJson()
    //    {
    //        return """
    //You are an e-commerce CRO auditor.

    //Return ONLY valid JSON.
    //No markdown.
    //No explanation text.

    //COVERAGE REQUIREMENT (CRITICAL):
    //- You MUST identify a MINIMUM of 15 and MAXIMUM of 20 findings total in analysis.items.
    //- Do NOT stop after finding the "obvious" issues. Continue analyzing until every category below has been checked.
    //- It is better to include a Low severity nitpick than to under-report. Minor issues (microcopy, spacing, missing trust badges, weak CTAs, etc.) are valid findings.
    //- Distribute findings across MULTIPLE categories — do not cluster all findings into 1-2 groupTypes.

    //CATEGORIES TO EVALUATE (check ALL of these; most sites will have issues in at least 8-10 of them):
    //1. Trust Signals (reviews, badges, guarantees, social proof)
    //2. Checkout Flow (steps, guest checkout, friction, payment options)
    //3. Product Page (images, descriptions, pricing clarity, stock status)
    //4. Navigation & Search (findability, filters, breadcrumbs)
    //5. Mobile Experience (tap targets, responsiveness, load behavior)
    //6. Page Speed & Performance (perceived load time, render blocking)
    //7. Calls to Action (visibility, copy, placement, urgency)
    //8. Pricing & Value Communication (discounts, shipping cost transparency)
    //9. Forms & Data Entry (field count, validation, autofill)
    //10. Upsell/Cross-sell Opportunities (related products, bundles)
    //11. Copywriting & Messaging (clarity, tone, benefit-focused language)
    //12. Accessibility (contrast, alt text, keyboard nav)
    //13. Urgency/Scarcity Messaging (stock counts, limited-time offers)
    //14. Returns/Shipping Policy Visibility
    //15. Brand Consistency & Visual Design

    //IMPORTANT:

    //- analysis.items contains ALL findings (15-20 of them, per the requirement above).
    //- groups.items MUST contain FULL AiInsightItem objects.
    //- groups.items MUST NOT contain strings, IDs, titles, or references.
    //- Every item inside groups.items must include:
    //  id,
    //  groupType,
    //  severity,
    //  title,
    //  whatWasRight,
    //  whatToImprove,
    //  recommendation.

    //Example:

    //{
    //  "groupType": "Trust Signals",
    //  "items": [
    //    {
    //      "id": "missing-reviews",
    //      "groupType": "Trust Signals",
    //      "severity": "High",
    //      "title": "Customer Reviews Missing",
    //      "whatWasRight": "...",
    //      "whatToImprove": "...",
    //      "recommendation": "..."
    //    }
    //  ]
    //}

    //Schema:

    //{
    //  "summary": "string",

    //  "items": [
    //    {
    //      "id": "string",
    //      "groupType": "string",
    //      "severity": "Low|Medium|High",
    //      "title": "string",
    //      "whatWasRight": "string",
    //      "whatToImprove": "string",
    //      "recommendation": "string"
    //    }
    //  ],

    //  "groups": [
    //    {
    //      "groupType": "string",
    //      "issueCount": 0,
    //      "highCount": 0,
    //      "mediumCount": 0,
    //      "lowCount": 0,
    //      "estimatedRevenueIncreasePercent": 0,
    //      "condition": "Good|Average|Poor",

    //      "items": [
    //        {
    //          "id": "string",
    //          "groupType": "string",
    //          "severity": "Low|Medium|High",
    //          "title": "string",
    //          "whatWasRight": "string",
    //          "whatToImprove": "string",
    //          "recommendation": "string"
    //        }
    //      ]
    //    }
    //  ],

    //  "stats": {
    //    "passedTests": 0,
    //    "issueCount": 0,
    //    "highCount": 0,
    //    "mediumCount": 0,
    //    "lowCount": 0,
    //    "totalChecks": 0,
    //    "score": 0,
    //    "estimatedRevenueIncreasePercent": 0
    //  }
    //}
    //""";
    //    }

    //    private static string BuildSystemPromptForJson(List<IssueGroup> IssueGroups, List<IssueSeverity> IssueSaverities)
    //    {
    //        return """
    //You are an e-commerce CRO auditor.

    //Return ONLY valid JSON.
    //No markdown.
    //No explanation text.

    //COVERAGE REQUIREMENT (CRITICAL):
    //- Identify as many GENUINE findings as you can by checking every category below.
    //- Do NOT stop after finding the "obvious" issues. Continue analyzing until every category below has been checked.
    //- The number of findings should reflect how many real issues actually exist on the site — typically this lands somewhere between 12 and 20, but it is NOT a fixed target. A cleaner site may surface fewer; a messier one may surface more. Never pad or invent issues just to hit a number, and never artificially cap or round to a "nice" total like exactly 20.
    //- It is better to include a Low severity nitpick than to under-report. Minor issues (microcopy, spacing, missing trust badges, weak CTAs, etc.) are valid findings — but only if they are real and specific to what you observed.
    //- Distribute findings across MULTIPLE categories — do not cluster all findings into 1-2 groupTypes.
    //- If a category genuinely has no issue, skip it entirely. Do not include a placeholder, "no issues found" item, or a group with zero items.

    //CATEGORIES TO EVALUATE (check ALL of these; most sites will have genuine issues in at least 8-10 of them):
    //1. Trust Signals (reviews, badges, guarantees, social proof)
    //2. Checkout Flow (steps, guest checkout, friction, payment options)
    //3. Product Page (images, descriptions, pricing clarity, stock status)
    //4. Navigation & Search (findability, filters, breadcrumbs)
    //5. Mobile Experience (tap targets, responsiveness, load behavior)
    //6. Page Speed & Performance (perceived load time, render blocking)
    //7. Calls to Action (visibility, copy, placement, urgency)
    //8. Pricing & Value Communication (discounts, shipping cost transparency)
    //9. Forms & Data Entry (field count, validation, autofill)
    //10. Upsell/Cross-sell Opportunities (related products, bundles)
    //11. Copywriting & Messaging (clarity, tone, benefit-focused language)
    //12. Accessibility (contrast, alt text, keyboard nav)
    //13. Urgency/Scarcity Messaging (stock counts, limited-time offers)
    //14. Returns/Shipping Policy Visibility
    //15. Brand Consistency & Visual Design

    //IMPORTANT:

    //- analysis.items contains ALL findings.
    //- groups MUST NOT include any group with zero items.
    //- groups.items MUST contain FULL AiInsightItem objects.
    //- groups.items MUST NOT contain strings, IDs, titles, or references.
    //- Every item inside groups.items must include:
    //  id,
    //  groupType,
    //  severity,
    //  title,
    //  whatWasRight,
    //  whatToImprove,
    //  recommendation.

    //Example:

    //{
    //  "groupType": "Trust Signals",
    //  "items": [
    //    {
    //      "id": "missing-reviews",
    //      "groupType": "Trust Signals",
    //      "severity": "High",
    //      "title": "Customer Reviews Missing",
    //      "whatWasRight": "...",
    //      "whatToImprove": "...",
    //      "recommendation": "..."
    //    }
    //  ]
    //}

    //Schema:

    //{
    //  "summary": "string",

    //  "items": [
    //    {
    //      "id": "string",
    //      "groupType": "string",
    //      "severity": "Low|Medium|High",
    //      "title": "string",
    //      "whatWasRight": "string",
    //      "whatToImprove": "string",
    //      "recommendation": "string"
    //    }
    //  ],

    //  "groups": [
    //    {
    //      "groupType": "string",
    //      "issueCount": 0,
    //      "highCount": 0,
    //      "mediumCount": 0,
    //      "lowCount": 0,
    //      "estimatedRevenueIncreasePercent": 0,
    //      "condition": "Good|Average|Poor",

    //      "items": [
    //        {
    //          "id": "string",
    //          "groupType": "string",
    //          "severity": "Low|Medium|High",
    //          "title": "string",
    //          "whatWasRight": "string",
    //          "whatToImprove": "string",
    //          "recommendation": "string"
    //        }
    //      ]
    //    }
    //  ],

    //  "stats": {
    //    "passedTests": 0,
    //    "issueCount": 0,
    //    "highCount": 0,
    //    "mediumCount": 0,
    //    "lowCount": 0,
    //    "totalChecks": 0,
    //    "score": 0,
    //    "estimatedRevenueIncreasePercent": 0
    //  }
    //}
    //""";
    //    }
    private static string BuildSystemPromptForJson(List<IssueGroup> issueGroups, List<IssueSeverity> issueSeverities)
    {
        var groups = string.Join("\n",
            issueGroups
                .OrderBy(x => x.SortingOrder)
                .Select(x => $"{x.Id}. {x.Name}\n{x.Detail}"));

        var severities = string.Join("\n",
            issueSeverities
                .OrderBy(x => x.SortingOrder)
                .Select(x => $"{x.Id}. {x.Name}"));

        return $$"""
        You are an e-commerce CRO auditor.
        Return ONLY valid JSON.
        No markdown.
        No explanation text.

        COVERAGE REQUIREMENT (CRITICAL):
        - Identify as many GENUINE findings as you can by checking every category below.
        - Do NOT stop after finding only the obvious issues.
        - Continue analyzing until every applicable category has been inspected.
        - The number of findings should reflect the actual website quality.
        - A clean website may produce fewer findings.
        - A poor website may produce many findings.
        - Never invent issues simply to increase the number of findings.
        - Minor but real CRO issues are valid findings.
        - Skip categories where no issue exists.

        ======================================================
        AVAILABLE ISSUE GROUPS
        ======================================================
        {{groups}}
        IMPORTANT:
        - Use ONLY the IssueGroupId values listed above.
        - Never invent a new IssueGroupId.
        - Every issue must belong to exactly one IssueGroupId.

        ======================================================
        AVAILABLE ISSUE SEVERITIES
        ======================================================
        {{severities}}
        IMPORTANT:
        - Use ONLY the IssueSeverityId values listed above.
        - Never invent a new IssueSeverityId.
        - Every issue must belong to exactly one IssueSeverityId.

        ======================================================
        CRITICAL COPYWRITING & TERMINOLOGY RULES (MANDATORY)
        ======================================================
        - NEVER quote internal property names, JSON keys, boolean flags, or code strings (e.g., do NOT write "hasReviews: false", "reviewCount: null", or "currentPrice").
        - Translate technical data structures into natural, human-friendly business language for non-technical store owners.
        - Instead of saying "The scan reports hasReviews: false", say "The product page lacks visible customer reviews and star ratings."
        - Focus purely on what the user experiences on the website, not what the code variables look like.

        ======================================================
        AUDIT REQUIREMENTS
        ======================================================
        - Inspect every possible CRO aspect including: Trust Signals, Checkout Flow, Product Page, Navigation, Search, Mobile Experience, Performance, Calls To Action, Pricing, Forms, Upsell Opportunities, Cross Sell, Copywriting, Accessibility, Urgency, Scarcity, Shipping, Returns, Branding, Visual Hierarchy, Social Proof, Reviews, Images, Videos, Product Information, Page Layout.

        ======================================================
        VERY IMPORTANT FOR EVERY ISSUE FIELD
        ======================================================
        - "title": Must clearly describe the issue. Keep it short, professional, and specific.
        - "whatWasRight": NEVER return "N/A". NEVER leave it blank. Always identify something that is already implemented correctly in the same section whenever possible, supported by the scan data.
        - "whatToImprove": Explain the actual UX/conversion issue and why it hurts conversion. Do not repeat the title. Never mention JSON keys or null properties.
        - "recommendation": Give a practical, specific recommendation avoiding generic advice.

        ======================================================
        OUTPUT FORMAT
        ======================================================
        Return ONLY a JSON array.
        [
            {
                "issueGroupId": 0,
                "issueSeverityId": 0,
                "title": "",
                "whatWasRight": "",
                "whatToImprove": "",
                "recommendation": ""
            }
        ]

        Rules:
        - The root MUST be a JSON array.
        - Do NOT wrap the array in an object.
        - Do NOT use an "items" property.
        - Return [] if no issues are found.
        - Do not return any additional properties or metadata.
        Return ONLY the JSON array.
        """;
    }
    //private static string BuildSystemPromptForJson(
    //List<IssueGroup> issueGroups,
    //List<IssueSeverity> issueSeverities)
    //{
    //    var groups = string.Join("\n",
    //        issueGroups
    //            .OrderBy(x => x.SortingOrder)
    //            .Select(x => $"{x.Id}. {x.Name} - {x.Detail}"));

    //    var severities = string.Join("\n",
    //        issueSeverities
    //            .OrderBy(x => x.SortingOrder)
    //            .Select(x => $"{x.Id}. {x.Name}"));

    //    return $$"""
    //            You are an e-commerce CRO auditor.

    //            Return ONLY valid JSON.
    //            No markdown.
    //            No explanation text.

    //            COVERAGE REQUIREMENT (CRITICAL):

    //            - Identify as many GENUINE findings as you can by checking every category below.
    //            - Do NOT stop after finding only the obvious issues.
    //            - Continue analyzing until every applicable category has been inspected.
    //            - The number of findings should reflect the actual website quality.
    //            - A clean website may produce fewer findings.
    //            - A poor website may produce many findings.
    //            - Never invent issues simply to increase the number of findings.
    //            - Minor but real CRO issues are valid findings.
    //            - Skip categories where no issue exists.

    //            ======================================================
    //            AVAILABLE ISSUE GROUPS
    //            ======================================================

    //            {{groups}}

    //            IMPORTANT:

    //            - Use ONLY the IssueGroupId values listed above.
    //            - Never invent a new IssueGroupId.
    //            - Every issue must belong to exactly one IssueGroupId.

    //            ======================================================
    //            AVAILABLE ISSUE SEVERITIES
    //            ======================================================

    //            {{severities}}

    //            IMPORTANT:

    //            - Use ONLY the IssueSeverityId values listed above.
    //            - Never invent a new IssueSeverityId.
    //            - Every issue must belong to exactly one IssueSeverityId.

    //            ======================================================
    //            AUDIT REQUIREMENTS
    //            ======================================================

    //            Inspect every possible CRO aspect including (but not limited to):

    //            - Trust Signals
    //            - Checkout Flow
    //            - Product Page
    //            - Navigation
    //            - Search
    //            - Mobile Experience
    //            - Performance
    //            - Calls To Action
    //            - Pricing
    //            - Forms
    //            - Upsell Opportunities
    //            - Cross Sell
    //            - Copywriting
    //            - Accessibility
    //            - Urgency
    //            - Scarcity
    //            - Shipping
    //            - Returns
    //            - Branding
    //            - Visual Hierarchy
    //            - Social Proof
    //            - Reviews
    //            - Images
    //            - Videos
    //            - Product Information
    //            - Page Layout

    //            ======================================================
    //            VERY IMPORTANT
    //            ======================================================

    //            For EVERY issue:

    //            "title"

    //            - Must clearly describe the issue.
    //            - Keep it short.
    //            - Professional.
    //            - Specific.

    //            "whatWasRight"

    //            - NEVER return "N/A".
    //            - NEVER leave it blank.
    //            - Always identify something that is already implemented correctly in the same section whenever possible.
    //            - Mention strengths supported by the scan.
    //            - Even if the section has problems, identify positive aspects that already exist.

    //            "whatToImprove"

    //            - Explain the actual issue.
    //            - Explain why it hurts conversion.
    //            - Do not repeat the title.

    //            "recommendation"

    //            - Give a practical recommendation.
    //            - Be specific.
    //            - Avoid generic advice.

    //            ======================================================
    //            OUTPUT FORMAT
    //            ======================================================

    //            Return ONLY a JSON array.

    //            [
    //                {
    //                    "issueGroupId": 0,
    //                    "issueSeverityId": 0,
    //                    "title": "",
    //                    "whatWasRight": "",
    //                    "whatToImprove": "",
    //                    "recommendation": ""
    //                }
    //            ]

    //            Rules:

    //            - The root MUST be a JSON array.
    //            - Do NOT wrap the array in an object.
    //            - Do NOT use an "items" property.
    //            - Return [] if no issues are found.
    //            - Do not return any additional properties.
    //            - Do not return:
    //              - summary
    //              - groups
    //              - stats
    //              - score
    //              - revenue
    //              - condition
    //              - counts

    //            Return ONLY the JSON array.
    //            """;
    //}
    //    private static string BuildSystemPromptForJson()
    //    {
    //        return """
    //You are an e-commerce CRO auditor.

    //Return ONLY valid JSON.
    //No markdown.
    //No explanation text.

    //IMPORTANT:

    //- analysis.items contains ALL findings.
    //- groups.items MUST contain FULL AiInsightItem objects.
    //- groups.items MUST NOT contain strings, IDs, titles, or references.
    //- Every item inside groups.items must include:
    //  id,
    //  groupType,
    //  severity,
    //  title,
    //  whatWasRight,
    //  whatToImprove,
    //  recommendation.

    //Example:

    //{
    //  "groupType": "Trust Signals",
    //  "items": [
    //    {
    //      "id": "missing-reviews",
    //      "groupType": "Trust Signals",
    //      "severity": "High",
    //      "title": "Customer Reviews Missing",
    //      "whatWasRight": "...",
    //      "whatToImprove": "...",
    //      "recommendation": "..."
    //    }
    //  ]
    //}

    //Schema:

    //{
    //  "summary": "string",

    //  "items": [
    //    {
    //      "id": "string",
    //      "groupType": "string",
    //      "severity": "Low|Medium|High",
    //      "title": "string",
    //      "whatWasRight": "string",
    //      "whatToImprove": "string",
    //      "recommendation": "string"
    //    }
    //  ],

    //  "groups": [
    //    {
    //      "groupType": "string",
    //      "issueCount": 0,
    //      "highCount": 0,
    //      "mediumCount": 0,
    //      "lowCount": 0,
    //      "estimatedRevenueIncreasePercent": 0,
    //      "condition": "Good|Average|Poor",

    //      "items": [
    //        {
    //          "id": "string",
    //          "groupType": "string",
    //          "severity": "Low|Medium|High",
    //          "title": "string",
    //          "whatWasRight": "string",
    //          "whatToImprove": "string",
    //          "recommendation": "string"
    //        }
    //      ]
    //    }
    //  ],

    //  "stats": {
    //    "passedTests": 0,
    //    "issueCount": 0,
    //    "highCount": 0,
    //    "mediumCount": 0,
    //    "lowCount": 0,
    //    "totalChecks": 0,
    //    "score": 0,
    //    "estimatedRevenueIncreasePercent": 0
    //  }
    //}
    //""";
    //    }

    private static string BuildSystemPromptForNdjson()
    {
        return """
You are an e-commerce CRO and UX auditor.
Return NDJSON only (one JSON object per line). No markdown.
Use these events in order:
1) item events as findings are discovered:
{"event":"item","payload":{"id":"string","type":"string","severity":"Info|Warning|Critical","title":"string","whatWasRight":"string","whatToImprove":"string","recommendation":"string"}}
2) one stats event:
{"event":"stats","payload":{"passedTests":0,"issueCount":0,"warningCount":0,"criticalCount":0,"totalChecks":0,"score":0}}
3) one summary event:
{"event":"summary","payload":{"message":"string"}}
4) one final event containing full schema:
{"event":"final","payload":{"summary":"string","items":[],"groups":[],"stats":{}}}
Do not emit any other text.
""";
    }

//    private static string BuildUserPrompt(PageScanResult pageScanResult, bool streaming)
//    {
//        var scanResultJson = JsonSerializer.Serialize(pageScanResult, pageScanResult.GetType(), _jsonOptions);

//        var modeText = streaming
//            ? "Use NDJSON event streaming format and emit items as soon as available."
//            : "Use final JSON object format.";

//        return $"""
//Analyze this page scan result and provide actionable findings suitable for frontend content lists.
//Focus on conversion optimization, UX clarity, trust signals, pricing clarity, CTA quality, and content completeness.
//{modeText}
//ScanResultJson:
//{scanResultJson}
//""";
//    }

    private static string BuildUserPrompt(PageScanResult pageScanResult, bool streaming)
    {
        var scanResultJson =
            JsonSerializer.Serialize(
                pageScanResult,
                pageScanResult.GetType(),
                _jsonOptions);

        return $"""
Analyze the following website scan.

Return only the required JSON using the IssueGroupIds and IssueSeverityIds defined in the system prompt.

Website Scan:

{scanResultJson}
""";
    }

    private static bool TryReadLine(StringBuilder buffer, out string line)
    {
        var text = buffer.ToString();
        var index = text.IndexOf('\n');

        if (index < 0)
        {
            line = string.Empty;
            return false;
        }

        line = text[..index].Trim();
        buffer.Remove(0, index + 1);
        return true;
    }

    private static AiAnalysisStreamEvent? ParseNdjsonLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || !line.StartsWith('{'))
            return null;

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            if (!root.TryGetProperty("event", out var eventElement))
                return null;

            var eventType = eventElement.GetString() ?? string.Empty;

            if (!root.TryGetProperty("payload", out var payloadElement))
            {
                return new AiAnalysisStreamEvent
                {
                    EventType = eventType
                };
            }

            return eventType switch
            {
                "item" => new AiAnalysisStreamEvent
                {
                    EventType = "item",
                    Item = payloadElement.Deserialize<AiInsightItem>(_jsonOptions)
                },
                "stats" => new AiAnalysisStreamEvent
                {
                    EventType = "stats",
                    Stats = payloadElement.Deserialize<AiAnalysisStats>(_jsonOptions)
                },
                "summary" => new AiAnalysisStreamEvent
                {
                    EventType = "summary",
                    Message = payloadElement.TryGetProperty("message", out var message)
                        ? message.GetString()
                        : null
                },
                "final" => new AiAnalysisStreamEvent
                {
                    EventType = "final",
                    Final = EnsureGrouped(payloadElement.Deserialize<AiAnalysisResponse>(_jsonOptions) ?? new())
                },
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static List<Issue>? TryDeserializeResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<Issue>>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeJsonPayload(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var trimmed = raw.Trim();

        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[7..].Trim();

        if (trimmed.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[3..].Trim();

        if (trimmed.EndsWith("```", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^3].Trim();

        return trimmed;
    }

    private static AiAnalysisResponse EnsureGrouped(AiAnalysisResponse response)
    {
        // 1. Build groups from items if the model didn't supply them (existing logic)
        if (response.Groups.Count == 0)
        {
            response.Groups.AddRange(response.Items
                .GroupBy(x => string.IsNullOrWhiteSpace(x.GroupType) ? "General" : x.GroupType)
                .Select(group => new AiInsightGroup
                {
                    GroupType = group.Key,
                    Items = group.ToList()
                }));
        }

        // 2. Recompute per-group counts from each group's own items (don't trust the model's arithmetic)
        foreach (var group in response.Groups)
        {
            var items = group.Items;
            group.IssueCount = items.Count;
            group.HighCount = items.Count(i => i.Severity.Equals("High", StringComparison.OrdinalIgnoreCase));
            group.MediumCount = items.Count(i => i.Severity.Equals("Medium", StringComparison.OrdinalIgnoreCase));
            group.LowCount = items.Count(i => i.Severity.Equals("Low", StringComparison.OrdinalIgnoreCase));
            group.Condition = DeriveCondition(group.HighCount, group.MediumCount, group.LowCount);
        }

        // 3. Recompute top-level stats from the full item list (single source of truth)
        var allItems = response.Items.Count > 0
            ? response.Items
            : response.Groups.SelectMany(g => g.Items).ToList();

        int high = allItems.Count(i => i.Severity.Equals("High", StringComparison.OrdinalIgnoreCase));
        int medium = allItems.Count(i => i.Severity.Equals("Medium", StringComparison.OrdinalIgnoreCase));
        int low = allItems.Count(i => i.Severity.Equals("Low", StringComparison.OrdinalIgnoreCase));
        int issueCount = allItems.Count;

        // totalChecks: keep model's value if it's at least as large as issueCount, else fall back to issueCount
        int totalChecks = response.Stats.TotalChecks >= issueCount
            ? response.Stats.TotalChecks
            : issueCount;

        int passed = Math.Max(totalChecks - issueCount, 0);

        decimal penalty = (high * 10) + (medium * 5) + (low * 2);
        decimal score = Math.Max(0, 100 - penalty);

        // estimatedRevenueIncreasePercent: sum from groups, capped at 100, rounded
        decimal revenueSum = response.Groups.Sum(g => g.EstimatedRevenueIncreasePercent);
        decimal estimatedRevenue = Math.Min(100, revenueSum);

        response.Stats.PassedTests = passed;
        response.Stats.IssueCount = issueCount;
        response.Stats.HighCount = high;
        response.Stats.MediumCount = medium;
        response.Stats.LowCount = low;
        response.Stats.TotalChecks = totalChecks;
        response.Stats.Score = Math.Round(score, 0);
        response.Stats.EstimatedRevenueIncreasePercent = Math.Round(estimatedRevenue, 0);

        return response;
    }

    private static string DeriveCondition(int high, int medium, int low)
    {
        if (high > 0) return "Poor";
        if (medium > 0) return "Average";
        return low > 0 ? "Average" : "Good";
    }

    //private static AiAnalysisResponse BuildFallbackResponse(PageScanResult pageScanResult)
    //{
    //    const int defaultTotalChecks = 12;
    //    const int defaultScore = 70;

    //    return new AiAnalysisResponse
    //    {
    //        Summary = "AI analysis could not be parsed. Returning safe baseline summary from extraction metadata.",
    //        Items =
    //        [
    //            new AiInsightItem
    //            {
    //                Type = "General",
    //                Severity = "Warning",
    //                Title = "Detailed AI insight unavailable",
    //                WhatWasRight = "Page scan completed successfully and extraction data is available.",
    //                WhatToImprove = "Set a valid Google AI Studio API key and retry analysis.",
    //                Recommendation = "Configure GoogleAiStudio:ApiKey in appsettings or user-secrets."
    //            }
    //        ],
    //        Stats = new AiAnalysisStats
    //        {
    //            PassedTests = 0,
    //            IssueCount = 1,
    //            WarningCount = 1,
    //            CriticalCount = 0,
    //            TotalChecks = defaultTotalChecks,
    //            Score = defaultScore
    //        },
    //        Groups =
    //        [
    //            new AiInsightGroup
    //            {
    //                Type = "General",
    //                Items =
    //                [
    //                    new AiInsightItem
    //                    {
    //                        Type = "General",
    //                        Severity = "Warning",
    //                        Title = $"Review needed for {pageScanResult.PageType} page",
    //                        WhatWasRight = "Base page scan and extraction succeeded.",
    //                        WhatToImprove = "AI-generated insights are currently unavailable.",
    //                        Recommendation = "Verify API key and model configuration before retrying analysis."
    //                    }
    //                ]
    //            }
    //        ]
    //    };
    //}
    private static AiAnalysisResponse BuildFallbackResponse(PageScanResult pageScanResult)
    {
        const int defaultTotalChecks = 12;
        const int defaultScore = 70;

        return new AiAnalysisResponse
        {
            Summary = "AI analysis could not be generated. Returning fallback CRO audit response.",

            Items =
            [
                new AiInsightItem
            {
                GroupType = "General",

                Severity = "Medium",

                Title = "Detailed AI insight unavailable",

                WhatWasRight =
                    "Page scan completed successfully and extraction data is available.",

                WhatToImprove =
                    "AI-generated CRO insights could not be completed.",

                Recommendation =
                    "Verify AI provider configuration and retry analysis."
            }
            ],

            Stats = new AiAnalysisStats
            {
                PassedTests = 0,

                IssueCount = 1,

                HighCount = 0,

                MediumCount = 1,

                LowCount = 0,

                TotalChecks = defaultTotalChecks,

                Score = defaultScore,

                EstimatedRevenueIncreasePercent = 3
            },

            Groups =
            [
                new AiInsightGroup
            {
                GroupType = "General",

                IssueCount = 1,

                HighCount = 0,

                MediumCount = 1,

                LowCount = 0,

                EstimatedRevenueIncreasePercent = 3,

                Condition = "Average",

                Items =
                [
                    new AiInsightItem
                    {
                        GroupType = "General",

                        Severity = "Medium",

                        Title = $"Review needed for {pageScanResult.PageType} page",

                        WhatWasRight =
                            "Base page scan and extraction succeeded.",

                        WhatToImprove =
                            "AI-generated CRO insights are currently unavailable.",

                        Recommendation =
                            "Verify AI provider configuration and retry analysis."
                    }
                ]
            }
            ]
        };
    }
}

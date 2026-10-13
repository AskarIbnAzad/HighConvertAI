using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HighConvertAI.Server.Configuration;
using HighConvertAI.Server.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace HighConvertAI.Server.Services.AiAnalysis;

public sealed class GoogleAiStudioProviderClient : IAiProviderClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly GoogleAiStudioOptions _options;

    public GoogleAiStudioProviderClient(HttpClient httpClient, IOptions<GoogleAiStudioOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var endpoint = BuildEndpoint(stream: false);
        var payload = BuildPayload(systemPrompt, userPrompt, responseMimeType: "application/json");

        using var response = await _httpClient.PostAsJsonAsync(endpoint, payload, SerializerOptions, cancellationToken);
        var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Google AI Studio request failed ({(int)response.StatusCode}): {rawBody}");

        return ExtractTextFromGeminiResponse(rawBody);
    }

    public async IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userPrompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var endpoint = BuildEndpoint(stream: true);
        var payload = BuildPayload(systemPrompt, userPrompt, responseMimeType: "text/plain");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Google AI Studio stream failed ({(int)response.StatusCode}): {body}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
                yield break;

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.Ordinal))
                continue;

            var jsonChunk = line[5..].Trim();
            if (string.Equals(jsonChunk, "[DONE]", StringComparison.OrdinalIgnoreCase))
                yield break;

            var text = ExtractTextFromGeminiResponse(jsonChunk);
            if (!string.IsNullOrWhiteSpace(text))
                yield return text;
        }
    }

    private string BuildEndpoint(bool stream)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var method = stream ? "streamGenerateContent" : "generateContent";
        var queryPrefix = stream ? "?alt=sse&" : "?";
        return $"{baseUrl}/models/{_options.Model}:{method}{queryPrefix}key={Uri.EscapeDataString(_options.ApiKey!)}";
    }

    private static object BuildPayload(string systemPrompt, string userPrompt, string responseMimeType)
    {
        return new
        {
            systemInstruction = new
            {
                parts = new[]
                {
                    new { text = systemPrompt }
                }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = userPrompt }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType,
                temperature = 0.2,
                topP = 0.9
            }
        };
    }

    private static string ExtractTextFromGeminiResponse(string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return string.Empty;

        using var document = JsonDocument.Parse(rawBody);
        var root = document.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var builder = new StringBuilder();

        foreach (var candidate in candidates.EnumerateArray())
        {
            if (!candidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textElement))
                {
                    var text = textElement.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        builder.Append(text);
                }
            }
        }

        return builder.ToString();
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Google AI Studio API key is missing. Configure GoogleAiStudio:ApiKey in appsettings.");
    }
}

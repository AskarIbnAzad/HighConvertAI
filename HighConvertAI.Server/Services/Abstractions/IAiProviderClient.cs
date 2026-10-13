namespace HighConvertAI.Server.Services.Abstractions;

public interface IAiProviderClient
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}

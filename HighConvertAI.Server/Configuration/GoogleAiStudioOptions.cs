namespace HighConvertAI.Server.Configuration;

public sealed class GoogleAiStudioOptions
{
    public const string SectionName = "GoogleAiStudio";

    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gemini-2.0-flash";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
}

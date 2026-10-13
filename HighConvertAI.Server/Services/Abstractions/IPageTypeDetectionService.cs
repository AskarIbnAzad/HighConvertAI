namespace HighConvertAI.Server.Services.Abstractions
{
    public interface IPageTypeDetectionService
    {
        string Detect(string html, string url);
    }
}

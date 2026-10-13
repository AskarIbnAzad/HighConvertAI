namespace HighConvertAI.Server.Services.Abstractions
{
    public interface IScreenshotService
    {
        Task<byte[]> DesktopAsync(string url);

        Task<byte[]> MobileAsync(string url);
    }
}

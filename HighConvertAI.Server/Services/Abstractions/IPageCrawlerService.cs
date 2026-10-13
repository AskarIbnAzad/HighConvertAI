namespace HighConvertAI.Server.Services.Abstractions
{
    public interface IPageCrawlerService
    {
        Task<string> CrawlHtmlAsync(string url, CancellationToken cancellationToken = default);

    }
}

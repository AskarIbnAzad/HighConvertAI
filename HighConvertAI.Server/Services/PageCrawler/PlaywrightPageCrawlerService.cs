using HighConvertAI.Server.Services.Abstractions;
using System.Xml;
using Microsoft.Playwright;

namespace HighConvertAI.Server.Services.PageCrawler
{

  

    public class PlaywrightPageCrawlerService : IPageCrawlerService
    {
        public async Task<string> CrawlHtmlAsync(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL is required.", nameof(url));

            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                throw new ArgumentException("Invalid absolute URL.", nameof(url));

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(45));

            var playwright = await Playwright.CreateAsync();

            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize
                {
                    Width = 1440,
                    Height = 2200
                },
                IgnoreHTTPSErrors = true
            });

            var page = await context.NewPageAsync();

            try
            {
                await page.RouteAsync("**/*", async route =>
                {
                    var resourceType = route.Request.ResourceType;

                    if (resourceType is "image" or "media" or "font")
                    {
                        await route.AbortAsync();
                        return;
                    }

                    await route.ContinueAsync();
                });

                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 30000
                });

                try
                {
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                    {
                        Timeout = 5000
                    });
                }
                catch
                {
                    // Ignore if network idle is not reached quickly.
                }

                await page.WaitForTimeoutAsync(1000);

                var html = await page.ContentAsync();

                if (string.IsNullOrWhiteSpace(html))
                    throw new InvalidOperationException("The crawled HTML is empty.");

                return html;
            }
            catch (TimeoutException ex)
            {
                throw new InvalidOperationException("Page loading timed out.", ex);
            }
            catch (PlaywrightException ex)
            {
                throw new InvalidOperationException($"Playwright crawl failed: {ex.Message}", ex);
            }
            finally
            {
                await page.CloseAsync();
                await context.CloseAsync();
                await browser.CloseAsync();
            }
        }
    }
}
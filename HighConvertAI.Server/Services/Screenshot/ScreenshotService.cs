using HighConvertAI.Server.Services.Abstractions;
using Microsoft.Playwright;

namespace HighConvertAI.Server.Services.Screenshot
{
    public class ScreenshotService : IScreenshotService
    {
        public async Task<byte[]> DesktopAsync(string url)
        {
            return await TakeScreenshot(
            url,
            1440,
            900,
            false
        );
        }

        public async Task<byte[]> MobileAsync(string url)
        {
            return await TakeScreenshot(
                        url,
                        390,
                        844,
                        true
                    );
        }

        private async Task<byte[]> TakeScreenshot(string url, int width, int height, bool isMobile)
        {
            using var playwright = await Playwright.CreateAsync();

            await using var browser = await playwright.Chromium.LaunchAsync(new()
            {
                Headless = true
            });

            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new()
                {
                    Width = width,
                    Height = height
                },
                IsMobile = isMobile
            });

            var page = await context.NewPageAsync();

            await page.GotoAsync(url, new()
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 15000
            });

            // Initial render
            await page.WaitForTimeoutAsync(1500);

            // Trigger lazy loading
            await page.EvaluateAsync(@"
            () => {
                window.scrollTo(0, document.body.scrollHeight);
            }");

            // Wait for images and AJAX content
            await page.WaitForTimeoutAsync(3000);

            // Back to top
            await page.EvaluateAsync("window.scrollTo(0, 0)");

            await page.WaitForTimeoutAsync(500);

            // Remove common popups
            await page.EvaluateAsync(@"
            () => {

                const selectors = [
                    '.modal',
                    '.popup',
                    '.cookie-banner',
                    '.cookie-consent',
                    '.overlay',
                    '.drawer',
                    '.klaviyo-form',
                    '[role=""dialog""]'
                ];

                selectors.forEach(selector => {

                    document
                        .querySelectorAll(selector)
                        .forEach(el => el.remove());

                });

            }");

            return await page.ScreenshotAsync(new()
            {
                FullPage = true,
                Type = ScreenshotType.Png
            });
        }
    }
}

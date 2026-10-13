using HighConvertAI.Server.Models.DatabaseModels;
using HighConvertAI.Server.Services;
using HighConvertAI.Server.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Playwright;

namespace HighConvertAI.Server.Controllers
{
    [ApiController]
    [Route("api/screenshot")]
    public class ScreenshotController : ControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly string _storageRoot;

        public ScreenshotController(IAuditService auditService, IWebHostEnvironment env)
        {
            _auditService = auditService;
            _storageRoot = Path.Combine(env.ContentRootPath, "App_Data", "Screenshots");
            Directory.CreateDirectory(_storageRoot);
        }

        [HttpGet("desktop")]
        public async Task<IActionResult> Desktop([FromQuery] int pageScanId)
        {
            try
            {
                var audit = await _auditService.GetAuditById(pageScanId);
                if (audit is null)
                    return NotFound();

                var bytes = await GetOrCreateScreenshot(audit.Url, audit.CreatedAtUtc, isMobile: false, width: 1440, height: 900);
                return File(bytes, "image/png");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        [HttpGet("mobile")]
        public async Task<IActionResult> Mobile([FromQuery] int pageScanId)
        {
            try
            {
                var audit = await _auditService.GetAuditById(pageScanId);
                if (audit is null)
                    return NotFound();

                var bytes = await GetOrCreateScreenshot(audit.Url, audit.CreatedAtUtc, isMobile: true, width: 390, height: 844);
                return File(bytes, "image/png");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        private async Task<byte[]> GetOrCreateScreenshot(string url, DateTime createdAtUtc, bool isMobile, int width, int height)
        {
            var id = GenerateScreenshotId(url, createdAtUtc, isMobile);
            var filePath = Path.Combine(_storageRoot, $"{id}.png");

            if (System.IO.File.Exists(filePath))
            {
                return await System.IO.File.ReadAllBytesAsync(filePath);
            }

            var bytes = await TakeScreenshot(url, width, height, isMobile);
            await System.IO.File.WriteAllBytesAsync(filePath, bytes);

            return bytes;
        }

        // Deterministic: same (url, createdAtUtc, isMobile) always produces the same GUID,
        // so repeated requests for the same audit hit the file on disk instead of re-screenshotting.
        private static Guid GenerateScreenshotId(string url, DateTime createdAtUtc, bool isMobile)
        {
            var input = $"{url}|{createdAtUtc:O}|{isMobile}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));

            var guidBytes = new byte[16];
            Array.Copy(hash, guidBytes, 16);
            return new Guid(guidBytes);
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
                ViewportSize = new() { Width = width, Height = height },
                IsMobile = isMobile
            });

            var page = await context.NewPageAsync();

            await page.AddInitScriptAsync(@"
        const style = document.createElement('style');
        style.innerHTML = `
            [class*='cookie' i], [id*='cookie' i],
            [class*='consent' i], [id*='consent' i],
            [class*='gdpr' i], [id*='gdpr' i],
            [class*='popup' i], [id*='popup' i],
            [class*='modal' i], [id*='modal' i],
            [class*='newsletter' i], [id*='newsletter' i],
            [class*='overlay' i], [id*='overlay' i],
            [role='dialog'], [aria-modal='true'] {
                display: none !important;
                visibility: hidden !important;
                opacity: 0 !important;
            }
            body { overflow: auto !important; }
        `;
        document.documentElement.appendChild(style);
    ");

            await page.GotoAsync(url, new()
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 15000
            });

            await page.WaitForTimeoutAsync(1500);

            await page.EvaluateAsync("() => window.scrollTo(0, document.body.scrollHeight)");
            await page.WaitForTimeoutAsync(3000);
            await page.EvaluateAsync("window.scrollTo(0, 0)");
            await page.WaitForTimeoutAsync(500);

            await page.EvaluateAsync(@"
                () => {
                    const selectors = [
                        '.modal', '.popup', '.overlay', '.drawer', '.klaviyo-form',
                        '[role=""dialog""]', '[aria-modal=""true""]',
                        '[class*=""cookie""]', '[id*=""cookie""]',
                        '[class*=""consent""]', '[id*=""consent""]',
                        '[class*=""gdpr""]', '[id*=""gdpr""]',
                        '[class*=""newsletter""]', '[id*=""newsletter""]'
                    ];
                    selectors.forEach(sel => {
                        document.querySelectorAll(sel).forEach(el => el.remove());
                    });

                    document.querySelectorAll('body *').forEach(el => {
                        const cs = getComputedStyle(el);
                        if ((cs.position === 'fixed' || cs.position === 'sticky') &&
                            parseInt(cs.zIndex || '0', 10) > 100) {
                            el.remove();
                        }
                    });

                    const dismissText = /accept|agree|got it|allow|ok|close|dismiss/i;
                    document.querySelectorAll('button, a').forEach(el => {
                        if (dismissText.test(el.textContent || '')) {
                            try { el.click(); } catch {}
                        }
                    });

                    document.body.style.overflow = 'auto';
                    document.documentElement.style.overflow = 'auto';
                    document.body.classList.remove('modal-open', 'no-scroll', 'overlay-active');
                }
            ");

            await page.WaitForTimeoutAsync(300);

            return await page.ScreenshotAsync(new()
            {
                FullPage = true,
                Type = ScreenshotType.Png
            });
        }
    }
}
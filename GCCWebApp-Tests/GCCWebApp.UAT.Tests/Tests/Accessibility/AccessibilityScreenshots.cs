using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GCCWebApp.UAT.Tests.Tests.Accessibility
{
    [TestFixture]
    public class AccessibilityScreenshots
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IBrowserContext? _context;
        private IPage? _page;

        [SetUp]
        public async Task Setup()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
            _context = await _browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 1920, Height = 1080 }
            });
            _page = await _context.NewPageAsync();
        }

        [TearDown]
        public async Task Teardown()
        {
            if (_page != null) await _page.CloseAsync();
            if (_context != null) await _context.CloseAsync();
            if (_browser != null) await _browser.CloseAsync();
            _playwright?.Dispose();
        }

        [Test]
        public async Task CaptureAllPagesWithAccessibilityChanges()
        {
            if (_page == null) throw new InvalidOperationException("Page not initialized");

            // Create screenshots directory in project root
            var projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));
            var screenshotsDir = Path.Combine(projectRoot, "screenshots", "accessibility-color-changes");
            Directory.CreateDirectory(screenshotsDir);

            var baseUrl = "http://localhost:5101";

            var pages = new[]
            {
                new { Url = "/Account/Login", Name = "01-Account-Login" },
                new { Url = "/Certificate/CertificateTileViewExternal", Name = "02-Certificate-TileView-External" },
                new { Url = "/Certificate/CertificateTileViewInternal", Name = "03-Certificate-TileView-Internal" },
                new { Url = "/Certificate/Create", Name = "04-Certificate-Create" },
                new { Url = "/Certificate/Edit/55", Name = "05-Certificate-Edit" },
                new { Url = "/Admin/UserManagement", Name = "06-Admin-UserManagement" },
                new { Url = "/Admin/Tags", Name = "07-Admin-Tags" },
                new { Url = "/Admin/RoleManagement", Name = "08-Admin-RoleManagement" },
            };

            Console.WriteLine("\n=== Starting Accessibility Screenshot Capture ===\n");

            foreach (var pageInfo in pages)
            {
                try
                {
                    Console.WriteLine($"📸 Capturing: {pageInfo.Name}...");
                    await _page.GotoAsync($"{baseUrl}{pageInfo.Url}", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

                    // Wait for any dynamic content
                    await _page.WaitForTimeoutAsync(2000);

                    var screenshotPath = Path.Combine(screenshotsDir, $"{pageInfo.Name}.png");
                    await _page.ScreenshotAsync(new()
                    {
                        Path = screenshotPath,
                        FullPage = true
                    });

                    Console.WriteLine($"✓ Saved: {pageInfo.Name}.png");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error capturing {pageInfo.Name}: {ex.Message}");
                }
            }

            Console.WriteLine($"\n✅ Screenshots saved to:\n   {screenshotsDir}\n");
        }
    }
}

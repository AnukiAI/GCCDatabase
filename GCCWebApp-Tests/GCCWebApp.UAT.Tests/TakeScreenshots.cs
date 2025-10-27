using Microsoft.Playwright;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GCCWebApp.UAT.Tests
{
    public class ScreenshotCapture
    {
        public static async Task Main(string[] args)
        {
            // Create screenshots directory
            var screenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "screenshots", "accessibility-color-changes");
            Directory.CreateDirectory(screenshotsDir);

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 1920, Height = 1080 }
            });
            var page = await context.NewPageAsync();

            var baseUrl = "http://localhost:5101";

            var pages = new[]
            {
                new { Url = "/Account/Login", Name = "Account-Login" },
                new { Url = "/Certificate/CertificateTileViewExternal", Name = "Certificate-TileView-External" },
                new { Url = "/Certificate/CertificateTileViewInternal", Name = "Certificate-TileView-Internal" },
                new { Url = "/Certificate/Create", Name = "Certificate-Create" },
                new { Url = "/Certificate/Edit/55", Name = "Certificate-Edit" },
                new { Url = "/Admin/UserManagement", Name = "Admin-UserManagement" },
                new { Url = "/Admin/Tags", Name = "Admin-Tags" },
                new { Url = "/Admin/RoleManagement", Name = "Admin-RoleManagement" },
            };

            Console.WriteLine("Starting screenshot capture...\n");

            foreach (var pageInfo in pages)
            {
                try
                {
                    Console.WriteLine($"Capturing: {pageInfo.Name}...");
                    await page.GotoAsync($"{baseUrl}{pageInfo.Url}", new() { WaitUntil = WaitUntilState.NetworkIdle });

                    // Wait for any dynamic content
                    await page.WaitForTimeoutAsync(2000);

                    var screenshotPath = Path.Combine(screenshotsDir, $"{pageInfo.Name}.png");
                    await page.ScreenshotAsync(new()
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

            await browser.CloseAsync();

            Console.WriteLine($"\n✅ Screenshots saved to: {screenshotsDir}");
        }
    }
}

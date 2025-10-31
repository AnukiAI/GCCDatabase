using Microsoft.Playwright;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GCCWebApp.UAT.Tests
{
    public class CaptureUIScreenshots
    {
        public static async Task Main(string[] args)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                IgnoreHTTPSErrors = true
            });

            var page = await context.NewPageAsync();

            var screenshotDir = Path.Combine(Directory.GetCurrentDirectory(), "UIScreenshots");
            Directory.CreateDirectory(screenshotDir);

            Console.WriteLine($"Capturing screenshots to: {screenshotDir}");

            try
            {
                // 1. Home/Landing Page (Public Certificate Tile View)
                Console.WriteLine("Capturing: Certificate Tile View (Public)...");
                await page.GotoAsync("http://localhost:5101/Certificate/CertificateTileView", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
                await page.WaitForTimeoutAsync(2000);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(screenshotDir, "01_certificate_tile_view_public.png"), FullPage = true });

                // 2. Login Page
                Console.WriteLine("Capturing: Login Page...");
                await page.GotoAsync("http://localhost:5101/Account/Login", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
                await page.WaitForTimeoutAsync(1000);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(screenshotDir, "02_login_page.png"), FullPage = true });

                // Login as Admin
                await page.FillAsync("#Email", "admin@greencirclecertified.com");
                await page.FillAsync("#Password", "Admin123!");
                await page.ClickAsync("button[type='submit']");
                await page.WaitForURLAsync("**/Dashboard**", new PageWaitForURLOptions { Timeout = 10000 });
                await page.WaitForTimeoutAsync(2000);

                // 3. Dashboard
                Console.WriteLine("Capturing: Dashboard...");
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(screenshotDir, "03_dashboard.png"), FullPage = true });

                // 4. Certificate Tile View Internal
                Console.WriteLine("Capturing: Certificate Tile View (Internal)...");
                await page.GotoAsync("http://localhost:5101/Certificate/CertificateTileViewInternal", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
                await page.WaitForTimeoutAsync(2000);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(screenshotDir, "04_certificate_tile_view_internal.png"), FullPage = true });

                // 5. Certificate Tile View External
                Console.WriteLine("Capturing: Certificate Tile View (External)...");
                await page.GotoAsync("http://localhost:5101/Certificate/CertificateTileViewExternal", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
                await page.WaitForTimeoutAsync(2000);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(screenshotDir, "05_certificate_tile_view_external.png"), FullPage = true });

                // 6. User Management
                Console.WriteLine("Capturing: User Management...");
                await page.GotoAsync("http://localhost:5101/Admin/UserManagement", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
                await page.WaitForTimeoutAsync(2000);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(screenshotDir, "06_user_management.png"), FullPage = true });

                // 7. Product Management
                Console.WriteLine("Capturing: Product Management...");
                await page.GotoAsync("http://localhost:5101/Product/Management", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
                await page.WaitForTimeoutAsync(2000);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(screenshotDir, "07_product_management.png"), FullPage = true });

                // 8. Company Management
                Console.WriteLine("Capturing: Company Management...");
                await page.GotoAsync("http://localhost:5101/Admin/CompanyManagement", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
                await page.WaitForTimeoutAsync(2000);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(screenshotDir, "08_company_management.png"), FullPage = true });

                // 9. Tag Management
                Console.WriteLine("Capturing: Tag Management...");
                await page.GotoAsync("http://localhost:5101/Admin/TagManagement", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
                await page.WaitForTimeoutAsync(2000);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(screenshotDir, "09_tag_management.png"), FullPage = true });

                // 10. Certificate Create
                Console.WriteLine("Capturing: Certificate Create...");
                await page.GotoAsync("http://localhost:5101/Certificate/Create", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
                await page.WaitForTimeoutAsync(2000);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(screenshotDir, "10_certificate_create.png"), FullPage = true });

                // 11. Certificate Details
                Console.WriteLine("Capturing: Certificate Details...");
                await page.GotoAsync("http://localhost:5101/Certificate/Details/24-0366", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
                await page.WaitForTimeoutAsync(2000);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(screenshotDir, "11_certificate_details.png"), FullPage = true });

                // 12. Role Management
                Console.WriteLine("Capturing: Role Management...");
                await page.GotoAsync("http://localhost:5101/Admin/RoleManagement", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
                await page.WaitForTimeoutAsync(2000);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(screenshotDir, "12_role_management.png"), FullPage = true });

                Console.WriteLine($"\nScreenshots captured successfully!");
                Console.WriteLine($"Location: {screenshotDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
            }
        }
    }
}

using Microsoft.Playwright;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GCCWebApp.E2E.Tests
{
    public class ScreenshotAllPages
    {
        private const string BaseUrl = "http://localhost:5101";
        private const string ScreenshotDir = @"C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\0_Testing";

        public static async Task Main(string[] args)
        {
            // Create screenshot directory if it doesn't exist
            Directory.CreateDirectory(ScreenshotDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var sessionDir = Path.Combine(ScreenshotDir, $"Screenshots_{timestamp}");
            Directory.CreateDirectory(sessionDir);

            Console.WriteLine($"Taking screenshots to: {sessionDir}");

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
            });

            var page = await context.NewPageAsync();

            // =============== PUBLIC PAGES ===============
            Console.WriteLine("\n=== PUBLIC PAGES ===");

            // 1. Login Page
            await TakeScreenshot(page, "/Account/Login", sessionDir, "01_Login_Page");

            // 2. Public Certificate Tile View
            await TakeScreenshot(page, "/Certificate/CertificateTileView", sessionDir, "02_Public_CertificateTileView");

            // =============== LOGIN AS ADMIN ===============
            Console.WriteLine("\n=== LOGGING IN AS ADMIN ===");
            await page.GotoAsync($"{BaseUrl}/Account/Login");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.FillAsync("input[name='Username'], input[name='Email'], #Username, #Email", "admin");
            await page.FillAsync("input[name='Password'], input[type='password'], #Password", "TestPass123!");
            await page.ClickAsync("button[type='submit'], input[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(2000);

            // =============== DASHBOARD ===============
            Console.WriteLine("\n=== DASHBOARD ===");
            await TakeScreenshot(page, "/Dashboard", sessionDir, "03_Dashboard");

            // =============== CERTIFICATE VIEWS ===============
            Console.WriteLine("\n=== CERTIFICATE VIEWS ===");

            // Internal Certificate Tile View
            await TakeScreenshot(page, "/Certificate/CertificateTileViewInternal", sessionDir, "04_CertificateTileViewInternal");

            // External Certificate Tile View
            await TakeScreenshot(page, "/Certificate/CertificateTileViewExternal", sessionDir, "05_CertificateTileViewExternal");

            // Certificate Create
            await TakeScreenshot(page, "/Certificate/Create", sessionDir, "06_Certificate_Create");

            // =============== ADMIN PAGES ===============
            Console.WriteLine("\n=== ADMIN PAGES ===");

            // User Management
            await TakeScreenshot(page, "/Admin/UserManagement", sessionDir, "07_Admin_UserManagement");

            // Role Management
            await TakeScreenshot(page, "/Admin/RoleManagement", sessionDir, "08_Admin_RoleManagement");

            // Partner API
            await TakeScreenshot(page, "/Admin/PartnerAPI", sessionDir, "09_Admin_PartnerAPI");

            // Partners
            await TakeScreenshot(page, "/Admin/Partners", sessionDir, "10_Admin_Partners");

            // Certificate Types
            await TakeScreenshot(page, "/Admin/CertificateTypes", sessionDir, "11_Admin_CertificateTypes");

            // Certificate Fields
            await TakeScreenshot(page, "/Admin/CertificateFields", sessionDir, "12_Admin_CertificateFields");

            // Product Types
            await TakeScreenshot(page, "/Admin/ProductTypes", sessionDir, "13_Admin_ProductTypes");

            // =============== PRODUCT MANAGEMENT ===============
            Console.WriteLine("\n=== PRODUCT MANAGEMENT ===");
            await TakeScreenshot(page, "/Product/Management", sessionDir, "14_Product_Management");

            // =============== COMPANY MANAGEMENT ===============
            Console.WriteLine("\n=== COMPANY MANAGEMENT ===");
            await TakeScreenshot(page, "/Admin/Companies", sessionDir, "15_Admin_Companies");

            // =============== TAG MANAGEMENT ===============
            Console.WriteLine("\n=== TAG MANAGEMENT ===");
            await TakeScreenshot(page, "/Admin/TagManagement", sessionDir, "16_Admin_TagManagement");

            Console.WriteLine($"\n=== COMPLETE! ===");
            Console.WriteLine($"All screenshots saved to: {sessionDir}");
        }

        private static async Task TakeScreenshot(IPage page, string path, string sessionDir, string filename)
        {
            try
            {
                Console.Write($"  {filename}... ");
                await page.GotoAsync($"{BaseUrl}{path}");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(1500); // Wait for any animations

                var screenshotPath = Path.Combine(sessionDir, $"{filename}.png");
                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = screenshotPath,
                    FullPage = true
                });
                Console.WriteLine("OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED: {ex.Message}");
            }
        }
    }
}

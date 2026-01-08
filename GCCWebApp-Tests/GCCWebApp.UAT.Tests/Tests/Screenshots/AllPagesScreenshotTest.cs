using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GCCWebApp.UAT.Tests.Tests.Screenshots
{
    [TestFixture]
    public class AllPagesScreenshotTest
    {
        private const string BaseUrl = "http://localhost:5101";
        private const string ScreenshotDir = @"C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\0_Testing";
        private string _sessionDir = "";

        [SetUp]
        public void Setup()
        {
            Directory.CreateDirectory(ScreenshotDir);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _sessionDir = Path.Combine(ScreenshotDir, $"Screenshots_{timestamp}");
            Directory.CreateDirectory(_sessionDir);
        }

        [Test]
        public async Task CaptureAllPageScreenshots()
        {
            TestContext.WriteLine($"Taking screenshots to: {_sessionDir}");

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
            TestContext.WriteLine("\n=== PUBLIC PAGES ===");

            // 1. Login Page
            await TakeScreenshot(page, "/Account/Login", "01_Login_Page");

            // 2. Public Certificate Tile View
            await TakeScreenshot(page, "/Certificate/CertificateTileView", "02_Public_CertificateTileView");

            // =============== LOGIN AS ADMIN ===============
            TestContext.WriteLine("\n=== LOGGING IN AS ADMIN ===");
            await page.GotoAsync($"{BaseUrl}/Account/Login");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Login using form
            await page.Locator("input[name='Username'], input[name='Email'], #Username, #Email").First.FillAsync("admin");
            await page.Locator("input[name='Password'], input[type='password'], #Password").First.FillAsync("TestPass123!");
            await page.Locator("button[type='submit'], input[type='submit']").First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(2000);
            TestContext.WriteLine("Login completed");

            // =============== DASHBOARD ===============
            TestContext.WriteLine("\n=== DASHBOARD ===");
            await TakeScreenshot(page, "/Dashboard", "03_Dashboard");

            // =============== CERTIFICATE VIEWS ===============
            TestContext.WriteLine("\n=== CERTIFICATE VIEWS ===");

            // Internal Certificate Tile View
            await TakeScreenshot(page, "/Certificate/CertificateTileViewInternal", "04_CertificateTileViewInternal");

            // External Certificate Tile View
            await TakeScreenshot(page, "/Certificate/CertificateTileViewExternal", "05_CertificateTileViewExternal");

            // Certificate Create
            await TakeScreenshot(page, "/Certificate/Create", "06_Certificate_Create");

            // =============== ADMIN PAGES ===============
            TestContext.WriteLine("\n=== ADMIN PAGES ===");

            // User Management
            await TakeScreenshot(page, "/Admin/UserManagement", "07_Admin_UserManagement");

            // Role Management
            await TakeScreenshot(page, "/Admin/RoleManagement", "08_Admin_RoleManagement");

            // Partner API
            await TakeScreenshot(page, "/Admin/PartnerAPI", "09_Admin_PartnerAPI");

            // Partners
            await TakeScreenshot(page, "/Admin/Partners", "10_Admin_Partners");

            // Certificate Types
            await TakeScreenshot(page, "/Admin/CertificateTypes", "11_Admin_CertificateTypes");

            // Certificate Fields
            await TakeScreenshot(page, "/Admin/CertificateFields", "12_Admin_CertificateFields");

            // Product Types
            await TakeScreenshot(page, "/Admin/ProductTypes", "13_Admin_ProductTypes");

            // =============== PRODUCT MANAGEMENT ===============
            TestContext.WriteLine("\n=== PRODUCT MANAGEMENT ===");
            await TakeScreenshot(page, "/Product/Management", "14_Product_Management");

            // =============== COMPANY MANAGEMENT ===============
            TestContext.WriteLine("\n=== COMPANY MANAGEMENT ===");
            await TakeScreenshot(page, "/Company/Management", "15_Company_Management");

            // =============== TAG MANAGEMENT ===============
            TestContext.WriteLine("\n=== TAG MANAGEMENT ===");
            await TakeScreenshot(page, "/Admin/Tags", "16_Admin_Tags");

            TestContext.WriteLine($"\n=== COMPLETE! ===");
            TestContext.WriteLine($"All screenshots saved to: {_sessionDir}");
        }

        private async Task TakeScreenshot(IPage page, string path, string filename)
        {
            try
            {
                TestContext.WriteLine($"  {filename}...");
                await page.GotoAsync($"{BaseUrl}{path}");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(1500); // Wait for any animations

                var screenshotPath = Path.Combine(_sessionDir, $"{filename}.png");
                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = screenshotPath,
                    FullPage = true
                });
                TestContext.WriteLine($"  {filename}... OK");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"  {filename}... FAILED: {ex.Message}");
            }
        }
    }
}

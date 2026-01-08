using Microsoft.Playwright;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace GCCWebApp.E2E.Tests.Tests
{
    [Collection("Sequential")]
    public class ScreenshotTests
    {
        private const string BaseUrl = "http://localhost:5101";
        private const string ScreenshotDir = @"C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\0_Testing";
        private readonly ITestOutputHelper _output;
        private static string _sessionDir;

        public ScreenshotTests(ITestOutputHelper output)
        {
            _output = output;

            // Create session directory once
            if (_sessionDir == null)
            {
                Directory.CreateDirectory(ScreenshotDir);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _sessionDir = Path.Combine(ScreenshotDir, $"Screenshots_{timestamp}");
                Directory.CreateDirectory(_sessionDir);
            }
        }

        [Fact]
        public async Task CaptureAllPageScreenshots()
        {
            _output.WriteLine($"Taking screenshots to: {_sessionDir}");

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
            _output.WriteLine("\n=== PUBLIC PAGES ===");

            // 1. Login Page
            await TakeScreenshot(page, "/Account/Login", "01_Login_Page");

            // 2. Public Certificate Tile View
            await TakeScreenshot(page, "/Certificate/CertificateTileView", "02_Public_CertificateTileView");

            // =============== LOGIN AS ADMIN ===============
            _output.WriteLine("\n=== LOGGING IN AS ADMIN ===");
            await page.GotoAsync($"{BaseUrl}/Account/Login");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Try multiple selector strategies for login form
            try
            {
                // Try username field
                var usernameField = await page.QuerySelectorAsync("input[name='Username']")
                    ?? await page.QuerySelectorAsync("#Username")
                    ?? await page.QuerySelectorAsync("input[name='Email']")
                    ?? await page.QuerySelectorAsync("#Email");

                if (usernameField != null)
                {
                    await usernameField.FillAsync("admin");
                }

                // Try password field
                var passwordField = await page.QuerySelectorAsync("input[name='Password']")
                    ?? await page.QuerySelectorAsync("#Password")
                    ?? await page.QuerySelectorAsync("input[type='password']");

                if (passwordField != null)
                {
                    await passwordField.FillAsync("TestPass123!");
                }

                // Click submit
                var submitButton = await page.QuerySelectorAsync("button[type='submit']")
                    ?? await page.QuerySelectorAsync("input[type='submit']");

                if (submitButton != null)
                {
                    await submitButton.ClickAsync();
                }

                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(2000);
                _output.WriteLine("Login completed");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Login failed: {ex.Message}");
            }

            // =============== DASHBOARD ===============
            _output.WriteLine("\n=== DASHBOARD ===");
            await TakeScreenshot(page, "/Dashboard", "03_Dashboard");

            // =============== CERTIFICATE VIEWS ===============
            _output.WriteLine("\n=== CERTIFICATE VIEWS ===");

            // Internal Certificate Tile View
            await TakeScreenshot(page, "/Certificate/CertificateTileViewInternal", "04_CertificateTileViewInternal");

            // External Certificate Tile View
            await TakeScreenshot(page, "/Certificate/CertificateTileViewExternal", "05_CertificateTileViewExternal");

            // Certificate Create
            await TakeScreenshot(page, "/Certificate/Create", "06_Certificate_Create");

            // =============== ADMIN PAGES ===============
            _output.WriteLine("\n=== ADMIN PAGES ===");

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
            _output.WriteLine("\n=== PRODUCT MANAGEMENT ===");
            await TakeScreenshot(page, "/Product/Management", "14_Product_Management");

            // =============== COMPANY MANAGEMENT ===============
            _output.WriteLine("\n=== COMPANY MANAGEMENT ===");
            await TakeScreenshot(page, "/Admin/Companies", "15_Admin_Companies");

            // =============== TAG MANAGEMENT ===============
            _output.WriteLine("\n=== TAG MANAGEMENT ===");
            await TakeScreenshot(page, "/Admin/TagManagement", "16_Admin_TagManagement");

            _output.WriteLine($"\n=== COMPLETE! ===");
            _output.WriteLine($"All screenshots saved to: {_sessionDir}");
        }

        private async Task TakeScreenshot(IPage page, string path, string filename)
        {
            try
            {
                _output.WriteLine($"  {filename}...");
                await page.GotoAsync($"{BaseUrl}{path}");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(1500); // Wait for any animations

                var screenshotPath = Path.Combine(_sessionDir, $"{filename}.png");
                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = screenshotPath,
                    FullPage = true
                });
                _output.WriteLine($"  {filename}... OK");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"  {filename}... FAILED: {ex.Message}");
            }
        }
    }
}

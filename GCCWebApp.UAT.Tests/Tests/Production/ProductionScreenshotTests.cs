using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GCCWebApp.UAT.Tests.Tests.Production
{
    [TestFixture]
    [Category("Production")]
    [Category("Screenshots")]
    public class ProductionScreenshotTests
    {
        private const string BaseUrl = "https://gccwebapp.com";
        private const string AdminUsername = "admin";
        private const string AdminPassword = "TestPass123!";
        private const string ExternalUsername = "external";
        private const string ExternalPassword = "TestPass123!";

        private IPlaywright _playwright = null!;
        private IBrowser _browser = null!;
        private IBrowserContext _context = null!;
        private IPage _page = null!;
        private string _screenshotDir = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            _screenshotDir = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "ProductionScreenshots",
                DateTime.Now.ToString("yyyyMMdd_HHmmss")
            );
            Directory.CreateDirectory(_screenshotDir);
            TestContext.WriteLine($"Screenshots will be saved to: {_screenshotDir}");
        }

        [SetUp]
        public async Task SetUp()
        {
            _context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                IgnoreHTTPSErrors = true
            });
            _page = await _context.NewPageAsync();
        }

        [TearDown]
        public async Task TearDown()
        {
            if (_context != null)
            {
                await _context.CloseAsync();
            }
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (_browser != null)
            {
                await _browser.CloseAsync();
            }
            _playwright?.Dispose();
        }

        private async Task TakeScreenshot(string name, string description)
        {
            var filename = $"{name}.png";
            var filepath = Path.Combine(_screenshotDir, filename);
            await _page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = filepath,
                FullPage = true
            });
            TestContext.WriteLine($"Screenshot saved: {filename} - {description}");
        }

        private async Task LoginAsAdmin()
        {
            await _page.GotoAsync($"{BaseUrl}/Account/Login");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Try username field first
            var usernameField = await _page.QuerySelectorAsync("input[name='Username'], #Username");
            if (usernameField != null)
            {
                await usernameField.FillAsync(AdminUsername);
            }
            else
            {
                var emailField = await _page.QuerySelectorAsync("input[name='Email'], #Email");
                if (emailField != null)
                {
                    await emailField.FillAsync(AdminUsername);
                }
            }

            await _page.FillAsync("input[name='Password'], #Password", AdminPassword);
            await _page.ClickAsync("button[type='submit'], input[type='submit']");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);
        }

        private async Task LoginAsExternal()
        {
            await _page.GotoAsync($"{BaseUrl}/Account/Login");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var usernameField = await _page.QuerySelectorAsync("input[name='Username'], #Username");
            if (usernameField != null)
            {
                await usernameField.FillAsync(ExternalUsername);
            }
            else
            {
                var emailField = await _page.QuerySelectorAsync("input[name='Email'], #Email");
                if (emailField != null)
                {
                    await emailField.FillAsync(ExternalUsername);
                }
            }

            await _page.FillAsync("input[name='Password'], #Password", ExternalPassword);
            await _page.ClickAsync("button[type='submit'], input[type='submit']");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);
        }

        [Test, Order(1)]
        public async Task Test01_LoginPage()
        {
            await _page.GotoAsync($"{BaseUrl}/Account/Login");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await TakeScreenshot("01_login_page", "Production login page");
            Assert.That(_page.Url, Does.Contain("/Account/Login"));
        }

        [Test, Order(2)]
        public async Task Test02_PublicCertificateTileView()
        {
            await _page.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(3000); // Wait for tiles to load via AJAX
            await TakeScreenshot("02_public_certificate_tiles", "Public certificate tile view");
        }

        [Test, Order(3)]
        public async Task Test03_AdminLogin()
        {
            await LoginAsAdmin();
            await TakeScreenshot("03_admin_after_login", "After admin login");

            // Check if we're logged in
            var url = _page.Url;
            TestContext.WriteLine($"Current URL after login: {url}");
        }

        [Test, Order(4)]
        public async Task Test04_AdminDashboard()
        {
            await LoginAsAdmin();
            await _page.GotoAsync($"{BaseUrl}/Dashboard");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);
            await TakeScreenshot("04_admin_dashboard", "Admin dashboard");
        }

        [Test, Order(5)]
        public async Task Test05_CertificationTypes()
        {
            await LoginAsAdmin();
            await _page.GotoAsync($"{BaseUrl}/Admin/CertificateTypes");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);
            await TakeScreenshot("05_certification_types", "Certification types management (22 types)");
        }

        [Test, Order(6)]
        public async Task Test06_TagsManagement()
        {
            await LoginAsAdmin();
            await _page.GotoAsync($"{BaseUrl}/Admin/Tags");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);
            await TakeScreenshot("06_tags_management", "Tags management (26 tags)");
        }

        [Test, Order(7)]
        public async Task Test07_CustomFields()
        {
            await LoginAsAdmin();
            await _page.GotoAsync($"{BaseUrl}/Admin/CertificateFields");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);
            await TakeScreenshot("07_custom_fields", "Custom fields management (30 fields)");
        }

        [Test, Order(8)]
        public async Task Test08_PartnerAPI()
        {
            await LoginAsAdmin();
            await _page.GotoAsync($"{BaseUrl}/Admin/PartnerAPI");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);
            await TakeScreenshot("08_partner_api", "Partner API management");
        }

        [Test, Order(9)]
        public async Task Test09_UserManagement()
        {
            await LoginAsAdmin();
            await _page.GotoAsync($"{BaseUrl}/Admin/UserManagement");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);
            await TakeScreenshot("09_user_management", "User management (18 users)");
        }

        [Test, Order(10)]
        public async Task Test10_InternalCertificateView()
        {
            await LoginAsAdmin();
            await _page.GotoAsync($"{BaseUrl}/Certificate/CertificateTileViewInternal");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(3000);
            await TakeScreenshot("10_internal_certificate_view", "Internal certificate tile view");
        }

        [Test, Order(11)]
        public async Task Test11_ExternalCertificateView()
        {
            await LoginAsExternal();
            await _page.GotoAsync($"{BaseUrl}/Certificate/CertificateTileViewExternal");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(3000);
            await TakeScreenshot("11_external_certificate_view", "External certificate tile view");
        }
    }
}

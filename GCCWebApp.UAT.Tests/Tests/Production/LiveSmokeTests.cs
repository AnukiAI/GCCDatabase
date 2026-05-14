using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GCCWebApp.UAT.Tests.Tests.Production
{
    [TestFixture]
    [Category("Production")]
    [Category("Smoke")]
    public class LiveSmokeTests
    {
        private const string BaseUrl = "https://gccwebapp.com";
        private const string GateUsername = "gcc";
        private const string GatePassword = "Cert-Test-Access-2026!";

        private const string AdminUser = "admin";
        private const string AnalystUser = "analyst";
        private const string ExternalUser = "external";
        private const string Pwd = "TestPass123!";

        private IPlaywright _pw = null!;
        private IBrowser _browser = null!;
        private IBrowserContext _ctx = null!;
        private IPage _page = null!;
        private string _outDir = null!;
        private readonly List<string> _findings = new();

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _pw = await Playwright.CreateAsync();
            _browser = await _pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

            _outDir = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "Screenshots",
                "LiveSmoke_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(_outDir);
            TestContext.WriteLine($"Output dir: {_outDir}");
        }

        [SetUp]
        public async Task SetUp()
        {
            _ctx = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
                IgnoreHTTPSErrors = true,
                HttpCredentials = new HttpCredentials
                {
                    Username = GateUsername,
                    Password = GatePassword
                }
            });
            _page = await _ctx.NewPageAsync();
            _page.SetDefaultTimeout(30000);
        }

        [TearDown]
        public async Task TearDown()
        {
            if (_ctx != null) await _ctx.CloseAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (_browser != null) await _browser.CloseAsync();
            _pw?.Dispose();

            var summaryPath = Path.Combine(_outDir, "_findings.txt");
            await File.WriteAllLinesAsync(summaryPath, _findings);
            TestContext.WriteLine($"Findings written: {summaryPath}");
        }

        private async Task Shot(string name)
        {
            var path = Path.Combine(_outDir, $"{name}.png");
            await _page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
            TestContext.WriteLine($"shot: {name}.png");
        }

        private void Record(string line)
        {
            _findings.Add(line);
            TestContext.WriteLine(line);
        }

        private async Task LoginAs(string username)
        {
            await _page.GotoAsync($"{BaseUrl}/Account/Login");
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            var userField = await _page.QuerySelectorAsync("input[name='Username'], #Username, input[name='Email'], #Email");
            if (userField == null) throw new Exception("Login form: no username/email field");
            await userField.FillAsync(username);

            await _page.FillAsync("input[name='Password'], #Password", Pwd);
            await _page.ClickAsync("button[type='submit'], input[type='submit']");
            try
            {
                await _page.WaitForURLAsync(url => !url.Contains("/Account/Login"), new() { Timeout = 15000 });
            }
            catch (TimeoutException)
            {
                Record($"  ❌ Login as '{username}' did not redirect away from /Account/Login");
                return;
            }
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        [Test, Order(1)]
        public async Task S01_SiteReachable_LoginPage()
        {
            Record("=== S01: Login page reachable through gate ===");
            var resp = await _page.GotoAsync($"{BaseUrl}/Account/Login");
            await Shot("01_login");
            Record($"  status={resp?.Status} url={_page.Url}");
            Assert.That(resp?.Status, Is.EqualTo(200), "Login page not 200");
            var title = await _page.TitleAsync();
            Record($"  title='{title}'");
            var banner = await _page.QuerySelectorAsync(".test-env-banner, [class*='test-banner'], #test-banner");
            Record(banner != null ? "  ✅ TEST banner present" : "  ⚠️ TEST banner element not detected (may be in body/text only)");
        }

        [Test, Order(2)]
        public async Task S02_PublicTileView_RendersCerts()
        {
            Record("=== S02: Public cert tile view renders cards ===");
            var resp = await _page.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(3000);
            await Shot("02_public_tiles");

            var cardSelectors = new[] { ".certificate-card", ".cert-tile", "[data-cert-id]", ".tile-card", ".card.certificate" };
            int count = 0;
            string hit = "";
            foreach (var sel in cardSelectors)
            {
                count = await _page.Locator(sel).CountAsync();
                if (count > 0) { hit = sel; break; }
            }
            Record($"  status={resp?.Status}  tile-selector='{hit}'  count={count}");
            if (count == 0) Record("  ❌ No tile cards rendered — JS data load may be broken");
            else Record($"  ✅ {count} tiles rendered ({hit})");
        }

        [Test, Order(3)]
        public async Task S03_AdminLogin_LandsAuthenticated()
        {
            Record("=== S03: Admin login ===");
            await LoginAs(AdminUser);
            await Shot("03_admin_landing");
            var url = _page.Url;
            Record($"  landed: {url}");
            Assert.That(url, Does.Not.Contain("/Account/Login"), "Admin login failed");
        }

        [Test, Order(4)]
        public async Task S04_Admin_UserManagement_Loads()
        {
            Record("=== S04: Admin UserManagement ===");
            await LoginAs(AdminUser);
            var resp = await _page.GotoAsync($"{BaseUrl}/Admin/UserManagement");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1500);
            await Shot("04_user_management");
            var rowCount = await _page.Locator("table tbody tr, .user-row, [data-user-id]").CountAsync();
            Record($"  status={resp?.Status}  url={_page.Url}  rows={rowCount}");
            if (resp?.Status >= 400) Record("  ❌ Non-2xx on UserManagement");
            else if (rowCount == 0) Record("  ⚠️ Page loaded but no user rows visible");
            else Record($"  ✅ UserManagement OK, {rowCount} rows");
        }

        [Test, Order(5)]
        public async Task S05_Admin_PartnerAPI_Loads()
        {
            Record("=== S05: Admin PartnerAPI page ===");
            await LoginAs(AdminUser);
            var resp = await _page.GotoAsync($"{BaseUrl}/Admin/PartnerAPI");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1500);
            await Shot("05_partner_api");
            Record($"  status={resp?.Status}  url={_page.Url}");
            if (resp?.Status >= 400) Record("  ❌ Non-2xx on PartnerAPI");
            else Record("  ✅ PartnerAPI page loaded");
        }

        [Test, Order(6)]
        public async Task S06_Admin_CertificateTypes_Loads()
        {
            Record("=== S06: Admin CertificateTypes ===");
            await LoginAs(AdminUser);
            var resp = await _page.GotoAsync($"{BaseUrl}/Admin/CertificateTypes");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1500);
            await Shot("06_certificate_types");
            Record($"  status={resp?.Status}  url={_page.Url}");
        }

        [Test, Order(7)]
        public async Task S07_AnalystLogin_InternalTileView()
        {
            Record("=== S07: Analyst login + internal tile view ===");
            await LoginAs(AnalystUser);
            await Shot("07a_analyst_landing");
            var resp = await _page.GotoAsync($"{BaseUrl}/Certificate/CertificateTileViewInternal");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(3000);
            await Shot("07b_analyst_internal_tiles");
            var count = await _page.Locator(".certificate-card, .cert-tile, [data-cert-id]").CountAsync();
            Record($"  status={resp?.Status}  internal-tile-count={count}");
            if (count == 0) Record("  ❌ Analyst internal tiles empty");
        }

        [Test, Order(8)]
        public async Task S08_ExternalLogin_ExternalTileView()
        {
            Record("=== S08: External login + external tile view ===");
            await LoginAs(ExternalUser);
            await Shot("08a_external_landing");
            var resp = await _page.GotoAsync($"{BaseUrl}/Certificate/CertificateTileViewExternal");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(3000);
            await Shot("08b_external_tiles");
            var count = await _page.Locator(".certificate-card, .cert-tile, [data-cert-id]").CountAsync();
            Record($"  status={resp?.Status}  external-tile-count={count}");
            if (count == 0) Record("  ❌ External tiles empty");
        }

        [Test, Order(9)]
        public async Task S09_CertDetail_Opens()
        {
            Record("=== S09: Click first public tile → detail ===");
            await _page.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(3000);

            var firstCard = _page.Locator(".certificate-card, .cert-tile, [data-cert-id]").First;
            if (await firstCard.CountAsync() == 0)
            {
                Record("  ❌ No tiles to click on public view");
                await Shot("09_no_tiles");
                return;
            }
            await firstCard.ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1500);
            await Shot("09_cert_detail");
            var url = _page.Url;
            Record($"  detail url: {url}");
            // Cert number should be in the detail page text
            var bodyText = await _page.InnerTextAsync("body");
            if (bodyText.Contains("Certificate", StringComparison.OrdinalIgnoreCase))
                Record("  ✅ Detail page body mentions 'Certificate'");
            else
                Record("  ⚠️ Detail page body does not contain 'Certificate' text");
        }

        [Test, Order(10)]
        public async Task S10_ApiGate_BypassesForApiRoutes()
        {
            Record("=== S10: API gate bypass — /api/* should not need Basic Auth ===");
            // Use a fresh context WITHOUT HttpCredentials to confirm bypass
            await using var ctxNoAuth = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true
            });
            var p = await ctxNoAuth.NewPageAsync();
            var resp = await p.GotoAsync($"{BaseUrl}/api/v1/certificates");
            var status = resp?.Status ?? 0;
            var wwwAuth = resp?.Headers != null && resp.Headers.ContainsKey("www-authenticate")
                ? resp.Headers["www-authenticate"]
                : "(none)";
            Record($"  /api/v1/certificates unauth: status={status} WWW-Auth='{wwwAuth}'");
            if (status == 401 && !wwwAuth.StartsWith("Basic", StringComparison.OrdinalIgnoreCase))
                Record("  ✅ API bypass working — 401 from app, not Basic gate");
            else if (wwwAuth.StartsWith("Basic", StringComparison.OrdinalIgnoreCase))
                Record("  ❌ API route is still behind Basic Auth gate — partners would be blocked");
            else
                Record($"  ⚠️ Unexpected status/headers");
        }
    }
}

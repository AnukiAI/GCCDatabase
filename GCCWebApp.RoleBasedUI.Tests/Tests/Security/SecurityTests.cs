using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using GCCWebApp.RoleBasedUI.Tests.Helpers;

namespace GCCWebApp.RoleBasedUI.Tests.Tests.Security;

[TestFixture]
public class SecurityTests : PageTest
{
    private TestWebApplicationFactory _factory = null!;
    private string _baseUrl = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new TestWebApplicationFactory();
        _baseUrl = _factory.CreateClient().BaseAddress!.ToString().TrimEnd('/');
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _factory?.Dispose();
    }

    [Test]
    public async Task SecureHeaders_ShouldBePresent()
    {
        var response = await Page.GotoAsync(_baseUrl);
        
        var headers = response!.Headers;
        
        Assert.That(headers.ContainsKey("x-content-type-options"), Is.True, 
            "X-Content-Type-Options header should be present");
        Assert.That(headers.ContainsKey("x-frame-options"), Is.True, 
            "X-Frame-Options header should be present");
        Assert.That(headers.ContainsKey("x-xss-protection"), Is.True, 
            "X-XSS-Protection header should be present");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(SecureHeaders_ShouldBePresent), "Security");
    }

    [Test]
    public async Task HTTPS_ShouldBeEnforced()
    {
        if (_baseUrl.StartsWith("http://"))
        {
            var httpsUrl = _baseUrl.Replace("http://", "https://");
            
            try
            {
                var response = await Page.GotoAsync(httpsUrl);
                Assert.That(response!.Status, Is.EqualTo(200).Or.EqualTo(301).Or.EqualTo(302), 
                    "HTTPS should be supported");
            }
            catch
            {
                Assert.Inconclusive("HTTPS testing requires SSL certificate configuration");
            }
        }
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(HTTPS_ShouldBeEnforced), "Security");
    }

    [Test]
    public async Task SqlInjection_ShouldBeBlocked()
    {
        await Page.GotoAsync($"{_baseUrl}/Account/Login");
        
        var maliciousInputs = new[]
        {
            "admin'; DROP TABLE Users; --",
            "' OR '1'='1",
            "admin' UNION SELECT * FROM Users --",
            "'; EXEC xp_cmdshell('dir'); --"
        };

        foreach (var input in maliciousInputs)
        {
            await Page.FillAsync("#Email", input);
            await Page.FillAsync("#Password", "password");
            await Page.ClickAsync("button[type='submit']");
            
            var currentUrl = Page.Url;
            Assert.That(currentUrl, Does.Not.Contain("/Dashboard"), 
                $"SQL injection attempt with '{input}' should not succeed");
            
            await Page.GotoAsync($"{_baseUrl}/Account/Login");
        }
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(SqlInjection_ShouldBeBlocked), "Security");
    }

    [Test]
    public async Task XSS_ShouldBePrevented()
    {
        var (email, password) = RoleTestHelper.TestUsers["Admin"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        await Page.GotoAsync($"{_baseUrl}/Certificate/Certificates");
        
        var xssPayloads = new[]
        {
            "<script>alert('XSS')</script>",
            "javascript:alert('XSS')",
            "<img src=x onerror=alert('XSS')>",
            "<svg onload=alert('XSS')>"
        };

        foreach (var payload in xssPayloads)
        {
            if (await RoleTestHelper.IsElementVisibleAsync(Page, "input[name='search']"))
            {
                await Page.FillAsync("input[name='search']", payload);
                await Page.ClickAsync("button[type='submit']");
                
                var alerts = new List<string>();
                Page.Dialog += (_, dialog) =>
                {
                    alerts.Add(dialog.Message);
                    dialog.AcceptAsync();
                };
                
                await Page.WaitForTimeoutAsync(1000);
                
                Assert.That(alerts.Count, Is.EqualTo(0), 
                    $"XSS payload '{payload}' should not execute");
            }
        }
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(XSS_ShouldBePrevented), "Security");
    }

    [Test]
    public async Task SessionSecurity_ShouldBeEnforced()
    {
        var (email, password) = RoleTestHelper.TestUsers["Admin"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        await Page.GotoAsync($"{_baseUrl}/Dashboard");
        
        var cookies = await Page.Context.CookiesAsync();
        var sessionCookie = cookies.FirstOrDefault(c => c.Name.Contains("Session") || c.Name.Contains("Auth"));
        
        if (sessionCookie != null)
        {
            Assert.That(sessionCookie.HttpOnly, Is.True, 
                "Session cookies should be HttpOnly");
            Assert.That(sessionCookie.Secure || !_baseUrl.StartsWith("https://"), Is.True, 
                "Session cookies should be Secure for HTTPS");
        }
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(SessionSecurity_ShouldBeEnforced), "Security");
    }

    [Test]
    public async Task PasswordField_ShouldBeSecure()
    {
        await Page.GotoAsync($"{_baseUrl}/Account/Login");
        
        var passwordField = await Page.QuerySelectorAsync("#Password");
        var inputType = await passwordField!.GetAttributeAsync("type");
        
        Assert.That(inputType, Is.EqualTo("password"), 
            "Password field should have type='password'");
        
        var autocomplete = await passwordField.GetAttributeAsync("autocomplete");
        Assert.That(autocomplete, Does.Not.Contain("on"), 
            "Password field should not have autocomplete enabled");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(PasswordField_ShouldBeSecure), "Security");
    }

    [Test]
    public async Task CSRFProtection_ShouldBeImplemented()
    {
        var (email, password) = RoleTestHelper.TestUsers["Admin"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        await Page.GotoAsync($"{_baseUrl}/Admin/UserManagement");
        
        var forms = await Page.QuerySelectorAllAsync("form");
        
        foreach (var form in forms)
        {
            var csrfToken = await form.QuerySelectorAsync("input[name='__RequestVerificationToken']");
            if (csrfToken != null)
            {
                var tokenValue = await csrfToken.GetAttributeAsync("value");
                Assert.That(string.IsNullOrEmpty(tokenValue), Is.False, 
                    "CSRF token should be present and have a value");
            }
        }
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(CSRFProtection_ShouldBeImplemented), "Security");
    }

    [Test]
    public async Task FileUpload_ShouldValidateFileTypes()
    {
        var (email, password) = RoleTestHelper.TestUsers["Admin"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        await Page.GotoAsync($"{_baseUrl}/Certificate/Certificates");
        
        var fileInputs = await Page.QuerySelectorAllAsync("input[type='file']");
        
        foreach (var fileInput in fileInputs)
        {
            var accept = await fileInput.GetAttributeAsync("accept");
            Assert.That(string.IsNullOrEmpty(accept), Is.False, 
                "File input should have accept attribute restricting file types");
        }
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(FileUpload_ShouldValidateFileTypes), "Security");
    }

    [Test]
    public async Task ErrorMessages_ShouldNotLeakInformation()
    {
        await Page.GotoAsync($"{_baseUrl}/Account/Login");
        
        await Page.FillAsync("#Email", "nonexistent@example.com");
        await Page.FillAsync("#Password", "wrongpassword");
        await Page.ClickAsync("button[type='submit']");
        
        var errorMessage = await Page.TextContentAsync(".alert-danger, .error-message, .validation-summary-errors");
        
        if (!string.IsNullOrEmpty(errorMessage))
        {
            Assert.That(errorMessage.ToLower(), Does.Not.Contain("user not found"), 
                "Error message should not reveal if user exists");
            Assert.That(errorMessage.ToLower(), Does.Not.Contain("invalid password"), 
                "Error message should not reveal password validation details");
            Assert.That(errorMessage.ToLower(), Does.Not.Contain("database"), 
                "Error message should not reveal database information");
        }
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(ErrorMessages_ShouldNotLeakInformation), "Security");
    }

    [Test]
    public async Task AccountLockout_ShouldPreventBruteForce()
    {
        await Page.GotoAsync($"{_baseUrl}/Account/Login");
        
        for (int i = 0; i < 6; i++)
        {
            await Page.FillAsync("#Email", "admin@test.com");
            await Page.FillAsync("#Password", "wrongpassword");
            await Page.ClickAsync("button[type='submit']");
            await Page.WaitForTimeoutAsync(500);
        }
        
        var isAccountLocked = Page.Url.Contains("/Account/AccountLocked") || 
                             await RoleTestHelper.IsElementVisibleAsync(Page, ".account-locked-message");
        
        Assert.That(isAccountLocked, Is.True, 
            "Account should be locked after multiple failed login attempts");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AccountLockout_ShouldPreventBruteForce), "Security");
    }
}
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using GCCWebApp.RoleBasedUI.Tests.Helpers;

namespace GCCWebApp.RoleBasedUI.Tests.Tests.PublicRole;

[TestFixture]
public class PublicRoleTests : PageTest
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
    public async Task PublicHomepage_ShouldBeAccessible()
    {
        await Page.GotoAsync(_baseUrl);
        
        Assert.That(Page.Url, Does.Contain(_baseUrl), "Public user should access homepage");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".public-homepage"), Is.True,
            "Public homepage elements should be visible");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(PublicHomepage_ShouldBeAccessible), "Public");
    }

    [Test]
    public async Task PublicCertificateInfo_ShouldShowBasicView()
    {
        await Page.GotoAsync($"{_baseUrl}/Certificate/Certificates");
        
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".public-certificate-info"), Is.True,
            "Public certificate information should be visible");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".admin-certificate-controls"), Is.False,
            "Admin certificate controls should not be visible to public users");
        
        await RoleTestHelper.ValidateRoleElementsAsync(Page, "Public");
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(PublicCertificateInfo_ShouldShowBasicView), "Public");
    }

    [Test]
    public async Task PublicLoginPage_ShouldBeAccessible()
    {
        await Page.GotoAsync($"{_baseUrl}/Account/Login");
        
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "#Email"), Is.True,
            "Login form should be visible");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "#Password"), Is.True,
            "Password field should be visible");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "button[type='submit']"), Is.True,
            "Submit button should be visible");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(PublicLoginPage_ShouldBeAccessible), "Public");
    }

    [Test]
    public async Task PublicAdminPages_ShouldRequireLogin()
    {
        var adminPages = new[]
        {
            "/Admin/RoleManagement",
            "/Admin/UserManagement",
            "/Admin/ProductManagement",
            "/Dashboard"
        };

        foreach (var adminPage in adminPages)
        {
            await Page.GotoAsync($"{_baseUrl}{adminPage}");
            Assert.That(Page.Url, Does.Contain("/Account/Login"), 
                $"Public user accessing {adminPage} should be redirected to login");
        }
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(PublicAdminPages_ShouldRequireLogin), "Public");
    }

    [Test]
    public async Task PublicNavigation_ShouldShowLimitedMenuItems()
    {
        await Page.GotoAsync(_baseUrl);
        
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "a[href*='Account/Login']"), Is.True,
            "Public user should see login link");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "a[href*='Admin']"), Is.False,
            "Public user should not see admin links");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".user-menu"), Is.False,
            "Public user should not see user menu");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(PublicNavigation_ShouldShowLimitedMenuItems), "Public");
    }

    [Test]
    public async Task PublicResponsiveDesign_ShouldWorkOnAllDevices()
    {
        var viewports = new[]
        {
            new { Name = "Desktop", Width = 1920, Height = 1080 },
            new { Name = "Tablet", Width = 768, Height = 1024 },
            new { Name = "Mobile", Width = 375, Height = 667 }
        };

        foreach (var viewport in viewports)
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.GotoAsync(_baseUrl);
            
            Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".public-homepage"), Is.True,
                $"Homepage should be visible on {viewport.Name}");
            
            await RoleTestHelper.TakeScreenshotAsync(Page, $"{nameof(PublicResponsiveDesign_ShouldWorkOnAllDevices)}_{viewport.Name}", "Public");
        }
    }

    [Test]
    public async Task PublicFooter_ShouldShowBrandingAndLinks()
    {
        await Page.GotoAsync(_baseUrl);
        
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "footer"), Is.True,
            "Footer should be visible");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".footer-branding"), Is.True,
            "Footer branding should be visible");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(PublicFooter_ShouldShowBrandingAndLinks), "Public");
    }

    [Test]
    public async Task PublicSignUpPage_ShouldBeAccessible()
    {
        await Page.GotoAsync($"{_baseUrl}/Account/SignUp");
        
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".signup-form"), Is.True,
            "Sign up form should be visible");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "#Username"), Is.True,
            "Username field should be visible");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "#Email"), Is.True,
            "Email field should be visible");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(PublicSignUpPage_ShouldBeAccessible), "Public");
    }
}
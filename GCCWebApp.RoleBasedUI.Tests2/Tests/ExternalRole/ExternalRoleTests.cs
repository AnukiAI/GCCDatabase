using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using GCCWebApp.RoleBasedUI.Tests.Helpers;

namespace GCCWebApp.RoleBasedUI.Tests.Tests.ExternalRole;

[TestFixture]
public class ExternalRoleTests : PageTest
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
    public async Task ExternalLogin_ShouldAccessDashboard()
    {
        var (email, password) = RoleTestHelper.TestUsers["External"];
        
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        Assert.That(Page.Url, Does.Contain("/Dashboard"), "External user should be redirected to dashboard after login");
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(ExternalLogin_ShouldAccessDashboard), "External");
    }

    [Test]
    public async Task ExternalDashboard_ShouldShowLimitedElements()
    {
        var (email, password) = RoleTestHelper.TestUsers["External"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        await Page.GotoAsync($"{_baseUrl}/Dashboard");
        
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".external-dashboard"), Is.True,
            "External dashboard elements should be visible");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".role-admin-show"), Is.False,
            "Admin-only elements should not be visible to external users");
        
        await RoleTestHelper.ValidateRoleElementsAsync(Page, "External");
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(ExternalDashboard_ShouldShowLimitedElements), "External");
    }

    [Test]
    public async Task ExternalAdminPages_ShouldBeBlocked()
    {
        var (email, password) = RoleTestHelper.TestUsers["External"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        var adminPages = new[]
        {
            "/Admin/RoleManagement",
            "/Admin/UserManagement",
            "/Admin/ProductManagement"
        };

        foreach (var adminPage in adminPages)
        {
            var isAccessible = await RoleTestHelper.IsPageAccessibleAsync(Page, $"{_baseUrl}{adminPage}");
            Assert.That(isAccessible, Is.False, $"External user should not have access to {adminPage}");
        }
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(ExternalAdminPages_ShouldBeBlocked), "External");
    }

    [Test]
    public async Task ExternalCertificateAccess_ShouldBeExternalView()
    {
        var (email, password) = RoleTestHelper.TestUsers["External"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        var isAccessible = await RoleTestHelper.IsPageAccessibleAsync(Page, $"{_baseUrl}/Certificate/CertificateTileViewExternal");
        
        Assert.That(isAccessible, Is.True, "External user should have access to external certificate view");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".external-certificate-view"), Is.True,
            "External certificate view should be visible");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(ExternalCertificateAccess_ShouldBeExternalView), "External");
    }

    [Test]
    public async Task ExternalCompanyAccess_ShouldBeRestricted()
    {
        var (email, password) = RoleTestHelper.TestUsers["External"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        var isAccessible = await RoleTestHelper.IsPageAccessibleAsync(Page, $"{_baseUrl}/Company/Management");
        
        Assert.That(isAccessible, Is.False, "External user should not have access to Company Management");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(ExternalCompanyAccess_ShouldBeRestricted), "External");
    }

    [Test]
    public async Task ExternalNavigation_ShouldShowLimitedMenuItems()
    {
        var (email, password) = RoleTestHelper.TestUsers["External"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        await Page.GotoAsync($"{_baseUrl}/Dashboard");
        
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "a[href*='Certificate']"), Is.True,
            "External user should see certificate menu item");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "a[href*='Admin']"), Is.False,
            "External user should not see admin menu items");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "a[href*='Company/Management']"), Is.False,
            "External user should not see company management menu item");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(ExternalNavigation_ShouldShowLimitedMenuItems), "External");
    }

    [Test]
    public async Task ExternalProductAccess_ShouldBeLimited()
    {
        var (email, password) = RoleTestHelper.TestUsers["External"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        await Page.GotoAsync($"{_baseUrl}/Product/Management");
        
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".external-product-view"), Is.True,
            "External product view should be visible");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".admin-product-controls"), Is.False,
            "Admin product controls should not be visible to external users");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(ExternalProductAccess_ShouldBeLimited), "External");
    }

    [Test]
    public async Task ExternalSearchFunctionality_ShouldBeLimited()
    {
        var (email, password) = RoleTestHelper.TestUsers["External"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        await Page.GotoAsync($"{_baseUrl}/Certificate/Certificates");
        
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".basic-search"), Is.True,
            "External user should have access to basic search");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".advanced-search"), Is.False,
            "External user should not have access to advanced search");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(ExternalSearchFunctionality_ShouldBeLimited), "External");
    }

    [Test]
    public async Task ExternalProfileAccess_ShouldBeBasic()
    {
        var (email, password) = RoleTestHelper.TestUsers["External"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".user-profile-link"), Is.True,
            "External user should have access to basic profile");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".admin-profile-options"), Is.False,
            "External user should not see admin profile options");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(ExternalProfileAccess_ShouldBeBasic), "External");
    }
}
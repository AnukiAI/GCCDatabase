using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using GCCWebApp.RoleBasedUI.Tests.Helpers;

namespace GCCWebApp.RoleBasedUI.Tests.Tests.AnalystRole;

[TestFixture]
public class AnalystRoleTests : PageTest
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
    public async Task AnalystLogin_ShouldAccessDashboard()
    {
        var (email, password) = RoleTestHelper.TestUsers["Analyst"];
        
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        Assert.That(Page.Url, Does.Contain("/Dashboard"), "Analyst should be redirected to dashboard after login");
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AnalystLogin_ShouldAccessDashboard), "Analyst");
    }

    [Test]
    public async Task AnalystDashboard_ShouldShowLimitedElements()
    {
        var (email, password) = RoleTestHelper.TestUsers["Analyst"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        await Page.GotoAsync($"{_baseUrl}/Dashboard");
        
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".analyst-dashboard"), Is.True,
            "Analyst dashboard elements should be visible");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".role-admin-show"), Is.False,
            "Admin-only elements should not be visible to analysts");
        
        await RoleTestHelper.ValidateRoleElementsAsync(Page, "Analyst");
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AnalystDashboard_ShouldShowLimitedElements), "Analyst");
    }

    [Test]
    public async Task AnalystRoleManagement_ShouldBeBlocked()
    {
        var (email, password) = RoleTestHelper.TestUsers["Analyst"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        var isAccessible = await RoleTestHelper.IsPageAccessibleAsync(Page, $"{_baseUrl}/Admin/RoleManagement");
        
        Assert.That(isAccessible, Is.False, "Analyst should not have access to Role Management");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AnalystRoleManagement_ShouldBeBlocked), "Analyst");
    }

    [Test]
    public async Task AnalystUserManagement_ShouldBeBlocked()
    {
        var (email, password) = RoleTestHelper.TestUsers["Analyst"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        var isAccessible = await RoleTestHelper.IsPageAccessibleAsync(Page, $"{_baseUrl}/Admin/UserManagement");
        
        Assert.That(isAccessible, Is.False, "Analyst should not have access to User Management");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AnalystUserManagement_ShouldBeBlocked), "Analyst");
    }

    [Test]
    public async Task AnalystCertificateAccess_ShouldHaveLimitedControls()
    {
        var (email, password) = RoleTestHelper.TestUsers["Analyst"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        await Page.GotoAsync($"{_baseUrl}/Certificate/Certificates");
        
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".analyst-certificate-controls"), Is.True,
            "Analyst certificate controls should be visible");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".admin-certificate-controls"), Is.False,
            "Admin certificate controls should not be visible to analysts");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AnalystCertificateAccess_ShouldHaveLimitedControls), "Analyst");
    }

    [Test]
    public async Task AnalystCompanyAccess_ShouldBeAccessible()
    {
        var (email, password) = RoleTestHelper.TestUsers["Analyst"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        var isAccessible = await RoleTestHelper.IsPageAccessibleAsync(Page, $"{_baseUrl}/Company/Management");
        
        Assert.That(isAccessible, Is.True, "Analyst should have access to Company Management");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AnalystCompanyAccess_ShouldBeAccessible), "Analyst");
    }

    [Test]
    public async Task AnalystProductManagement_ShouldBeBlocked()
    {
        var (email, password) = RoleTestHelper.TestUsers["Analyst"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        var isAccessible = await RoleTestHelper.IsPageAccessibleAsync(Page, $"{_baseUrl}/Admin/ProductManagement");
        
        Assert.That(isAccessible, Is.False, "Analyst should not have access to Product Management");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AnalystProductManagement_ShouldBeBlocked), "Analyst");
    }

    [Test]
    public async Task AnalystNavigation_ShouldShowLimitedMenuItems()
    {
        var (email, password) = RoleTestHelper.TestUsers["Analyst"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        await Page.GotoAsync($"{_baseUrl}/Dashboard");
        
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "a[href*='Certificate']"), Is.True,
            "Analyst should see certificate menu item");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "a[href*='Company']"), Is.True,
            "Analyst should see company menu item");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "a[href*='Admin/RoleManagement']"), Is.False,
            "Analyst should not see role management menu item");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "a[href*='Admin/UserManagement']"), Is.False,
            "Analyst should not see user management menu item");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AnalystNavigation_ShouldShowLimitedMenuItems), "Analyst");
    }

    [Test]
    public async Task AnalystExportFunctionality_ShouldBeAvailable()
    {
        var (email, password) = RoleTestHelper.TestUsers["Analyst"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        await Page.GotoAsync($"{_baseUrl}/Certificate/Certificates");
        
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "button[data-action='export']", 2000), Is.True,
            "Analyst should see export functionality");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AnalystExportFunctionality_ShouldBeAvailable), "Analyst");
    }
}
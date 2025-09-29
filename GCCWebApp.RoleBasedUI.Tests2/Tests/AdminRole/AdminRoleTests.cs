using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using GCCWebApp.RoleBasedUI.Tests.Helpers;

namespace GCCWebApp.RoleBasedUI.Tests.Tests.AdminRole;

[TestFixture]
public class AdminRoleTests : PageTest
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
    public async Task AdminLogin_ShouldAccessDashboard()
    {
        var (email, password) = RoleTestHelper.TestUsers["Admin"];
        
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        Assert.That(Page.Url, Does.Contain("/Dashboard"), "Admin should be redirected to dashboard after login");
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AdminLogin_ShouldAccessDashboard), "Admin");
    }

    [Test]
    public async Task AdminDashboard_ShouldShowAllAdminElements()
    {
        var (email, password) = RoleTestHelper.TestUsers["Admin"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        await Page.GotoAsync($"{_baseUrl}/Dashboard");
        
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".admin-dashboard"), Is.True,
            "Admin dashboard elements should be visible");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "a[href*='Admin']"), Is.True,
            "Admin navigation links should be visible");
        
        await RoleTestHelper.ValidateRoleElementsAsync(Page, "Admin");
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AdminDashboard_ShouldShowAllAdminElements), "Admin");
    }

    [Test]
    public async Task AdminRoleManagement_ShouldBeAccessible()
    {
        var (email, password) = RoleTestHelper.TestUsers["Admin"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        var isAccessible = await RoleTestHelper.IsPageAccessibleAsync(Page, $"{_baseUrl}/Admin/RoleManagement");
        
        Assert.That(isAccessible, Is.True, "Admin should have access to Role Management");
        Assert.That(Page.Url, Does.Contain("/Admin/RoleManagement"), 
            "Admin should be on Role Management page");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AdminRoleManagement_ShouldBeAccessible), "Admin");
    }

    [Test]
    public async Task AdminUserManagement_ShouldBeAccessible()
    {
        var (email, password) = RoleTestHelper.TestUsers["Admin"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        var isAccessible = await RoleTestHelper.IsPageAccessibleAsync(Page, $"{_baseUrl}/Admin/UserManagement");
        
        Assert.That(isAccessible, Is.True, "Admin should have access to User Management");
        Assert.That(Page.Url, Does.Contain("/Admin/UserManagement"), 
            "Admin should be on User Management page");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AdminUserManagement_ShouldBeAccessible), "Admin");
    }

    [Test]
    public async Task AdminCertificateManagement_ShouldHaveFullAccess()
    {
        var (email, password) = RoleTestHelper.TestUsers["Admin"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        await Page.GotoAsync($"{_baseUrl}/Certificate/Certificates");
        
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, ".admin-certificate-controls"), Is.True,
            "Admin certificate controls should be visible");
        Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, "button[data-action='create']", 2000), Is.True,
            "Admin should see create certificate button");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AdminCertificateManagement_ShouldHaveFullAccess), "Admin");
    }

    [Test]
    public async Task AdminProductManagement_ShouldBeAccessible()
    {
        var (email, password) = RoleTestHelper.TestUsers["Admin"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        var isAccessible = await RoleTestHelper.IsPageAccessibleAsync(Page, $"{_baseUrl}/Admin/ProductManagement");
        
        Assert.That(isAccessible, Is.True, "Admin should have access to Product Management");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AdminProductManagement_ShouldBeAccessible), "Admin");
    }

    [Test]
    public async Task AdminCompanyManagement_ShouldBeAccessible()
    {
        var (email, password) = RoleTestHelper.TestUsers["Admin"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        var isAccessible = await RoleTestHelper.IsPageAccessibleAsync(Page, $"{_baseUrl}/Company/Management");
        
        Assert.That(isAccessible, Is.True, "Admin should have access to Company Management");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AdminCompanyManagement_ShouldBeAccessible), "Admin");
    }

    [Test]
    public async Task AdminNavigation_ShouldShowAllMenuItems()
    {
        var (email, password) = RoleTestHelper.TestUsers["Admin"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        await Page.GotoAsync($"{_baseUrl}/Dashboard");
        
        var adminMenuItems = new[]
        {
            "a[href*='Admin/RoleManagement']",
            "a[href*='Admin/UserManagement']",
            "a[href*='Admin/ProductManagement']",
            "a[href*='Certificate']",
            "a[href*='Company']"
        };

        foreach (var menuItem in adminMenuItems)
        {
            Assert.That(await RoleTestHelper.IsElementVisibleAsync(Page, menuItem, 2000), Is.True,
                $"Admin menu item {menuItem} should be visible");
        }
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(AdminNavigation_ShouldShowAllMenuItems), "Admin");
    }
}
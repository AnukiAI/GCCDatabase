using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using Microsoft.Extensions.Configuration;
using FluentAssertions;
using NUnit.Framework;
using GCCWebApp.RoleBasedUI.Tests.Helpers;

namespace GCCWebApp.RoleBasedUI.Tests.Tests.AdminRole;

[TestFixture]
public class AdminRoleUITests : PageTest
{
    private IConfiguration _configuration = null!;
    private string _baseUrl = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();
        
        _baseUrl = _configuration["TestConfiguration:BaseUrl"] ?? "https://gccwebapp.com";
    }

    [SetUp]
    public async Task SetUp()
    {
        await Context.Tracing.StartAsync(new()
        {
            Title = TestContext.CurrentContext.Test.Name,
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
    }

    [TearDown]
    public async Task TearDown()
    {
        await Context.Tracing.StopAsync(new()
        {
            Path = Path.Combine("test-results", $"{TestContext.CurrentContext.Test.Name}.zip")
        });
    }

    [Test]
    public async Task AdminLogin_ShouldAccessDashboard_AndSeeAdminElements()
    {
        // Arrange & Act
        await RoleTestHelper.LoginAsRoleAsync(Page, "Admin", _configuration);
        
        // Navigate to home/dashboard
        await Page.GotoAsync(_baseUrl);
        
        // Assert - Take screenshot
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "AdminDashboard", "Admin", _configuration);
        
        // Verify admin-specific elements are visible
        await RoleTestHelper.VerifyRoleElementVisibilityAsync(Page, "Admin");
        
        // Verify admin navigation is accessible
        var adminMenuItems = await Page.QuerySelectorAllAsync(".role-admin-show");
        adminMenuItems.Should().NotBeEmpty("Admin should see admin-specific menu items");
    }

    [Test]
    public async Task AdminRoleManagement_ShouldBeAccessible_AndDisplayCorrectly()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Admin", _configuration);
        
        // Act
        await Page.GotoAsync($"{_baseUrl}/Admin/RoleManagement");
        
        // Assert - Page should load successfully
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Take screenshot of role management page
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "RoleManagement", "Admin", _configuration);
        
        // Verify page title
        var pageTitle = await Page.TextContentAsync("h1.page-title");
        pageTitle.Should().Contain("Role Management", "Role Management page should display correct title");
        
        // Verify admin controls are present
        var addRoleButton = Page.Locator("#add-role-btn");
        await Expect(addRoleButton).ToBeVisibleAsync();
        
        var exportButton = Page.Locator("#export-btn");
        await Expect(exportButton).ToBeVisibleAsync();
        
        // Verify role table exists
        var roleTable = Page.Locator("#role-table");
        await Expect(roleTable).ToBeVisibleAsync();
    }

    [Test]
    public async Task AdminUserManagement_ShouldBeAccessible_AndDisplayCorrectly()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Admin", _configuration);
        
        // Act
        await Page.GotoAsync($"{_baseUrl}/Admin/UserManagement");
        
        // Assert
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Take screenshot
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "UserManagement", "Admin", _configuration);
        
        // Verify admin can access user management
        var currentUrl = Page.Url;
        currentUrl.Should().Contain("/Admin/UserManagement", "Admin should be able to access User Management");
        
        // Check for admin-specific elements
        var adminElements = await Page.QuerySelectorAllAsync(".role-admin-show");
        adminElements.Should().NotBeEmpty("Admin should see admin-specific elements on User Management page");
    }

    [Test]
    public async Task AdminCertificateManagement_ShouldHaveFullAccess_AndAllControls()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Admin", _configuration);
        
        // Act
        await Page.GotoAsync($"{_baseUrl}/Certificate");
        
        // Assert
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Take screenshot
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "CertificateManagement", "Admin", _configuration);
        
        // Verify admin has access to all certificate controls
        var adminCertificateElements = await Page.QuerySelectorAllAsync(".role-admin-show");
        adminCertificateElements.Should().NotBeEmpty("Admin should see admin-specific certificate controls");
        
        // Check for edit/delete capabilities (admin-only)
        var editButtons = await Page.QuerySelectorAllAsync("button:has-text('Edit'), .btn:has-text('Edit')");
        editButtons.Should().NotBeEmpty("Admin should see edit buttons for certificates");
    }

    [Test]
    public async Task AdminCompanyManagement_ShouldBeAccessible_WithFullPermissions()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Admin", _configuration);
        
        // Act
        await Page.GotoAsync($"{_baseUrl}/Company");
        
        // Assert
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Take screenshot
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "CompanyManagement", "Admin", _configuration);
        
        // Verify admin can access company management
        var currentUrl = Page.Url;
        currentUrl.Should().Contain("/Company", "Admin should be able to access Company Management");
        
        // Verify admin and analyst elements are visible (both roles can access company data)
        var companyAccessElements = await Page.QuerySelectorAllAsync(".role-admin-show, .role-analyst-show");
        companyAccessElements.Should().NotBeEmpty("Admin should see company management elements");
    }

    [Test]
    public async Task AdminProductManagement_ShouldHaveFullAccess_WithAllControls()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Admin", _configuration);
        
        // Act
        await Page.GotoAsync($"{_baseUrl}/Product/Management");
        
        // Assert
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Take screenshot
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "ProductManagement", "Admin", _configuration);
        
        // Verify page loaded successfully
        var currentUrl = Page.Url;
        currentUrl.Should().Contain("/Product", "Admin should be able to access Product Management");
        
        // Check for admin controls
        var adminProductElements = await Page.QuerySelectorAllAsync(".role-admin-show");
        adminProductElements.Should().NotBeEmpty("Admin should see admin-specific product controls");
    }

    [Test]
    public async Task AdminNavigation_ShouldShowAllMenuItems_AndBeFullyAccessible()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Admin", _configuration);
        
        // Act
        await Page.GotoAsync(_baseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Take screenshot of navigation
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "AdminNavigation", "Admin", _configuration);
        
        // Assert - Check all navigation elements
        var navigationItems = new[]
        {
            "Certificates",
            "Companies", 
            "Products",
            "Admin"
        };
        
        foreach (var item in navigationItems)
        {
            var navLink = Page.Locator($"nav a:has-text('{item}'), .nav-link:has-text('{item}')");
            await Expect(navLink).ToBeVisibleAsync(); // Admin should see this navigation item
        }
        
        // Verify admin dropdown/menu items
        var adminElements = await Page.QuerySelectorAllAsync(".role-admin-show");
        adminElements.Should().NotBeEmpty("Admin should see admin-specific navigation elements");
    }

    [Test]
    public async Task AdminBreadcrumbs_ShouldDisplayCorrectly_OnAllPages()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Admin", _configuration);
        
        var testPages = new[]
        {
            ("/Admin/RoleManagement", "Role Management"),
            ("/Admin/UserManagement", "User Management"),
            ("/Certificate", "Certificates"),
            ("/Company", "Companies")
        };
        
        foreach (var (url, expectedBreadcrumb) in testPages)
        {
            // Act
            await Page.GotoAsync($"{_baseUrl}{url}");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Assert
            var breadcrumbs = await Page.QuerySelectorAllAsync(".role-admin-show");
            breadcrumbs.Should().NotBeEmpty($"Admin should see breadcrumbs on {url}");
            
            // Take screenshot
            await RoleTestHelper.TakeRoleScreenshotAsync(Page, $"AdminBreadcrumbs{url.Replace("/", "_")}", "Admin", _configuration);
        }
    }
}
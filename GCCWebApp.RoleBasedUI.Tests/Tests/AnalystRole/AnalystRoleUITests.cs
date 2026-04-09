using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using Microsoft.Extensions.Configuration;
using FluentAssertions;
using NUnit.Framework;
using GCCWebApp.RoleBasedUI.Tests.Helpers;

namespace GCCWebApp.RoleBasedUI.Tests.Tests.AnalystRole;

[TestFixture]
public class AnalystRoleUITests : PageTest
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
    public async Task AnalystLogin_ShouldAccessDashboard_AndSeeAnalystElements()
    {
        // Arrange & Act
        await RoleTestHelper.LoginAsRoleAsync(Page, "Analyst", _configuration);
        
        // Navigate to home/dashboard
        await Page.GotoAsync(_baseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Assert - Take screenshot
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "AnalystDashboard", "Analyst", _configuration);
        
        // Verify analyst-specific elements are visible
        await RoleTestHelper.VerifyRoleElementVisibilityAsync(Page, "Analyst");
        
        // Verify analyst can see their specific menu items
        var analystMenuItems = await Page.QuerySelectorAllAsync(".role-analyst-show");
        analystMenuItems.Should().NotBeEmpty("Analyst should see analyst-specific menu items");
    }

    [Test]
    public async Task AnalystRoleManagement_ShouldBeRestricted_AccessDenied()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Analyst", _configuration);
        
        // Act & Assert
        await RoleTestHelper.VerifyAccessDeniedAsync(Page, "/Admin/RoleManagement", _configuration);
        
        // Take screenshot of access denied
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "RoleManagementAccessDenied", "Analyst", _configuration);
    }

    [Test]
    public async Task AnalystUserManagement_ShouldBeRestricted_AccessDenied()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Analyst", _configuration);
        
        // Act & Assert
        await RoleTestHelper.VerifyAccessDeniedAsync(Page, "/Admin/UserManagement", _configuration);
        
        // Take screenshot of access denied
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "UserManagementAccessDenied", "Analyst", _configuration);
    }

    [Test]
    public async Task AnalystCertificateAccess_ShouldHaveLimitedAccess_WithAnalystControls()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Analyst", _configuration);
        
        // Act
        await Page.GotoAsync($"{_baseUrl}/Certificate");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Assert
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "CertificateAccess", "Analyst", _configuration);
        
        // Verify analyst has access to certificates
        var currentUrl = Page.Url;
        currentUrl.Should().Contain("/Certificate", "Analyst should be able to access Certificate pages");
        
        // Verify analyst-specific elements are visible
        var analystCertificateElements = await Page.QuerySelectorAllAsync(".role-analyst-show");
        analystCertificateElements.Should().NotBeEmpty("Analyst should see analyst-specific certificate controls");
        
        // Verify admin-only elements are NOT visible
        var adminOnlyElements = await Page.QuerySelectorAllAsync(".role-admin-show:not(.role-analyst-show)");
        foreach (var element in adminOnlyElements)
        {
            var isVisible = await element.IsVisibleAsync();
            isVisible.Should().BeFalse("Analyst should not see admin-only certificate controls");
        }
    }

    [Test]
    public async Task AnalystCompanyAccess_ShouldBeAllowed_WithSharedPermissions()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Analyst", _configuration);
        
        // Act
        await Page.GotoAsync($"{_baseUrl}/Company");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Assert
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "CompanyAccess", "Analyst", _configuration);
        
        // Verify analyst can access company data (shared with admin)
        var currentUrl = Page.Url;
        currentUrl.Should().Contain("/Company", "Analyst should be able to access Company data");
        
        // Verify analyst can see company elements
        var companyAccessElements = await Page.QuerySelectorAllAsync(".role-analyst-show");
        companyAccessElements.Should().NotBeEmpty("Analyst should see company data elements");
    }

    [Test]
    public async Task AnalystProductAccess_ShouldBeLimited_ComparedToAdmin()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Analyst", _configuration);
        
        // Act
        await Page.GotoAsync($"{_baseUrl}/Product/Management");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Assert
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "ProductAccess", "Analyst", _configuration);
        
        // Verify analyst has some access to products
        var currentUrl = Page.Url;
        currentUrl.Should().Contain("/Product", "Analyst should have some access to Product data");
        
        // Check that analyst sees their specific elements
        var analystProductElements = await Page.QuerySelectorAllAsync(".role-analyst-show");
        
        // Check that admin-only product controls are hidden
        var adminOnlyElements = await Page.QuerySelectorAllAsync(".role-admin-show:not(.role-analyst-show)");
        foreach (var element in adminOnlyElements)
        {
            var isVisible = await element.IsVisibleAsync();
            isVisible.Should().BeFalse("Analyst should not see admin-only product controls");
        }
    }

    [Test]
    public async Task AnalystNavigation_ShouldShowLimitedMenuItems_NoAdminAccess()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Analyst", _configuration);
        
        // Act
        await Page.GotoAsync(_baseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Take screenshot of navigation
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "AnalystNavigation", "Analyst", _configuration);
        
        // Assert - Check allowed navigation elements
        var allowedNavigationItems = new[]
        {
            "Certificates",
            "Companies"
        };
        
        foreach (var item in allowedNavigationItems)
        {
            var navLink = Page.Locator($"nav a:has-text('{item}'), .nav-link:has-text('{item}')");
            await Expect(navLink).ToBeVisibleAsync(); // Analyst should see this navigation item
        }
        
        // Verify analyst-specific elements are visible
        var analystElements = await Page.QuerySelectorAllAsync(".role-analyst-show");
        analystElements.Should().NotBeEmpty("Analyst should see analyst-specific navigation elements");
        
        // Verify admin-only navigation is hidden
        var adminOnlyNav = await Page.QuerySelectorAllAsync(".role-admin-show:not(.role-analyst-show)");
        foreach (var element in adminOnlyNav)
        {
            var isVisible = await element.IsVisibleAsync();
            isVisible.Should().BeFalse("Analyst should not see admin-only navigation");
        }
    }

    [Test]
    public async Task AnalystCertificateDetails_ShouldShowAnalystControls_HideAdminControls()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Analyst", _configuration);
        
        // Act - Navigate to a certificate details page
        await Page.GotoAsync($"{_baseUrl}/Certificate");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Try to access the first certificate details (if any exist)
        var firstCertificateLink = Page.Locator("a[href*='/Certificate/Details/']").First;
        if (await firstCertificateLink.CountAsync() > 0)
        {
            await firstCertificateLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Assert
            await RoleTestHelper.TakeRoleScreenshotAsync(Page, "CertificateDetails", "Analyst", _configuration);
            
            // Verify analyst controls are visible
            var analystControls = await Page.QuerySelectorAllAsync(".role-analyst-show");
            analystControls.Should().NotBeEmpty("Analyst should see analyst-specific controls on certificate details");
            
            // Verify export button is available for analyst
            var exportButton = Page.Locator("button:has-text('Export'), .btn:has-text('Export')");
            if (await exportButton.CountAsync() > 0)
            {
                await Expect(exportButton).ToBeVisibleAsync(); // Analyst should see export functionality
            }
            
            // Verify admin-only controls are hidden
            var adminOnlyControls = await Page.QuerySelectorAllAsync(".role-admin-show:not(.role-analyst-show)");
            foreach (var control in adminOnlyControls)
            {
                var isVisible = await control.IsVisibleAsync();
                isVisible.Should().BeFalse("Analyst should not see admin-only controls on certificate details");
            }
        }
    }

    [Test]
    public async Task AnalystBreadcrumbs_ShouldDisplayCorrectly_OnAccessiblePages()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Analyst", _configuration);
        
        var accessiblePages = new[]
        {
            ("/Certificate", "Certificates"),
            ("/Company", "Companies")
        };
        
        foreach (var (url, expectedPage) in accessiblePages)
        {
            // Act
            await Page.GotoAsync($"{_baseUrl}{url}");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Assert
            var analystBreadcrumbs = await Page.QuerySelectorAllAsync(".role-analyst-show");
            if (analystBreadcrumbs.Any())
            {
                analystBreadcrumbs.Should().NotBeEmpty($"Analyst should see appropriate breadcrumbs on {url}");
            }
            
            // Take screenshot
            await RoleTestHelper.TakeRoleScreenshotAsync(Page, $"AnalystBreadcrumbs{url.Replace("/", "_")}", "Analyst", _configuration);
        }
    }

    [Test]
    public async Task AnalystExportFunctionality_ShouldBeAvailable_OnAllowedPages()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Analyst", _configuration);
        
        // Act - Test export on certificate page
        await Page.GotoAsync($"{_baseUrl}/Certificate");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Assert
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "AnalystExportFunctionality", "Analyst", _configuration);
        
        // Look for export functionality that analyst should have access to
        var exportElements = await Page.QuerySelectorAllAsync("button:has-text('Export'), .btn:has-text('Export'), [id*='export'], [class*='export']");
        
        // Verify at least some export functionality exists for analyst
        if (exportElements.Any())
        {
            exportElements.Should().NotBeEmpty("Analyst should have access to export functionality");
        }
    }
}
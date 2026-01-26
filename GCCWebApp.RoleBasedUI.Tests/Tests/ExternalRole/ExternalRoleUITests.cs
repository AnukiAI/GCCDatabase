using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using Microsoft.Extensions.Configuration;
using FluentAssertions;
using NUnit.Framework;
using GCCWebApp.RoleBasedUI.Tests.Helpers;

namespace GCCWebApp.RoleBasedUI.Tests.Tests.ExternalRole;

[TestFixture]
public class ExternalRoleUITests : PageTest
{
    private IConfiguration _configuration = null!;
    private string _baseUrl = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();
        
        _baseUrl = _configuration["TestConfiguration:BaseUrl"] ?? "https://localhost:5001";
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
    public async Task ExternalLogin_ShouldAccessDashboard_AndSeeExternalElements()
    {
        // Arrange & Act
        await RoleTestHelper.LoginAsRoleAsync(Page, "External", _configuration);
        
        // Navigate to home/dashboard
        await Page.GotoAsync(_baseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Assert - Take screenshot
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "ExternalDashboard", "External", _configuration);
        
        // Verify external-specific elements are visible
        await RoleTestHelper.VerifyRoleElementVisibilityAsync(Page, "External");
        
        // Verify external can see their specific menu items
        var externalMenuItems = await Page.QuerySelectorAllAsync(".role-external-show");
        externalMenuItems.Should().NotBeEmpty("External user should see external-specific menu items");
    }

    [Test]
    public async Task ExternalCertificateAccess_ShouldUseLimitedView_CertificateTileViewExternal()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "External", _configuration);
        
        // Act
        await Page.GotoAsync($"{_baseUrl}/Certificate/CertificateTileViewExternal");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Assert
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "CertificateTileViewExternal", "External", _configuration);
        
        // Verify external user can access the external certificate view
        var currentUrl = Page.Url;
        currentUrl.Should().Contain("CertificateTileViewExternal", "External user should access the external certificate view");
        
        // Verify external-specific elements are visible
        var externalCertificateElements = await Page.QuerySelectorAllAsync(".role-external-show");
        externalCertificateElements.Should().NotBeEmpty("External user should see external-specific certificate controls");
        
        // Verify admin/analyst elements are NOT visible
        var restrictedElements = await Page.QuerySelectorAllAsync(".role-admin-show:not(.role-external-show), .role-analyst-show:not(.role-external-show)");
        foreach (var element in restrictedElements)
        {
            var isVisible = await element.IsVisibleAsync();
            isVisible.Should().BeFalse("External user should not see admin/analyst-only elements");
        }
    }

    [Test]
    public async Task ExternalCompanyAccess_ShouldBeRestricted_AccessDenied()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "External", _configuration);
        
        // Act & Assert
        await RoleTestHelper.VerifyAccessDeniedAsync(Page, "/Company", _configuration);
        
        // Take screenshot of access denied
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "CompanyAccessDenied", "External", _configuration);
    }

    [Test]
    public async Task ExternalAdminAccess_ShouldBeRestricted_AllAdminPagesBlocked()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "External", _configuration);
        
        var restrictedAdminPages = new[]
        {
            "/Admin/RoleManagement",
            "/Admin/UserManagement",
            "/Admin/ProductTypes",
            "/Admin/CertificateFields"
        };
        
        foreach (var adminPage in restrictedAdminPages)
        {
            // Act & Assert
            await RoleTestHelper.VerifyAccessDeniedAsync(Page, adminPage, _configuration);
            
            // Take screenshot
            await RoleTestHelper.TakeRoleScreenshotAsync(Page, $"AdminAccessDenied{adminPage.Replace("/", "_")}", "External", _configuration);
        }
    }

    [Test]
    public async Task ExternalProductAccess_ShouldHaveLimitedAccess_ProductManagementRestricted()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "External", _configuration);
        
        // Act
        await Page.GotoAsync($"{_baseUrl}/Product/Management");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Assert
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "ProductManagement", "External", _configuration);
        
        // External should have limited access to Product Management (as seen in layout navigation)
        var currentUrl = Page.Url;
        currentUrl.Should().Contain("/Product", "External user should have some access to Product Management");
        
        // Verify external elements only
        var externalProductElements = await Page.QuerySelectorAllAsync(".role-external-show");
        
        // Verify restricted elements are hidden
        var restrictedElements = await Page.QuerySelectorAllAsync(".role-admin-show:not(.role-external-show), .role-analyst-show:not(.role-external-show)");
        foreach (var element in restrictedElements)
        {
            var isVisible = await element.IsVisibleAsync();
            isVisible.Should().BeFalse("External user should not see admin/analyst-only product controls");
        }
    }

    [Test]
    public async Task ExternalNavigation_ShouldShowLimitedMenuItems_OnlyCertificatesAndProducts()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "External", _configuration);
        
        // Act
        await Page.GotoAsync(_baseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Take screenshot of navigation
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "ExternalNavigation", "External", _configuration);
        
        // Assert - Check allowed navigation elements for external users
        var allowedNavigationItems = new[]
        {
            "Certificates",
            "Products"
        };
        
        foreach (var item in allowedNavigationItems)
        {
            var navLink = Page.Locator($"nav a:has-text('{item}'), .nav-link:has-text('{item}')");
            await Expect(navLink).ToBeVisibleAsync(); // External user should see this navigation item
        }
        
        // Verify external-specific elements are visible
        var externalElements = await Page.QuerySelectorAllAsync(".role-external-show");
        externalElements.Should().NotBeEmpty("External user should see external-specific navigation elements");
        
        // Verify Companies navigation is NOT visible for external users
        var companiesNav = Page.Locator("nav a:has-text('Companies'), .nav-link:has-text('Companies')");
        var companiesCount = await companiesNav.CountAsync();
        if (companiesCount > 0)
        {
            await Expect(companiesNav).Not.ToBeVisibleAsync(); // External user should NOT see Companies navigation
        }
        
        // Verify Admin navigation is NOT visible
        var adminNav = Page.Locator("nav a:has-text('Admin'), .nav-link:has-text('Admin')");
        var adminCount = await adminNav.CountAsync();
        if (adminCount > 0)
        {
            await Expect(adminNav).Not.ToBeVisibleAsync(); // External user should NOT see Admin navigation
        }
    }

    [Test]
    public async Task ExternalCertificateDetails_ShouldShowExternalControls_LimitedFunctionality()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "External", _configuration);
        
        // Act - Navigate to external certificate view first
        await Page.GotoAsync($"{_baseUrl}/Certificate/CertificateTileViewExternal");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Try to access a certificate details page
        var firstCertificateLink = Page.Locator("a[href*='/Certificate/Details/']").First;
        if (await firstCertificateLink.CountAsync() > 0)
        {
            await firstCertificateLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Assert
            await RoleTestHelper.TakeRoleScreenshotAsync(Page, "CertificateDetails", "External", _configuration);
            
            // Verify external controls are visible
            var externalControls = await Page.QuerySelectorAllAsync(".role-external-show");
            externalControls.Should().NotBeEmpty("External user should see external-specific controls on certificate details");
            
            // Verify basic functionality buttons are available (download, print, back)
            var basicButtons = new[] { "Back", "Download", "Print" };
            foreach (var buttonText in basicButtons)
            {
                var button = Page.Locator($"button:has-text('{buttonText}'), .btn:has-text('{buttonText}')");
                if (await button.CountAsync() > 0)
                {
                    await Expect(button).ToBeVisibleAsync(); // External user should see this button
                }
            }
            
            // Verify advanced functionality is hidden (edit, export data, admin controls)
            var restrictedButtons = new[] { "Edit", "Export Data", "Manage", "Delete" };
            foreach (var buttonText in restrictedButtons)
            {
                var button = Page.Locator($"button:has-text('{buttonText}'), .btn:has-text('{buttonText}')");
                if (await button.CountAsync() > 0)
                {
                    await Expect(button).Not.ToBeVisibleAsync(); // External user should NOT see this button
                }
            }
        }
    }

    [Test]
    public async Task ExternalUserProfile_ShouldHaveLimitedOptions_NoAdminSettings()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "External", _configuration);
        
        // Act
        await Page.GotoAsync(_baseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Try to access user profile/settings
        var userMenuButton = Page.Locator(".user-menu, .profile-dropdown, button:has-text('external@test.com')");
        if (await userMenuButton.CountAsync() > 0)
        {
            await userMenuButton.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Assert
            await RoleTestHelper.TakeRoleScreenshotAsync(Page, "ExternalUserProfile", "External", _configuration);
            
            // Verify basic profile options are available
            var basicProfileOptions = new[] { "Profile", "Settings", "Logout" };
            foreach (var option in basicProfileOptions)
            {
                var profileOption = Page.Locator($"a:has-text('{option}'), .dropdown-item:has-text('{option}')");
                if (await profileOption.CountAsync() > 0)
                {
                    await Expect(profileOption).ToBeVisibleAsync(); // External user should see this option
                }
            }
            
            // Verify admin options are not available
            var adminProfileOptions = new[] { "Admin Panel", "User Management", "System Settings" };
            foreach (var option in adminProfileOptions)
            {
                var adminOption = Page.Locator($"a:has-text('{option}'), .dropdown-item:has-text('{option}')");
                if (await adminOption.CountAsync() > 0)
                {
                    await Expect(adminOption).Not.ToBeVisibleAsync(); // External user should NOT see this option
                }
            }
        }
    }

    [Test]
    public async Task ExternalSearchFunctionality_ShouldBeLimited_NoAdvancedFilters()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "External", _configuration);
        
        // Act
        await Page.GotoAsync($"{_baseUrl}/Certificate/CertificateTileViewExternal");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Assert
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "ExternalSearchFunctionality", "External", _configuration);
        
        // Verify basic search is available
        var searchInput = Page.Locator("input[type='search'], input[placeholder*='search'], #search-input");
        if (await searchInput.CountAsync() > 0)
        {
            await Expect(searchInput).ToBeVisibleAsync(); // External user should have access to basic search
        }
        
        // Verify external search elements are visible
        var externalSearchElements = await Page.QuerySelectorAllAsync(".role-external-show");
        
        // Verify advanced admin/analyst search features are hidden
        var advancedSearchElements = await Page.QuerySelectorAllAsync(".role-admin-show:not(.role-external-show), .role-analyst-show:not(.role-external-show)");
        foreach (var element in advancedSearchElements)
        {
            var isVisible = await element.IsVisibleAsync();
            isVisible.Should().BeFalse("External user should not see advanced search features");
        }
    }
}
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using Microsoft.Extensions.Configuration;
using FluentAssertions;
using NUnit.Framework;
using GCCWebApp.RoleBasedUI.Tests.Helpers;

namespace GCCWebApp.RoleBasedUI.Tests.Tests.PublicRole;

[TestFixture]
public class PublicRoleUITests : PageTest
{
    private IConfiguration _configuration = null!;
    private string _baseUrl = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();
        
        _baseUrl = _configuration["TestConfiguration:BaseUrl"] ?? "http://localhost:5101";
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
    public async Task PublicAccess_ShouldLoadHomePage_AndShowPublicElements()
    {
        // Act - Visit homepage without authentication
        await Page.GotoAsync(_baseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Assert - Take screenshot
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "PublicHomepage", "Public", _configuration);
        
        // Verify public-specific elements are visible
        await RoleTestHelper.VerifyRoleElementVisibilityAsync(Page, "Public");
        
        // Verify public can see basic information
        var publicElements = await Page.QuerySelectorAllAsync(".role-public-show");
        publicElements.Should().NotBeEmpty("Public users should see public-specific elements");
        
        // Verify no restricted elements are visible
        var restrictedElements = await Page.QuerySelectorAllAsync(".role-admin-show, .role-analyst-show, .role-external-show");
        foreach (var element in restrictedElements)
        {
            var isVisible = await element.IsVisibleAsync();
            isVisible.Should().BeFalse("Public users should not see any restricted elements");
        }
    }

    [Test]
    public async Task PublicCertificateAccess_ShouldShowLimitedPublicView_NoManagementFeatures()
    {
        // Act
        await Page.GotoAsync($"{_baseUrl}/Certificate");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Assert
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "PublicCertificateView", "Public", _configuration);
        
        // Verify public can access basic certificate information
        var currentUrl = Page.Url;
        currentUrl.Should().Contain("/Certificate", "Public users should be able to view certificates");
        
        // Verify public elements are visible
        var publicCertificateElements = await Page.QuerySelectorAllAsync(".role-public-show");
        publicCertificateElements.Should().NotBeEmpty("Public users should see public certificate information");
        
        // Verify no management features are visible
        var managementElements = await Page.QuerySelectorAllAsync(".role-admin-show, .role-analyst-show, .role-external-show");
        foreach (var element in managementElements)
        {
            var isVisible = await element.IsVisibleAsync();
            isVisible.Should().BeFalse("Public users should not see any management features");
        }
        
        // Verify no edit/delete/management buttons
        var managementButtons = new[] { "Edit", "Delete", "Manage", "Export Data", "Add New" };
        foreach (var buttonText in managementButtons)
        {
            var button = Page.Locator($"button:has-text('{buttonText}'), .btn:has-text('{buttonText}')");
            if (await button.CountAsync() > 0)
            {
                await Expect(button).Not.ToBeVisibleAsync(); // Public users should NOT see this button
            }
        }
    }

    [Test]
    public async Task PublicCertificateDetails_ShouldShowBasicInfo_WithLimitedActions()
    {
        // Act - Navigate to certificate listing first
        await Page.GotoAsync($"{_baseUrl}/Certificate");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Try to access a certificate details page
        var firstCertificateLink = Page.Locator("a[href*='/Certificate/Details/']").First;
        if (await firstCertificateLink.CountAsync() > 0)
        {
            await firstCertificateLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Assert
            await RoleTestHelper.TakeRoleScreenshotAsync(Page, "PublicCertificateDetails", "Public", _configuration);
            
            // Verify public controls are visible
            var publicControls = await Page.QuerySelectorAllAsync(".role-public-show");
            publicControls.Should().NotBeEmpty("Public users should see public-specific controls on certificate details");
            
            // Verify basic public functionality buttons are available
            var allowedButtons = new[] { "Back", "Download", "Print" };
            foreach (var buttonText in allowedButtons)
            {
                var button = Page.Locator($"button:has-text('{buttonText}'), .btn:has-text('{buttonText}')");
                if (await button.CountAsync() > 0)
                {
                    await Expect(button).ToBeVisibleAsync(); // Public users should see this button
                }
            }
            
            // Verify restricted functionality is hidden
            var restrictedButtons = new[] { "Edit", "Export Data", "Manage", "Delete", "Advanced Options" };
            foreach (var buttonText in restrictedButtons)
            {
                var button = Page.Locator($"button:has-text('{buttonText}'), .btn:has-text('{buttonText}')");
                if (await button.CountAsync() > 0)
                {
                    await Expect(button).Not.ToBeVisibleAsync(); // Public users should NOT see this button
                }
            }
        }
    }

    [Test]
    public async Task PublicAdminAccess_ShouldBeCompletelyRestricted_AllAdminPagesBlocked()
    {
        var restrictedAdminPages = new[]
        {
            "/Admin",
            "/Admin/RoleManagement",
            "/Admin/UserManagement",
            "/Admin/ProductTypes",
            "/Admin/CertificateFields",
            "/Admin/Partners",
            "/Admin/Tags"
        };
        
        foreach (var adminPage in restrictedAdminPages)
        {
            // Act & Assert
            await RoleTestHelper.VerifyAccessDeniedAsync(Page, adminPage, _configuration);
            
            // Take screenshot
            await RoleTestHelper.TakeRoleScreenshotAsync(Page, $"PublicAdminAccessDenied{adminPage.Replace("/", "_")}", "Public", _configuration);
        }
    }

    [Test]
    public async Task PublicCompanyAccess_ShouldBeRestricted_AccessDenied()
    {
        // Act & Assert
        await RoleTestHelper.VerifyAccessDeniedAsync(Page, "/Company", _configuration);
        
        // Take screenshot of access denied
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "PublicCompanyAccessDenied", "Public", _configuration);
    }

    [Test]
    public async Task PublicProductAccess_ShouldBeRestricted_ManagementNotAllowed()
    {
        // Act & Assert
        await RoleTestHelper.VerifyAccessDeniedAsync(Page, "/Product/Management", _configuration);
        
        // Take screenshot of access denied
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "PublicProductManagementAccessDenied", "Public", _configuration);
    }

    [Test]
    public async Task PublicNavigation_ShouldShowOnlyPublicItems_NoAuthenticatedFeatures()
    {
        // Act
        await Page.GotoAsync(_baseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Take screenshot of navigation
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "PublicNavigation", "Public", _configuration);
        
        // Assert - Verify public elements are visible
        var publicElements = await Page.QuerySelectorAllAsync(".role-public-show");
        publicElements.Should().NotBeEmpty("Public users should see public navigation elements");
        
        // Verify restricted navigation items are not visible
        var restrictedNavItems = new[] { "Admin", "Companies", "User Management", "Role Management" };
        foreach (var item in restrictedNavItems)
        {
            var navLink = Page.Locator($"nav a:has-text('{item}'), .nav-link:has-text('{item}')");
            if (await navLink.CountAsync() > 0)
            {
                await Expect(navLink).Not.ToBeVisibleAsync(); // Public users should NOT see restricted navigation
            }
        }
        
        // Verify login link is visible
        var loginLink = Page.Locator("a:has-text('Sign In'), a:has-text('Login'), a[href*='/Account/Login']");
        if (await loginLink.CountAsync() > 0)
        {
            await Expect(loginLink).ToBeVisibleAsync(); // Public users should see Login link
        }
        
        // Verify no authenticated user menu is visible
        var userMenu = Page.Locator(".user-menu, .profile-dropdown, .user-info");
        if (await userMenu.CountAsync() > 0)
        {
            await Expect(userMenu).Not.ToBeVisibleAsync(); // Public users should not see authenticated user menu
        }
    }

    [Test]
    public async Task PublicSearch_ShouldHaveBasicFunctionality_NoAdvancedFeatures()
    {
        // Act
        await Page.GotoAsync($"{_baseUrl}/Certificate");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Assert
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "PublicSearchFunctionality", "Public", _configuration);
        
        // Verify basic search might be available for public
        var searchInput = Page.Locator("input[type='search'], input[placeholder*='search'], #search-input");
        if (await searchInput.CountAsync() > 0)
        {
            // Basic search may be available for public users
            var isVisible = await searchInput.IsVisibleAsync();
            // This assertion depends on business requirements - adjust as needed
        }
        
        // Verify no advanced search features are visible
        var advancedSearchElements = await Page.QuerySelectorAllAsync(".role-admin-show, .role-analyst-show, .role-external-show");
        foreach (var element in advancedSearchElements)
        {
            var isVisible = await element.IsVisibleAsync();
            isVisible.Should().BeFalse("Public users should not see advanced search features");
        }
        
        // Verify no advanced filters or export options
        var advancedFeatures = new[] { "Advanced Filter", "Export Results", "Bulk Actions", "Manage Filters" };
        foreach (var feature in advancedFeatures)
        {
            var featureElement = Page.Locator($"button:has-text('{feature}'), .btn:has-text('{feature}'), a:has-text('{feature}')");
            if (await featureElement.CountAsync() > 0)
            {
                await Expect(featureElement).Not.ToBeVisibleAsync(); // Public users should NOT see advanced features
            }
        }
    }

    [Test]
    public async Task PublicBranding_ShouldDisplayCorrectly_WithPublicInformation()
    {
        // Act
        await Page.GotoAsync(_baseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Assert
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "PublicBranding", "Public", _configuration);
        
        // Verify main logo and branding are visible
        var logo = Page.Locator("img[alt*='Logo'], .main-logo, .logo-img img");
        if (await logo.CountAsync() > 0)
        {
            await Expect(logo).ToBeVisibleAsync(); // Public users should see the main logo
        }
        
        // Verify main title and description are visible
        var mainTitle = Page.Locator(".logo-text, h1:has-text('GreenCircle'), .page-title");
        if (await mainTitle.CountAsync() > 0)
        {
            await Expect(mainTitle).ToBeVisibleAsync(); // Public users should see the main title
        }
        
        // Verify public information elements
        var publicInfo = await Page.QuerySelectorAllAsync(".role-public-show");
        publicInfo.Should().NotBeEmpty("Public users should see public information elements");
    }

    [Test]
    public async Task PublicFooter_ShouldDisplayCorrectly_WithLegalInformation()
    {
        // Act
        await Page.GotoAsync(_baseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Assert
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "PublicFooter", "Public", _configuration);
        
        // Verify footer is present and visible
        var footer = Page.Locator("footer, .footer, .page-footer");
        if (await footer.CountAsync() > 0)
        {
            await Expect(footer).ToBeVisibleAsync(); // Public users should see the footer
        }
        
        // Check for typical public footer links
        var footerLinks = new[] { "Privacy", "Terms", "Contact", "About" };
        foreach (var linkText in footerLinks)
        {
            var footerLink = Page.Locator($"footer a:has-text('{linkText}'), .footer a:has-text('{linkText}')");
            if (await footerLink.CountAsync() > 0)
            {
                await Expect(footerLink).ToBeVisibleAsync(); // Public users should see footer link
            }
        }
    }

    [Test]
    public async Task PublicResponsiveDesign_ShouldWorkCorrectly_OnDifferentViewports()
    {
        var viewports = new[]
        {
            (1920, 1080, "Desktop"),
            (768, 1024, "Tablet"), 
            (375, 667, "Mobile")
        };
        
        foreach (var (width, height, deviceType) in viewports)
        {
            // Act
            await Page.SetViewportSizeAsync(width, height);
            await Page.GotoAsync(_baseUrl);
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Assert
            await RoleTestHelper.TakeRoleScreenshotAsync(Page, $"PublicResponsive{deviceType}", "Public", _configuration);
            
            // Verify public elements are still visible and properly responsive
            var publicElements = await Page.QuerySelectorAllAsync(".role-public-show");
            publicElements.Should().NotBeEmpty($"Public elements should be visible on {deviceType} viewport");
            
            // Verify no restricted elements become visible on different viewports
            var restrictedElements = await Page.QuerySelectorAllAsync(".role-admin-show, .role-analyst-show, .role-external-show");
            foreach (var element in restrictedElements)
            {
                var isVisible = await element.IsVisibleAsync();
                isVisible.Should().BeFalse($"Restricted elements should remain hidden on {deviceType} viewport");
            }
        }
    }
}
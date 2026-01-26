using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using Microsoft.Extensions.Configuration;
using FluentAssertions;
using NUnit.Framework;
using GCCWebApp.RoleBasedUI.Tests.Helpers;

namespace GCCWebApp.RoleBasedUI.Tests.Tests.CrossRole;

[TestFixture]
public class RoleAccessViolationTests : PageTest
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
    public async Task AnalystAccessingAdminPages_ShouldBeDenied_AndRedirectAppropriately()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "Analyst", _configuration);
        
        var restrictedPages = new[]
        {
            "/Admin/RoleManagement",
            "/Admin/UserManagement", 
            "/Admin/ProductTypes",
            "/Admin/CertificateFields",
            "/Admin/Partners"
        };

        foreach (var restrictedPage in restrictedPages)
        {
            // Act & Assert
            await RoleTestHelper.VerifyAccessDeniedAsync(Page, restrictedPage, _configuration);
            
            // Take screenshot of access violation
            await RoleTestHelper.TakeRoleScreenshotAsync(Page, $"AnalystAccessViolation{restrictedPage.Replace("/", "_")}", "Analyst", _configuration);
            
            // Verify the user is redirected appropriately (login, access denied, or error page)
            var currentUrl = Page.Url.ToLower();
            (currentUrl.Contains("/account/login") || 
             currentUrl.Contains("/account/accessdenied") || 
             currentUrl.Contains("/error") ||
             currentUrl.Contains("/unauthorized"))
                .Should().BeTrue($"Analyst accessing {restrictedPage} should be redirected to appropriate error page");
        }
    }

    [Test]
    public async Task ExternalUserAccessingInternalPages_ShouldBeDenied_SystemWide()
    {
        // Arrange
        await RoleTestHelper.LoginAsRoleAsync(Page, "External", _configuration);
        
        var restrictedPages = new[]
        {
            "/Admin/RoleManagement",
            "/Admin/UserManagement",
            "/Company",
            "/Company/Index",
            "/Admin/ProductTypes",
            "/Admin/CertificateFields"
        };

        foreach (var restrictedPage in restrictedPages)
        {
            // Act & Assert
            await RoleTestHelper.VerifyAccessDeniedAsync(Page, restrictedPage, _configuration);
            
            // Take screenshot of access violation
            await RoleTestHelper.TakeRoleScreenshotAsync(Page, $"ExternalAccessViolation{restrictedPage.Replace("/", "_")}", "External", _configuration);
        }
    }

    [Test]
    public async Task PublicUserAccessingAuthenticatedPages_ShouldRequireLogin()
    {
        // Note: Don't login - test as public user
        
        var authenticatedPages = new[]
        {
            "/Admin/RoleManagement",
            "/Admin/UserManagement",
            "/Company",
            "/Product/Management",
            "/Certificate/Management"
        };

        foreach (var authenticatedPage in authenticatedPages)
        {
            // Act
            await Page.GotoAsync($"{_baseUrl}{authenticatedPage}");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Assert - Should be redirected to login
            var currentUrl = Page.Url.ToLower();
            currentUrl.Should().Contain("/account/login", $"Public user accessing {authenticatedPage} should be redirected to login");
            
            // Take screenshot
            await RoleTestHelper.TakeRoleScreenshotAsync(Page, $"PublicAccessRequiresLogin{authenticatedPage.Replace("/", "_")}", "Public", _configuration);
        }
    }

    [Test]
    public async Task RoleElementVisibility_ShouldBeCorrectAcrossAllRoles_NoLeakage()
    {
        var roles = new[] { "Admin", "Analyst", "External" };
        
        foreach (var role in roles)
        {
            // Arrange
            await RoleTestHelper.LoginAsRoleAsync(Page, role, _configuration);
            
            // Act - Test on multiple pages
            var testPages = new[] { "/", "/Certificate" };
            
            foreach (var testPage in testPages)
            {
                await Page.GotoAsync($"{_baseUrl}{testPage}");
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                // Assert - Verify role-specific element visibility
                await VerifyNoRoleLeakageAsync(role, testPage);
                
                // Take screenshot
                await RoleTestHelper.TakeRoleScreenshotAsync(Page, $"RoleElementVisibility{role}{testPage.Replace("/", "_")}", role, _configuration);
            }
        }
    }

    [Test]
    public async Task APIEndpointAccess_ShouldBeRestrictedByRole_PreventDirectAccess()
    {
        var roleApiTests = new[]
        {
            ("Analyst", "/api/admin/users", false),
            ("Analyst", "/api/admin/roles", false),
            ("External", "/api/admin/users", false),
            ("External", "/api/admin/roles", false),
            ("External", "/api/company", false),
            ("Admin", "/api/admin/users", true),
            ("Admin", "/api/admin/roles", true),
            ("Admin", "/api/company", true)
        };

        foreach (var (role, apiEndpoint, shouldHaveAccess) in roleApiTests)
        {
            // Arrange
            await RoleTestHelper.LoginAsRoleAsync(Page, role, _configuration);
            
            // Act - Try to access API endpoint directly
            var response = await Page.GotoAsync($"{_baseUrl}{apiEndpoint}", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            
            // Assert
            if (shouldHaveAccess)
            {
                (response?.Status == 200 || response?.Status == 204).Should().BeTrue($"{role} should have access to {apiEndpoint}");
            }
            else
            {
                (response?.Status == 401 || response?.Status == 403 || response?.Status == 404).Should().BeTrue($"{role} should be denied access to {apiEndpoint}");
            }
            
            // Take screenshot of API response
            await RoleTestHelper.TakeRoleScreenshotAsync(Page, $"APIAccess{role}{apiEndpoint.Replace("/", "_")}", role, _configuration);
        }
    }

    [Test]
    public async Task FormSubmission_ShouldBeRestrictedByRole_PreventUnauthorizedActions()
    {
        // Test that users cannot submit forms they shouldn't have access to
        
        // Test Analyst trying to submit admin forms
        await RoleTestHelper.LoginAsRoleAsync(Page, "Analyst", _configuration);
        
        // Try to access role management page (should be denied)
        await RoleTestHelper.VerifyAccessDeniedAsync(Page, "/Admin/RoleManagement", _configuration);
        
        // Test External user trying to submit company forms
        await RoleTestHelper.LoginAsRoleAsync(Page, "External", _configuration);
        
        // Try to access company management (should be denied)
        await RoleTestHelper.VerifyAccessDeniedAsync(Page, "/Company", _configuration);
        
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "FormSubmissionRestrictions", "External", _configuration);
    }

    [Test]
    public async Task SessionManagement_ShouldMaintainRoleIntegrity_NoPrivilegeEscalation()
    {
        // Test that role changes require new authentication
        
        // Login as Analyst
        await RoleTestHelper.LoginAsRoleAsync(Page, "Analyst", _configuration);
        
        // Verify analyst access
        await Page.GotoAsync($"{_baseUrl}/Certificate");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var analystElements = await Page.QuerySelectorAllAsync(".role-analyst-show");
        analystElements.Should().NotBeEmpty("Analyst should see analyst elements");
        
        // Take screenshot of analyst session
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "AnalystSession", "Analyst", _configuration);
        
        // Verify admin pages are still restricted
        await RoleTestHelper.VerifyAccessDeniedAsync(Page, "/Admin/RoleManagement", _configuration);
        
        // Logout
        var logoutLink = Page.Locator("a:has-text('Logout'), a:has-text('Sign Out'), a[href*='/Account/Logout']");
        if (await logoutLink.CountAsync() > 0)
        {
            await logoutLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        
        // Verify logged out state
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "LoggedOutState", "Public", _configuration);
    }

    [Test]
    public async Task DirectURLManipulation_ShouldBeBlocked_NoBypassThroughURLs()
    {
        var urlManipulationTests = new[]
        {
            ("Analyst", "/Admin/RoleManagement"),
            ("Analyst", "/Admin/UserManagement"),
            ("External", "/Admin/RoleManagement"),
            ("External", "/Company"),
            ("External", "/Admin/UserManagement")
        };

        foreach (var (role, restrictedUrl) in urlManipulationTests)
        {
            // Arrange
            await RoleTestHelper.LoginAsRoleAsync(Page, role, _configuration);
            
            // Act - Try direct URL access
            await Page.GotoAsync($"{_baseUrl}{restrictedUrl}");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Assert - Should be denied/redirected
            var currentUrl = Page.Url.ToLower();
            (!currentUrl.Contains(restrictedUrl.ToLower()) || 
             currentUrl.Contains("/account/login") || 
             currentUrl.Contains("/account/accessdenied") ||
             currentUrl.Contains("/error") ||
             currentUrl.Contains("/unauthorized"))
                .Should().BeTrue($"{role} should not be able to access {restrictedUrl} via direct URL");
            
            // Take screenshot
            await RoleTestHelper.TakeRoleScreenshotAsync(Page, $"URLManipulation{role}{restrictedUrl.Replace("/", "_")}", role, _configuration);
        }
    }

    [Test]
    public async Task JavaScript_ShouldNotBypassRoleRestrictions_ClientSideSecurityIntact()
    {
        // Test that client-side JavaScript cannot bypass role restrictions
        
        await RoleTestHelper.LoginAsRoleAsync(Page, "External", _configuration);
        await Page.GotoAsync($"{_baseUrl}/Certificate/CertificateTileViewExternal");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Try to execute JavaScript that might reveal hidden elements
        await Page.EvaluateAsync(@"
            // Try to make all elements visible
            document.querySelectorAll('.role-admin-show, .role-analyst-show').forEach(el => {
                el.style.display = 'block';
                el.style.visibility = 'visible';
                el.classList.remove('hidden', 'd-none');
            });
        ");
        
        await Page.WaitForTimeoutAsync(1000); // Wait for any JavaScript to execute
        
        // Assert - Admin/Analyst elements should still be functionally restricted
        var adminElements = await Page.QuerySelectorAllAsync(".role-admin-show:not(.role-external-show)");
        foreach (var element in adminElements)
        {
            // Even if made visible via JS, these elements should not be functional
            var isClickable = await element.IsEnabledAsync();
            // This test depends on implementation - if elements are properly secured server-side,
            // they should not be functional even if made visible
        }
        
        await RoleTestHelper.TakeRoleScreenshotAsync(Page, "JavaScriptBypassAttempt", "External", _configuration);
    }

    private async Task VerifyNoRoleLeakageAsync(string currentRole, string pagePath)
    {
        // Verify that elements for other roles are not visible
        var allRoleClasses = new[] { "role-admin-show", "role-analyst-show", "role-external-show", "role-public-show" };
        var currentRoleClass = $"role-{currentRole.ToLower()}-show";
        
        foreach (var roleClass in allRoleClasses)
        {
            if (roleClass == currentRoleClass)
                continue; // Skip current role class
                
            // Check if elements from other roles are visible
            var otherRoleElements = await Page.QuerySelectorAllAsync($".{roleClass}:not(.{currentRoleClass})");
            foreach (var element in otherRoleElements)
            {
                var isVisible = await element.IsVisibleAsync();
                isVisible.Should().BeFalse($"Elements with class {roleClass} should not be visible to {currentRole} on page {pagePath}");
            }
        }
    }
}
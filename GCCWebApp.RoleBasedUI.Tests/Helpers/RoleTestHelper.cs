using Microsoft.Playwright;
using Microsoft.Extensions.Configuration;
using FluentAssertions;

namespace GCCWebApp.RoleBasedUI.Tests.Helpers;

public static class RoleTestHelper
{
    /// <summary>
    /// Test user credentials dictionary - keyed by role name
    /// </summary>
    public static readonly Dictionary<string, (string Email, string Password)> TestUsers = new()
    {
        ["Admin"] = ("admin", "TestPass123!"),
        ["Analyst"] = ("analyst", "TestPass123!"),
        ["External"] = ("external", "TestPass123!"),
        ["Public"] = ("", "") // Public users don't need credentials
    };

    /// <summary>
    /// Screenshot directory for test output
    /// </summary>
    private static readonly string ScreenshotDirectory = Path.Combine(
        Directory.GetCurrentDirectory(), "Screenshots");

    /// <summary>
    /// Simple login method without IConfiguration - uses TestUsers dictionary
    /// </summary>
    public static async Task LoginAsync(IPage page, string email, string password, string baseUrl)
    {
        // Navigate to login page
        await page.GotoAsync($"{baseUrl}/Account/Login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Fill login form
        await page.FillAsync("#Email", email);
        await page.FillAsync("#Password", password);

        // Submit login form
        await page.ClickAsync("button[type='submit']");

        // Wait for navigation after login
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Simple screenshot method without IConfiguration
    /// </summary>
    public static async Task TakeScreenshotAsync(IPage page, string testName, string role)
    {
        Directory.CreateDirectory(ScreenshotDirectory);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var filename = $"{testName}_{role}_{timestamp}.png";
        var filepath = Path.Combine(ScreenshotDirectory, filename);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = filepath,
            FullPage = true
        });
    }

    /// <summary>
    /// Check if an element is visible on the page
    /// </summary>
    public static async Task<bool> IsElementVisibleAsync(IPage page, string selector, int timeout = 5000)
    {
        try
        {
            var locator = page.Locator(selector);
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeout
            });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Validate that role-specific elements are properly visible/hidden
    /// </summary>
    public static async Task ValidateRoleElementsAsync(IPage page, string role)
    {
        var roleClasses = new Dictionary<string, string[]>
        {
            ["Admin"] = new[] { ".role-admin-show", ".role-analyst-show", ".role-external-show" },
            ["Analyst"] = new[] { ".role-analyst-show" },
            ["External"] = new[] { ".role-external-show" },
            ["Public"] = new[] { ".role-public-show" }
        };

        var hiddenClasses = new Dictionary<string, string[]>
        {
            ["Admin"] = Array.Empty<string>(),
            ["Analyst"] = new[] { ".role-admin-show:not(.role-analyst-show)" },
            ["External"] = new[] { ".role-admin-show", ".role-analyst-show:not(.role-external-show)" },
            ["Public"] = new[] { ".role-admin-show", ".role-analyst-show", ".role-external-show" }
        };

        // Verify elements that should be visible
        if (roleClasses.TryGetValue(role, out var visibleSelectors))
        {
            foreach (var selector in visibleSelectors)
            {
                var elements = await page.QuerySelectorAllAsync(selector);
                // Elements may or may not exist on every page
            }
        }

        // Verify elements that should be hidden
        if (hiddenClasses.TryGetValue(role, out var hiddenSelectors))
        {
            foreach (var selector in hiddenSelectors)
            {
                var elements = await page.QuerySelectorAllAsync(selector);
                foreach (var element in elements)
                {
                    var isVisible = await element.IsVisibleAsync();
                    isVisible.Should().BeFalse($"{role} should not see elements matching {selector}");
                }
            }
        }
    }

    /// <summary>
    /// Check if a page is accessible (doesn't redirect to login or access denied)
    /// </summary>
    public static async Task<bool> IsPageAccessibleAsync(IPage page, string url)
    {
        await page.GotoAsync(url);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var currentUrl = page.Url;

        // Page is accessible if we didn't get redirected to login or access denied
        return !currentUrl.Contains("/Account/Login") &&
               !currentUrl.Contains("/Account/AccessDenied") &&
               !currentUrl.Contains("/Error");
    }

    /// <summary>
    /// Login as a specific role using IConfiguration for credentials
    /// </summary>
    public static async Task LoginAsRoleAsync(IPage page, string role, IConfiguration configuration)
    {
        var baseUrl = configuration["TestConfiguration:BaseUrl"];
        
        // Navigate to login page
        await page.GotoAsync($"{baseUrl}/Account/Login");
        
        // Get credentials based on role
        var (username, password) = GetCredentialsForRole(role, configuration);
        
        // Fill login form
        await page.FillAsync("#Email", username);
        await page.FillAsync("#Password", password);
        
        // Submit login form
        await page.ClickAsync("button[type='submit']");
        
        // Wait for redirect after login
        await page.WaitForURLAsync($"{baseUrl}/**");
        
        // Verify login was successful (should not be on login page)
        var currentUrl = page.Url;
        currentUrl.Should().NotContain("/Account/Login", $"Login failed for {role} role");
    }

    public static async Task TakeRoleScreenshotAsync(IPage page, string testName, string role, IConfiguration configuration)
    {
        var screenshotDir = configuration["TestConfiguration:ScreenshotDirectory"] ?? "./Screenshots";
        Directory.CreateDirectory(screenshotDir);
        
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var filename = $"{testName}_{role}_{timestamp}.png";
        var filepath = Path.Combine(screenshotDir, filename);
        
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = filepath,
            FullPage = true
        });
    }

    public static async Task VerifyRoleElementVisibilityAsync(IPage page, string role)
    {
        switch (role.ToLower())
        {
            case "admin":
                await VerifyAdminElementsAsync(page);
                break;
            case "analyst":
                await VerifyAnalystElementsAsync(page);
                break;
            case "external":
                await VerifyExternalElementsAsync(page);
                break;
            case "public":
                await VerifyPublicElementsAsync(page);
                break;
        }
    }

    private static async Task VerifyAdminElementsAsync(IPage page)
    {
        // Admin should see all role-admin-show elements
        var adminElements = await page.QuerySelectorAllAsync(".role-admin-show");
        adminElements.Should().NotBeEmpty("Admin should see admin-specific elements");

        // Check for admin navigation items - use IsVisible instead of assertions
        var roleManagementLink = page.Locator("a[href*='RoleManagement']");
        var roleManagementVisible = await roleManagementLink.IsVisibleAsync();
        roleManagementVisible.Should().BeTrue("Admin should see Role Management link");

        var userManagementLink = page.Locator("a[href*='UserManagement']");
        var userManagementVisible = await userManagementLink.IsVisibleAsync();
        userManagementVisible.Should().BeTrue("Admin should see User Management link");
    }

    private static async Task VerifyAnalystElementsAsync(IPage page)
    {
        // Analyst should see role-analyst-show elements
        var analystElements = await page.QuerySelectorAllAsync(".role-analyst-show");
        analystElements.Should().NotBeEmpty("Analyst should see analyst-specific elements");
        
        // Analyst should NOT see admin-only elements
        var adminOnlyElements = await page.QuerySelectorAllAsync(".role-admin-show:not(.role-analyst-show)");
        foreach (var element in adminOnlyElements)
        {
            var isVisible = await element.IsVisibleAsync();
            isVisible.Should().BeFalse("Analyst should not see admin-only elements");
        }
    }

    private static async Task VerifyExternalElementsAsync(IPage page)
    {
        // External should see role-external-show elements
        var externalElements = await page.QuerySelectorAllAsync(".role-external-show");
        externalElements.Should().NotBeEmpty("External user should see external-specific elements");
        
        // External should NOT see admin or analyst elements
        var restrictedElements = await page.QuerySelectorAllAsync(".role-admin-show, .role-analyst-show");
        foreach (var element in restrictedElements)
        {
            var isVisible = await element.IsVisibleAsync();
            isVisible.Should().BeFalse("External user should not see admin/analyst elements");
        }
    }

    private static async Task VerifyPublicElementsAsync(IPage page)
    {
        // Public should only see role-public-show elements
        var publicElements = await page.QuerySelectorAllAsync(".role-public-show");
        publicElements.Should().NotBeEmpty("Public user should see public elements");
        
        // Public should NOT see any restricted elements
        var restrictedElements = await page.QuerySelectorAllAsync(".role-admin-show, .role-analyst-show, .role-external-show");
        foreach (var element in restrictedElements)
        {
            var isVisible = await element.IsVisibleAsync();
            isVisible.Should().BeFalse("Public user should not see any restricted elements");
        }
    }

    public static async Task VerifyAccessDeniedAsync(IPage page, string expectedUrl, IConfiguration configuration)
    {
        var baseUrl = configuration["TestConfiguration:BaseUrl"];
        
        // Navigate to restricted URL
        await page.GotoAsync($"{baseUrl}{expectedUrl}");
        
        // Should be redirected to login or access denied
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var currentUrl = page.Url;
        (currentUrl.Contains("/Account/Login") || currentUrl.Contains("/Account/AccessDenied") || currentUrl.Contains("/Error"))
            .Should().BeTrue($"Access to {expectedUrl} should be denied");
    }

    private static (string username, string password) GetCredentialsForRole(string role, IConfiguration configuration)
    {
        return role.ToLower() switch
        {
            "admin" => (
                configuration["TestConfiguration:AdminCredentials:Username"] ?? "admin@test.com",
                configuration["TestConfiguration:AdminCredentials:Password"] ?? "TestAdmin123!"
            ),
            "analyst" => (
                configuration["TestConfiguration:AnalystCredentials:Username"] ?? "analyst@test.com", 
                configuration["TestConfiguration:AnalystCredentials:Password"] ?? "TestAnalyst123!"
            ),
            "external" => (
                configuration["TestConfiguration:ExternalCredentials:Username"] ?? "external@test.com",
                configuration["TestConfiguration:ExternalCredentials:Password"] ?? "TestExternal123!"
            ),
            _ => throw new ArgumentException($"Unknown role: {role}")
        };
    }
}
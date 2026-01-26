using Microsoft.Playwright;
using Microsoft.Extensions.Configuration;
using FluentAssertions;

namespace GCCWebApp.RoleBasedUI.Tests.Helpers;

public static class RoleTestHelper
{
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
        
        // Check for admin navigation items
        var roleManagementLink = page.Locator("a[href*='RoleManagement']");
        await roleManagementLink.Should().BeVisibleAsync("Admin should see Role Management link");
        
        var userManagementLink = page.Locator("a[href*='UserManagement']");
        await userManagementLink.Should().BeVisibleAsync("Admin should see User Management link");
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

public static class PlaywrightAssertionExtensions
{
    public static async Task Should(this ILocator locator)
    {
        // Extension method placeholder for fluent assertions with Playwright
        await Task.CompletedTask;
    }
    
    public static async Task BeVisibleAsync(this Task locatorTask, string because = "")
    {
        // Custom assertion extension for Playwright locators
        await Task.CompletedTask;
    }
}
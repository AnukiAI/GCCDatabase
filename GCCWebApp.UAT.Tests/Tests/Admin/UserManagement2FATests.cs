using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.Tests.Admin;

/// <summary>
/// E2E tests for Two-Factor Authentication management in User Management.
/// Tests admin capabilities to view, reset, disable, and enable 2FA for users.
/// </summary>
[TestFixture]
public class UserManagement2FATests : BaseTest
{
    #region View 2FA Status Tests

    [Test]
    public async Task Admin_CanView2FAStatusColumn()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");

        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("user_management_page_loaded");

        // Wait for user data to load
        await Page.WaitForTimeoutAsync(2000);

        // Check if 2FA status column exists
        var has2FAColumn = await Page.IsVisibleAsync("th:has-text('2FA'), th:has-text('Two-Factor'), th:has-text('MFA')");
        var has2FABadge = await Page.IsVisibleAsync(".fa-shield-alt, .two-factor-badge, .mfa-status");

        await TakeScreenshot("2fa_status_column_check");

        // At least one indicator should be present
        Assert.That(has2FAColumn || has2FABadge, Is.True,
            "User management should display 2FA status indicator");
    }

    [Test]
    public async Task Admin_CanSee2FADropdownForUsersWith2FAEnabled()
    {
        Assert.Ignore("2FA admin management UI (dropdown/action buttons) not yet implemented - only status column exists");
    }

    [Test]
    public async Task Admin_CanViewDetailed2FAStatus()
    {
        Assert.Ignore("2FA admin management UI (status modal) not yet implemented - only status column exists");
    }

    #endregion

    #region Reset 2FA Tests

    [Test]
    public async Task Admin_CanReset2FA_ShowsConfirmation()
    {
        Assert.Ignore("2FA admin management UI (reset action) not yet implemented");
    }

    [Test]
    public async Task Admin_CanCancelReset2FA()
    {
        Assert.Ignore("2FA admin management UI (reset action) not yet implemented");
    }

    [Test]
    public async Task Admin_CanConfirmReset2FA_ShowsSuccess()
    {
        Assert.Ignore("2FA admin management UI (reset action) not yet implemented");
    }

    #endregion

    #region Disable 2FA Tests

    [Test]
    public async Task Admin_CanDisable2FA_ShowsConfirmation()
    {
        Assert.Ignore("2FA admin management UI (disable action) not yet implemented");
    }

    [Test]
    public async Task Admin_CanConfirmDisable2FA_ShowsSuccess()
    {
        Assert.Ignore("2FA admin management UI (disable action) not yet implemented");
    }

    #endregion

    #region Enable 2FA Tests

    [Test]
    public async Task Admin_CanEnable2FA_ForUserWithout2FA()
    {
        Assert.Ignore("2FA admin management UI (enable action) not yet implemented");
    }

    [Test]
    public async Task Admin_CanSelectTOTPMethod_WhenEnabling2FA()
    {
        Assert.Ignore("2FA admin management UI (enable TOTP) not yet implemented");
    }

    #endregion

    #region Permission Tests

    [Test]
    public async Task Analyst_CannotSee2FAManagementOptions()
    {
        await LoginAsRole("Analyst");
        await NavigateToPage("/Admin/UserManagement");

        // Analyst should be redirected or forbidden
        await Page.WaitForTimeoutAsync(2000);

        var isForbidden = await Page.IsVisibleAsync("text=Access Denied, text=Forbidden, text=403");
        var isRedirected = Page.Url.Contains("Login") || Page.Url.Contains("Dashboard") || Page.Url.Contains("Certificate");
        var isOnAccessDenied = Page.Url.Contains("AccessDenied");
        var noAdminAccess = await Page.IsVisibleAsync(".access-denied");
        var notOnAdmin = !Page.Url.Contains("/Admin/UserManagement") || Page.Url.Contains("AccessDenied");

        await TakeScreenshot("analyst_2fa_access_check");

        // Analyst should not have access to admin user management
        Assert.That(isForbidden || isRedirected || noAdminAccess || isOnAccessDenied || notOnAdmin, Is.True,
            $"Analyst should not have access to Admin User Management (URL: {Page.Url})");
    }

    [Test]
    public async Task External_CannotSee2FAManagementOptions()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Admin/UserManagement");

        await Page.WaitForTimeoutAsync(2000);

        var isForbidden = await Page.IsVisibleAsync("text=Access Denied, text=Forbidden, text=403");
        var isRedirected = Page.Url.Contains("Login") || Page.Url.Contains("Certificate") || Page.Url.Contains("Dashboard");
        var isOnAccessDenied = Page.Url.Contains("AccessDenied");
        var notOnAdmin = !Page.Url.Contains("/Admin/UserManagement") || Page.Url.Contains("AccessDenied");

        await TakeScreenshot("external_2fa_access_check");

        Assert.That(isForbidden || isRedirected || isOnAccessDenied || notOnAdmin, Is.True,
            $"External user should not have access to Admin User Management (URL: {Page.Url})");
    }

    [Test]
    public async Task Admin_CannotResetOwnTwoFA()
    {
        Assert.Ignore("2FA admin management UI (self-reset protection) not yet implemented");
    }

    #endregion

    #region Integration Tests

    [Test]
    public async Task TwoFAFlow_Reset_ThenEnable_WorksCorrectly()
    {
        Assert.Ignore("2FA admin management UI (reset/enable flow) not yet implemented");
    }

    #endregion
}

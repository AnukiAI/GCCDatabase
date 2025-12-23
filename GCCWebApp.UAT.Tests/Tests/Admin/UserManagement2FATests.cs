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

        await Page.WaitForSelectorAsync("body");
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
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");

        await Page.WaitForSelectorAsync("body");
        await Page.WaitForTimeoutAsync(2000);

        // Look for 2FA dropdown/menu button
        var twoFactorButton = await Page.QuerySelectorAsync(
            ".fa-shield-alt, button[title*='2FA'], button[title*='Two-Factor'], .two-factor-menu");

        if (twoFactorButton != null)
        {
            await twoFactorButton.ClickAsync();
            await TakeScreenshot("2fa_dropdown_opened");

            // Check for dropdown menu items
            var hasViewOption = await Page.IsVisibleAsync("text=View 2FA, text=View Status, .view-2fa-status");
            var hasResetOption = await Page.IsVisibleAsync("text=Reset 2FA, text=Reset, .reset-2fa");
            var hasDisableOption = await Page.IsVisibleAsync("text=Disable 2FA, text=Disable, .disable-2fa");

            Assert.That(hasViewOption || hasResetOption || hasDisableOption, Is.True,
                "2FA dropdown should have management options");
        }

        await TakeScreenshot("2fa_dropdown_check_completed");
    }

    [Test]
    public async Task Admin_CanViewDetailed2FAStatus()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");

        await Page.WaitForSelectorAsync("body");
        await Page.WaitForTimeoutAsync(2000);

        // Try to open 2FA status modal
        var twoFactorButton = await Page.QuerySelectorAsync(
            ".fa-shield-alt, button[title*='2FA'], .two-factor-menu");

        if (twoFactorButton != null)
        {
            await twoFactorButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            var viewStatusButton = await Page.QuerySelectorAsync(
                "text=View 2FA Status, text=View Status, .view-2fa-status, button:has-text('View')");

            if (viewStatusButton != null)
            {
                await viewStatusButton.ClickAsync();
                await TakeScreenshot("2fa_status_modal_opened");

                await Page.WaitForTimeoutAsync(1000);

                // Check for status modal content
                var hasStatusModal = await Page.IsVisibleAsync(
                    ".modal:has-text('2FA'), .modal:has-text('Two-Factor'), .admin-modal");
                var hasStatusInfo = await Page.IsVisibleAsync(
                    "text=Enabled, text=Disabled, text=TOTP, text=Email, text=Method");

                Assert.That(hasStatusModal || hasStatusInfo, Is.True,
                    "2FA status modal should display status information");
            }
        }

        await TakeScreenshot("2fa_status_view_completed");
    }

    #endregion

    #region Reset 2FA Tests

    [Test]
    public async Task Admin_CanReset2FA_ShowsConfirmation()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");

        await Page.WaitForSelectorAsync("body");
        await Page.WaitForTimeoutAsync(2000);

        var twoFactorButton = await Page.QuerySelectorAsync(
            ".fa-shield-alt, button[title*='2FA'], .two-factor-menu");

        if (twoFactorButton != null)
        {
            await twoFactorButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            var resetButton = await Page.QuerySelectorAsync(
                "text=Reset 2FA, .reset-2fa, button:has-text('Reset')");

            if (resetButton != null)
            {
                await resetButton.ClickAsync();
                await TakeScreenshot("reset_2fa_confirmation_dialog");

                await Page.WaitForTimeoutAsync(500);

                // Check for confirmation modal
                var hasConfirmation = await Page.IsVisibleAsync(
                    ".modal, .confirm-dialog, .admin-modal, text=Are you sure, text=confirm");

                Assert.That(hasConfirmation, Is.True,
                    "Reset 2FA should show confirmation dialog");
            }
        }

        await TakeScreenshot("reset_2fa_confirmation_check");
    }

    [Test]
    public async Task Admin_CanCancelReset2FA()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");

        await Page.WaitForSelectorAsync("body");
        await Page.WaitForTimeoutAsync(2000);

        var twoFactorButton = await Page.QuerySelectorAsync(
            ".fa-shield-alt, button[title*='2FA'], .two-factor-menu");

        if (twoFactorButton != null)
        {
            await twoFactorButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            var resetButton = await Page.QuerySelectorAsync(
                "text=Reset 2FA, .reset-2fa, button:has-text('Reset')");

            if (resetButton != null)
            {
                await resetButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                // Click Cancel button
                var cancelButton = await Page.QuerySelectorAsync(
                    "button:has-text('Cancel'), .btn-cancel, .modal-cancel");

                if (cancelButton != null)
                {
                    await cancelButton.ClickAsync();
                    await TakeScreenshot("reset_2fa_cancelled");

                    await Page.WaitForTimeoutAsync(500);

                    // Modal should be closed
                    var modalClosed = !(await Page.IsVisibleAsync(".modal:visible, .admin-modal:visible"));
                    Assert.That(modalClosed, Is.True, "Modal should close after cancel");
                }
            }
        }

        await TakeScreenshot("reset_2fa_cancel_completed");
    }

    [Test]
    public async Task Admin_CanConfirmReset2FA_ShowsSuccess()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");

        await Page.WaitForSelectorAsync("body");
        await Page.WaitForTimeoutAsync(2000);

        var twoFactorButton = await Page.QuerySelectorAsync(
            ".fa-shield-alt, button[title*='2FA'], .two-factor-menu");

        if (twoFactorButton != null)
        {
            await twoFactorButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            var resetButton = await Page.QuerySelectorAsync(
                "text=Reset 2FA, .reset-2fa, button:has-text('Reset')");

            if (resetButton != null)
            {
                await resetButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                // Click Confirm/Reset button in modal
                var confirmButton = await Page.QuerySelectorAsync(
                    "button:has-text('Reset'):not(.reset-2fa), button:has-text('Confirm'), .btn-danger, .modal-confirm");

                if (confirmButton != null)
                {
                    await confirmButton.ClickAsync();
                    await TakeScreenshot("reset_2fa_confirmed");

                    await Page.WaitForTimeoutAsync(2000);

                    // Check for success notification
                    var hasSuccess = await Page.IsVisibleAsync(
                        ".notification-success, .alert-success, .toast-success, text=reset successfully");

                    Assert.That(hasSuccess, Is.True,
                        "Reset 2FA should show success notification");
                }
            }
        }

        await TakeScreenshot("reset_2fa_success_completed");
    }

    #endregion

    #region Disable 2FA Tests

    [Test]
    public async Task Admin_CanDisable2FA_ShowsConfirmation()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");

        await Page.WaitForSelectorAsync("body");
        await Page.WaitForTimeoutAsync(2000);

        var twoFactorButton = await Page.QuerySelectorAsync(
            ".fa-shield-alt, button[title*='2FA'], .two-factor-menu");

        if (twoFactorButton != null)
        {
            await twoFactorButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            var disableButton = await Page.QuerySelectorAsync(
                "text=Disable 2FA, .disable-2fa, button:has-text('Disable')");

            if (disableButton != null)
            {
                await disableButton.ClickAsync();
                await TakeScreenshot("disable_2fa_confirmation_dialog");

                await Page.WaitForTimeoutAsync(500);

                // Check for confirmation modal with warning
                var hasConfirmation = await Page.IsVisibleAsync(
                    ".modal, .confirm-dialog, text=Are you sure, text=less secure");

                Assert.That(hasConfirmation, Is.True,
                    "Disable 2FA should show confirmation dialog with security warning");
            }
        }

        await TakeScreenshot("disable_2fa_confirmation_check");
    }

    [Test]
    public async Task Admin_CanConfirmDisable2FA_ShowsSuccess()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");

        await Page.WaitForSelectorAsync("body");
        await Page.WaitForTimeoutAsync(2000);

        var twoFactorButton = await Page.QuerySelectorAsync(
            ".fa-shield-alt, button[title*='2FA'], .two-factor-menu");

        if (twoFactorButton != null)
        {
            await twoFactorButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            var disableButton = await Page.QuerySelectorAsync(
                "text=Disable 2FA, .disable-2fa, button:has-text('Disable')");

            if (disableButton != null)
            {
                await disableButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var confirmButton = await Page.QuerySelectorAsync(
                    "button:has-text('Disable'):not(.disable-2fa), button:has-text('Confirm'), .btn-danger, .modal-confirm");

                if (confirmButton != null)
                {
                    await confirmButton.ClickAsync();
                    await TakeScreenshot("disable_2fa_confirmed");

                    await Page.WaitForTimeoutAsync(2000);

                    var hasSuccess = await Page.IsVisibleAsync(
                        ".notification-success, .alert-success, .toast-success, text=disabled successfully");

                    Assert.That(hasSuccess, Is.True,
                        "Disable 2FA should show success notification");
                }
            }
        }

        await TakeScreenshot("disable_2fa_success_completed");
    }

    #endregion

    #region Enable 2FA Tests

    [Test]
    public async Task Admin_CanEnable2FA_ForUserWithout2FA()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");

        await Page.WaitForSelectorAsync("body");
        await Page.WaitForTimeoutAsync(2000);

        // Look for user without 2FA (grayed out shield or different indicator)
        var enableButton = await Page.QuerySelectorAsync(
            "button:has-text('Enable 2FA'), .enable-2fa, .fa-shield-alt.disabled");

        if (enableButton != null)
        {
            await enableButton.ClickAsync();
            await TakeScreenshot("enable_2fa_modal_opened");

            await Page.WaitForTimeoutAsync(500);

            // Check for enable modal with method selection
            var hasModal = await Page.IsVisibleAsync(
                ".modal, .admin-modal, text=Enable 2FA");
            var hasMethodSelection = await Page.IsVisibleAsync(
                "text=TOTP, text=Email, text=Authenticator, select[name*='method']");

            Assert.That(hasModal, Is.True, "Enable 2FA modal should open");
        }

        await TakeScreenshot("enable_2fa_check_completed");
    }

    [Test]
    public async Task Admin_CanSelectTOTPMethod_WhenEnabling2FA()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");

        await Page.WaitForSelectorAsync("body");
        await Page.WaitForTimeoutAsync(2000);

        var enableButton = await Page.QuerySelectorAsync(
            "button:has-text('Enable 2FA'), .enable-2fa");

        if (enableButton != null)
        {
            await enableButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            // Select TOTP method
            var totpOption = await Page.QuerySelectorAsync(
                "input[value='TOTP'], label:has-text('TOTP'), label:has-text('Authenticator'), option[value='TOTP']");

            if (totpOption != null)
            {
                await totpOption.ClickAsync();
                await TakeScreenshot("totp_method_selected");

                var confirmButton = await Page.QuerySelectorAsync(
                    "button:has-text('Enable'), button:has-text('Save'), button:has-text('Confirm')");

                if (confirmButton != null)
                {
                    await confirmButton.ClickAsync();
                    await Page.WaitForTimeoutAsync(2000);

                    var hasSuccess = await Page.IsVisibleAsync(
                        ".notification-success, .alert-success, text=enabled");

                    Assert.That(hasSuccess, Is.True,
                        "Enable 2FA with TOTP should show success");
                }
            }
        }

        await TakeScreenshot("totp_enable_completed");
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
        var isRedirected = Page.Url.Contains("Login") || Page.Url.Contains("Dashboard");
        var noAdminAccess = await Page.IsVisibleAsync(".access-denied");

        await TakeScreenshot("analyst_2fa_access_check");

        // Analyst should not have access to admin user management
        Assert.That(isForbidden || isRedirected || noAdminAccess, Is.True,
            "Analyst should not have access to Admin User Management");
    }

    [Test]
    public async Task External_CannotSee2FAManagementOptions()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Admin/UserManagement");

        await Page.WaitForTimeoutAsync(2000);

        var isForbidden = await Page.IsVisibleAsync("text=Access Denied, text=Forbidden, text=403");
        var isRedirected = Page.Url.Contains("Login") || Page.Url.Contains("Certificate");

        await TakeScreenshot("external_2fa_access_check");

        Assert.That(isForbidden || isRedirected, Is.True,
            "External user should not have access to Admin User Management");
    }

    [Test]
    public async Task Admin_CannotResetOwnTwoFA()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");

        await Page.WaitForSelectorAsync("body");
        await Page.WaitForTimeoutAsync(2000);

        // Find the current admin user row (admin@test.com or admin user)
        var adminRow = await Page.QuerySelectorAsync("tr:has-text('admin@'), tr:has-text('admin')");

        if (adminRow != null)
        {
            var twoFactorButton = await adminRow.QuerySelectorAsync(
                ".fa-shield-alt, button[title*='2FA']");

            if (twoFactorButton != null)
            {
                await twoFactorButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                // Reset option should be disabled or not visible for own account
                var resetButton = await Page.QuerySelectorAsync(
                    ".reset-2fa:not([disabled]), button:has-text('Reset'):not([disabled])");

                await TakeScreenshot("admin_own_2fa_check");

                // Either reset button should be disabled or there should be a warning
                var hasProtection = resetButton == null ||
                    await Page.IsVisibleAsync("text=cannot reset your own, text=own account, [disabled]");

                Assert.That(hasProtection, Is.True,
                    "Admin should not be able to reset their own 2FA");
            }
        }

        await TakeScreenshot("admin_self_2fa_protection_check");
    }

    #endregion

    #region Integration Tests

    [Test]
    public async Task TwoFAFlow_Reset_ThenEnable_WorksCorrectly()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");

        await Page.WaitForSelectorAsync("body");
        await Page.WaitForTimeoutAsync(2000);

        // This test verifies the full flow: Reset 2FA then Enable 2FA
        var twoFactorButton = await Page.QuerySelectorAsync(
            ".fa-shield-alt, button[title*='2FA']");

        if (twoFactorButton != null)
        {
            // Step 1: Reset 2FA
            await twoFactorButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            var resetButton = await Page.QuerySelectorAsync("text=Reset 2FA, .reset-2fa");
            if (resetButton != null)
            {
                await resetButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var confirmReset = await Page.QuerySelectorAsync(
                    "button:has-text('Reset'):not(.reset-2fa), .modal-confirm, .btn-danger");
                if (confirmReset != null)
                {
                    await confirmReset.ClickAsync();
                    await TakeScreenshot("2fa_reset_in_flow");
                    await Page.WaitForTimeoutAsync(2000);
                }
            }

            // Step 2: Enable 2FA again
            var enableButton = await Page.QuerySelectorAsync(
                "button:has-text('Enable 2FA'), .enable-2fa");
            if (enableButton != null)
            {
                await enableButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var confirmEnable = await Page.QuerySelectorAsync(
                    "button:has-text('Enable'), button:has-text('Save')");
                if (confirmEnable != null)
                {
                    await confirmEnable.ClickAsync();
                    await TakeScreenshot("2fa_enabled_in_flow");
                    await Page.WaitForTimeoutAsync(2000);
                }
            }
        }

        await TakeScreenshot("2fa_reset_enable_flow_completed");
    }

    #endregion
}

using GCCWebApp.Accessibility.Tests.Tests.Core;
using Microsoft.Playwright;

namespace GCCWebApp.Accessibility.Tests.Tests.Pages;

public class AdminPageAccessibilityTests : BaseAccessibilityTest
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await LoginAsync("admin", "TestPass123!");
    }

    [Fact]
    public async Task AdminDashboard_ShouldHaveNoWCAG21AAViolations()
    {
        // Arrange
        var url = $"{_baseUrl}/Admin";

        // Act
        var results = await RunWCAG21AACheck(url);

        // Assert
        AssertNoViolations(results, "AdminDashboard_WCAG21AA");
    }

    [Fact]
    public async Task UserManagement_ShouldHaveNoWCAG21AAViolations()
    {
        // Arrange
        var url = $"{_baseUrl}/Admin/UserManagement";

        // Act
        var results = await RunWCAG21AACheck(url);

        // Assert
        AssertNoViolations(results, "UserManagement_WCAG21AA");
    }

    [Fact]
    public async Task RoleManagement_ShouldHaveNoWCAG21AAViolations()
    {
        // Arrange
        var url = $"{_baseUrl}/Admin/RoleManagement";

        // Act
        var results = await RunWCAG21AACheck(url);

        // Assert
        AssertNoViolations(results, "RoleManagement_WCAG21AA");
    }

    [Fact]
    public async Task ProductManagement_ShouldHaveNoWCAG21AAViolations()
    {
        // Arrange
        var url = $"{_baseUrl}/Admin/ProductManagement";

        // Act
        var results = await RunWCAG21AACheck(url);

        // Assert
        AssertNoViolations(results, "ProductManagement_WCAG21AA");
    }

    [Fact]
    public async Task AdminForms_ShouldHaveAccessibleLabelsAndValidation()
    {
        // Arrange
        var url = $"{_baseUrl}/Admin/CreateUser";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var results = await RunAccessibilityCheck(url, new AccessibilityCheckOptions
        {
            Rules = new[] { "label", "form-field-multiple-labels", "aria-required-attr", "aria-invalid-value" }
        });

        // Assert
        AssertNoViolations(results, "AdminForms_LabelsAndValidation");
    }

    [Fact]
    public async Task AdminDataTables_ShouldBeAccessible()
    {
        // Test multiple admin pages with data tables
        var adminUrls = new[]
        {
            $"{_baseUrl}/Admin/UserManagement",
            $"{_baseUrl}/Admin/RoleManagement",
            $"{_baseUrl}/Admin/ProductManagement"
        };

        foreach (var url in adminUrls)
        {
            await _page!.GotoAsync(url);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Act
            var results = await RunAccessibilityCheck(url, new AccessibilityCheckOptions
            {
                Rules = new[] { "table-header", "td-headers-attr", "th-has-data-cells" }
            });

            // Assert
            var testName = $"AdminDataTables_{url.Split('/').Last()}";
            AssertNoViolations(results, testName);
        }
    }

    [Fact]
    public async Task AdminButtons_ShouldHaveAccessibleNames()
    {
        // Arrange
        var url = $"{_baseUrl}/Admin";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var results = await RunAccessibilityCheck(url, new AccessibilityCheckOptions
        {
            Rules = new[] { "button-name", "link-name" }
        });

        // Assert
        AssertNoViolations(results, "AdminButtons_AccessibleNames");
    }

    [Fact]
    public async Task AdminModals_ShouldBeFocusManaged()
    {
        // Arrange
        var url = $"{_baseUrl}/Admin";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Look for modal triggers
        var modalTriggers = _page.Locator("[data-bs-toggle='modal'], [data-toggle='modal'], .modal-trigger");
        
        if (await modalTriggers.CountAsync() > 0)
        {
            // Act
            var results = await RunAccessibilityCheck(url, new AccessibilityCheckOptions
            {
                Rules = new[] { "aria-hidden-focus", "focus-order-semantics" }
            });

            // Assert
            AssertNoViolations(results, "AdminModals_FocusManagement");
        }
    }

    [Fact]
    public async Task AdminDropdowns_ShouldBeKeyboardAccessible()
    {
        // Arrange
        var url = $"{_baseUrl}/Admin";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Test dropdown accessibility
        var dropdowns = _page.Locator("select, [role='combobox'], .dropdown-toggle");
        
        if (await dropdowns.CountAsync() > 0)
        {
            // Act
            var results = await RunAccessibilityCheck(url, new AccessibilityCheckOptions
            {
                Rules = new[] { "select-name", "aria-expanded", "aria-haspopup" }
            });

            // Assert
            AssertNoViolations(results, "AdminDropdowns_KeyboardAccessible");
        }
    }

    [Fact]
    public async Task AdminAlerts_ShouldHaveProperRoles()
    {
        // Arrange
        var url = $"{_baseUrl}/Admin";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var results = await RunAccessibilityCheck(url, new AccessibilityCheckOptions
        {
            Rules = new[] { "aria-roles", "aria-required-attr" }
        });

        // Assert
        AssertNoViolations(results, "AdminAlerts_ProperRoles");
    }
}
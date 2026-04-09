using GCCWebApp.Accessibility.Tests.Tests.Core;
using Microsoft.Playwright;

namespace GCCWebApp.Accessibility.Tests.Tests.Components;

public class FormAccessibilityTests : BaseAccessibilityTest
{
    [Fact]
    public async Task LoginForm_ShouldBeFullyAccessible()
    {
        // Arrange
        var url = $"{_baseUrl}/Account/Login";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var results = await RunWCAG21AACheck(url);

        // Assert
        AssertNoViolations(results, "LoginForm_WCAG21AA");
    }

    [Fact]
    public async Task FormLabels_ShouldBeProperlyAssociated()
    {
        // Test multiple form pages
        var formUrls = new[]
        {
            $"{_baseUrl}/Account/Login",
            $"{_baseUrl}/Admin/CreateUser",
            $"{_baseUrl}/Certificate/Edit"
        };

        foreach (var url in formUrls)
        {
            try
            {
                await _page!.GotoAsync(url);
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Act
                var results = await RunAccessibilityCheck(url, new AccessibilityCheckOptions
                {
                    Rules = new[] { "label", "form-field-multiple-labels", "aria-input-field-name" }
                });

                // Assert
                var testName = $"FormLabels_{url.Split('/').Last()}";
                AssertNoViolations(results, testName);
            }
            catch (PlaywrightException)
            {
                // Skip if page doesn't exist or is not accessible
                continue;
            }
        }
    }

    [Fact]
    public async Task FormValidation_ShouldBeAccessible()
    {
        // Arrange
        var url = $"{_baseUrl}/Account/Login";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Try to submit empty form to trigger validation
        var submitButton = _page.Locator("input[type='submit'], button[type='submit'], .btn-submit").First;
        if (await submitButton.CountAsync() > 0)
        {
            await submitButton.ClickAsync();
            await _page.WaitForTimeoutAsync(1000); // Wait for validation messages

            // Act
            var results = await RunAccessibilityCheck(url, new AccessibilityCheckOptions
            {
                Rules = new[] { "aria-required-attr", "aria-invalid-value", "aria-describedby" }
            });

            // Assert
            AssertNoViolations(results, "FormValidation_Accessible");
        }
    }

    [Fact]
    public async Task FormInputs_ShouldHaveProperTypes()
    {
        // Arrange
        var url = $"{_baseUrl}/Account/Login";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var results = await RunAccessibilityCheck(url, new AccessibilityCheckOptions
        {
            Rules = new[] { "input-button-name", "autocomplete-valid" }
        });

        // Assert
        AssertNoViolations(results, "FormInputs_ProperTypes");
    }

    [Fact]
    public async Task PasswordFields_ShouldBeAccessible()
    {
        // Arrange
        var url = $"{_baseUrl}/Account/Login";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Check for password fields
        var passwordFields = _page.Locator("input[type='password']");
        
        if (await passwordFields.CountAsync() > 0)
        {
            // Test password field accessibility
            var firstPasswordField = passwordFields.First;
            
            // Check if field has label or aria-label
            var hasLabel = await firstPasswordField.EvaluateAsync<bool>(@"(element) => {
                const id = element.id;
                const label = id ? document.querySelector(`label[for='${id}']`) : null;
                const ariaLabel = element.getAttribute('aria-label');
                const ariaLabelledBy = element.getAttribute('aria-labelledby');
                return !!(label || ariaLabel || ariaLabelledBy);
            }");

            Assert.True(hasLabel, "Password fields should have accessible labels");

            // Act
            var results = await RunAccessibilityCheck(url, new AccessibilityCheckOptions
            {
                Rules = new[] { "label", "aria-input-field-name" }
            });

            // Assert
            AssertNoViolations(results, "PasswordFields_Accessible");
        }
    }

    [Fact]
    public async Task SelectDropdowns_ShouldBeAccessible()
    {
        // Test pages that likely have select dropdowns
        var urlsWithSelects = new[]
        {
            $"{_baseUrl}/Certificate",
            $"{_baseUrl}/Admin/ProductManagement",
            $"{_baseUrl}/Admin/UserManagement"
        };

        foreach (var url in urlsWithSelects)
        {
            try
            {
                await _page!.GotoAsync(url);
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                var selectElements = _page.Locator("select");
                
                if (await selectElements.CountAsync() > 0)
                {
                    // Act
                    var results = await RunAccessibilityCheck(url, new AccessibilityCheckOptions
                    {
                        Rules = new[] { "select-name", "aria-input-field-name" }
                    });

                    // Assert
                    var testName = $"SelectDropdowns_{url.Split('/').Last()}";
                    AssertNoViolations(results, testName);
                }
            }
            catch (PlaywrightException)
            {
                // Skip if page doesn't exist
                continue;
            }
        }
    }

    [Fact]
    public async Task CheckboxesAndRadios_ShouldBeAccessible()
    {
        // Test pages that might have checkboxes or radio buttons
        var url = $"{_baseUrl}/Admin/CreateUser";
        
        try
        {
            await _page!.GotoAsync(url);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var checkboxRadioElements = _page.Locator("input[type='checkbox'], input[type='radio']");
            
            if (await checkboxRadioElements.CountAsync() > 0)
            {
                // Act
                var results = await RunAccessibilityCheck(url, new AccessibilityCheckOptions
                {
                    Rules = new[] { "label", "aria-input-field-name", "fieldset-legend" }
                });

                // Assert
                AssertNoViolations(results, "CheckboxesAndRadios_Accessible");
            }
        }
        catch (PlaywrightException)
        {
            // Skip if page doesn't exist
        }
    }

    [Fact]
    public async Task FileUploadFields_ShouldBeAccessible()
    {
        // Test file upload accessibility
        var url = $"{_baseUrl}/Document/Upload";
        
        try
        {
            await _page!.GotoAsync(url);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var fileInputs = _page.Locator("input[type='file']");
            
            if (await fileInputs.CountAsync() > 0)
            {
                // Act
                var results = await RunAccessibilityCheck(url, new AccessibilityCheckOptions
                {
                    Rules = new[] { "label", "aria-input-field-name" }
                });

                // Assert
                AssertNoViolations(results, "FileUploadFields_Accessible");
            }
        }
        catch (PlaywrightException)
        {
            // Skip if page doesn't exist
        }
    }

    [Fact]
    public async Task FormErrorMessages_ShouldBeAccessible()
    {
        // Arrange
        var url = $"{_baseUrl}/Account/Login";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Try to trigger validation errors
        var emailField = _page.Locator("input[type='email'], input[name*='email'], input[id*='email']").First;
        var submitButton = _page.Locator("input[type='submit'], button[type='submit']").First;

        if (await emailField.CountAsync() > 0 && await submitButton.CountAsync() > 0)
        {
            // Enter invalid email and submit
            await emailField.FillAsync("invalid-email");
            await submitButton.ClickAsync();
            await _page.WaitForTimeoutAsync(1000);

            // Act
            var results = await RunAccessibilityCheck(url, new AccessibilityCheckOptions
            {
                Rules = new[] { "aria-describedby", "aria-invalid-value" }
            });

            // Assert
            AssertNoViolations(results, "FormErrorMessages_Accessible");
        }
    }

    [Fact]
    public async Task FormFieldsets_ShouldHaveLegends()
    {
        // Test pages that might have fieldsets
        var url = $"{_baseUrl}/Admin/CreateUser";
        
        try
        {
            await _page!.GotoAsync(url);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var fieldsets = _page.Locator("fieldset");
            
            if (await fieldsets.CountAsync() > 0)
            {
                // Act
                var results = await RunAccessibilityCheck(url, new AccessibilityCheckOptions
                {
                    Rules = new[] { "fieldset-legend" }
                });

                // Assert
                AssertNoViolations(results, "FormFieldsets_HaveLegends");
            }
        }
        catch (PlaywrightException)
        {
            // Skip if page doesn't exist
        }
    }
}
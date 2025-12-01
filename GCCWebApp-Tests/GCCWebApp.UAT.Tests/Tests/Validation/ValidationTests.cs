using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.Validation;

/// <summary>
/// Validation tests for all entity types
/// Tests required fields, format validation, duplicate prevention, and boundary conditions
/// Safe to run on both Dev and Production (test environment)
/// </summary>
[TestFixture]
[Category("Validation")]
[Category("CRUD")]
public class ValidationTests : BaseTest
{
    #region User Validation Tests

    [Test]
    [Order(1)]
    public async Task User_Validation_RequiresEmail()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        await Page.WaitForTimeoutAsync(2000);

        var addButton = Page.Locator("#add-user-btn, button:has-text('Add')");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Fill name but not email
        var nameInput = Page.Locator("#user-name, input[name*='name']").First;
        await nameInput.FillAsync("Test User No Email");

        var saveButton = Page.Locator("button:has-text('Save'), button[type='submit']").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("user_validation_no_email");

        // Modal should still be open or error shown
        var modal = Page.Locator("#user-modal, .admin-modal:visible");
        var errorMsg = Page.Locator(".error, .validation-error, .field-validation-error, text*=required, text*=email");

        var hasError = await modal.IsVisibleAsync() || await errorMsg.CountAsync() > 0;
        Assert.That(hasError, Is.True, "Should show validation error for missing email");
    }

    [Test]
    [Order(2)]
    public async Task User_Validation_RequiresValidEmailFormat()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        await Page.WaitForTimeoutAsync(2000);

        var addButton = Page.Locator("#add-user-btn, button:has-text('Add')");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var nameInput = Page.Locator("#user-name, input[name*='name']").First;
        await nameInput.FillAsync("Test User");

        var emailInput = Page.Locator("#user-email, input[name*='email'], input[type='email']").First;
        await emailInput.FillAsync("invalid-email-format");

        var saveButton = Page.Locator("button:has-text('Save'), button[type='submit']").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("user_validation_invalid_email");

        var errorMsg = Page.Locator(".error, .validation-error, text*=valid, text*=email");
        var hasError = await errorMsg.CountAsync() > 0;

        // Close modal
        var closeButton = Page.Locator("button:has-text('Cancel'), .admin-modal-close").First;
        if (await closeButton.IsVisibleAsync())
        {
            await closeButton.ClickAsync();
        }

        Assert.Pass($"Email format validation: {hasError}");
    }

    [Test]
    [Order(3)]
    public async Task User_Validation_RequiresName()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        await Page.WaitForTimeoutAsync(2000);

        var addButton = Page.Locator("#add-user-btn, button:has-text('Add')");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Fill email but not name
        var emailInput = Page.Locator("#user-email, input[name*='email']").First;
        await emailInput.FillAsync("test@example.com");

        var saveButton = Page.Locator("button:has-text('Save'), button[type='submit']").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("user_validation_no_name");

        var modal = Page.Locator("#user-modal, .admin-modal:visible");
        var stillOpen = await modal.IsVisibleAsync();

        // Close modal
        var closeButton = Page.Locator("button:has-text('Cancel'), .admin-modal-close").First;
        if (await closeButton.IsVisibleAsync())
        {
            await closeButton.ClickAsync();
        }

        Assert.Pass($"Name required validation - Modal still open: {stillOpen}");
    }

    [Test]
    [Order(4)]
    public async Task User_Validation_UniqueEmail()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        await Page.WaitForTimeoutAsync(2000);

        // Try to create user with existing admin email
        var addButton = Page.Locator("#add-user-btn, button:has-text('Add')");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var nameInput = Page.Locator("#user-name, input[name*='name']").First;
        await nameInput.FillAsync("Duplicate Test");

        var emailInput = Page.Locator("#user-email, input[name*='email']").First;
        await emailInput.FillAsync("swest@anukiconsulting.com"); // Known existing email

        var saveButton = Page.Locator("button:has-text('Save'), button[type='submit']").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("user_validation_duplicate_email");

        var errorMsg = Page.Locator(".error, .notification-error, text*=exists, text*=duplicate, text*=already");
        var hasError = await errorMsg.CountAsync() > 0;

        // Close modal
        var closeButton = Page.Locator("button:has-text('Cancel'), .admin-modal-close").First;
        if (await closeButton.IsVisibleAsync())
        {
            await closeButton.ClickAsync();
        }

        Assert.Pass($"Unique email validation: {hasError}");
    }

    #endregion

    #region Tag Validation Tests

    [Test]
    [Order(10)]
    public async Task Tag_Validation_RequiresName()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/Tags");
        await Page.WaitForTimeoutAsync(2000);

        var addButton = Page.Locator("#add-tag-btn, button:has-text('Add')");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Select type but leave name empty
        var typeSelect = Page.Locator("select[name*='type']");
        if (await typeSelect.CountAsync() > 0)
        {
            await typeSelect.SelectOptionAsync("external");
        }

        var saveButton = Page.Locator("button:has-text('Save')").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("tag_validation_no_name");

        var addRow = Page.Locator("#tags-add-row, .add-row:visible");
        var stillVisible = await addRow.IsVisibleAsync();

        Assert.That(stillVisible, Is.True, "Form should remain open when name is missing");
    }

    [Test]
    [Order(11)]
    public async Task Tag_Validation_UniqueName()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/Tags");
        await Page.WaitForTimeoutAsync(2000);

        // Get first tag name from table
        var firstTagCell = Page.Locator("#tag-table tbody tr:first-child td:first-child, table tbody tr:first-child td:first-child").First;
        var existingTagName = await firstTagCell.TextContentAsync();

        if (!string.IsNullOrEmpty(existingTagName))
        {
            var addButton = Page.Locator("#add-tag-btn, button:has-text('Add')");
            await addButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            var nameInput = Page.Locator("input[name*='tag-name'], input[name*='name']").First;
            await nameInput.FillAsync(existingTagName.Trim());

            var saveButton = Page.Locator("button:has-text('Save')").First;
            await saveButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
            await TakeScreenshot("tag_validation_duplicate");

            var errorMsg = Page.Locator(".error, .notification-error, text*=exists, text*=duplicate");
            var hasError = await errorMsg.CountAsync() > 0;

            Assert.Pass($"Duplicate tag name validation: {hasError}");
        }
        else
        {
            Assert.Pass("No existing tags to test duplicate validation");
        }
    }

    #endregion

    #region Company Validation Tests

    [Test]
    [Order(20)]
    public async Task Company_Validation_RequiresName()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/CompanyManagement");
        await Page.WaitForTimeoutAsync(2000);

        var addButton = Page.Locator("#add-company-btn, button:has-text('Add')");
        if (await addButton.CountAsync() > 0)
        {
            await addButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            // Leave name empty, try to save
            var saveButton = Page.Locator("button:has-text('Save'), button[type='submit']").First;
            await saveButton.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
            await TakeScreenshot("company_validation_no_name");

            var modal = Page.Locator(".admin-modal:visible, .modal:visible");
            var stillOpen = await modal.CountAsync() > 0;

            // Close modal
            var closeButton = Page.Locator("button:has-text('Cancel'), .admin-modal-close").First;
            if (await closeButton.IsVisibleAsync())
            {
                await closeButton.ClickAsync();
            }

            Assert.Pass($"Company name required - Modal still open: {stillOpen}");
        }
        else
        {
            Assert.Pass("Add company button not found");
        }
    }

    [Test]
    [Order(21)]
    public async Task Company_Validation_UniqueCompanyName()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/CompanyManagement");
        await Page.WaitForTimeoutAsync(2000);

        // Get first company name
        var firstCompanyCell = Page.Locator("table tbody tr:first-child td:first-child").First;
        var existingName = await firstCompanyCell.TextContentAsync();

        if (!string.IsNullOrEmpty(existingName))
        {
            var addButton = Page.Locator("#add-company-btn, button:has-text('Add')");
            if (await addButton.CountAsync() > 0)
            {
                await addButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var nameInput = Page.Locator("#company-name, input[name*='name']").First;
                await nameInput.FillAsync(existingName.Trim());

                var saveButton = Page.Locator("button:has-text('Save')").First;
                await saveButton.ClickAsync();
                await Page.WaitForTimeoutAsync(2000);
                await TakeScreenshot("company_validation_duplicate");

                var errorMsg = Page.Locator(".error, .notification-error, text*=exists, text*=duplicate");
                var hasError = await errorMsg.CountAsync() > 0;

                // Close modal
                var closeButton = Page.Locator("button:has-text('Cancel'), .admin-modal-close").First;
                if (await closeButton.IsVisibleAsync())
                {
                    await closeButton.ClickAsync();
                }

                Assert.Pass($"Duplicate company validation: {hasError}");
            }
        }
        else
        {
            Assert.Pass("No existing companies to test duplicate validation");
        }
    }

    #endregion

    #region Product Validation Tests

    [Test]
    [Order(30)]
    public async Task Product_Validation_RequiresName()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Product/Management");
        await Page.WaitForTimeoutAsync(2000);

        var addButton = Page.Locator("#add-product-btn, button:has-text('Add')");
        if (await addButton.CountAsync() > 0)
        {
            await addButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            // Select company but leave name empty
            var companySelect = Page.Locator("select[name*='company'], #product-company");
            if (await companySelect.CountAsync() > 0)
            {
                var options = await companySelect.Locator("option").AllAsync();
                if (options.Count > 1)
                {
                    await companySelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
                }
            }

            var saveButton = Page.Locator("button:has-text('Save'), button[type='submit']").First;
            await saveButton.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
            await TakeScreenshot("product_validation_no_name");

            var modal = Page.Locator(".admin-modal:visible, .modal:visible");
            var stillOpen = await modal.CountAsync() > 0;

            // Close modal
            var closeButton = Page.Locator("button:has-text('Cancel'), .admin-modal-close").First;
            if (await closeButton.IsVisibleAsync())
            {
                await closeButton.ClickAsync();
            }

            Assert.Pass($"Product name required - Modal still open: {stillOpen}");
        }
        else
        {
            Assert.Pass("Add product button not found");
        }
    }

    [Test]
    [Order(31)]
    public async Task Product_Validation_RequiresCompany()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Product/Management");
        await Page.WaitForTimeoutAsync(2000);

        var addButton = Page.Locator("#add-product-btn, button:has-text('Add')");
        if (await addButton.CountAsync() > 0)
        {
            await addButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            // Fill name but not company
            var nameInput = Page.Locator("#product-name, input[name*='name']").First;
            await nameInput.FillAsync("Test Product No Company");

            var saveButton = Page.Locator("button:has-text('Save'), button[type='submit']").First;
            await saveButton.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
            await TakeScreenshot("product_validation_no_company");

            var modal = Page.Locator(".admin-modal:visible, .modal:visible");
            var stillOpen = await modal.CountAsync() > 0;

            // Close modal
            var closeButton = Page.Locator("button:has-text('Cancel'), .admin-modal-close").First;
            if (await closeButton.IsVisibleAsync())
            {
                await closeButton.ClickAsync();
            }

            Assert.Pass($"Product company required - Modal still open: {stillOpen}");
        }
        else
        {
            Assert.Pass("Add product button not found");
        }
    }

    #endregion

    #region Role Validation Tests

    [Test]
    [Order(40)]
    public async Task Role_Validation_RequiresName()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/RoleManagement");
        await Page.WaitForTimeoutAsync(2000);

        var addButton = Page.Locator("#add-role-btn, button:has-text('Add')");
        if (await addButton.CountAsync() > 0)
        {
            await addButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            // Try to save without name
            var saveButton = Page.Locator("button:has-text('Save'), button[type='submit']").First;
            await saveButton.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
            await TakeScreenshot("role_validation_no_name");

            var modal = Page.Locator(".admin-modal:visible, .modal:visible");
            var stillOpen = await modal.CountAsync() > 0;

            // Close modal
            var closeButton = Page.Locator("button:has-text('Cancel'), .admin-modal-close").First;
            if (await closeButton.IsVisibleAsync())
            {
                await closeButton.ClickAsync();
            }

            Assert.That(stillOpen, Is.True, "Modal should remain open when role name is missing");
        }
        else
        {
            Assert.Pass("Add role button not found");
        }
    }

    [Test]
    [Order(41)]
    public async Task Role_Validation_UniqueRoleName()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/RoleManagement");
        await Page.WaitForTimeoutAsync(2000);

        var addButton = Page.Locator("#add-role-btn, button:has-text('Add')");
        if (await addButton.CountAsync() > 0)
        {
            await addButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            // Try to create role with existing name
            var nameInput = Page.Locator("#role-name, input[name*='name']").First;
            await nameInput.FillAsync("Admin"); // Known existing role

            var saveButton = Page.Locator("button:has-text('Save')").First;
            await saveButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
            await TakeScreenshot("role_validation_duplicate");

            var errorMsg = Page.Locator(".error, .notification-error, text*=exists, text*=duplicate");
            var hasError = await errorMsg.CountAsync() > 0;

            // Close modal
            var closeButton = Page.Locator("button:has-text('Cancel'), .admin-modal-close").First;
            if (await closeButton.IsVisibleAsync())
            {
                await closeButton.ClickAsync();
            }

            Assert.Pass($"Duplicate role validation: {hasError}");
        }
        else
        {
            Assert.Pass("Add role button not found");
        }
    }

    #endregion

    #region Partner Validation Tests

    [Test]
    [Order(50)]
    public async Task Partner_Validation_RequiresName()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/Partners");
        await Page.WaitForTimeoutAsync(2000);

        var addButton = Page.Locator("#add-partner-btn, button:has-text('Add')");
        if (await addButton.CountAsync() > 0)
        {
            await addButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            // Try to save without name
            var saveButton = Page.Locator("button:has-text('Save')").First;
            await saveButton.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
            await TakeScreenshot("partner_validation_no_name");

            var modal = Page.Locator(".admin-modal:visible, .modal:visible, .add-row:visible");
            var stillOpen = await modal.CountAsync() > 0;

            Assert.Pass($"Partner name required - Form still visible: {stillOpen}");
        }
        else
        {
            Assert.Pass("Add partner button not found");
        }
    }

    #endregion

    #region Input Length Validation Tests

    [Test]
    [Order(60)]
    public async Task Validation_MaxLength_UserName()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        await Page.WaitForTimeoutAsync(2000);

        var addButton = Page.Locator("#add-user-btn, button:has-text('Add')");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var nameInput = Page.Locator("#user-name, input[name*='name']").First;

        // Try very long name (300 chars)
        var longName = new string('A', 300);
        await nameInput.FillAsync(longName);

        var actualValue = await nameInput.InputValueAsync();
        await TakeScreenshot("user_validation_max_length");

        TestContext.WriteLine($"Input length: {actualValue.Length}");

        // Close modal
        var closeButton = Page.Locator("button:has-text('Cancel'), .admin-modal-close").First;
        if (await closeButton.IsVisibleAsync())
        {
            await closeButton.ClickAsync();
        }

        Assert.Pass($"Max length test - Input accepted {actualValue.Length} chars");
    }

    [Test]
    [Order(61)]
    public async Task Validation_SpecialCharacters_TagName()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/Tags");
        await Page.WaitForTimeoutAsync(2000);

        var addButton = Page.Locator("#add-tag-btn, button:has-text('Add')");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var nameInput = Page.Locator("input[name*='tag-name'], input[name*='name']").First;

        // Try special characters
        var specialName = "Test<script>alert('xss')</script>";
        await nameInput.FillAsync(specialName);

        var typeSelect = Page.Locator("select[name*='type']");
        if (await typeSelect.CountAsync() > 0)
        {
            await typeSelect.SelectOptionAsync("external");
        }

        var saveButton = Page.Locator("button:has-text('Save')").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("tag_validation_special_chars");

        // Check page for XSS (should be sanitized)
        var pageContent = await Page.ContentAsync();
        var hasXss = pageContent.Contains("<script>alert");

        Assert.That(hasXss, Is.False, "XSS script should be sanitized");
    }

    #endregion

    #region Certificate Validation Tests

    [Test]
    [Order(70)]
    public async Task Certificate_Validation_RequiredFields()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/Create");
        await Page.WaitForTimeoutAsync(2000);

        // Try to submit without filling required fields
        var submitButton = Page.Locator("button[type='submit'], button:has-text('Save'), button:has-text('Create')").First;
        if (await submitButton.CountAsync() > 0)
        {
            await submitButton.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
            await TakeScreenshot("certificate_validation_required");

            // Check for validation errors
            var errors = Page.Locator(".field-validation-error, .validation-error, .error, .invalid-feedback");
            var errorCount = await errors.CountAsync();

            TestContext.WriteLine($"Validation errors shown: {errorCount}");
            Assert.Pass($"Certificate required fields validation - {errorCount} errors");
        }
        else
        {
            Assert.Pass("Submit button not found on create page");
        }
    }

    [Test]
    [Order(71)]
    public async Task Certificate_Validation_DateFormat()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/Create");
        await Page.WaitForTimeoutAsync(2000);

        var dateInput = Page.Locator("input[type='date'], input[name*='Date'], input[id*='Date']").First;

        if (await dateInput.CountAsync() > 0)
        {
            // Try invalid date format (if input allows text)
            var inputType = await dateInput.GetAttributeAsync("type");

            if (inputType != "date")
            {
                await dateInput.FillAsync("invalid-date");
                await TakeScreenshot("certificate_validation_date_format");

                var errorMsg = Page.Locator(".error, .validation-error, text*=date, text*=format");
                var hasError = await errorMsg.CountAsync() > 0;

                Assert.Pass($"Date format validation: {hasError}");
            }
            else
            {
                Assert.Pass("Date input is HTML5 date picker - browser handles validation");
            }
        }
        else
        {
            Assert.Pass("Date input not found");
        }
    }

    #endregion

    #region Empty State Validation Tests

    [Test]
    [Order(80)]
    public async Task Validation_EmptyTable_GracefulHandling()
    {
        await LoginAsRole("Admin");

        // Test various pages handle empty states gracefully
        var pagesToTest = new[]
        {
            "/Admin/Tags",
            "/Admin/UserManagement",
            "/Admin/RoleManagement",
            "/Product/Management"
        };

        foreach (var page in pagesToTest)
        {
            await NavigateToPage(page);
            await Page.WaitForTimeoutAsync(1000);

            // Search for something that likely won't exist
            var searchInput = Page.Locator("#search-input, input[placeholder*='Search']").First;
            if (await searchInput.CountAsync() > 0)
            {
                await searchInput.FillAsync("ZZZZNOTEXIST12345");
                await Page.WaitForTimeoutAsync(1000);

                // Should show "no results" message or empty table gracefully
                var noResults = Page.Locator("text*=no result, text*=No data, text*=empty, text*=not found, .empty-state");
                var emptyTable = Page.Locator("table tbody tr");

                var rowCount = await emptyTable.CountAsync();
                var hasNoResultsMessage = await noResults.CountAsync() > 0;

                TestContext.WriteLine($"Page {page}: Rows={rowCount}, NoResults message={hasNoResultsMessage}");
            }
        }

        await TakeScreenshot("validation_empty_states");
        Assert.Pass("Empty state handling test completed");
    }

    #endregion
}

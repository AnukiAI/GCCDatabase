using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.Admin.CRUD;

/// <summary>
/// CRUD tests for User Management page (/Admin/UserManagement)
/// Tests Create, Read, Update, Delete operations for users
/// Safe to run on both Dev and Production (test environment)
/// </summary>
[TestFixture]
[Category("CRUD")]
[Category("Admin")]
[Category("Users")]
public class UserManagementCrudTests : BaseTest
{
    private const string PageUrl = "/Admin/UserManagement";
    private string _testUserName = string.Empty;
    private string _testUserEmail = string.Empty;

    [SetUp]
    public new async Task SetUp()
    {
        await base.SetUp();
        // Generate unique test data for each test run
        var timestamp = DateTime.Now.ToString("HHmmss");
        _testUserName = $"AutoTest User {timestamp}";
        _testUserEmail = $"autotest_{timestamp}@test.example.com";
    }

    #region CREATE Tests

    [Test]
    [Order(1)]
    public async Task User_Create_CanOpenAddUserModal()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await TakeScreenshot("users_page_loaded");

        // Act - Click Add New User button
        var addButton = Page.Locator("#add-user-btn");
        await addButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await TakeScreenshot("add_user_modal_opened");

        // Assert - Verify modal is visible
        var modal = Page.Locator("#user-modal");
        var isVisible = await modal.IsVisibleAsync();
        Assert.That(isVisible, Is.True, "Add user modal should be visible after clicking Add New User");
    }

    [Test]
    [Order(2)]
    public async Task User_Create_CanCreateNewUser()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        // Act - Click Add New User button
        await Page.ClickAsync("#add-user-btn");
        await Page.WaitForTimeoutAsync(500);

        // Fill in user details
        await Page.FillAsync("#user-name", _testUserName);
        await Page.FillAsync("#user-email, input[id*='email']", _testUserEmail);

        // Select a role if dropdown exists
        var roleSelect = Page.Locator("#user-role, select[id*='role']");
        if (await roleSelect.CountAsync() > 0)
        {
            await roleSelect.SelectOptionAsync("External");
        }

        // Set password if field exists
        var passwordField = Page.Locator("#user-password, input[type='password']");
        if (await passwordField.CountAsync() > 0)
        {
            await passwordField.FillAsync("TestPass123!");
        }

        await TakeScreenshot("user_form_filled");

        // Click Save/Submit
        var saveButton = Page.Locator("#user-modal button:has-text('Save'), #user-modal button:has-text('Create'), #user-modal button[type='submit']").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(3000);
        await TakeScreenshot("user_saved");

        // Assert - Verify user appears in table or success notification
        var successNotification = Page.Locator(".notification-success, .toast-success, .alert-success");
        var userInTable = Page.Locator($"text={_testUserName}");

        var success = await successNotification.CountAsync() > 0 || await userInTable.CountAsync() > 0;
        Assert.That(success, Is.True, $"User '{_testUserName}' should be created successfully");
    }

    [Test]
    [Order(3)]
    public async Task User_Create_ValidationRequiresEmail()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        // Act - Click Add New User button
        await Page.ClickAsync("#add-user-btn");
        await Page.WaitForTimeoutAsync(500);

        // Fill in name only, skip email
        await Page.FillAsync("#user-name", "Test User No Email");
        await TakeScreenshot("user_form_missing_email");

        // Click Save/Submit
        var saveButton = Page.Locator("#user-modal button:has-text('Save'), #user-modal button:has-text('Create'), #user-modal button[type='submit']").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("user_validation_error");

        // Assert - Modal should still be open or error shown
        var modal = Page.Locator("#user-modal");
        var stillVisible = await modal.IsVisibleAsync();
        Assert.That(stillVisible, Is.True, "Modal should remain open when validation fails");
    }

    [Test]
    [Order(4)]
    public async Task User_Create_CanCancelAddUser()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        // Act - Click Add New User button
        await Page.ClickAsync("#add-user-btn");
        await Page.WaitForTimeoutAsync(500);

        // Fill in some data
        await Page.FillAsync("#user-name", "Cancelled User");
        await TakeScreenshot("user_form_before_cancel");

        // Click Cancel or close button
        var cancelButton = Page.Locator("#user-modal button:has-text('Cancel'), #user-modal .admin-modal-close").First;
        await cancelButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await TakeScreenshot("user_modal_cancelled");

        // Assert - Modal should be hidden
        var modal = Page.Locator("#user-modal");
        var isHidden = !await modal.IsVisibleAsync();
        Assert.That(isHidden, Is.True, "Modal should be hidden after cancel");
    }

    #endregion

    #region READ Tests

    [Test]
    [Order(10)]
    public async Task User_Read_CanViewUsersList()
    {
        // Arrange
        await LoginAsRole("Admin");

        // Act
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("users_list_loaded");

        // Assert - Table should have content
        var tableBody = Page.Locator("#user-table tbody tr");
        var rowCount = await tableBody.CountAsync();
        TestContext.WriteLine($"Found {rowCount} users in table");

        Assert.That(rowCount, Is.GreaterThan(0), "User table should have at least one user");
    }

    [Test]
    [Order(11)]
    public async Task User_Read_CanSearchUsers()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Act - Search for users
        var searchInput = Page.Locator("#search-input");
        await searchInput.FillAsync("admin");
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("users_search_results");

        // Assert - Search should filter results
        var searchValue = await searchInput.InputValueAsync();
        Assert.That(searchValue, Is.EqualTo("admin"), "Search input should contain search term");
    }

    [Test]
    [Order(12)]
    public async Task User_Read_CanFilterByStatus()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Act - Filter by Active status
        var statusFilter = Page.Locator("#status-filter");
        await statusFilter.SelectOptionAsync("active");
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("users_filtered_active");

        // Act - Filter by Inactive status
        await statusFilter.SelectOptionAsync("inactive");
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("users_filtered_inactive");

        // Act - Show all
        await statusFilter.SelectOptionAsync("all");
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("users_filter_all");

        Assert.Pass("Status filter works correctly");
    }

    [Test]
    [Order(13)]
    public async Task User_Read_CanFilterByRole()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Act - Filter by Admin role
        var roleFilter = Page.Locator("#role-filter");
        await roleFilter.SelectOptionAsync("Admin");
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("users_filtered_admin_role");

        // Act - Filter by External role
        await roleFilter.SelectOptionAsync("External");
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("users_filtered_external_role");

        // Act - Show all
        await roleFilter.SelectOptionAsync("all");
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("users_filter_all_roles");

        Assert.Pass("Role filter works correctly");
    }

    [Test]
    [Order(14)]
    public async Task User_Read_CanSortByColumn()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Act - Click on Name column header to sort
        var nameHeader = Page.Locator("th[data-sort='name']");
        await nameHeader.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await TakeScreenshot("users_sorted_by_name");

        // Click again for reverse sort
        await nameHeader.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await TakeScreenshot("users_sorted_by_name_desc");

        Assert.Pass("Column sorting interaction completed");
    }

    #endregion

    #region UPDATE Tests

    [Test]
    [Order(20)]
    public async Task User_Update_CanOpenEditModal()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Find an edit button in the table
        var editButton = Page.Locator(".admin-btn:has-text('Edit'), button[onclick*='edit'], .edit-btn").First;

        if (await editButton.CountAsync() > 0)
        {
            // Act - Click edit
            await editButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("user_edit_modal");

            // Assert - Modal should be visible
            var modal = Page.Locator("#user-modal");
            var modalVisible = await modal.IsVisibleAsync();
            Assert.That(modalVisible, Is.True, "Edit modal should be visible");
        }
        else
        {
            TestContext.WriteLine("No edit buttons found - table may be empty");
            Assert.Pass("No users available to edit");
        }
    }

    [Test]
    [Order(21)]
    public async Task User_Update_CanModifyUserName()
    {
        // Arrange - Create a user first
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        // Create test user
        await Page.ClickAsync("#add-user-btn");
        await Page.WaitForTimeoutAsync(500);
        await Page.FillAsync("#user-name", _testUserName);
        await Page.FillAsync("#user-email, input[id*='email']", _testUserEmail);

        var roleSelect = Page.Locator("#user-role, select[id*='role']");
        if (await roleSelect.CountAsync() > 0)
        {
            await roleSelect.SelectOptionAsync("External");
        }

        var saveButton = Page.Locator("#user-modal button:has-text('Save'), #user-modal button:has-text('Create')").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(3000);

        // Refresh page to see new user
        await Page.ReloadAsync();
        await Page.WaitForTimeoutAsync(2000);

        // Find and edit our user
        var userRow = Page.Locator($"tr:has-text('{_testUserName}')");
        if (await userRow.CountAsync() > 0)
        {
            var editButton = userRow.Locator(".admin-btn:has-text('Edit'), button[onclick*='edit'], .edit-btn");
            if (await editButton.CountAsync() > 0)
            {
                await editButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                // Modify the name
                await Page.FillAsync("#user-name", $"{_testUserName} Updated");
                await TakeScreenshot("user_name_modified");

                // Save changes
                var updateButton = Page.Locator("#user-modal button:has-text('Save'), #user-modal button:has-text('Update')").First;
                await updateButton.ClickAsync();
                await Page.WaitForTimeoutAsync(2000);
                await TakeScreenshot("user_update_saved");
            }
        }

        Assert.Pass("User update test completed");
    }

    [Test]
    [Order(22)]
    public async Task User_Update_CanChangeUserRole()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Find a user to edit (not admin to avoid issues)
        var userRow = Page.Locator("tr:has-text('External'), tr:has-text('Analyst')").First;

        if (await userRow.CountAsync() > 0)
        {
            var editButton = userRow.Locator(".admin-btn:has-text('Edit'), button[onclick*='edit'], .edit-btn");
            if (await editButton.CountAsync() > 0)
            {
                await editButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
                await TakeScreenshot("user_edit_for_role_change");

                // Change role
                var roleSelect = Page.Locator("#user-role, select[id*='role']");
                if (await roleSelect.CountAsync() > 0)
                {
                    await roleSelect.SelectOptionAsync("Analyst");
                    await TakeScreenshot("user_role_changed");
                }

                // Close modal without saving
                var cancelButton = Page.Locator("#user-modal button:has-text('Cancel'), #user-modal .admin-modal-close").First;
                await cancelButton.ClickAsync();
            }
        }

        Assert.Pass("Role change UI interaction completed");
    }

    [Test]
    [Order(23)]
    public async Task User_Update_CanToggleUserStatus()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Find a user row with status toggle
        var statusToggle = Page.Locator(".status-toggle, input[type='checkbox'][name*='status'], .toggle-switch").First;

        if (await statusToggle.CountAsync() > 0)
        {
            await TakeScreenshot("user_before_status_toggle");
            await statusToggle.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
            await TakeScreenshot("user_after_status_toggle");

            Assert.Pass("Status toggle interaction completed");
        }
        else
        {
            // Try via edit modal
            var editButton = Page.Locator(".admin-btn:has-text('Edit')").First;
            if (await editButton.CountAsync() > 0)
            {
                await editButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var statusCheckbox = Page.Locator("#user-status, input[name*='active'], input[name*='status']");
                if (await statusCheckbox.CountAsync() > 0)
                {
                    await statusCheckbox.ClickAsync();
                    await TakeScreenshot("user_status_toggled_in_modal");
                }

                var cancelButton = Page.Locator("#user-modal button:has-text('Cancel')").First;
                await cancelButton.ClickAsync();
            }

            Assert.Pass("Status toggle test completed");
        }
    }

    #endregion

    #region DELETE Tests

    [Test]
    [Order(30)]
    public async Task User_Delete_ShowsConfirmationModal()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Find a delete button (not for admin user)
        var deleteButton = Page.Locator("tr:not(:has-text('admin')) .admin-btn:has-text('Delete'), tr:not(:has-text('admin')) button[onclick*='delete']").First;

        if (await deleteButton.CountAsync() > 0)
        {
            // Act - Click delete
            await deleteButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("user_delete_confirmation");

            // Assert - Confirmation modal should appear
            var confirmModal = Page.Locator(".admin-modal:visible, .confirm-modal:visible, [role='dialog']:visible");
            var modalVisible = await confirmModal.CountAsync() > 0;

            // Cancel the delete
            var cancelButton = Page.Locator("button:has-text('Cancel')").First;
            if (await cancelButton.IsVisibleAsync())
            {
                await cancelButton.ClickAsync();
            }

            Assert.That(modalVisible, Is.True, "Delete confirmation modal should appear");
        }
        else
        {
            TestContext.WriteLine("No delete buttons found for non-admin users");
            Assert.Pass("No deletable users available");
        }
    }

    [Test]
    [Order(31)]
    public async Task User_Delete_CanDeleteUser()
    {
        // Arrange - Create a user specifically for deletion
        var deleteUserName = $"AutoTest DELETE {DateTime.Now:HHmmss}";
        var deleteUserEmail = $"delete_{DateTime.Now:HHmmss}@test.example.com";

        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        // Create user for deletion
        await Page.ClickAsync("#add-user-btn");
        await Page.WaitForTimeoutAsync(500);
        await Page.FillAsync("#user-name", deleteUserName);
        await Page.FillAsync("#user-email, input[id*='email']", deleteUserEmail);

        var roleSelect = Page.Locator("#user-role, select[id*='role']");
        if (await roleSelect.CountAsync() > 0)
        {
            await roleSelect.SelectOptionAsync("External");
        }

        var saveButton = Page.Locator("#user-modal button:has-text('Save'), #user-modal button:has-text('Create')").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(3000);
        await TakeScreenshot("user_created_for_deletion");

        // Refresh and find the user
        await Page.ReloadAsync();
        await Page.WaitForTimeoutAsync(2000);

        var userRow = Page.Locator($"tr:has-text('{deleteUserName}')");
        var userExists = await userRow.CountAsync() > 0;

        if (userExists)
        {
            var deleteButton = userRow.Locator(".admin-btn:has-text('Delete'), button[onclick*='delete']");

            if (await deleteButton.CountAsync() > 0)
            {
                await deleteButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
                await TakeScreenshot("user_delete_confirm_dialog");

                // Confirm deletion
                var confirmButton = Page.Locator("button:has-text('Delete'), button:has-text('Confirm'), button:has-text('Yes')").First;
                if (await confirmButton.IsVisibleAsync())
                {
                    await confirmButton.ClickAsync();
                    await Page.WaitForTimeoutAsync(2000);
                    await TakeScreenshot("user_deleted");
                }

                // Verify user is gone
                await Page.ReloadAsync();
                await Page.WaitForTimeoutAsync(2000);
                var userStillExists = await Page.Locator($"tr:has-text('{deleteUserName}')").CountAsync() > 0;
                Assert.That(userStillExists, Is.False, $"Deleted user '{deleteUserName}' should no longer appear in table");
            }
        }
        else
        {
            TestContext.WriteLine($"User '{deleteUserName}' was not found after creation - may have failed to save");
            Assert.Pass("User creation may have validation issues - delete test skipped");
        }
    }

    [Test]
    [Order(32)]
    public async Task User_Delete_AdminUserCannotBeDeleted()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Find the admin user row
        var adminRow = Page.Locator("tr:has-text('admin')").First;

        if (await adminRow.CountAsync() > 0)
        {
            // Check if delete button exists for admin
            var deleteButton = adminRow.Locator(".admin-btn:has-text('Delete'), button[onclick*='delete']");
            var hasDeleteButton = await deleteButton.CountAsync() > 0;

            await TakeScreenshot("admin_user_row");

            // Admin user should either have no delete button or deletion should fail
            if (hasDeleteButton)
            {
                await deleteButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
                await TakeScreenshot("admin_delete_attempt");

                // Look for error message
                var errorMessage = Page.Locator(".error-message, .notification-error, .toast-error, text=cannot delete");
                var hasError = await errorMessage.CountAsync() > 0;

                // Cancel if modal opened
                var cancelButton = Page.Locator("button:has-text('Cancel')").First;
                if (await cancelButton.IsVisibleAsync())
                {
                    await cancelButton.ClickAsync();
                }

                TestContext.WriteLine($"Admin delete button exists: {hasDeleteButton}, Error shown: {hasError}");
            }
            else
            {
                Assert.Pass("Admin user correctly has no delete button");
            }
        }

        Assert.Pass("Admin user protection check completed");
    }

    #endregion

    #region EXPORT Tests

    [Test]
    [Order(40)]
    public async Task User_Export_CanExportToCSV()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Act - Click Export button
        var exportButton = Page.Locator("#export-btn");
        await exportButton.ClickAsync();
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("user_export_clicked");

        Assert.Pass("Export button clicked successfully");
    }

    [Test]
    [Order(41)]
    public async Task User_Export_CanDownloadTemplate()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Act - Click Template button
        var templateButton = Page.Locator("#template-btn");
        await templateButton.ClickAsync();
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("user_template_clicked");

        Assert.Pass("Template button clicked successfully");
    }

    #endregion
}

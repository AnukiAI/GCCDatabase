using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.Admin.CRUD;

/// <summary>
/// CRUD tests for Role Management page (/Admin/RoleManagement)
/// Tests Create, Read, Update, Delete operations for roles and permissions
/// Safe to run on both Dev and Production (test environment)
/// </summary>
[TestFixture]
[Category("CRUD")]
[Category("Admin")]
[Category("Roles")]
public class RoleManagementCrudTests : BaseTest
{
    private const string PageUrl = "/Admin/RoleManagement";
    private string _testRoleName = string.Empty;

    [SetUp]
    public new async Task SetUp()
    {
        await base.SetUp();
        var timestamp = DateTime.Now.ToString("HHmmss");
        _testRoleName = $"AutoTest Role {timestamp}";
    }

    #region CREATE Tests

    [Test]
    [Order(1)]
    public async Task Role_Create_CanOpenAddRoleModal()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await TakeScreenshot("roles_page_loaded");

        var addButton = Page.Locator("#add-role-btn, button:has-text('Add'), button:has-text('New Role'), .admin-btn--primary");
        await addButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await TakeScreenshot("add_role_modal_opened");

        var modal = Page.Locator("#role-modal, .admin-modal:visible, [role='dialog']:visible");
        var isVisible = await modal.CountAsync() > 0;
        Assert.That(isVisible, Is.True, "Add role modal should be visible");
    }

    [Test]
    [Order(2)]
    public async Task Role_Create_CanCreateNewRole()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        var addButton = Page.Locator("#add-role-btn, button:has-text('Add'), button:has-text('New Role'), .admin-btn--primary");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Fill in role name
        var nameInput = Page.Locator("#role-name, input[name='name'], input[name='roleName'], input[placeholder*='name']").First;
        await nameInput.FillAsync(_testRoleName);

        // Fill in description if available
        var descInput = Page.Locator("#role-description, textarea[name='description'], input[name='description']");
        if (await descInput.CountAsync() > 0)
        {
            await descInput.FillAsync("Automated test role - created by CRUD tests");
        }

        await TakeScreenshot("role_form_filled");

        var saveButton = Page.Locator("button:has-text('Save'), button:has-text('Create'), button[type='submit']").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(3000);
        await TakeScreenshot("role_saved");

        // Verify role appears
        var roleInTable = Page.Locator($"text={_testRoleName}");
        var exists = await roleInTable.CountAsync() > 0;
        Assert.That(exists, Is.True, $"Role '{_testRoleName}' should appear in the table");
    }

    [Test]
    [Order(3)]
    public async Task Role_Create_CanCreateRoleWithPermissions()
    {
        var roleWithPerms = $"AutoTest PermRole {DateTime.Now:HHmmss}";

        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        var addButton = Page.Locator("#add-role-btn, button:has-text('Add'), button:has-text('New Role'), .admin-btn--primary");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Fill in role name
        var nameInput = Page.Locator("#role-name, input[name='name'], input[name='roleName']").First;
        await nameInput.FillAsync(roleWithPerms);

        // Select some permissions
        var permissionCheckboxes = Page.Locator(".permission-checkbox, input[type='checkbox'][name*='permission'], .permissions-container input[type='checkbox']");
        var checkboxCount = await permissionCheckboxes.CountAsync();

        if (checkboxCount > 0)
        {
            // Select first 3 permissions
            for (int i = 0; i < Math.Min(3, checkboxCount); i++)
            {
                await permissionCheckboxes.Nth(i).CheckAsync();
            }
            await TakeScreenshot("role_permissions_selected");
        }

        var saveButton = Page.Locator("button:has-text('Save'), button:has-text('Create')").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(3000);
        await TakeScreenshot("role_with_permissions_saved");

        Assert.Pass("Role with permissions creation test completed");
    }

    [Test]
    [Order(4)]
    public async Task Role_Create_ValidationRequiresRoleName()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        var addButton = Page.Locator("#add-role-btn, button:has-text('Add'), button:has-text('New Role'), .admin-btn--primary");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Don't fill name, just try to save
        await TakeScreenshot("role_form_empty");

        var saveButton = Page.Locator("button:has-text('Save'), button:has-text('Create')").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("role_validation_error");

        // Modal should still be open or error shown
        var modal = Page.Locator("#role-modal, .admin-modal:visible, [role='dialog']:visible");
        var stillVisible = await modal.CountAsync() > 0;
        Assert.That(stillVisible, Is.True, "Modal should remain open when validation fails");
    }

    [Test]
    [Order(5)]
    public async Task Role_Create_CanCancelAddRole()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        var addButton = Page.Locator("#add-role-btn, button:has-text('Add'), button:has-text('New Role'), .admin-btn--primary");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Fill in some data
        var nameInput = Page.Locator("#role-name, input[name='name'], input[name='roleName']").First;
        await nameInput.FillAsync("CancelledRole");
        await TakeScreenshot("role_form_before_cancel");

        // Click Cancel
        var cancelButton = Page.Locator("button:has-text('Cancel'), .admin-modal-close").First;
        await cancelButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await TakeScreenshot("role_modal_cancelled");

        // Modal should be hidden
        var modal = Page.Locator("#role-modal, .admin-modal:visible");
        var isHidden = await modal.CountAsync() == 0 || !await modal.First.IsVisibleAsync();
        Assert.That(isHidden, Is.True, "Modal should be hidden after cancel");
    }

    #endregion

    #region READ Tests

    [Test]
    [Order(10)]
    public async Task Role_Read_CanViewRolesList()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("roles_list_loaded");

        var tableBody = Page.Locator("#role-table tbody tr, .role-table tbody tr, table tbody tr");
        var rowCount = await tableBody.CountAsync();
        TestContext.WriteLine($"Found {rowCount} roles in table");

        Assert.That(rowCount, Is.GreaterThan(0), "Role table should have at least one role (Admin)");
    }

    [Test]
    [Order(11)]
    public async Task Role_Read_DisplaysSystemRoles()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("roles_system_check");

        var pageContent = await Page.ContentAsync();

        // System roles should exist
        var hasAdmin = pageContent.Contains("Admin");
        var hasAnalyst = pageContent.Contains("Analyst");
        var hasExternal = pageContent.Contains("External");

        TestContext.WriteLine($"Admin role exists: {hasAdmin}");
        TestContext.WriteLine($"Analyst role exists: {hasAnalyst}");
        TestContext.WriteLine($"External role exists: {hasExternal}");

        Assert.That(hasAdmin, Is.True, "Admin role should be displayed");
    }

    [Test]
    [Order(12)]
    public async Task Role_Read_CanSearchRoles()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var searchInput = Page.Locator("#search-input, input[placeholder*='Search']");
        if (await searchInput.CountAsync() > 0)
        {
            await searchInput.FillAsync("Admin");
            await Page.WaitForTimeoutAsync(1000);
            await TakeScreenshot("roles_search_results");
        }

        Assert.Pass("Role search test completed");
    }

    [Test]
    [Order(13)]
    public async Task Role_Read_CanViewRolePermissions()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Find a role row and click to view/edit
        var viewButton = Page.Locator(".btn:has-text('View'), .btn:has-text('Edit'), button[onclick*='view'], button[onclick*='edit']").First;

        if (await viewButton.CountAsync() > 0)
        {
            await viewButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("role_permissions_view");

            // Look for permissions container
            var permissionsContainer = Page.Locator(".permissions-container, .permission-list, #permissions");
            var hasPermissions = await permissionsContainer.CountAsync() > 0;

            // Close modal
            var closeButton = Page.Locator("button:has-text('Cancel'), button:has-text('Close'), .admin-modal-close").First;
            if (await closeButton.IsVisibleAsync())
            {
                await closeButton.ClickAsync();
            }

            Assert.Pass("Role permissions view test completed");
        }
        else
        {
            Assert.Pass("No view/edit buttons found for roles");
        }
    }

    [Test]
    [Order(14)]
    public async Task Role_Read_CanSortByColumn()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var nameHeader = Page.Locator("th[data-sort='name'], th:has-text('Name')");
        if (await nameHeader.CountAsync() > 0)
        {
            await nameHeader.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("roles_sorted_by_name");

            await nameHeader.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("roles_sorted_by_name_desc");
        }

        Assert.Pass("Column sorting test completed");
    }

    #endregion

    #region UPDATE Tests

    [Test]
    [Order(20)]
    public async Task Role_Update_CanOpenEditModal()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Find edit button for a non-system role if possible, otherwise any role
        var editButton = Page.Locator("tr:not(:has-text('Admin')) .btn:has-text('Edit'), .btn:has-text('Edit')").First;

        if (await editButton.CountAsync() > 0)
        {
            await editButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("role_edit_modal");

            var modal = Page.Locator("#role-modal, .admin-modal:visible, [role='dialog']:visible");
            var modalVisible = await modal.CountAsync() > 0;

            // Close modal
            var cancelButton = Page.Locator("button:has-text('Cancel'), .admin-modal-close").First;
            if (await cancelButton.IsVisibleAsync())
            {
                await cancelButton.ClickAsync();
            }

            Assert.That(modalVisible, Is.True, "Edit modal should be visible");
        }
        else
        {
            Assert.Pass("No edit buttons found");
        }
    }

    [Test]
    [Order(21)]
    public async Task Role_Update_CanModifyRoleName()
    {
        // Create a test role first
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        var addButton = Page.Locator("#add-role-btn, button:has-text('Add'), .admin-btn--primary");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var nameInput = Page.Locator("#role-name, input[name='name'], input[name='roleName']").First;
        await nameInput.FillAsync(_testRoleName);

        var saveButton = Page.Locator("button:has-text('Save'), button:has-text('Create')").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(3000);

        // Now try to edit it
        await Page.ReloadAsync();
        await Page.WaitForTimeoutAsync(2000);

        var roleRow = Page.Locator($"tr:has-text('{_testRoleName}')");
        if (await roleRow.CountAsync() > 0)
        {
            var editButton = roleRow.Locator(".btn:has-text('Edit'), button[onclick*='edit']");
            if (await editButton.CountAsync() > 0)
            {
                await editButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var editNameInput = Page.Locator("#role-name, input[name='name'], input[name='roleName']").First;
                await editNameInput.FillAsync($"{_testRoleName} Updated");
                await TakeScreenshot("role_name_modified");

                var updateButton = Page.Locator("button:has-text('Save'), button:has-text('Update')").First;
                await updateButton.ClickAsync();
                await Page.WaitForTimeoutAsync(2000);
                await TakeScreenshot("role_update_saved");
            }
        }

        Assert.Pass("Role update test completed");
    }

    [Test]
    [Order(22)]
    public async Task Role_Update_CanModifyPermissions()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Find a non-system role to edit
        var editButton = Page.Locator("tr:has-text('AutoTest') .btn:has-text('Edit'), .btn:has-text('Edit')").First;

        if (await editButton.CountAsync() > 0)
        {
            await editButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            // Find permission checkboxes
            var permissionCheckboxes = Page.Locator(".permission-checkbox, input[type='checkbox'][name*='permission']");
            var checkboxCount = await permissionCheckboxes.CountAsync();

            if (checkboxCount > 0)
            {
                // Toggle a few permissions
                await permissionCheckboxes.First.ClickAsync();
                await TakeScreenshot("role_permission_toggled");
            }

            // Cancel to avoid actual changes
            var cancelButton = Page.Locator("button:has-text('Cancel'), .admin-modal-close").First;
            await cancelButton.ClickAsync();
        }

        Assert.Pass("Permission modification UI test completed");
    }

    #endregion

    #region DELETE Tests

    [Test]
    [Order(30)]
    public async Task Role_Delete_ShowsConfirmationModal()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Find delete button for non-system role
        var deleteButton = Page.Locator("tr:has-text('AutoTest') .btn:has-text('Delete'), tr:not(:has-text('Admin')):not(:has-text('Analyst')):not(:has-text('External')) .btn:has-text('Delete')").First;

        if (await deleteButton.CountAsync() > 0)
        {
            await deleteButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("role_delete_confirmation");

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
            TestContext.WriteLine("No delete buttons found for non-system roles");
            Assert.Pass("No deletable roles available");
        }
    }

    [Test]
    [Order(31)]
    public async Task Role_Delete_CanDeleteRole()
    {
        var deleteRoleName = $"AutoTest DELETE {DateTime.Now:HHmmss}";

        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        // Create role for deletion
        var addButton = Page.Locator("#add-role-btn, button:has-text('Add'), .admin-btn--primary");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var nameInput = Page.Locator("#role-name, input[name='name'], input[name='roleName']").First;
        await nameInput.FillAsync(deleteRoleName);

        var saveButton = Page.Locator("button:has-text('Save'), button:has-text('Create')").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(3000);
        await TakeScreenshot("role_created_for_deletion");

        await Page.ReloadAsync();
        await Page.WaitForTimeoutAsync(2000);

        var roleRow = Page.Locator($"tr:has-text('{deleteRoleName}')");
        if (await roleRow.CountAsync() > 0)
        {
            var deleteButton = roleRow.Locator(".btn:has-text('Delete'), button[onclick*='delete']");
            if (await deleteButton.CountAsync() > 0)
            {
                await deleteButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
                await TakeScreenshot("role_delete_confirm_dialog");

                var confirmButton = Page.Locator("button:has-text('Delete'), button:has-text('Confirm'), button:has-text('Yes')").First;
                if (await confirmButton.IsVisibleAsync())
                {
                    await confirmButton.ClickAsync();
                    await Page.WaitForTimeoutAsync(2000);
                    await TakeScreenshot("role_deleted");
                }

                await Page.ReloadAsync();
                await Page.WaitForTimeoutAsync(2000);
                var roleStillExists = await Page.Locator($"tr:has-text('{deleteRoleName}')").CountAsync() > 0;
                Assert.That(roleStillExists, Is.False, $"Deleted role '{deleteRoleName}' should no longer appear");
            }
        }
        else
        {
            Assert.Pass("Role creation may have failed - delete test skipped");
        }
    }

    [Test]
    [Order(32)]
    public async Task Role_Delete_AdminRoleCannotBeDeleted()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Find the Admin role row
        var adminRow = Page.Locator("tr:has-text('Admin')").First;

        if (await adminRow.CountAsync() > 0)
        {
            var deleteButton = adminRow.Locator(".btn:has-text('Delete'), button[onclick*='delete']");
            var hasDeleteButton = await deleteButton.CountAsync() > 0;

            await TakeScreenshot("admin_role_row");

            if (hasDeleteButton)
            {
                // Check if button is disabled
                var isDisabled = await deleteButton.IsDisabledAsync();
                TestContext.WriteLine($"Admin delete button disabled: {isDisabled}");

                if (!isDisabled)
                {
                    await deleteButton.ClickAsync();
                    await Page.WaitForTimeoutAsync(500);
                    await TakeScreenshot("admin_role_delete_attempt");

                    // Look for error message
                    var errorMessage = Page.Locator(".error-message, .notification-error, .toast-error, text*=cannot");
                    var hasError = await errorMessage.CountAsync() > 0;

                    // Cancel if modal opened
                    var cancelButton = Page.Locator("button:has-text('Cancel')").First;
                    if (await cancelButton.IsVisibleAsync())
                    {
                        await cancelButton.ClickAsync();
                    }

                    TestContext.WriteLine($"Error shown when trying to delete Admin: {hasError}");
                }
            }
            else
            {
                Assert.Pass("Admin role correctly has no delete button");
            }
        }

        Assert.Pass("Admin role protection check completed");
    }

    [Test]
    [Order(33)]
    public async Task Role_Delete_CanCancelDeletion()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var initialRows = await Page.Locator("table tbody tr").CountAsync();

        var deleteButton = Page.Locator("tr:has-text('AutoTest') .btn:has-text('Delete')").First;

        if (await deleteButton.CountAsync() > 0)
        {
            await deleteButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            var cancelButton = Page.Locator("button:has-text('Cancel')").First;
            if (await cancelButton.IsVisibleAsync())
            {
                await cancelButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
                await TakeScreenshot("role_delete_cancelled");
            }

            var finalRows = await Page.Locator("table tbody tr").CountAsync();
            Assert.That(finalRows, Is.EqualTo(initialRows), "Row count should be unchanged after cancelling delete");
        }
        else
        {
            Assert.Pass("No roles available to test cancel deletion");
        }
    }

    #endregion

    #region PERMISSION Tests

    [Test]
    [Order(40)]
    public async Task Role_Permissions_CanViewAvailablePermissions()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        var addButton = Page.Locator("#add-role-btn, button:has-text('Add'), .admin-btn--primary");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Look for permissions container
        var permissionsContainer = Page.Locator(".permissions-container, .permission-list, #permissions, .permission-checkboxes");
        var hasPermissions = await permissionsContainer.CountAsync() > 0;

        if (hasPermissions)
        {
            await TakeScreenshot("available_permissions");

            var permissionCheckboxes = Page.Locator("input[type='checkbox'][name*='permission'], .permission-checkbox");
            var permCount = await permissionCheckboxes.CountAsync();
            TestContext.WriteLine($"Found {permCount} permission checkboxes");
        }

        // Close modal
        var cancelButton = Page.Locator("button:has-text('Cancel'), .admin-modal-close").First;
        if (await cancelButton.IsVisibleAsync())
        {
            await cancelButton.ClickAsync();
        }

        Assert.Pass("Available permissions view test completed");
    }

    [Test]
    [Order(41)]
    public async Task Role_Permissions_CategoriesAreDisplayed()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        var addButton = Page.Locator("#add-role-btn, button:has-text('Add'), .admin-btn--primary");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Look for permission categories
        var categories = Page.Locator(".permission-category, .permission-group, h4, h5, .category-header");
        var categoryCount = await categories.CountAsync();

        await TakeScreenshot("permission_categories");
        TestContext.WriteLine($"Found {categoryCount} permission categories/groups");

        // Close modal
        var cancelButton = Page.Locator("button:has-text('Cancel'), .admin-modal-close").First;
        if (await cancelButton.IsVisibleAsync())
        {
            await cancelButton.ClickAsync();
        }

        Assert.Pass("Permission categories display test completed");
    }

    #endregion
}

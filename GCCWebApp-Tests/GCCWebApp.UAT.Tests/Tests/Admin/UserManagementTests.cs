using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.Tests.Admin;

[TestFixture]
public class UserManagementTests : BaseTest
{
    [Test]
    public async Task Admin_CanViewUserList()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("user_management_loaded");
        
        var hasUserList = await Page.IsVisibleAsync(".user-grid, .user-table, .user-list, table");
        var hasLoadButton = await Page.IsVisibleAsync("button:has-text('Load'), button:has-text('Get'), .load-users");
        
        if (hasLoadButton)
        {
            await Page.ClickAsync("button:has-text('Load'), button:has-text('Get'), .load-users");
            await Page.WaitForTimeoutAsync(2000);
            await TakeScreenshot("users_loaded");
        }
        
        Assert.That(hasUserList || hasLoadButton, Is.True, "User management should display user list or load option");
    }

    [Test]
    public async Task Admin_CreateUserFormValidation()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        
        await Page.WaitForSelectorAsync("body");
        
        var createButton = await Page.QuerySelectorAsync("button:has-text('Add'), button:has-text('Create'), button:has-text('New'), .add-user, .create-user");
        if (createButton != null)
        {
            await createButton.ClickAsync();
            await TakeScreenshot("create_user_form_opened");
            
            await Page.WaitForSelectorAsync("form, .user-form, .modal");
            
            var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Save'), button:has-text('Create'), .submit-user");
            if (submitButton != null)
            {
                await submitButton.ClickAsync();
                await TakeScreenshot("form_validation_triggered");
                
                var hasValidationErrors = await Page.IsVisibleAsync(".error, .validation-error, .field-validation-error, .alert-danger");
                Assert.That(hasValidationErrors, Is.True, "Empty form should show validation errors");
            }
        }
    }

    [Test]
    public async Task Admin_CanCreateUser()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        
        await Page.WaitForSelectorAsync("body");
        
        var createButton = await Page.QuerySelectorAsync("button:has-text('Add'), button:has-text('Create'), button:has-text('New'), .add-user, .create-user");
        if (createButton != null)
        {
            await createButton.ClickAsync();
            await TakeScreenshot("create_user_form_opened");
            
            await Page.WaitForSelectorAsync("form, .user-form, .modal");
            
            var emailField = await Page.QuerySelectorAsync("input[type='email'], input[name*='email'], input[id*='email'], #Email");
            var nameField = await Page.QuerySelectorAsync("input[name*='name'], input[id*='name'], #Name, #FirstName");
            
            if (emailField != null && nameField != null)
            {
                await emailField.FillAsync("testuser@example.com");
                await nameField.FillAsync("Test User");
                await TakeScreenshot("user_form_filled");
                
                var roleDropdown = await Page.QuerySelectorAsync("select[name*='role'], select[id*='role'], #Role");
                if (roleDropdown != null)
                {
                    await roleDropdown.SelectOptionAsync("External");
                }
                
                var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Save'), button:has-text('Create'), .submit-user");
                if (submitButton != null)
                {
                    await submitButton.ClickAsync();
                    await TakeScreenshot("user_creation_submitted");
                    
                    await Page.WaitForTimeoutAsync(2000);
                    
                    var hasSuccessMessage = await Page.IsVisibleAsync(".success, .alert-success, .notification-success");
                    var formClosed = !(await Page.IsVisibleAsync("form, .user-form, .modal"));
                    
                    Assert.That(hasSuccessMessage || formClosed, Is.True, "User creation should show success or close form");
                }
            }
        }
    }

    [Test]
    public async Task Admin_CanEditUser()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("before_edit_user");
        
        var loadButton = await Page.QuerySelectorAsync("button:has-text('Load'), button:has-text('Get'), .load-users");
        if (loadButton != null)
        {
            await loadButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
        }
        
        var editButton = await Page.QuerySelectorAsync("button:has-text('Edit'), .edit-user, .btn-edit, .fa-edit");
        if (editButton != null)
        {
            await editButton.ClickAsync();
            await TakeScreenshot("edit_user_form_opened");
            
            await Page.WaitForSelectorAsync("form, .user-form, .modal");
            
            var nameField = await Page.QuerySelectorAsync("input[name*='name'], input[id*='name'], #Name, #FirstName");
            if (nameField != null)
            {
                await nameField.FillAsync("Updated Test User");
                await TakeScreenshot("user_edit_form_filled");
                
                var saveButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Save'), button:has-text('Update'), .submit-user");
                if (saveButton != null)
                {
                    await saveButton.ClickAsync();
                    await TakeScreenshot("user_update_submitted");
                    
                    await Page.WaitForTimeoutAsync(2000);
                    
                    var hasSuccessMessage = await Page.IsVisibleAsync(".success, .alert-success, .notification-success");
                    var formClosed = !(await Page.IsVisibleAsync("form, .user-form, .modal"));
                    
                    Assert.That(hasSuccessMessage || formClosed, Is.True, "User update should show success or close form");
                }
            }
        }
    }

    [Test]
    public async Task Admin_CanViewUserDetails()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        
        await Page.WaitForSelectorAsync("body");
        
        var loadButton = await Page.QuerySelectorAsync("button:has-text('Load'), button:has-text('Get'), .load-users");
        if (loadButton != null)
        {
            await loadButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
        }
        
        var viewButton = await Page.QuerySelectorAsync("button:has-text('View'), .view-user, .btn-view, .fa-eye");
        var userRow = await Page.QuerySelectorAsync("tr:has(td), .user-row, .user-item");
        
        if (viewButton != null)
        {
            await viewButton.ClickAsync();
        }
        else if (userRow != null)
        {
            await userRow.ClickAsync();
        }
        
        if (viewButton != null || userRow != null)
        {
            await TakeScreenshot("user_details_view");
            
            var hasUserDetails = await Page.IsVisibleAsync(".user-details, .user-info, .modal, .details-panel");
            Assert.That(hasUserDetails, Is.True, "User details should be visible");
        }
    }

    [Test]
    public async Task Admin_CanDeleteUser()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        
        await Page.WaitForSelectorAsync("body");
        
        var loadButton = await Page.QuerySelectorAsync("button:has-text('Load'), button:has-text('Get'), .load-users");
        if (loadButton != null)
        {
            await loadButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
        }
        
        var deleteButton = await Page.QuerySelectorAsync("button:has-text('Delete'), .delete-user, .btn-delete, .fa-trash");
        if (deleteButton != null)
        {
            await TakeScreenshot("before_delete_user");
            await deleteButton.ClickAsync();
            
            var confirmDialog = await Page.IsVisibleAsync(".confirm, .modal, .alert");
            if (confirmDialog)
            {
                await TakeScreenshot("delete_confirmation_shown");
                
                var confirmButton = await Page.QuerySelectorAsync("button:has-text('Yes'), button:has-text('Confirm'), button:has-text('Delete'), .btn-confirm");
                if (confirmButton != null)
                {
                    await confirmButton.ClickAsync();
                    await TakeScreenshot("user_deletion_confirmed");
                    
                    await Page.WaitForTimeoutAsync(2000);
                    
                    var hasSuccessMessage = await Page.IsVisibleAsync(".success, .alert-success, .notification-success");
                    Assert.That(hasSuccessMessage, Is.True, "User deletion should show success message");
                }
            }
        }
    }

    [Test]
    public async Task Admin_UserFormHasRequiredFields()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        
        await Page.WaitForSelectorAsync("body");
        
        var createButton = await Page.QuerySelectorAsync("button:has-text('Add'), button:has-text('Create'), button:has-text('New'), .add-user, .create-user");
        if (createButton != null)
        {
            await createButton.ClickAsync();
            await Page.WaitForSelectorAsync("form, .user-form, .modal");
            await TakeScreenshot("user_form_fields_check");
            
            var hasEmailField = await Page.IsVisibleAsync("input[type='email'], input[name*='email'], input[id*='email'], #Email");
            var hasNameField = await Page.IsVisibleAsync("input[name*='name'], input[id*='name'], #Name, #FirstName");
            var hasRoleField = await Page.IsVisibleAsync("select[name*='role'], select[id*='role'], #Role");
            
            Assert.That(hasEmailField, Is.True, "User form should have email field");
            Assert.That(hasNameField, Is.True, "User form should have name field");
            Assert.That(hasRoleField, Is.True, "User form should have role field");
        }
    }

    [Test]
    public async Task Admin_UserListDisplaysUserInformation()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        
        await Page.WaitForSelectorAsync("body");
        
        var loadButton = await Page.QuerySelectorAsync("button:has-text('Load'), button:has-text('Get'), .load-users");
        if (loadButton != null)
        {
            await loadButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
        }
        
        await TakeScreenshot("user_list_information_check");
        
        var hasTableHeaders = await Page.IsVisibleAsync("th, .header, .column-header");
        var hasUserRows = await Page.IsVisibleAsync("tr:has(td), .user-row, .user-item");
        var hasActionButtons = await Page.IsVisibleAsync("button:has-text('Edit'), button:has-text('Delete'), .btn-edit, .btn-delete");
        
        Assert.That(hasTableHeaders || hasUserRows || hasActionButtons, Is.True, 
            "User list should display user information with headers, rows, or action buttons");
    }

    [Test]
    public async Task Admin_CanFilterUsers()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("user_filter_check");
        
        var hasSearchBox = await Page.IsVisibleAsync("input[type='search'], input[placeholder*='search'], .search-input");
        var hasFilterDropdown = await Page.IsVisibleAsync("select:has(option), .filter-dropdown");
        var hasFilterButton = await Page.IsVisibleAsync("button:has-text('Filter'), .filter-btn");
        
        if (hasSearchBox)
        {
            var searchBox = await Page.QuerySelectorAsync("input[type='search'], input[placeholder*='search'], .search-input");
            await searchBox!.FillAsync("test");
            await TakeScreenshot("user_search_entered");
            
            var searchButton = await Page.QuerySelectorAsync("button:has-text('Search'), .search-btn");
            if (searchButton != null)
            {
                await searchButton.ClickAsync();
                await TakeScreenshot("user_search_executed");
            }
        }
        
        Assert.That(hasSearchBox || hasFilterDropdown || hasFilterButton, Is.True, 
            "User management should have search or filter capabilities");
    }

    [Test]
    public async Task NonAdmin_CannotAccessUserManagement()
    {
        await LoginAsRole("External");
        
        var response = await Page.GotoAsync(BaseUrl + "/Admin/UserManagement");
        await TakeScreenshot("non_admin_user_management_blocked");
        
        var is403Or401 = response?.Status == 403 || response?.Status == 401;
        var hasUnauthorizedContent = await Page.IsVisibleAsync("text=Unauthorized, text=Access Denied, text=Forbidden");
        var redirectedToLogin = Page.Url.Contains("/Account/Login") || Page.Url.Contains("/login");
        
        Assert.That(is403Or401 || hasUnauthorizedContent || redirectedToLogin, Is.True, 
            "Non-admin users should not access user management");
    }
}
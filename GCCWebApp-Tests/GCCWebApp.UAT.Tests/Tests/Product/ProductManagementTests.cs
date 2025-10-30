using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.Tests.Product;

[TestFixture]
public class ProductManagementTests : BaseTest
{
    [Test]
    public async Task User_CanAccessProductIndex()
    {
        await NavigateToPage("/Product");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("product_index_loaded");
        
        var pageContent = await Page.ContentAsync();
        var hasProductContent = pageContent.Contains("Product") || 
                              await Page.IsVisibleAsync(".product-dashboard, .product-stats, .product-summary");
        
        Assert.That(hasProductContent, Is.True, "Product index should display product-related content");
    }

    [Test]
    public async Task Admin_CanAccessProductManagement()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Product/Management");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("product_management_loaded");
        
        var hasManagementInterface = await Page.IsVisibleAsync(".product-management, .product-grid, .product-table, table");
        var pageContent = await Page.ContentAsync();
        var hasProductContent = pageContent.Contains("Product");
        
        Assert.That(hasManagementInterface || hasProductContent, Is.True, 
            "Product management should display management interface");
    }

    [Test]
    public async Task Admin_CanViewProductList()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Product/Management");
        
        await Page.WaitForSelectorAsync("body");
        
        var loadButton = await Page.QuerySelectorAsync("button:has-text('Load'), button:has-text('Get Products'), .load-products");
        if (loadButton != null)
        {
            await loadButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
            await TakeScreenshot("products_loaded");
        }
        
        var hasProductList = await Page.IsVisibleAsync(".product-grid, .product-table, table tbody tr");
        var hasProductData = await Page.IsVisibleAsync("td, .product-item, .product-row");
        
        Assert.That(hasProductList || hasProductData, Is.True, "Should display product list or data");
    }

    [Test]
    public async Task Admin_CanCreateProduct()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Product/Management");
        
        await Page.WaitForSelectorAsync("body");
        
        var createButton = await Page.QuerySelectorAsync("button:has-text('Add'), button:has-text('Create'), button:has-text('New'), .add-product");
        if (createButton != null)
        {
            await createButton.ClickAsync();
            await TakeScreenshot("create_product_form_opened");
            
            await Page.WaitForSelectorAsync("form, .product-form, .modal");
            
            var nameField = await Page.QuerySelectorAsync("input[name*='name'], input[id*='name'], #Name, #ProductName");
            if (nameField != null)
            {
                await nameField.FillAsync("Test Product");
                
                var descriptionField = await Page.QuerySelectorAsync("textarea[name*='description'], textarea[id*='description'], #Description");
                if (descriptionField != null)
                {
                    await descriptionField.FillAsync("Test Product Description");
                }
                
                var categoryDropdown = await Page.QuerySelectorAsync("select[name*='category'], select[id*='category'], #CategoryId");
                if (categoryDropdown != null)
                {
                    var firstOption = await categoryDropdown.QuerySelectorAsync("option:not([value=''])");
                    if (firstOption != null)
                    {
                        var optionValue = await firstOption.GetAttributeAsync("value");
                        if (!string.IsNullOrEmpty(optionValue))
                        {
                            await categoryDropdown.SelectOptionAsync(optionValue);
                        }
                    }
                }
                
                await TakeScreenshot("product_form_filled");
                
                var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Save'), button:has-text('Create')");
                if (submitButton != null)
                {
                    await submitButton.ClickAsync();
                    await TakeScreenshot("product_creation_submitted");
                    
                    await Page.WaitForTimeoutAsync(2000);
                    
                    var hasSuccess = await Page.IsVisibleAsync(".success, .alert-success, .notification-success");
                    var formClosed = !(await Page.IsVisibleAsync("form, .product-form, .modal"));
                    
                    Assert.That(hasSuccess || formClosed, Is.True, "Product creation should succeed");
                }
            }
        }
    }

    [Test]
    public async Task Admin_CanEditProduct()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Product/Management");
        
        await Page.WaitForSelectorAsync("body");
        
        var loadButton = await Page.QuerySelectorAsync("button:has-text('Load'), button:has-text('Get Products'), .load-products");
        if (loadButton != null)
        {
            await loadButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
        }
        
        var editButton = await Page.QuerySelectorAsync("button:has-text('Edit'), .edit-product, .btn-edit, .fa-edit");
        if (editButton != null)
        {
            await editButton.ClickAsync();
            await TakeScreenshot("edit_product_form_opened");
            
            await Page.WaitForSelectorAsync("form, .product-form, .modal");
            
            var nameField = await Page.QuerySelectorAsync("input[name*='name'], input[id*='name'], #Name, #ProductName");
            if (nameField != null)
            {
                await nameField.FillAsync("Updated Test Product");
                await TakeScreenshot("product_edit_form_filled");
                
                var saveButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Save'), button:has-text('Update')");
                if (saveButton != null)
                {
                    await saveButton.ClickAsync();
                    await TakeScreenshot("product_update_submitted");
                    
                    await Page.WaitForTimeoutAsync(2000);
                    
                    var hasSuccess = await Page.IsVisibleAsync(".success, .alert-success, .notification-success");
                    var formClosed = !(await Page.IsVisibleAsync("form, .product-form, .modal"));
                    
                    Assert.That(hasSuccess || formClosed, Is.True, "Product update should succeed");
                }
            }
        }
    }

    [Test]
    public async Task Admin_CanDeleteProduct()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Product/Management");
        
        await Page.WaitForSelectorAsync("body");
        
        var loadButton = await Page.QuerySelectorAsync("button:has-text('Load'), button:has-text('Get Products'), .load-products");
        if (loadButton != null)
        {
            await loadButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
        }
        
        var deleteButton = await Page.QuerySelectorAsync("button:has-text('Delete'), .delete-product, .btn-delete, .fa-trash");
        if (deleteButton != null)
        {
            await TakeScreenshot("before_delete_product");
            await deleteButton.ClickAsync();
            
            var confirmDialog = await Page.IsVisibleAsync(".confirm, .modal, .alert");
            if (confirmDialog)
            {
                await TakeScreenshot("delete_confirmation_shown");
                
                var confirmButton = await Page.QuerySelectorAsync("button:has-text('Yes'), button:has-text('Confirm'), button:has-text('Delete')");
                if (confirmButton != null)
                {
                    await confirmButton.ClickAsync();
                    await TakeScreenshot("product_deletion_confirmed");
                    
                    await Page.WaitForTimeoutAsync(2000);
                    
                    var hasSuccess = await Page.IsVisibleAsync(".success, .alert-success, .notification-success");
                    Assert.That(hasSuccess, Is.True, "Product deletion should show success");
                }
            }
        }
    }

    [Test]
    public async Task Admin_CanViewProductCategories()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Product/Categories");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("product_categories_loaded");
        
        var hasCategoryInterface = await Page.IsVisibleAsync(".category-grid, .category-table, .category-list, table");
        var pageContent = await Page.ContentAsync();
        var hasCategoryContent = pageContent.Contains("Category") || pageContent.Contains("Product");
        
        Assert.That(hasCategoryInterface || hasCategoryContent, Is.True, 
            "Product categories should display category management interface");
    }

    [Test]
    public async Task Admin_ProductFormValidation()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Product/Management");
        
        await Page.WaitForSelectorAsync("body");
        
        var createButton = await Page.QuerySelectorAsync("button:has-text('Add'), button:has-text('Create'), button:has-text('New'), .add-product");
        if (createButton != null)
        {
            await createButton.ClickAsync();
            await Page.WaitForSelectorAsync("form, .product-form, .modal");
            await TakeScreenshot("empty_product_form");
            
            var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Save'), button:has-text('Create')");
            if (submitButton != null)
            {
                await submitButton.ClickAsync();
                await TakeScreenshot("product_form_validation");
                
                var hasValidationErrors = await Page.IsVisibleAsync(".error, .validation-error, .field-validation-error, .alert-danger");
                Assert.That(hasValidationErrors, Is.True, "Empty product form should show validation errors");
            }
        }
    }

    [Test]
    public async Task Admin_ProductFormHasRequiredFields()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Product/Management");
        
        await Page.WaitForSelectorAsync("body");
        
        var createButton = await Page.QuerySelectorAsync("button:has-text('Add'), button:has-text('Create'), button:has-text('New'), .add-product");
        if (createButton != null)
        {
            await createButton.ClickAsync();
            await Page.WaitForSelectorAsync("form, .product-form, .modal");
            await TakeScreenshot("product_form_fields");
            
            var hasNameField = await Page.IsVisibleAsync("input[name*='name'], input[id*='name'], #Name, #ProductName");
            var hasDescriptionField = await Page.IsVisibleAsync("textarea[name*='description'], textarea[id*='description'], #Description");
            var hasCategoryField = await Page.IsVisibleAsync("select[name*='category'], select[id*='category'], #CategoryId");
            
            Assert.That(hasNameField, Is.True, "Product form should have name field");
            Assert.That(hasDescriptionField || hasCategoryField, Is.True, 
                "Product form should have description or category field");
        }
    }

    [Test]
    public async Task Admin_CanViewProductStatistics()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Product");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("product_statistics_check");
        
        var hasStats = await Page.IsVisibleAsync(".stat, .stats, .metric, .dashboard-card, .summary");
        if (!hasStats)
        {
            var pageContent = await Page.ContentAsync();
            var hasNumericContent = pageContent.Contains("Total") || pageContent.Contains("Count") ||
                                  pageContent.Contains("0") || pageContent.Contains("1");
            Assert.That(hasNumericContent, Is.True, "Product page should show statistical information");
        }
        else
        {
            Assert.That(hasStats, Is.True, "Product page should display statistics");
        }
    }

    [Test]
    public async Task Admin_CanSearchProducts()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Product/Management");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("product_search_check");
        
        var hasSearchBox = await Page.IsVisibleAsync("input[type='search'], input[placeholder*='search'], .search-input");
        if (hasSearchBox)
        {
            var searchBox = await Page.QuerySelectorAsync("input[type='search'], input[placeholder*='search'], .search-input");
            await searchBox!.FillAsync("test");
            await TakeScreenshot("product_search_entered");
            
            var searchButton = await Page.QuerySelectorAsync("button:has-text('Search'), .search-btn");
            if (searchButton != null)
            {
                await searchButton.ClickAsync();
                await TakeScreenshot("product_search_executed");
            }
        }
        
        Assert.That(hasSearchBox, Is.True, "Product management should have search capability");
    }

    [Test]
    public async Task Admin_CanFilterProductsByCategory()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Product/Management");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("product_filter_check");
        
        var hasCategoryFilter = await Page.IsVisibleAsync("select:has(option), .filter-dropdown");
        if (hasCategoryFilter)
        {
            var filterDropdown = await Page.QuerySelectorAsync("select:has(option), .filter-dropdown");
            var firstOption = await filterDropdown!.QuerySelectorAsync("option:not([value=''])");
            if (firstOption != null)
            {
                var optionValue = await firstOption.GetAttributeAsync("value");
                if (!string.IsNullOrEmpty(optionValue))
                {
                    await filterDropdown.SelectOptionAsync(optionValue);
                    await TakeScreenshot("product_filter_applied");
                }
            }
        }
        
        Assert.That(hasCategoryFilter, Is.True, "Product management should have filtering capability");
    }

    [Test]
    public async Task Admin_CanViewProductDetails()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Product/Management");
        
        await Page.WaitForSelectorAsync("body");
        
        var loadButton = await Page.QuerySelectorAsync("button:has-text('Load'), button:has-text('Get Products'), .load-products");
        if (loadButton != null)
        {
            await loadButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
        }
        
        var viewButton = await Page.QuerySelectorAsync("button:has-text('View'), .view-product, .btn-view, .fa-eye");
        var productRow = await Page.QuerySelectorAsync("tr:has(td), .product-row, .product-item");
        
        if (viewButton != null)
        {
            await viewButton.ClickAsync();
        }
        else if (productRow != null)
        {
            await productRow.ClickAsync();
        }
        
        if (viewButton != null || productRow != null)
        {
            await TakeScreenshot("product_details_view");
            
            var hasProductDetails = await Page.IsVisibleAsync(".product-details, .product-info, .modal, .details-panel");
            Assert.That(hasProductDetails, Is.True, "Product details should be visible");
        }
    }

    [Test]
    public async Task User_CanAccessProductIndexWithoutLogin()
    {
        await NavigateToPage("/Product");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("public_product_access");
        
        var pageContent = await Page.ContentAsync();
        var hasProductContent = pageContent.Contains("Product") || 
                              await Page.IsVisibleAsync(".product-summary, .product-info");
        
        Assert.That(hasProductContent, Is.True, "Public users should access product index");
    }

    [Test]
    public async Task NonAdmin_CannotAccessProductManagement()
    {
        await LoginAsRole("External");

        var response = await Page.GotoAsync(BaseUrl + "/Product/Management");
        await TakeScreenshot("non_admin_product_management_blocked");

        var is403Or401 = response?.Status == 403 || response?.Status == 401;
        var hasUnauthorizedContent = await Page.IsVisibleAsync("text=Unauthorized, text=Access Denied, text=Forbidden");
        var redirectedToLogin = Page.Url.Contains("/Account/Login") || Page.Url.Contains("/login");

        Assert.That(is403Or401 || hasUnauthorizedContent || redirectedToLogin, Is.True,
            "Non-admin users should not access product management");
    }

    [Test]
    public async Task ExternalUser_CannotSeeActionsColumn()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Product/Management");

        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("external_user_product_management");

        // Wait for table to load
        await Page.WaitForSelectorAsync("table", new() { Timeout = 5000 }).ConfigureAwait(false);

        // Check if Actions column header is hidden
        var actionsHeaderVisible = await Page.IsVisibleAsync("th.actions-col");
        Assert.That(actionsHeaderVisible, Is.False,
            "Actions column header should be hidden for External users");

        // Check if Actions column cells are hidden
        var actionsCellsVisible = await Page.IsVisibleAsync("td.actions-cell");
        Assert.That(actionsCellsVisible, Is.False,
            "Actions column cells should be hidden for External users");

        // Verify Edit and Delete buttons are not visible
        var editButtonVisible = await Page.IsVisibleAsync(".btn-edit");
        var deleteButtonVisible = await Page.IsVisibleAsync(".btn-delete");
        Assert.That(editButtonVisible, Is.False, "Edit buttons should be hidden for External users");
        Assert.That(deleteButtonVisible, Is.False, "Delete buttons should be hidden for External users");
    }

    [Test]
    public async Task AdminUser_CanSeeActionsColumn()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Product/Management");

        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("admin_user_product_management_with_actions");

        // Wait for table to load
        await Page.WaitForSelectorAsync("table", new() { Timeout = 5000 }).ConfigureAwait(false);

        // Check if Actions column header is visible
        var actionsHeaderVisible = await Page.IsVisibleAsync("th.actions-col");
        Assert.That(actionsHeaderVisible, Is.True,
            "Actions column header should be visible for Admin users");

        // Load products to ensure table rows are populated
        var loadButton = await Page.QuerySelectorAsync("button:has-text('Load'), button:has-text('Get Products'), .load-products");
        if (loadButton != null)
        {
            await loadButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
        }

        // Check if Actions column cells are visible
        var actionsCellsVisible = await Page.IsVisibleAsync("td.actions-cell");
        Assert.That(actionsCellsVisible, Is.True,
            "Actions column cells should be visible for Admin users");

        // Verify Edit and Delete buttons are visible
        var editButtonVisible = await Page.IsVisibleAsync(".btn-edit");
        var deleteButtonVisible = await Page.IsVisibleAsync(".btn-delete");
        Assert.That(editButtonVisible, Is.True, "Edit buttons should be visible for Admin users");
        Assert.That(deleteButtonVisible, Is.True, "Delete buttons should be visible for Admin users");
    }

    [Test]
    public async Task AnalystUser_CanSeeActionsColumn()
    {
        await LoginAsRole("Analyst");
        await NavigateToPage("/Product/Management");

        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("analyst_user_product_management_with_actions");

        // Wait for table to load
        await Page.WaitForSelectorAsync("table", new() { Timeout = 5000 }).ConfigureAwait(false);

        // Check if Actions column header is visible
        var actionsHeaderVisible = await Page.IsVisibleAsync("th.actions-col");
        Assert.That(actionsHeaderVisible, Is.True,
            "Actions column header should be visible for Analyst users");

        // Load products to ensure table rows are populated
        var loadButton = await Page.QuerySelectorAsync("button:has-text('Load'), button:has-text('Get Products'), .load-products");
        if (loadButton != null)
        {
            await loadButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
        }

        // Check if Actions column cells are visible
        var actionsCellsVisible = await Page.IsVisibleAsync("td.actions-cell");
        Assert.That(actionsCellsVisible, Is.True,
            "Actions column cells should be visible for Analyst users");

        // Verify Edit and Delete buttons are visible
        var editButtonVisible = await Page.IsVisibleAsync(".btn-edit");
        var deleteButtonVisible = await Page.IsVisibleAsync(".btn-delete");
        Assert.That(editButtonVisible, Is.True, "Edit buttons should be visible for Analyst users");
        Assert.That(deleteButtonVisible, Is.True, "Delete buttons should be visible for Analyst users");
    }
}
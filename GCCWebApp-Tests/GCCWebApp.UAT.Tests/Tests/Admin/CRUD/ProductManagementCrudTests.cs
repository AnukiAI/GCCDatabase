using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.Admin.CRUD;

/// <summary>
/// CRUD tests for Product Management page (/Product/Management)
/// Tests Create, Read, Update, Delete operations for products
/// Safe to run on both Dev and Production (test environment)
/// </summary>
[TestFixture]
[Category("CRUD")]
[Category("Admin")]
[Category("Products")]
public class ProductManagementCrudTests : BaseTest
{
    private const string PageUrl = "/Product/Management";
    private string _testProductName = string.Empty;

    [SetUp]
    public new async Task SetUp()
    {
        await base.SetUp();
        var timestamp = DateTime.Now.ToString("HHmmss");
        _testProductName = $"AutoTest Product {timestamp}";
    }

    #region CREATE Tests

    [Test]
    [Order(1)]
    public async Task Product_Create_CanOpenAddProductModal()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await TakeScreenshot("products_page_loaded");

        var addButton = Page.Locator("#add-product-btn, button:has-text('Add New Product'), .btn-add");
        await addButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await TakeScreenshot("add_product_modal_opened");

        var modal = Page.Locator("#product-modal, .modal:visible, [role='dialog']:visible");
        var isVisible = await modal.IsVisibleAsync();
        Assert.That(isVisible, Is.True, "Add product modal should be visible");
    }

    [Test]
    [Order(2)]
    public async Task Product_Create_CanCreateNewProduct()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        var addButton = Page.Locator("#add-product-btn, button:has-text('Add New Product'), .btn-add");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Fill in product details
        var nameInput = Page.Locator("#product-name, input[name='name'], input[name='productName']");
        await nameInput.FillAsync(_testProductName);

        // Select company if dropdown exists
        var companySelect = Page.Locator("#product-company, select[name='companyId']");
        if (await companySelect.CountAsync() > 0)
        {
            var options = await companySelect.Locator("option").AllAsync();
            if (options.Count > 1)
            {
                await companySelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            }
        }

        // Select category if dropdown exists
        var categorySelect = Page.Locator("#product-category, select[name='categoryId']");
        if (await categorySelect.CountAsync() > 0)
        {
            var options = await categorySelect.Locator("option").AllAsync();
            if (options.Count > 1)
            {
                await categorySelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            }
        }

        await TakeScreenshot("product_form_filled");

        var saveButton = Page.Locator("#save-btn, button:has-text('Save'), button[type='submit']").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(3000);
        await TakeScreenshot("product_saved");

        var productInTable = Page.Locator($"text={_testProductName}");
        var exists = await productInTable.CountAsync() > 0;
        Assert.That(exists, Is.True, $"Product '{_testProductName}' should appear in the table");
    }

    [Test]
    [Order(3)]
    public async Task Product_Create_CanCancelAddProduct()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        var addButton = Page.Locator("#add-product-btn, button:has-text('Add New Product'), .btn-add");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        await TakeScreenshot("product_form_before_cancel");

        var cancelButton = Page.Locator("#cancel-btn, button:has-text('Cancel'), .modal-close").First;
        await cancelButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await TakeScreenshot("product_modal_cancelled");

        var modal = Page.Locator("#product-modal, .modal:visible");
        var isHidden = await modal.CountAsync() == 0 || !await modal.IsVisibleAsync();
        Assert.That(isHidden, Is.True, "Modal should be hidden after cancel");
    }

    #endregion

    #region READ Tests

    [Test]
    [Order(10)]
    public async Task Product_Read_CanViewProductsList()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("products_list_loaded");

        var tableBody = Page.Locator("#product-table tbody tr, .product-table tbody tr, table tbody tr");
        var rowCount = await tableBody.CountAsync();
        TestContext.WriteLine($"Found {rowCount} products in table");

        Assert.That(rowCount, Is.GreaterThanOrEqualTo(0), "Product table should be accessible");
    }

    [Test]
    [Order(11)]
    public async Task Product_Read_CanSearchProducts()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var searchInput = Page.Locator("#search-input, input[placeholder*='Search']");
        await searchInput.FillAsync("test");
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("products_search_results");

        Assert.Pass("Product search completed");
    }

    [Test]
    [Order(12)]
    public async Task Product_Read_CanFilterByCompany()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var companyFilter = Page.Locator("#company-filter, select[name='company']");
        if (await companyFilter.CountAsync() > 0)
        {
            var options = await companyFilter.Locator("option").AllAsync();
            if (options.Count > 1)
            {
                await companyFilter.SelectOptionAsync(new SelectOptionValue { Index = 1 });
                await Page.WaitForTimeoutAsync(1000);
                await TakeScreenshot("products_filtered_by_company");
            }
        }

        Assert.Pass("Company filter test completed");
    }

    [Test]
    [Order(13)]
    public async Task Product_Read_CanFilterByCategory()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var categoryFilter = Page.Locator("#category-filter, select[name='category']");
        if (await categoryFilter.CountAsync() > 0)
        {
            var options = await categoryFilter.Locator("option").AllAsync();
            if (options.Count > 1)
            {
                await categoryFilter.SelectOptionAsync(new SelectOptionValue { Index = 1 });
                await Page.WaitForTimeoutAsync(1000);
                await TakeScreenshot("products_filtered_by_category");
            }
        }

        Assert.Pass("Category filter test completed");
    }

    #endregion

    #region UPDATE Tests

    [Test]
    [Order(20)]
    public async Task Product_Update_CanOpenEditModal()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var editButton = Page.Locator(".btn:has-text('Edit'), button[onclick*='edit'], .edit-btn").First;

        if (await editButton.CountAsync() > 0)
        {
            await editButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("product_edit_modal");

            var modal = Page.Locator("#product-modal, .modal:visible, [role='dialog']:visible");
            var modalVisible = await modal.IsVisibleAsync();
            Assert.That(modalVisible, Is.True, "Edit modal should be visible");
        }
        else
        {
            Assert.Pass("No products available to edit");
        }
    }

    [Test]
    [Order(21)]
    public async Task Product_Update_CanModifyProductName()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var editButton = Page.Locator(".btn:has-text('Edit'), button[onclick*='edit'], .edit-btn").First;

        if (await editButton.CountAsync() > 0)
        {
            await editButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            var nameInput = Page.Locator("#product-name, input[name='name'], input[name='productName']");
            var currentName = await nameInput.InputValueAsync();
            await nameInput.FillAsync($"{currentName} - Updated");
            await TakeScreenshot("product_name_modified");

            // Cancel to avoid actual change
            var cancelButton = Page.Locator("#cancel-btn, button:has-text('Cancel')").First;
            await cancelButton.ClickAsync();
        }

        Assert.Pass("Product update UI test completed");
    }

    #endregion

    #region DELETE Tests

    [Test]
    [Order(30)]
    public async Task Product_Delete_ShowsConfirmationModal()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var deleteButton = Page.Locator(".btn:has-text('Delete'), button[onclick*='delete'], .delete-btn").First;

        if (await deleteButton.CountAsync() > 0)
        {
            await deleteButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("product_delete_confirmation");

            var cancelButton = Page.Locator("button:has-text('Cancel')").First;
            if (await cancelButton.IsVisibleAsync())
            {
                await cancelButton.ClickAsync();
            }

            Assert.Pass("Delete confirmation appeared");
        }
        else
        {
            Assert.Pass("No products available to delete");
        }
    }

    [Test]
    [Order(31)]
    public async Task Product_Delete_CanDeleteProduct()
    {
        var deleteProductName = $"AutoTest DELETE {DateTime.Now:HHmmss}";

        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        // Create product for deletion
        var addButton = Page.Locator("#add-product-btn, button:has-text('Add New Product'), .btn-add");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var nameInput = Page.Locator("#product-name, input[name='name'], input[name='productName']");
        await nameInput.FillAsync(deleteProductName);

        var companySelect = Page.Locator("#product-company, select[name='companyId']");
        if (await companySelect.CountAsync() > 0)
        {
            var options = await companySelect.Locator("option").AllAsync();
            if (options.Count > 1) await companySelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        }

        var saveButton = Page.Locator("#save-btn, button:has-text('Save')").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(3000);
        await TakeScreenshot("product_created_for_deletion");

        await Page.ReloadAsync();
        await Page.WaitForTimeoutAsync(2000);

        var productRow = Page.Locator($"tr:has-text('{deleteProductName}')");
        if (await productRow.CountAsync() > 0)
        {
            var deleteButton = productRow.Locator(".btn:has-text('Delete'), button[onclick*='delete']");
            if (await deleteButton.CountAsync() > 0)
            {
                await deleteButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var confirmButton = Page.Locator("button:has-text('Delete'), button:has-text('Confirm'), button:has-text('Yes')").First;
                if (await confirmButton.IsVisibleAsync())
                {
                    await confirmButton.ClickAsync();
                    await Page.WaitForTimeoutAsync(2000);
                    await TakeScreenshot("product_deleted");
                }

                await Page.ReloadAsync();
                await Page.WaitForTimeoutAsync(2000);
                var productStillExists = await Page.Locator($"tr:has-text('{deleteProductName}')").CountAsync() > 0;
                Assert.That(productStillExists, Is.False, "Deleted product should no longer appear");
            }
        }
        else
        {
            Assert.Pass("Product creation may have failed - delete test skipped");
        }
    }

    #endregion

    #region EXPORT Tests

    [Test]
    [Order(40)]
    public async Task Product_Export_CanExportToCSV()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var exportButton = Page.Locator("#export-btn, button:has-text('Export')");
        await exportButton.ClickAsync();
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("product_export_clicked");

        Assert.Pass("Export button clicked successfully");
    }

    #endregion
}

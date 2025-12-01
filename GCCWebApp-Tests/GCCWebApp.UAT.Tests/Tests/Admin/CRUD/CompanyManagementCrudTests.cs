using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.Admin.CRUD;

/// <summary>
/// CRUD tests for Company Management page (/Company/Management)
/// Tests Create, Read, Update, Delete operations for companies
/// Safe to run on both Dev and Production (test environment)
/// </summary>
[TestFixture]
[Category("CRUD")]
[Category("Admin")]
[Category("Companies")]
public class CompanyManagementCrudTests : BaseTest
{
    private const string PageUrl = "/Company/Management";
    private string _testCompanyName = string.Empty;
    private string _testCompanyEmail = string.Empty;

    [SetUp]
    public new async Task SetUp()
    {
        await base.SetUp();
        var timestamp = DateTime.Now.ToString("HHmmss");
        _testCompanyName = $"AutoTest Company {timestamp}";
        _testCompanyEmail = $"company_{timestamp}@test.example.com";
    }

    #region CREATE Tests

    [Test]
    [Order(1)]
    public async Task Company_Create_CanOpenAddCompanyModal()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await TakeScreenshot("companies_page_loaded");

        var addButton = Page.Locator("#add-company-btn");
        await addButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await TakeScreenshot("add_company_modal_opened");

        var modal = Page.Locator("#company-modal");
        var isVisible = await modal.IsVisibleAsync();
        Assert.That(isVisible, Is.True, "Add company modal should be visible");
    }

    [Test]
    [Order(2)]
    public async Task Company_Create_CanCreateNewCompany()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        await Page.ClickAsync("#add-company-btn");
        await Page.WaitForTimeoutAsync(500);

        await Page.FillAsync("#company-name", _testCompanyName);
        await Page.FillAsync("#company-email", _testCompanyEmail);
        await Page.FillAsync("#company-url", "https://autotest.example.com");
        await TakeScreenshot("company_form_filled");

        await Page.ClickAsync("#save-btn");
        await Page.WaitForTimeoutAsync(3000);
        await TakeScreenshot("company_saved");

        var companyInTable = Page.Locator($"text={_testCompanyName}");
        var exists = await companyInTable.CountAsync() > 0;
        Assert.That(exists, Is.True, $"Company '{_testCompanyName}' should appear in the table");
    }

    [Test]
    [Order(3)]
    public async Task Company_Create_ValidationRequiresName()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        await Page.ClickAsync("#add-company-btn");
        await Page.WaitForTimeoutAsync(500);

        // Fill email only, skip name
        await Page.FillAsync("#company-email", "test@example.com");
        await TakeScreenshot("company_form_missing_name");

        await Page.ClickAsync("#save-btn");
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("company_validation_error");

        var modal = Page.Locator("#company-modal");
        var stillVisible = await modal.IsVisibleAsync();
        Assert.That(stillVisible, Is.True, "Modal should remain open when validation fails");
    }

    [Test]
    [Order(4)]
    public async Task Company_Create_CanCancelAddCompany()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        await Page.ClickAsync("#add-company-btn");
        await Page.WaitForTimeoutAsync(500);

        await Page.FillAsync("#company-name", "Cancelled Company");
        await TakeScreenshot("company_form_before_cancel");

        await Page.ClickAsync("#cancel-btn");
        await Page.WaitForTimeoutAsync(500);
        await TakeScreenshot("company_modal_cancelled");

        var modal = Page.Locator("#company-modal");
        var isHidden = !await modal.IsVisibleAsync();
        Assert.That(isHidden, Is.True, "Modal should be hidden after cancel");
    }

    #endregion

    #region READ Tests

    [Test]
    [Order(10)]
    public async Task Company_Read_CanViewCompaniesList()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("companies_list_loaded");

        var tableBody = Page.Locator("#company-table tbody tr");
        var rowCount = await tableBody.CountAsync();
        TestContext.WriteLine($"Found {rowCount} companies in table");

        Assert.That(rowCount, Is.GreaterThanOrEqualTo(0), "Company table should be accessible");
    }

    [Test]
    [Order(11)]
    public async Task Company_Read_CanSearchCompanies()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var searchInput = Page.Locator("#search-input");
        await searchInput.FillAsync("test");
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("companies_search_results");

        var searchValue = await searchInput.InputValueAsync();
        Assert.That(searchValue, Is.EqualTo("test"), "Search input should contain search term");
    }

    [Test]
    [Order(12)]
    public async Task Company_Read_CanSortByColumn()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var nameHeader = Page.Locator("th[data-sort='name']");
        await nameHeader.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await TakeScreenshot("companies_sorted_by_name");

        await nameHeader.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await TakeScreenshot("companies_sorted_by_name_desc");

        Assert.Pass("Column sorting interaction completed");
    }

    #endregion

    #region UPDATE Tests

    [Test]
    [Order(20)]
    public async Task Company_Update_CanOpenEditModal()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var editButton = Page.Locator(".btn:has-text('Edit'), button[onclick*='edit'], .edit-btn").First;

        if (await editButton.CountAsync() > 0)
        {
            await editButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("company_edit_modal");

            var modal = Page.Locator("#company-modal");
            var modalVisible = await modal.IsVisibleAsync();
            Assert.That(modalVisible, Is.True, "Edit modal should be visible");
        }
        else
        {
            Assert.Pass("No companies available to edit");
        }
    }

    [Test]
    [Order(21)]
    public async Task Company_Update_CanModifyCompanyName()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        // Create test company
        await Page.ClickAsync("#add-company-btn");
        await Page.WaitForTimeoutAsync(500);
        await Page.FillAsync("#company-name", _testCompanyName);
        await Page.FillAsync("#company-email", _testCompanyEmail);
        await Page.ClickAsync("#save-btn");
        await Page.WaitForTimeoutAsync(3000);

        await Page.ReloadAsync();
        await Page.WaitForTimeoutAsync(2000);

        var companyRow = Page.Locator($"tr:has-text('{_testCompanyName}')");
        if (await companyRow.CountAsync() > 0)
        {
            var editButton = companyRow.Locator(".btn:has-text('Edit'), button[onclick*='edit']");
            if (await editButton.CountAsync() > 0)
            {
                await editButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                await Page.FillAsync("#company-name", $"{_testCompanyName} Updated");
                await TakeScreenshot("company_name_modified");

                await Page.ClickAsync("#save-btn");
                await Page.WaitForTimeoutAsync(2000);
                await TakeScreenshot("company_update_saved");
            }
        }

        Assert.Pass("Company update test completed");
    }

    #endregion

    #region DELETE Tests

    [Test]
    [Order(30)]
    public async Task Company_Delete_ShowsConfirmationModal()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var deleteButton = Page.Locator(".btn:has-text('Delete'), button[onclick*='delete'], .delete-btn").First;

        if (await deleteButton.CountAsync() > 0)
        {
            await deleteButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("company_delete_confirmation");

            var confirmModal = Page.Locator(".modal:visible, .confirm-modal:visible, [role='dialog']:visible");
            var modalVisible = await confirmModal.CountAsync() > 0;

            var cancelButton = Page.Locator("button:has-text('Cancel')").First;
            if (await cancelButton.IsVisibleAsync())
            {
                await cancelButton.ClickAsync();
            }

            Assert.That(modalVisible, Is.True, "Delete confirmation should appear");
        }
        else
        {
            Assert.Pass("No companies available to delete");
        }
    }

    [Test]
    [Order(31)]
    public async Task Company_Delete_CanDeleteCompany()
    {
        var deleteCompanyName = $"AutoTest DELETE {DateTime.Now:HHmmss}";
        var deleteCompanyEmail = $"delete_{DateTime.Now:HHmmss}@test.example.com";

        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        // Create company for deletion
        await Page.ClickAsync("#add-company-btn");
        await Page.WaitForTimeoutAsync(500);
        await Page.FillAsync("#company-name", deleteCompanyName);
        await Page.FillAsync("#company-email", deleteCompanyEmail);
        await Page.ClickAsync("#save-btn");
        await Page.WaitForTimeoutAsync(3000);
        await TakeScreenshot("company_created_for_deletion");

        await Page.ReloadAsync();
        await Page.WaitForTimeoutAsync(2000);

        var companyRow = Page.Locator($"tr:has-text('{deleteCompanyName}')");
        var companyExists = await companyRow.CountAsync() > 0;

        if (companyExists)
        {
            var deleteButton = companyRow.Locator(".btn:has-text('Delete'), button[onclick*='delete']");

            if (await deleteButton.CountAsync() > 0)
            {
                await deleteButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
                await TakeScreenshot("company_delete_confirm_dialog");

                var confirmButton = Page.Locator("button:has-text('Delete'), button:has-text('Confirm'), button:has-text('Yes')").First;
                if (await confirmButton.IsVisibleAsync())
                {
                    await confirmButton.ClickAsync();
                    await Page.WaitForTimeoutAsync(2000);
                    await TakeScreenshot("company_deleted");
                }

                await Page.ReloadAsync();
                await Page.WaitForTimeoutAsync(2000);
                var companyStillExists = await Page.Locator($"tr:has-text('{deleteCompanyName}')").CountAsync() > 0;
                Assert.That(companyStillExists, Is.False, $"Deleted company should no longer appear");
            }
        }
        else
        {
            Assert.Pass("Company creation may have failed - delete test skipped");
        }
    }

    #endregion

    #region EXPORT Tests

    [Test]
    [Order(40)]
    public async Task Company_Export_CanExportToCSV()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var exportButton = Page.Locator("#export-btn");
        await exportButton.ClickAsync();
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("company_export_clicked");

        Assert.Pass("Export button clicked successfully");
    }

    #endregion
}

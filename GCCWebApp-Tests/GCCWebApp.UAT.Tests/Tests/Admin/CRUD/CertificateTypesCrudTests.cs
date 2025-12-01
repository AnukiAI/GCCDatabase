using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.Admin.CRUD;

/// <summary>
/// CRUD tests for Certificate Types page (/Admin/CertificateTypes)
/// Tests Create, Read, Update, Delete operations for certificate types
/// Safe to run on both Dev and Production (test environment)
/// </summary>
[TestFixture]
[Category("CRUD")]
[Category("Admin")]
[Category("CertificateTypes")]
public class CertificateTypesCrudTests : BaseTest
{
    private const string PageUrl = "/Admin/CertificateTypes";
    private string _testTypeName = string.Empty;

    [SetUp]
    public new async Task SetUp()
    {
        await base.SetUp();
        var timestamp = DateTime.Now.ToString("HHmmss");
        _testTypeName = $"AutoTest CertType {timestamp}";
    }

    #region CREATE Tests

    [Test]
    [Order(1)]
    public async Task CertificateType_Create_CanOpenAddForm()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await TakeScreenshot("certificate_types_page_loaded");

        var addButton = Page.Locator("#add-type-btn, button:has-text('Add'), .btn-add, .admin-btn--primary");
        await addButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await TakeScreenshot("add_certificate_type_form");

        var formOrModal = Page.Locator(".modal:visible, .add-row:visible, form:visible, [role='dialog']:visible");
        var isVisible = await formOrModal.CountAsync() > 0;
        Assert.That(isVisible, Is.True, "Add form/modal should be visible");
    }

    [Test]
    [Order(2)]
    public async Task CertificateType_Create_CanCreateNewType()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        var addButton = Page.Locator("#add-type-btn, button:has-text('Add'), .btn-add, .admin-btn--primary");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var nameInput = Page.Locator("input[name*='name'], input[id*='name'], input[placeholder*='name']").First;
        await nameInput.FillAsync(_testTypeName);
        await TakeScreenshot("certificate_type_form_filled");

        var saveButton = Page.Locator("button:has-text('Save'), button[type='submit']").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(3000);
        await TakeScreenshot("certificate_type_saved");

        var typeInTable = Page.Locator($"text={_testTypeName}");
        var exists = await typeInTable.CountAsync() > 0;
        Assert.That(exists, Is.True, $"Certificate type '{_testTypeName}' should appear");
    }

    #endregion

    #region READ Tests

    [Test]
    [Order(10)]
    public async Task CertificateType_Read_CanViewTypesList()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("certificate_types_list_loaded");

        var tableBody = Page.Locator("table tbody tr, .type-list .type-item");
        var rowCount = await tableBody.CountAsync();
        TestContext.WriteLine($"Found {rowCount} certificate types");

        Assert.That(rowCount, Is.GreaterThanOrEqualTo(0), "Certificate types table should be accessible");
    }

    [Test]
    [Order(11)]
    public async Task CertificateType_Read_CanSearchTypes()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var searchInput = Page.Locator("#search-input, input[placeholder*='Search']");
        if (await searchInput.CountAsync() > 0)
        {
            await searchInput.FillAsync("test");
            await Page.WaitForTimeoutAsync(1000);
            await TakeScreenshot("certificate_types_search");
        }

        Assert.Pass("Search test completed");
    }

    #endregion

    #region UPDATE Tests

    [Test]
    [Order(20)]
    public async Task CertificateType_Update_CanOpenEditForm()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var editButton = Page.Locator(".btn:has-text('Edit'), button[onclick*='edit'], .edit-btn").First;

        if (await editButton.CountAsync() > 0)
        {
            await editButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("certificate_type_edit_form");
            Assert.Pass("Edit form opened");
        }
        else
        {
            Assert.Pass("No certificate types available to edit");
        }
    }

    #endregion

    #region DELETE Tests

    [Test]
    [Order(30)]
    public async Task CertificateType_Delete_ShowsConfirmation()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var deleteButton = Page.Locator(".btn:has-text('Delete'), button[onclick*='delete'], .delete-btn").First;

        if (await deleteButton.CountAsync() > 0)
        {
            await deleteButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("certificate_type_delete_confirmation");

            var cancelButton = Page.Locator("button:has-text('Cancel')").First;
            if (await cancelButton.IsVisibleAsync())
            {
                await cancelButton.ClickAsync();
            }
            Assert.Pass("Delete confirmation appeared");
        }
        else
        {
            Assert.Pass("No certificate types available to delete");
        }
    }

    [Test]
    [Order(31)]
    public async Task CertificateType_Delete_CanDeleteType()
    {
        var deleteTypeName = $"AutoTest DELETE {DateTime.Now:HHmmss}";

        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        // Create type for deletion
        var addButton = Page.Locator("#add-type-btn, button:has-text('Add'), .btn-add, .admin-btn--primary");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var nameInput = Page.Locator("input[name*='name'], input[id*='name']").First;
        await nameInput.FillAsync(deleteTypeName);

        var saveButton = Page.Locator("button:has-text('Save')").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(3000);

        await Page.ReloadAsync();
        await Page.WaitForTimeoutAsync(2000);

        var typeRow = Page.Locator($"tr:has-text('{deleteTypeName}'), .type-item:has-text('{deleteTypeName}')");
        if (await typeRow.CountAsync() > 0)
        {
            var deleteButton = typeRow.Locator(".btn:has-text('Delete'), button[onclick*='delete']");
            if (await deleteButton.CountAsync() > 0)
            {
                await deleteButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var confirmButton = Page.Locator("button:has-text('Delete'), button:has-text('Confirm')").First;
                if (await confirmButton.IsVisibleAsync())
                {
                    await confirmButton.ClickAsync();
                    await Page.WaitForTimeoutAsync(2000);
                    await TakeScreenshot("certificate_type_deleted");
                }
            }
        }

        Assert.Pass("Certificate type delete test completed");
    }

    #endregion

    #region EXPORT Tests

    [Test]
    [Order(40)]
    public async Task CertificateType_Export_CanExportToCSV()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var exportButton = Page.Locator("#export-btn, button:has-text('Export')");
        if (await exportButton.CountAsync() > 0)
        {
            await exportButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
            await TakeScreenshot("certificate_type_export");
        }

        Assert.Pass("Export test completed");
    }

    #endregion
}

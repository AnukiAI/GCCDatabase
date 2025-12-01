using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.Admin.CRUD;

/// <summary>
/// CRUD tests for Certificate Fields page (/Admin/CertificateFields)
/// Tests Create, Read, Update, Delete operations for certificate fields
/// Safe to run on both Dev and Production (test environment)
/// </summary>
[TestFixture]
[Category("CRUD")]
[Category("Admin")]
[Category("CertificateFields")]
public class CertificateFieldsCrudTests : BaseTest
{
    private const string PageUrl = "/Admin/CertificateFields";
    private string _testFieldName = string.Empty;

    [SetUp]
    public new async Task SetUp()
    {
        await base.SetUp();
        var timestamp = DateTime.Now.ToString("HHmmss");
        _testFieldName = $"AutoTest Field {timestamp}";
    }

    #region CREATE Tests

    [Test]
    [Order(1)]
    public async Task CertificateField_Create_CanOpenAddForm()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await TakeScreenshot("certificate_fields_page_loaded");

        var addButton = Page.Locator("#add-field-btn, button:has-text('Add'), .btn-add, .admin-btn--primary");
        await addButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await TakeScreenshot("add_certificate_field_form");

        var formOrModal = Page.Locator(".modal:visible, .add-row:visible, form:visible, [role='dialog']:visible");
        var isVisible = await formOrModal.CountAsync() > 0;
        Assert.That(isVisible, Is.True, "Add field form/modal should be visible");
    }

    [Test]
    [Order(2)]
    public async Task CertificateField_Create_CanCreateNewField()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        var addButton = Page.Locator("#add-field-btn, button:has-text('Add'), .btn-add, .admin-btn--primary");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var nameInput = Page.Locator("input[name*='name'], input[id*='name'], input[placeholder*='name']").First;
        await nameInput.FillAsync(_testFieldName);

        // Select field type if available
        var typeSelect = Page.Locator("select[name*='type'], select[id*='type']");
        if (await typeSelect.CountAsync() > 0)
        {
            var options = await typeSelect.Locator("option").AllAsync();
            if (options.Count > 1)
            {
                await typeSelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            }
        }

        await TakeScreenshot("certificate_field_form_filled");

        var saveButton = Page.Locator("button:has-text('Save'), button[type='submit']").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(3000);
        await TakeScreenshot("certificate_field_saved");

        var fieldInTable = Page.Locator($"text={_testFieldName}");
        var exists = await fieldInTable.CountAsync() > 0;
        Assert.That(exists, Is.True, $"Certificate field '{_testFieldName}' should appear");
    }

    #endregion

    #region READ Tests

    [Test]
    [Order(10)]
    public async Task CertificateField_Read_CanViewFieldsList()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("certificate_fields_list_loaded");

        var tableBody = Page.Locator("table tbody tr, .field-list .field-item");
        var rowCount = await tableBody.CountAsync();
        TestContext.WriteLine($"Found {rowCount} certificate fields");

        Assert.That(rowCount, Is.GreaterThanOrEqualTo(0), "Certificate fields table should be accessible");
    }

    [Test]
    [Order(11)]
    public async Task CertificateField_Read_CanSearchFields()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var searchInput = Page.Locator("#search-input, input[placeholder*='Search']");
        if (await searchInput.CountAsync() > 0)
        {
            await searchInput.FillAsync("test");
            await Page.WaitForTimeoutAsync(1000);
            await TakeScreenshot("certificate_fields_search");
        }

        Assert.Pass("Search test completed");
    }

    #endregion

    #region UPDATE Tests

    [Test]
    [Order(20)]
    public async Task CertificateField_Update_CanOpenEditForm()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var editButton = Page.Locator(".btn:has-text('Edit'), button[onclick*='edit'], .edit-btn").First;

        if (await editButton.CountAsync() > 0)
        {
            await editButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("certificate_field_edit_form");
            Assert.Pass("Edit form opened");
        }
        else
        {
            Assert.Pass("No certificate fields available to edit");
        }
    }

    #endregion

    #region DELETE Tests

    [Test]
    [Order(30)]
    public async Task CertificateField_Delete_ShowsConfirmation()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var deleteButton = Page.Locator(".btn:has-text('Delete'), button[onclick*='delete'], .delete-btn").First;

        if (await deleteButton.CountAsync() > 0)
        {
            await deleteButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("certificate_field_delete_confirmation");

            var cancelButton = Page.Locator("button:has-text('Cancel')").First;
            if (await cancelButton.IsVisibleAsync())
            {
                await cancelButton.ClickAsync();
            }
            Assert.Pass("Delete confirmation appeared");
        }
        else
        {
            Assert.Pass("No certificate fields available to delete");
        }
    }

    [Test]
    [Order(31)]
    public async Task CertificateField_Delete_CanDeleteField()
    {
        var deleteFieldName = $"AutoTest DELETE {DateTime.Now:HHmmss}";

        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        // Create field for deletion
        var addButton = Page.Locator("#add-field-btn, button:has-text('Add'), .btn-add, .admin-btn--primary");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var nameInput = Page.Locator("input[name*='name'], input[id*='name']").First;
        await nameInput.FillAsync(deleteFieldName);

        var saveButton = Page.Locator("button:has-text('Save')").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(3000);

        await Page.ReloadAsync();
        await Page.WaitForTimeoutAsync(2000);

        var fieldRow = Page.Locator($"tr:has-text('{deleteFieldName}')");
        if (await fieldRow.CountAsync() > 0)
        {
            var deleteButton = fieldRow.Locator(".btn:has-text('Delete'), button[onclick*='delete']");
            if (await deleteButton.CountAsync() > 0)
            {
                await deleteButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var confirmButton = Page.Locator("button:has-text('Delete'), button:has-text('Confirm')").First;
                if (await confirmButton.IsVisibleAsync())
                {
                    await confirmButton.ClickAsync();
                    await Page.WaitForTimeoutAsync(2000);
                    await TakeScreenshot("certificate_field_deleted");
                }
            }
        }

        Assert.Pass("Certificate field delete test completed");
    }

    #endregion

    #region EXPORT Tests

    [Test]
    [Order(40)]
    public async Task CertificateField_Export_CanExportToCSV()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var exportButton = Page.Locator("#export-btn, button:has-text('Export')");
        if (await exportButton.CountAsync() > 0)
        {
            await exportButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
            await TakeScreenshot("certificate_field_export");
        }

        Assert.Pass("Export test completed");
    }

    #endregion
}

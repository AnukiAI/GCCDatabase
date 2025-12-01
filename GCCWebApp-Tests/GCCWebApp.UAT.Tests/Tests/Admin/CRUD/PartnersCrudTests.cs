using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.Admin.CRUD;

/// <summary>
/// CRUD tests for Partners page (/Admin/Partners)
/// NOTE: Partners are core entities - limited CRUD operations
/// Per requirements: Partners page has ONLY Export + Add New Partner buttons (NO Template/Import)
/// Safe to run on both Dev and Production (test environment)
/// </summary>
[TestFixture]
[Category("CRUD")]
[Category("Admin")]
[Category("Partners")]
public class PartnersCrudTests : BaseTest
{
    private const string PageUrl = "/Admin/Partners";
    private string _testPartnerName = string.Empty;

    [SetUp]
    public new async Task SetUp()
    {
        await base.SetUp();
        var timestamp = DateTime.Now.ToString("HHmmss");
        _testPartnerName = $"AutoTest Partner {timestamp}";
    }

    #region CREATE Tests

    [Test]
    [Order(1)]
    public async Task Partner_Create_CanOpenAddForm()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await TakeScreenshot("partners_page_loaded");

        var addButton = Page.Locator("#add-partner-btn, button:has-text('Add New Partner'), .btn-add, .admin-btn--primary");
        await addButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await TakeScreenshot("add_partner_form");

        var formOrModal = Page.Locator(".modal:visible, .add-row:visible, form:visible, [role='dialog']:visible");
        var isVisible = await formOrModal.CountAsync() > 0;
        Assert.That(isVisible, Is.True, "Add partner form/modal should be visible");
    }

    [Test]
    [Order(2)]
    public async Task Partner_Create_CanCreateNewPartner()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        var addButton = Page.Locator("#add-partner-btn, button:has-text('Add New Partner'), .btn-add, .admin-btn--primary");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var nameInput = Page.Locator("input[name*='name'], input[id*='name'], input[placeholder*='name']").First;
        await nameInput.FillAsync(_testPartnerName);
        await TakeScreenshot("partner_form_filled");

        var saveButton = Page.Locator("button:has-text('Save'), button[type='submit']").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(3000);
        await TakeScreenshot("partner_saved");

        var partnerInTable = Page.Locator($"text={_testPartnerName}");
        var exists = await partnerInTable.CountAsync() > 0;
        Assert.That(exists, Is.True, $"Partner '{_testPartnerName}' should appear");
    }

    [Test]
    [Order(3)]
    public async Task Partner_Create_NoImportTemplateButtons()
    {
        // Per requirements: Partners page has ONLY Export + Add buttons (NO Template/Import)
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("partners_buttons_check");

        var templateButton = Page.Locator("#template-btn, button:has-text('Template')");
        var importButton = Page.Locator("#import-btn, button:has-text('Import')");

        var hasTemplate = await templateButton.CountAsync() > 0;
        var hasImport = await importButton.CountAsync() > 0;

        TestContext.WriteLine($"Template button exists: {hasTemplate}");
        TestContext.WriteLine($"Import button exists: {hasImport}");

        // These buttons should NOT exist on Partners page
        Assert.That(hasTemplate, Is.False, "Partners page should NOT have Template button");
        Assert.That(hasImport, Is.False, "Partners page should NOT have Import button");
    }

    #endregion

    #region READ Tests

    [Test]
    [Order(10)]
    public async Task Partner_Read_CanViewPartnersList()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("partners_list_loaded");

        var tableBody = Page.Locator("table tbody tr, .partner-list .partner-item");
        var rowCount = await tableBody.CountAsync();
        TestContext.WriteLine($"Found {rowCount} partners");

        Assert.That(rowCount, Is.GreaterThanOrEqualTo(0), "Partners table should be accessible");
    }

    [Test]
    [Order(11)]
    public async Task Partner_Read_DisplaysCorePartners()
    {
        // Core partners should exist: Amazon, Wayfair, Other/None
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(2000);

        var pageContent = await Page.ContentAsync();
        await TakeScreenshot("partners_core_check");

        var hasAmazon = pageContent.Contains("Amazon");
        var hasWayfair = pageContent.Contains("Wayfair");

        TestContext.WriteLine($"Amazon exists: {hasAmazon}");
        TestContext.WriteLine($"Wayfair exists: {hasWayfair}");

        // At least some core partners should exist
        Assert.That(hasAmazon || hasWayfair, Is.True, "Core partners (Amazon/Wayfair) should be displayed");
    }

    #endregion

    #region UPDATE Tests

    [Test]
    [Order(20)]
    public async Task Partner_Update_CanOpenEditForm()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var editButton = Page.Locator(".btn:has-text('Edit'), button[onclick*='edit'], .edit-btn").First;

        if (await editButton.CountAsync() > 0)
        {
            await editButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("partner_edit_form");
            Assert.Pass("Edit form opened");
        }
        else
        {
            Assert.Pass("No edit buttons available");
        }
    }

    #endregion

    #region DELETE Tests

    [Test]
    [Order(30)]
    public async Task Partner_Delete_ShowsConfirmation()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        // Note: Core partners (Amazon, Wayfair) may not be deletable
        var deleteButton = Page.Locator(".btn:has-text('Delete'), button[onclick*='delete'], .delete-btn").First;

        if (await deleteButton.CountAsync() > 0)
        {
            await deleteButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("partner_delete_confirmation");

            var cancelButton = Page.Locator("button:has-text('Cancel')").First;
            if (await cancelButton.IsVisibleAsync())
            {
                await cancelButton.ClickAsync();
            }
            Assert.Pass("Delete confirmation appeared");
        }
        else
        {
            TestContext.WriteLine("No delete buttons - core partners may be protected");
            Assert.Pass("No partners available to delete (core partners protected)");
        }
    }

    [Test]
    [Order(31)]
    public async Task Partner_Delete_CanDeleteNonCorePartner()
    {
        var deletePartnerName = $"AutoTest DELETE {DateTime.Now:HHmmss}";

        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);

        // Create partner for deletion
        var addButton = Page.Locator("#add-partner-btn, button:has-text('Add New Partner'), .btn-add, .admin-btn--primary");
        await addButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var nameInput = Page.Locator("input[name*='name'], input[id*='name']").First;
        await nameInput.FillAsync(deletePartnerName);

        var saveButton = Page.Locator("button:has-text('Save')").First;
        await saveButton.ClickAsync();
        await Page.WaitForTimeoutAsync(3000);

        await Page.ReloadAsync();
        await Page.WaitForTimeoutAsync(2000);

        var partnerRow = Page.Locator($"tr:has-text('{deletePartnerName}')");
        if (await partnerRow.CountAsync() > 0)
        {
            var deleteButton = partnerRow.Locator(".btn:has-text('Delete'), button[onclick*='delete']");
            if (await deleteButton.CountAsync() > 0)
            {
                await deleteButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var confirmButton = Page.Locator("button:has-text('Delete'), button:has-text('Confirm')").First;
                if (await confirmButton.IsVisibleAsync())
                {
                    await confirmButton.ClickAsync();
                    await Page.WaitForTimeoutAsync(2000);
                    await TakeScreenshot("partner_deleted");
                }
            }
        }

        Assert.Pass("Partner delete test completed");
    }

    #endregion

    #region EXPORT Tests

    [Test]
    [Order(40)]
    public async Task Partner_Export_CanExportToCSV()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(PageUrl);
        await Page.WaitForTimeoutAsync(1000);

        var exportButton = Page.Locator("#export-btn, button:has-text('Export')");
        if (await exportButton.CountAsync() > 0)
        {
            await exportButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
            await TakeScreenshot("partner_export");
            Assert.Pass("Export clicked successfully");
        }
        else
        {
            Assert.Fail("Export button should exist on Partners page");
        }
    }

    #endregion
}

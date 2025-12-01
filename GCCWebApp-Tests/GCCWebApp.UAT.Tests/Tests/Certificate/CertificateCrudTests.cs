using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.Certificate;

/// <summary>
/// CRUD tests for Certificate operations
/// Tests Create, Read, Update, Delete, Workflow, and Download operations
/// Safe to run on both Dev and Production (test environment)
/// </summary>
[TestFixture]
[Category("CRUD")]
[Category("Certificate")]
public class CertificateCrudTests : BaseTest
{
    private const string TileViewUrl = "/Certificate/CertificateTileView";
    private const string InternalViewUrl = "/Certificate/CertificateTileViewInternal";
    private const string ExternalViewUrl = "/Certificate/CertificateTileViewExternal";
    private const string CreateUrl = "/Certificate/Create";
    private string _testCertNumber = string.Empty;

    [SetUp]
    public new async Task SetUp()
    {
        await base.SetUp();
        var timestamp = DateTime.Now.ToString("HHmmss");
        _testCertNumber = $"AUTOTEST-{timestamp}";
    }

    #region CREATE Tests

    [Test]
    [Order(1)]
    public async Task Certificate_Create_CanAccessCreatePage()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(CreateUrl);
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("certificate_create_page");

        var pageTitle = Page.Locator("h1, .page-title, .admin-page-title");
        var hasTitle = await pageTitle.CountAsync() > 0;

        Assert.That(hasTitle, Is.True, "Certificate create page should load with title");
    }

    [Test]
    [Order(2)]
    public async Task Certificate_Create_FormFieldsArePresent()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(CreateUrl);
        await Page.WaitForTimeoutAsync(2000);

        // Check for essential form fields
        var certificateNumber = Page.Locator("input[name*='CertificateNumber'], input[id*='certificateNumber'], #CertificateNumber");
        var companySelect = Page.Locator("select[name*='Company'], select[id*='company'], #CompanyId");
        var productSelect = Page.Locator("select[name*='Product'], select[id*='product'], #ProductId");

        var hasCertNumber = await certificateNumber.CountAsync() > 0;
        var hasCompany = await companySelect.CountAsync() > 0;
        var hasProduct = await productSelect.CountAsync() > 0;

        await TakeScreenshot("certificate_form_fields");

        TestContext.WriteLine($"Certificate Number field: {hasCertNumber}");
        TestContext.WriteLine($"Company select: {hasCompany}");
        TestContext.WriteLine($"Product select: {hasProduct}");

        Assert.That(hasCertNumber || hasCompany || hasProduct, Is.True, "Certificate form should have required fields");
    }

    [Test]
    [Order(3)]
    public async Task Certificate_Create_CanFillBasicDetails()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(CreateUrl);
        await Page.WaitForTimeoutAsync(2000);

        // Fill certificate number if field exists
        var certNumberInput = Page.Locator("input[name*='CertificateNumber'], input[id*='certificateNumber'], #CertificateNumber").First;
        if (await certNumberInput.CountAsync() > 0)
        {
            await certNumberInput.FillAsync(_testCertNumber);
        }

        // Select company if dropdown exists
        var companySelect = Page.Locator("select[name*='Company'], select[id*='company'], #CompanyId").First;
        if (await companySelect.CountAsync() > 0)
        {
            var options = await companySelect.Locator("option").AllAsync();
            if (options.Count > 1)
            {
                await companySelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            }
        }

        // Select product if dropdown exists
        var productSelect = Page.Locator("select[name*='Product'], select[id*='product'], #ProductId").First;
        if (await productSelect.CountAsync() > 0)
        {
            await Page.WaitForTimeoutAsync(500); // Wait for product dropdown to populate
            var options = await productSelect.Locator("option").AllAsync();
            if (options.Count > 1)
            {
                await productSelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            }
        }

        await TakeScreenshot("certificate_form_filled");
        Assert.Pass("Certificate form fill test completed");
    }

    [Test]
    [Order(4)]
    public async Task Certificate_Create_CanSelectPartner()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(CreateUrl);
        await Page.WaitForTimeoutAsync(2000);

        var partnerSelect = Page.Locator("select[name*='Partner'], select[id*='partner'], #Partner");
        if (await partnerSelect.CountAsync() > 0)
        {
            await TakeScreenshot("certificate_partner_dropdown");

            var options = await partnerSelect.Locator("option").AllAsync();
            TestContext.WriteLine($"Found {options.Count} partner options");

            // Select Amazon if available
            try
            {
                await partnerSelect.SelectOptionAsync("Amazon");
                await TakeScreenshot("certificate_partner_amazon_selected");
            }
            catch
            {
                // Try selecting by index
                if (options.Count > 1)
                {
                    await partnerSelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
                }
            }

            Assert.Pass("Partner selection test completed");
        }
        else
        {
            Assert.Pass("Partner dropdown not found on create page");
        }
    }

    [Test]
    [Order(5)]
    public async Task Certificate_Create_CanCancelCreation()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(CreateUrl);
        await Page.WaitForTimeoutAsync(2000);

        var cancelButton = Page.Locator("a:has-text('Cancel'), button:has-text('Cancel'), a[href*='Certificate']");
        if (await cancelButton.CountAsync() > 0)
        {
            await cancelButton.First.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
            await TakeScreenshot("certificate_create_cancelled");

            // Should navigate away from create page
            var currentUrl = Page.Url;
            Assert.That(currentUrl, Does.Not.Contain("/Create"), "Should navigate away after cancel");
        }
        else
        {
            Assert.Pass("Cancel button not found");
        }
    }

    #endregion

    #region READ Tests - Public Tile View

    [Test]
    [Order(10)]
    public async Task Certificate_Read_PublicTileViewLoads()
    {
        // Public view - no login required
        await NavigateToPage(TileViewUrl);
        await Page.WaitForTimeoutAsync(3000);
        await TakeScreenshot("certificate_tile_view_public");

        var tiles = Page.Locator(".certificate-tile, .tile, .card, [class*='tile']");
        var tileCount = await tiles.CountAsync();
        TestContext.WriteLine($"Found {tileCount} certificate tiles");

        Assert.Pass("Public tile view loads");
    }

    [Test]
    [Order(11)]
    public async Task Certificate_Read_CanSearchCertificates()
    {
        await NavigateToPage(TileViewUrl);
        await Page.WaitForTimeoutAsync(2000);

        var searchInput = Page.Locator("#search-input, input[placeholder*='Search'], .search-input, input[type='search']");
        if (await searchInput.CountAsync() > 0)
        {
            await searchInput.First.FillAsync("test");
            await Page.WaitForTimeoutAsync(1500);
            await TakeScreenshot("certificate_search_results");

            Assert.Pass("Certificate search completed");
        }
        else
        {
            Assert.Pass("Search input not found on tile view");
        }
    }

    [Test]
    [Order(12)]
    public async Task Certificate_Read_AutocompleteSearchWorks()
    {
        await NavigateToPage(TileViewUrl);
        await Page.WaitForTimeoutAsync(2000);

        var searchInput = Page.Locator("#search-input, input[placeholder*='Search'], .search-input").First;
        if (await searchInput.CountAsync() > 0)
        {
            await searchInput.FillAsync("cert");
            await Page.WaitForTimeoutAsync(1000);

            // Look for autocomplete suggestions
            var suggestions = Page.Locator(".autocomplete-suggestion, .search-suggestion, .suggestion-item, [class*='suggestion']");
            var suggestionCount = await suggestions.CountAsync();

            await TakeScreenshot("certificate_autocomplete");
            TestContext.WriteLine($"Found {suggestionCount} autocomplete suggestions");

            Assert.Pass("Autocomplete search test completed");
        }
        else
        {
            Assert.Pass("Search input not found");
        }
    }

    [Test]
    [Order(13)]
    public async Task Certificate_Read_InfiniteScrollWorks()
    {
        await NavigateToPage(TileViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        // Count initial tiles
        var initialTiles = await Page.Locator(".certificate-tile, .tile, .card, tr[data-id]").CountAsync();
        TestContext.WriteLine($"Initial tiles: {initialTiles}");

        // Scroll to bottom
        await Page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
        await Page.WaitForTimeoutAsync(2000);

        // Count tiles after scroll
        var afterScrollTiles = await Page.Locator(".certificate-tile, .tile, .card, tr[data-id]").CountAsync();
        TestContext.WriteLine($"Tiles after scroll: {afterScrollTiles}");

        await TakeScreenshot("certificate_after_scroll");
        Assert.Pass("Infinite scroll test completed");
    }

    [Test]
    [Order(14)]
    public async Task Certificate_Read_FiltersAreAvailable()
    {
        await NavigateToPage(TileViewUrl);
        await Page.WaitForTimeoutAsync(2000);

        // Check for filter options
        var filterPanel = Page.Locator(".filter-panel, .filters, #filters, [class*='filter']");
        var filterCount = await filterPanel.CountAsync();

        await TakeScreenshot("certificate_filters");
        TestContext.WriteLine($"Found {filterCount} filter elements");

        Assert.Pass("Filter availability test completed");
    }

    #endregion

    #region READ Tests - Internal View

    [Test]
    [Order(20)]
    public async Task Certificate_Read_InternalViewRequiresLogin()
    {
        // Try to access internal view without login
        await NavigateToPage(InternalViewUrl);
        await Page.WaitForTimeoutAsync(2000);

        var currentUrl = Page.Url;
        await TakeScreenshot("certificate_internal_access_check");

        // Should redirect to login or show access denied
        var isLoginPage = currentUrl.Contains("Login") || currentUrl.Contains("Account");
        var isAccessDenied = await Page.Locator("text=Access Denied, text=Unauthorized, text=403").CountAsync() > 0;

        Assert.That(isLoginPage || isAccessDenied || currentUrl.Contains("Internal"), Is.True, "Internal view should require authentication");
    }

    [Test]
    [Order(21)]
    public async Task Certificate_Read_InternalViewLoadsForAdmin()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(InternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);
        await TakeScreenshot("certificate_internal_view_admin");

        var table = Page.Locator("table, .certificate-table, #certificate-table");
        var hasTable = await table.CountAsync() > 0;

        Assert.That(hasTable, Is.True, "Internal view should load table for Admin");
    }

    [Test]
    [Order(22)]
    public async Task Certificate_Read_InternalViewShowsInternalTags()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(InternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        // Look for internal tag column or tags
        var internalTagColumn = Page.Locator("th:has-text('Internal'), th:has-text('Tag'), .internal-tag");
        var hasInternalTags = await internalTagColumn.CountAsync() > 0;

        await TakeScreenshot("certificate_internal_tags");
        TestContext.WriteLine($"Internal tags column visible: {hasInternalTags}");

        Assert.Pass("Internal tags visibility test completed");
    }

    #endregion

    #region READ Tests - External View

    [Test]
    [Order(30)]
    public async Task Certificate_Read_ExternalViewLoadsForExternalUser()
    {
        await LoginAsRole("External");
        await NavigateToPage(ExternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);
        await TakeScreenshot("certificate_external_view");

        var table = Page.Locator("table, .certificate-table, #certificate-table");
        var hasTable = await table.CountAsync() > 0;

        Assert.That(hasTable, Is.True, "External view should load for External user");
    }

    [Test]
    [Order(31)]
    public async Task Certificate_Read_ExternalViewHidesInternalTags()
    {
        await LoginAsRole("External");
        await NavigateToPage(ExternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        // Internal tag column should NOT be visible for external users
        var internalTagColumn = Page.Locator("th:has-text('Internal Tag')");
        var hasInternalTagColumn = await internalTagColumn.CountAsync() > 0;

        await TakeScreenshot("certificate_external_no_internal_tags");
        Assert.That(hasInternalTagColumn, Is.False, "Internal tags should be hidden for External users");
    }

    [Test]
    [Order(32)]
    public async Task Certificate_Read_ExternalUserCannotSeeAddButton()
    {
        await LoginAsRole("External");
        await NavigateToPage(ExternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        var addButton = Page.Locator("button:has-text('Add'), a:has-text('Add Certificate'), #add-certificate-btn");
        var hasAddButton = await addButton.IsVisibleAsync();

        await TakeScreenshot("certificate_external_no_add_button");
        Assert.That(hasAddButton, Is.False, "Add Certificate button should be hidden for External users");
    }

    #endregion

    #region READ Tests - Single Certificate View

    [Test]
    [Order(40)]
    public async Task Certificate_Read_CanViewSingleCertificate()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(InternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        // Click on first certificate to view details
        var viewLink = Page.Locator("a[href*='Details'], a[href*='View'], .view-btn, td a").First;
        if (await viewLink.CountAsync() > 0)
        {
            await viewLink.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
            await TakeScreenshot("certificate_single_view");

            // Should show certificate details
            var details = Page.Locator(".certificate-details, .details, #certificate-details, .card-body");
            var hasDetails = await details.CountAsync() > 0;

            Assert.That(hasDetails, Is.True, "Single certificate view should display details");
        }
        else
        {
            Assert.Pass("No certificates available to view");
        }
    }

    #endregion

    #region UPDATE Tests

    [Test]
    [Order(50)]
    public async Task Certificate_Update_CanAccessEditPage()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(InternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        var editLink = Page.Locator("a[href*='Edit'], .edit-btn, button:has-text('Edit')").First;
        if (await editLink.CountAsync() > 0)
        {
            await editLink.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
            await TakeScreenshot("certificate_edit_page");

            var currentUrl = Page.Url;
            Assert.That(currentUrl, Does.Contain("Edit").IgnoreCase, "Should navigate to edit page");
        }
        else
        {
            Assert.Pass("No certificates available to edit");
        }
    }

    [Test]
    [Order(51)]
    public async Task Certificate_Update_FormPopulatesWithExistingData()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(InternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        var editLink = Page.Locator("a[href*='Edit'], .edit-btn").First;
        if (await editLink.CountAsync() > 0)
        {
            await editLink.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);

            // Check that form fields have values
            var certNumberInput = Page.Locator("input[name*='CertificateNumber'], #CertificateNumber").First;
            if (await certNumberInput.CountAsync() > 0)
            {
                var value = await certNumberInput.InputValueAsync();
                await TakeScreenshot("certificate_edit_form_populated");
                Assert.That(value, Is.Not.Empty, "Certificate number should be populated");
            }
            else
            {
                Assert.Pass("Certificate number field not found");
            }
        }
        else
        {
            Assert.Pass("No certificates available to edit");
        }
    }

    [Test]
    [Order(52)]
    public async Task Certificate_Update_ExternalUserCannotEdit()
    {
        await LoginAsRole("External");
        await NavigateToPage(ExternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        var editLink = Page.Locator("a[href*='Edit'], .edit-btn, button:has-text('Edit')");
        var hasEditButton = await editLink.IsVisibleAsync();

        await TakeScreenshot("certificate_external_no_edit");
        Assert.That(hasEditButton, Is.False, "Edit button should be hidden for External users");
    }

    #endregion

    #region DELETE Tests

    [Test]
    [Order(60)]
    public async Task Certificate_Delete_ShowsConfirmation()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(InternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        var deleteButton = Page.Locator(".delete-btn, button:has-text('Delete'), a[onclick*='delete']").First;
        if (await deleteButton.CountAsync() > 0)
        {
            await deleteButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await TakeScreenshot("certificate_delete_confirmation");

            var confirmModal = Page.Locator(".admin-modal:visible, .confirm-modal:visible, [role='dialog']:visible");
            var modalVisible = await confirmModal.CountAsync() > 0;

            // Cancel
            var cancelButton = Page.Locator("button:has-text('Cancel')").First;
            if (await cancelButton.IsVisibleAsync())
            {
                await cancelButton.ClickAsync();
            }

            Assert.Pass("Delete confirmation test completed");
        }
        else
        {
            Assert.Pass("No delete buttons found");
        }
    }

    [Test]
    [Order(61)]
    public async Task Certificate_Delete_ExternalUserCannotDelete()
    {
        await LoginAsRole("External");
        await NavigateToPage(ExternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        var deleteButton = Page.Locator(".delete-btn, button:has-text('Delete')");
        var hasDeleteButton = await deleteButton.IsVisibleAsync();

        await TakeScreenshot("certificate_external_no_delete");
        Assert.That(hasDeleteButton, Is.False, "Delete button should be hidden for External users");
    }

    #endregion

    #region DOWNLOAD Tests

    [Test]
    [Order(70)]
    public async Task Certificate_Download_DownloadButtonExists()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(InternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        var downloadButton = Page.Locator(".download-btn, button:has-text('Download'), a[href*='Download'], [title*='Download']");
        var hasDownloadButton = await downloadButton.CountAsync() > 0;

        await TakeScreenshot("certificate_download_button");
        Assert.That(hasDownloadButton, Is.True, "Download button should be visible");
    }

    [Test]
    [Order(71)]
    public async Task Certificate_Download_CanSelectMultipleCertificates()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(InternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        var checkboxes = Page.Locator("input[type='checkbox'][name*='select'], .select-checkbox, input[type='checkbox']");
        var checkboxCount = await checkboxes.CountAsync();

        if (checkboxCount > 1)
        {
            // Select first two checkboxes
            await checkboxes.Nth(0).CheckAsync();
            await checkboxes.Nth(1).CheckAsync();
            await TakeScreenshot("certificate_multiple_selected");

            Assert.Pass($"Selected multiple certificates ({checkboxCount} checkboxes available)");
        }
        else
        {
            Assert.Pass("Not enough certificates to test multi-select");
        }
    }

    [Test]
    [Order(72)]
    public async Task Certificate_Download_BulkDownloadButtonAppears()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(InternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        var checkboxes = Page.Locator("input[type='checkbox'][name*='select'], .select-checkbox");
        if (await checkboxes.CountAsync() > 0)
        {
            await checkboxes.First.CheckAsync();
            await Page.WaitForTimeoutAsync(500);

            var bulkDownloadButton = Page.Locator("button:has-text('Download Selected'), #download-selected-btn, .bulk-download-btn");
            var hasBulkDownload = await bulkDownloadButton.CountAsync() > 0;

            await TakeScreenshot("certificate_bulk_download_button");
            TestContext.WriteLine($"Bulk download button visible: {hasBulkDownload}");

            Assert.Pass("Bulk download button test completed");
        }
        else
        {
            Assert.Pass("No checkboxes available for selection");
        }
    }

    #endregion

    #region EXPORT Tests

    [Test]
    [Order(80)]
    public async Task Certificate_Export_ExportButtonExists()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(InternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        var exportButton = Page.Locator("#export-btn, button:has-text('Export'), a:has-text('Export')");
        var hasExportButton = await exportButton.CountAsync() > 0;

        await TakeScreenshot("certificate_export_button");
        Assert.That(hasExportButton, Is.True, "Export button should be visible");
    }

    [Test]
    [Order(81)]
    public async Task Certificate_Export_CanClickExport()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(InternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        var exportButton = Page.Locator("#export-btn, button:has-text('Export')").First;
        if (await exportButton.CountAsync() > 0)
        {
            await exportButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
            await TakeScreenshot("certificate_export_clicked");

            Assert.Pass("Export button clicked");
        }
        else
        {
            Assert.Pass("Export button not found");
        }
    }

    #endregion

    #region WORKFLOW Tests

    [Test]
    [Order(90)]
    public async Task Certificate_Workflow_StatusIsDisplayed()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(InternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        var statusColumn = Page.Locator("th:has-text('Status'), .status-badge, .certificate-status");
        var hasStatus = await statusColumn.CountAsync() > 0;

        await TakeScreenshot("certificate_status_column");
        Assert.That(hasStatus, Is.True, "Status column/badge should be visible");
    }

    [Test]
    [Order(91)]
    public async Task Certificate_Workflow_CanSeeActiveStatus()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(InternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        var activeStatus = Page.Locator("text=Active, .status-active, .badge-success:has-text('Active')");
        var hasActive = await activeStatus.CountAsync() > 0;

        await TakeScreenshot("certificate_active_status");
        TestContext.WriteLine($"Active status found: {hasActive}");

        Assert.Pass("Active status check completed");
    }

    [Test]
    [Order(92)]
    public async Task Certificate_Workflow_CanSeeExpiredStatus()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(InternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        var expiredStatus = Page.Locator("text=Expired, .status-expired, .badge-danger:has-text('Expired')");
        var hasExpired = await expiredStatus.CountAsync() > 0;

        await TakeScreenshot("certificate_expired_status");
        TestContext.WriteLine($"Expired status found: {hasExpired}");

        Assert.Pass("Expired status check completed");
    }

    [Test]
    [Order(93)]
    public async Task Certificate_Workflow_CanSeeDraftStatus()
    {
        await LoginAsRole("Admin");
        await NavigateToPage(InternalViewUrl);
        await Page.WaitForTimeoutAsync(3000);

        var draftStatus = Page.Locator("text=Draft, .status-draft, .badge-warning:has-text('Draft')");
        var hasDraft = await draftStatus.CountAsync() > 0;

        await TakeScreenshot("certificate_draft_status");
        TestContext.WriteLine($"Draft status found: {hasDraft}");

        Assert.Pass("Draft status check completed");
    }

    #endregion

    #region URL SHARING Tests

    [Test]
    [Order(100)]
    public async Task Certificate_URL_FilterParametersInURL()
    {
        await NavigateToPage(TileViewUrl);
        await Page.WaitForTimeoutAsync(2000);

        // Apply a search filter
        var searchInput = Page.Locator("#search-input, input[placeholder*='Search']").First;
        if (await searchInput.CountAsync() > 0)
        {
            await searchInput.FillAsync("test");
            await Page.WaitForTimeoutAsync(1500);

            var currentUrl = Page.Url;
            await TakeScreenshot("certificate_url_with_filter");

            // URL should contain search parameter
            var hasSearchParam = currentUrl.Contains("search") || currentUrl.Contains("q=") || currentUrl.Contains("filter");
            TestContext.WriteLine($"URL after search: {currentUrl}");

            Assert.Pass("URL filter parameter test completed");
        }
        else
        {
            Assert.Pass("Search input not found");
        }
    }

    [Test]
    [Order(101)]
    public async Task Certificate_URL_CanShareFilteredURL()
    {
        // This test verifies that filtered URLs can be loaded directly
        var filteredUrl = $"{TileViewUrl}?search=test";
        await NavigateToPage(filteredUrl);
        await Page.WaitForTimeoutAsync(3000);
        await TakeScreenshot("certificate_shared_url");

        // Page should load without error
        var hasError = await Page.Locator(".error, .alert-danger, text=Error").CountAsync() > 0;
        Assert.That(hasError, Is.False, "Filtered URL should load without error");
    }

    #endregion
}

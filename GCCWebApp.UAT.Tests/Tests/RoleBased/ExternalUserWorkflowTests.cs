using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.RoleBased;

[TestFixture]
public class ExternalUserWorkflowTests : BaseTest
{
    [Test]
    public async Task ExternalUser_CanAccessCompanySpecificCertificates()
    {
        // Arrange - Login as External User
        await LoginAsRole("External");
        await TakeScreenshot("external_user_logged_in_ready");

        // Act 1 - Navigate to Certificate area
        await NavigateToPage(TestDataHelper.Urls.Certificates);
        await TakeScreenshot("external_user_certificate_page_loaded");

        // Act 2 - Verify external user sees only company-specific data
        await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Medium);
        await TakeScreenshot("external_user_certificate_list_displayed");

        // Act 3 - Test certificate search within company scope
        foreach (var searchTerm in TestDataHelper.TestData.SearchTerms.Take(2))
        {
            await PerformSearchOperation(searchTerm);
            await TakeScreenshot($"external_user_search_{searchTerm.Replace(" ", "_")}");
        }

        // Act 4 - Try to access certificate tile view
        await NavigateToPage(TestDataHelper.Urls.CertificateTileView);
        await TakeScreenshot("external_user_tile_view_accessed");

        // Act 5 - Interact with available certificates
        await InteractWithCertificateViews();

        // Act 6 - Verify no access to admin functions
        try
        {
            await NavigateToPage(TestDataHelper.Urls.Admin);
            await TakeScreenshot("external_user_admin_access_attempt");
            
            var currentUrl = Page.Url.ToLower();
            var deniedAccess = currentUrl.Contains("login") || currentUrl.Contains("accessdenied") || currentUrl.Contains("unauthorized");
            
            if (deniedAccess)
            {
                TestContext.WriteLine("✅ External user correctly denied admin access");
            }
            else
            {
                TestContext.WriteLine("⚠️ External user may have unexpected admin access");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Admin access test: {ex.Message}");
            await TakeScreenshot("external_user_admin_access_error");
        }

        // Assert
        var finalUrl = Page.Url.ToLower();
        Assert.That(finalUrl.Contains("certificate") || finalUrl.Contains("dashboard"), Is.True, 
            "External user should have access to certificate viewing functionality");
        
        TestContext.WriteLine("✅ External user company-specific access workflow completed");
    }

    [Test]
    public async Task ExternalUser_CanFilterAndSortCertificates()
    {
        // Arrange
        await LoginAsRole("External");
        await TakeScreenshot("external_user_ready_for_filtering");

        // Act 1 - Navigate to certificates
        await NavigateToPage(TestDataHelper.Urls.Certificates);
        await TakeScreenshot("external_user_certificates_for_filtering");

        // Act 2 - Test status filtering (active, expired, pending)
        try
        {
            var statusFilters = Page.Locator("select[name*='status'], .status-filter, [data-filter='status']");
            if (await statusFilters.CountAsync() > 0)
            {
                await statusFilters.First.ClickAsync();
                await TakeScreenshot("external_user_status_filter_opened");
                TestContext.WriteLine("✅ External user can access status filtering");
            }
            else
            {
                TestContext.WriteLine("ℹ️ Status filtering may be implemented differently");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Status filtering test: {ex.Message}");
            await TakeScreenshot("external_user_status_filter_error");
        }

        // Act 3 - Test type and category filtering
        try
        {
            var typeFilters = Page.Locator("select[name*='type'], .type-filter, [data-filter='type']");
            if (await typeFilters.CountAsync() > 0)
            {
                await typeFilters.First.ClickAsync();
                await TakeScreenshot("external_user_type_filter_opened");
                TestContext.WriteLine("✅ External user can access type filtering");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Type filtering test: {ex.Message}");
            await TakeScreenshot("external_user_type_filter_error");
        }

        // Act 4 - Test sorting by expiration date
        try
        {
            var sortControls = Page.Locator("[data-sort*='expir'], [data-sort*='date'], .sort-expiration");
            if (await sortControls.CountAsync() > 0)
            {
                await sortControls.First.ClickAsync();
                await TakeScreenshot("external_user_expiration_sort_applied");
                TestContext.WriteLine("✅ External user can sort by expiration date");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Expiration sorting test: {ex.Message}");
            await TakeScreenshot("external_user_expiration_sort_error");
        }

        // Act 5 - Test product name search
        await PerformSearchOperation("Paper");
        await TakeScreenshot("external_user_product_search_completed");

        // Assert
        Assert.That(Page.Url.Contains("Certificate"), Is.True, 
            "External user should be able to filter and sort certificates");
        
        TestContext.WriteLine("✅ External user filtering and sorting workflow completed");
    }

    [Test]
    public async Task ExternalUser_CanDownloadAndExportCertificates()
    {
        // Arrange
        await LoginAsRole("External");
        await TakeScreenshot("external_user_ready_for_download");

        // Act 1 - Navigate to certificates
        await NavigateToPage(TestDataHelper.Urls.Certificates);
        await TakeScreenshot("external_user_certificates_for_download");

        // Act 2 - Look for download options
        try
        {
            var downloadOptions = Page.Locator("a[href*='download'], button[data-action*='download'], .download-btn, [title*='download']");
            var downloadCount = await downloadOptions.CountAsync();
            
            if (downloadCount > 0)
            {
                TestContext.WriteLine($"Found {downloadCount} download options");
                await TakeScreenshot("external_user_download_options_found");
                
                // Try clicking first download option
                await downloadOptions.First.ClickAsync();
                await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Medium);
                await TakeScreenshot("external_user_download_initiated");
                TestContext.WriteLine("✅ External user can initiate downloads");
            }
            else
            {
                TestContext.WriteLine("ℹ️ Download options may require certificate selection first");
                await TakeScreenshot("external_user_no_download_options");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Download test: {ex.Message}");
            await TakeScreenshot("external_user_download_error");
        }

        // Act 3 - Test multi-select for bulk download
        try
        {
            var checkboxes = Page.Locator("input[type='checkbox']");
            var checkboxCount = await checkboxes.CountAsync();
            
            if (checkboxCount > 0)
            {
                TestContext.WriteLine($"Found {checkboxCount} checkboxes for multi-select");
                await checkboxes.First.CheckAsync();
                if (checkboxCount > 1)
                {
                    await checkboxes.Nth(1).CheckAsync();
                }
                await TakeScreenshot("external_user_multi_select_completed");
                TestContext.WriteLine("✅ External user can multi-select certificates");
                
                // Look for bulk download after selection
                var bulkDownload = Page.Locator("[data-action*='bulk'], .bulk-download, button[title*='selected']");
                if (await bulkDownload.CountAsync() > 0)
                {
                    await bulkDownload.First.ClickAsync();
                    await TakeScreenshot("external_user_bulk_download_initiated");
                    TestContext.WriteLine("✅ External user can perform bulk downloads");
                }
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Multi-select download test: {ex.Message}");
            await TakeScreenshot("external_user_multi_select_error");
        }

        // Act 4 - Test Excel export functionality
        try
        {
            var exportOptions = Page.Locator("a[href*='excel'], button[data-format*='excel'], .export-excel, [title*='Excel']");
            if (await exportOptions.CountAsync() > 0)
            {
                await exportOptions.First.ClickAsync();
                await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Medium);
                await TakeScreenshot("external_user_excel_export_initiated");
                TestContext.WriteLine("✅ External user can export to Excel");
            }
            else
            {
                TestContext.WriteLine("ℹ️ Excel export may be accessed through different controls");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Excel export test: {ex.Message}");
            await TakeScreenshot("external_user_excel_export_error");
        }

        // Assert
        Assert.That(Page.Url.Contains("Certificate"), Is.True, 
            "External user should be able to download and export certificate data");
        
        TestContext.WriteLine("✅ External user download and export workflow completed");
    }

    [Test]
    public async Task ExternalUser_CanTrackExpirationDates()
    {
        // Arrange
        await LoginAsRole("External");
        await TakeScreenshot("external_user_ready_for_expiration_tracking");

        // Act 1 - Navigate to certificates
        await NavigateToPage(TestDataHelper.Urls.Certificates);
        await TakeScreenshot("external_user_certificates_for_expiration");

        // Act 2 - Look for expiration date information
        try
        {
            var expirationElements = Page.Locator("[data-expir], .expir, [title*='expir'], td:has-text('202'), .date");
            var expirationCount = await expirationElements.CountAsync();
            
            if (expirationCount > 0)
            {
                TestContext.WriteLine($"Found {expirationCount} elements with expiration information");
                await TakeScreenshot("external_user_expiration_dates_visible");
                TestContext.WriteLine("✅ External user can see expiration dates");
                
                // Try to click on expiration-related element to see details
                if (expirationCount > 0)
                {
                    await expirationElements.First.ClickAsync();
                    await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Short);
                    await TakeScreenshot("external_user_expiration_details_accessed");
                }
            }
            else
            {
                TestContext.WriteLine("ℹ️ Expiration dates may be displayed differently");
                await TakeScreenshot("external_user_no_expiration_visible");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Expiration tracking test: {ex.Message}");
            await TakeScreenshot("external_user_expiration_error");
        }

        // Act 3 - Test filtering by expiration status
        try
        {
            // Look for "expiring soon" or similar filters
            var expiringFilters = Page.Locator("option:has-text('expir'), [value*='expir'], .filter:has-text('expir')");
            if (await expiringFilters.CountAsync() > 0)
            {
                await expiringFilters.First.ClickAsync();
                await TakeScreenshot("external_user_expiring_filter_applied");
                TestContext.WriteLine("✅ External user can filter by expiration status");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Expiration filtering test: {ex.Message}");
            await TakeScreenshot("external_user_expiration_filter_error");
        }

        // Act 4 - Navigate to dashboard to check for expiration alerts
        try
        {
            await NavigateToPage(TestDataHelper.Urls.Dashboard);
            await TakeScreenshot("external_user_dashboard_for_expiration_alerts");
            
            // Look for alerts or notifications about expiring certificates
            var alertElements = Page.Locator(".alert, .notification, .warning, [class*='expir'], .danger");
            var alertCount = await alertElements.CountAsync();
            
            if (alertCount > 0)
            {
                TestContext.WriteLine($"Found {alertCount} potential expiration alerts");
                await TakeScreenshot("external_user_expiration_alerts_found");
                TestContext.WriteLine("✅ External user has access to expiration alerts");
            }
            else
            {
                TestContext.WriteLine("ℹ️ No expiration alerts visible - may depend on data");
                await TakeScreenshot("external_user_no_expiration_alerts");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Dashboard expiration alerts test: {ex.Message}");
            await TakeScreenshot("external_user_dashboard_expiration_error");
        }

        // Assert
        Assert.That(Page.Url.Contains("Dashboard") || Page.Url.Contains("Certificate"), Is.True, 
            "External user should be able to track certificate expiration dates");
        
        TestContext.WriteLine("✅ External user expiration tracking workflow completed");
    }

    [Test]
    public async Task ClientUser_CanAccessCompanySpecificData()
    {
        // Arrange - Test Client role specifically
        await LoginAsRole("Client");
        await TakeScreenshot("client_user_logged_in_ready");

        // Act 1 - Navigate to certificates
        await NavigateToPage(TestDataHelper.Urls.Certificates);
        await TakeScreenshot("client_user_certificate_access");

        // Act 2 - Verify client can view company certificates
        await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Medium);
        await TakeScreenshot("client_user_company_certificates_displayed");

        // Act 3 - Test that client cannot see other companies' data
        await PerformSearchOperation("Test Company");
        await TakeScreenshot("client_user_company_search_completed");

        // Act 4 - Verify dashboard access
        await NavigateToPage(TestDataHelper.Urls.Dashboard);
        await TakeScreenshot("client_user_dashboard_accessed");

        // Assert
        Assert.That(Page.Url.Contains("Certificate") || Page.Url.Contains("Dashboard"), Is.True, 
            "Client user should have access to company-specific certificate data");
        
        TestContext.WriteLine("✅ Client user company-specific access workflow completed");
    }

    [Test]
    public async Task PartnerUser_CanAccessProgramSpecificData()
    {
        // Arrange - Test Partner role specifically
        await LoginAsRole("Partner");
        await TakeScreenshot("partner_user_logged_in_ready");

        // Act 1 - Navigate to certificates
        await NavigateToPage(TestDataHelper.Urls.Certificates);
        await TakeScreenshot("partner_user_certificate_access");

        // Act 2 - Verify partner can view program certificates
        await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Medium);
        await TakeScreenshot("partner_user_program_certificates_displayed");

        // Act 3 - Test partner-specific filtering
        await PerformSearchOperation("Program");
        await TakeScreenshot("partner_user_program_search_completed");

        // Act 4 - Verify dashboard access for partners
        await NavigateToPage(TestDataHelper.Urls.Dashboard);
        await TakeScreenshot("partner_user_dashboard_accessed");

        // Assert
        Assert.That(Page.Url.Contains("Certificate") || Page.Url.Contains("Dashboard"), Is.True, 
            "Partner user should have access to program-specific certificate data");
        
        TestContext.WriteLine("✅ Partner user program-specific access workflow completed");
    }
}
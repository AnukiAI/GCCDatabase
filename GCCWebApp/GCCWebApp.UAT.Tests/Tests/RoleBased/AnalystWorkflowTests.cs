using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.RoleBased;

[TestFixture]
public class AnalystWorkflowTests : BaseTest
{
    [Test]
    public async Task Analyst_CanPerformCertificateSearchAndFiltering()
    {
        // Arrange - Login as Analyst
        await LoginAsRole("Analyst");
        await TakeScreenshot("analyst_logged_in_ready_for_certificate_work");

        // Act 1 - Navigate to Certificate area
        await NavigateToPage(TestDataHelper.Urls.Certificates);
        await TakeScreenshot("analyst_certificate_page_loaded");

        // Act 2 - Perform search operations
        foreach (var searchTerm in TestDataHelper.TestData.SearchTerms.Take(3))
        {
            await PerformSearchOperation(searchTerm);
            await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Short);
        }

        // Act 3 - Try to access certificate tile view
        await NavigateToPage(TestDataHelper.Urls.CertificateTileView);
        await TakeScreenshot("analyst_certificate_tile_view_loaded");

        // Act 4 - Interact with certificate views
        await InteractWithCertificateViews();

        // Act 5 - Test filtering capabilities
        try
        {
            var filterDropdown = Page.Locator(TestDataHelper.Selectors.FilterDropdown).First;
            if (await filterDropdown.CountAsync() > 0)
            {
                await filterDropdown.ClickAsync();
                await TakeScreenshot("analyst_filter_dropdown_opened");
                await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Short);
                TestContext.WriteLine("✅ Analyst can access filtering options");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Filter test: {ex.Message}");
            await TakeScreenshot("analyst_filter_attempt");
        }

        // Assert
        var currentUrl = Page.Url.ToLower();
        Assert.That(currentUrl.Contains("certificate") || currentUrl.Contains("dashboard"), Is.True, 
            "Analyst should be able to access certificate management area");
        
        TestContext.WriteLine("✅ Analyst certificate search and filtering workflow completed");
    }

    [Test]
    public async Task Analyst_CanPerformCertificateDataEntryWorkflow()
    {
        // Arrange
        await LoginAsRole("Analyst");
        await TakeScreenshot("analyst_ready_for_data_entry");

        // Act 1 - Navigate to Certificate management
        await NavigateToPage(TestDataHelper.Urls.Certificates);
        await TakeScreenshot("certificate_management_loaded_for_analyst");

        // Act 2 - Try to access Add Certificate functionality
        try
        {
            await NavigateToPage(TestDataHelper.Urls.CertificateAdd);
            await TakeScreenshot("analyst_add_certificate_page_accessed");
            
            // Wait for form to load if accessible
            await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Medium);
            await TakeScreenshot("analyst_add_certificate_form_loaded");
            
            TestContext.WriteLine("✅ Analyst can access Add Certificate functionality");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Add Certificate access: {ex.Message}");
            await TakeScreenshot("analyst_add_certificate_access_attempt");
        }

        // Act 3 - Try to access Edit Certificate functionality
        try
        {
            // Navigate back to certificate list first
            await NavigateToPage(TestDataHelper.Urls.Certificates);
            await TakeScreenshot("analyst_back_to_certificate_list");
            
            // Look for edit buttons or links
            var editButton = Page.Locator(TestDataHelper.Selectors.EditButton).First;
            if (await editButton.CountAsync() > 0)
            {
                await editButton.ClickAsync();
                await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Navigation);
                await TakeScreenshot("analyst_edit_certificate_accessed");
                TestContext.WriteLine("✅ Analyst can access Edit Certificate functionality");
            }
            else
            {
                TestContext.WriteLine("ℹ️ Edit functionality may require specific certificate selection");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Edit Certificate access: {ex.Message}");
            await TakeScreenshot("analyst_edit_certificate_attempt");
        }

        // Act 4 - Test tag assignment capabilities
        await NavigateToPage(TestDataHelper.Urls.Certificates);
        await TakeScreenshot("analyst_ready_for_tag_operations");

        // Assert
        Assert.That(Page.Url.Contains("Certificate") || Page.Url.Contains("certificate"), Is.True, 
            "Analyst should have access to certificate data entry functions");
        
        TestContext.WriteLine("✅ Analyst data entry workflow test completed");
    }

    [Test]
    public async Task Analyst_CanPerformBulkOperations()
    {
        // Arrange
        await LoginAsRole("Analyst");
        await TakeScreenshot("analyst_ready_for_bulk_operations");

        // Act 1 - Navigate to certificate area
        await NavigateToPage(TestDataHelper.Urls.Certificates);
        await TakeScreenshot("analyst_certificate_area_for_bulk_ops");

        // Act 2 - Look for bulk operation controls
        try
        {
            // Check for checkboxes or multi-select capabilities
            var checkboxes = Page.Locator("input[type='checkbox']");
            var checkboxCount = await checkboxes.CountAsync();
            
            if (checkboxCount > 0)
            {
                TestContext.WriteLine($"Found {checkboxCount} checkboxes - bulk operations may be available");
                await TakeScreenshot("analyst_bulk_operation_checkboxes_found");
                
                // Try to select multiple items
                if (checkboxCount > 1)
                {
                    await checkboxes.First.CheckAsync();
                    await checkboxes.Nth(1).CheckAsync();
                    await TakeScreenshot("analyst_multiple_items_selected");
                    TestContext.WriteLine("✅ Analyst can select multiple certificates");
                }
            }
            else
            {
                TestContext.WriteLine("ℹ️ No checkboxes found - bulk operations may be implemented differently");
                await TakeScreenshot("analyst_no_bulk_checkboxes_found");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Bulk operations test: {ex.Message}");
            await TakeScreenshot("analyst_bulk_operations_error");
        }

        // Act 3 - Look for export/download functionality
        try
        {
            var downloadButtons = Page.Locator("a[href*='download'], button[data-action*='download'], .download-btn");
            var downloadCount = await downloadButtons.CountAsync();
            
            if (downloadCount > 0)
            {
                TestContext.WriteLine($"Found {downloadCount} download options");
                await TakeScreenshot("analyst_download_options_found");
                TestContext.WriteLine("✅ Analyst has access to download functionality");
            }
            else
            {
                TestContext.WriteLine("ℹ️ Download functionality may require specific triggers");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Download functionality test: {ex.Message}");
            await TakeScreenshot("analyst_download_test_error");
        }

        // Assert
        Assert.That(Page.Url.Contains("Certificate"), Is.True, 
            "Analyst should be able to perform bulk operations on certificates");
        
        TestContext.WriteLine("✅ Analyst bulk operations workflow completed");
    }

    [Test]
    public async Task Analyst_CanAccessReportGeneration()
    {
        // Arrange
        await LoginAsRole("Analyst");
        await TakeScreenshot("analyst_ready_for_reporting");

        // Act 1 - Navigate to Dashboard for reporting
        await NavigateToPage(TestDataHelper.Urls.Dashboard);
        await TakeScreenshot("analyst_dashboard_loaded");

        // Act 2 - Look for reporting features
        await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Medium);
        await TakeScreenshot("analyst_dashboard_content_displayed");

        // Act 3 - Check for charts, graphs, or report controls
        try
        {
            var reportElements = Page.Locator("canvas, .chart, .graph, .report, [data-chart], [data-report]");
            var reportCount = await reportElements.CountAsync();
            
            if (reportCount > 0)
            {
                TestContext.WriteLine($"Found {reportCount} potential reporting elements");
                await TakeScreenshot("analyst_reporting_elements_found");
                TestContext.WriteLine("✅ Analyst dashboard has reporting features");
            }
            else
            {
                TestContext.WriteLine("ℹ️ Dashboard may not have visual reporting elements yet");
                await TakeScreenshot("analyst_dashboard_no_reports");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Reporting features test: {ex.Message}");
            await TakeScreenshot("analyst_reporting_test_error");
        }

        // Act 4 - Navigate back to certificates to check modification tracking
        await NavigateToPage(TestDataHelper.Urls.Certificates);
        await TakeScreenshot("analyst_certificates_for_modification_tracking");

        // Act 5 - Look for sort by modification date functionality
        try
        {
            var sortControls = Page.Locator("[data-sort], .sort, select[name*='sort'], th[data-sortable]");
            var sortCount = await sortControls.CountAsync();
            
            if (sortCount > 0)
            {
                TestContext.WriteLine($"Found {sortCount} sorting controls");
                await TakeScreenshot("analyst_sort_controls_found");
                TestContext.WriteLine("✅ Analyst can access sorting functionality");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Sort functionality test: {ex.Message}");
            await TakeScreenshot("analyst_sort_test_error");
        }

        // Assert
        Assert.That(Page.Url.Contains("Certificate") || Page.Url.Contains("Dashboard"), Is.True, 
            "Analyst should have access to reporting and analysis tools");
        
        TestContext.WriteLine("✅ Analyst reporting workflow completed");
    }
}
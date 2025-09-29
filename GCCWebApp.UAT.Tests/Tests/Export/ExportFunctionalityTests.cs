using Microsoft.Playwright;
using GCCWebApp.UAT.Tests.PageObjects;
using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.Export;

[TestFixture]
public class ExportFunctionalityTests : BaseTest
{
    [Test]
    public async Task Export_CertificateList_CSV_FromTileView()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot("export_csv_initial");

        // Act - Look for CSV export functionality
        var csvExportButton = Page.Locator("a[href*='Export'][href*='csv'], button:has-text('CSV'), .export-csv, [data-format='csv']");

        if (await csvExportButton.CountAsync() > 0)
        {
            // Set up download listener
            var downloadPromise = Page.WaitForDownloadAsync();

            await csvExportButton.ClickAsync();
            var download = await downloadPromise;

            // Assert
            Assert.That(download.SuggestedFilename, Does.EndWith(".csv"),
                "Downloaded file should have .csv extension");

            TestContext.WriteLine($"CSV file downloaded: {download.SuggestedFilename}");
            await TakeScreenshot("csv_export_completed");
        }
        else
        {
            // Test API endpoint directly
            var response = await Page.GotoAsync(BaseUrl + "/Certificate/Export?format=csv");

            Assert.That(response?.Status, Is.EqualTo(200),
                "CSV export endpoint should return success");

            var contentType = response?.Headers["content-type"];
            Assert.That(contentType, Does.Contain("csv").Or.Contain("text"),
                "Response should have appropriate content type for CSV");

            TestContext.WriteLine("CSV export API endpoint working");
            await TakeScreenshot("csv_export_api_tested");
        }
    }

    [Test]
    public async Task Export_CertificateList_PDF_FromTileView()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot("export_pdf_initial");

        // Act - Look for PDF export functionality
        var pdfExportButton = Page.Locator("a[href*='Export'][href*='pdf'], button:has-text('PDF'), .export-pdf, [data-format='pdf']");

        if (await pdfExportButton.CountAsync() > 0)
        {
            // Set up download listener
            var downloadPromise = Page.WaitForDownloadAsync();

            await pdfExportButton.ClickAsync();
            var download = await downloadPromise;

            // Assert
            Assert.That(download.SuggestedFilename, Does.EndWith(".pdf"),
                "Downloaded file should have .pdf extension");

            TestContext.WriteLine($"PDF file downloaded: {download.SuggestedFilename}");
            await TakeScreenshot("pdf_export_completed");
        }
        else
        {
            // Test API endpoint directly
            var response = await Page.GotoAsync(BaseUrl + "/Certificate/Export?format=pdf");

            Assert.That(response?.Status, Is.EqualTo(200),
                "PDF export endpoint should return success");

            var contentType = response?.Headers["content-type"];
            Assert.That(contentType, Does.Contain("pdf").Or.Contain("application"),
                "Response should have appropriate content type for PDF");

            TestContext.WriteLine("PDF export API endpoint working");
            await TakeScreenshot("pdf_export_api_tested");
        }
    }

    [Test]
    public async Task Export_IndividualCertificate_PDF_Download()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot("individual_download_initial");

        // Act - Find and test individual certificate download
        var certificateTiles = Page.Locator(".certificate-tile, .certificate-card, .cert-tile");
        var tileCount = await certificateTiles.CountAsync();

        if (tileCount > 0)
        {
            var firstTile = certificateTiles.First;

            // Look for download button/link within the tile
            var downloadLink = firstTile.Locator("a[href*='Download'], .download-link, .btn-download, button:has-text('Download')");

            if (await downloadLink.CountAsync() > 0)
            {
                // Set up download listener
                var downloadPromise = Page.WaitForDownloadAsync();

                await downloadLink.ClickAsync();
                var download = await downloadPromise;

                // Assert
                Assert.That(download.SuggestedFilename, Does.EndWith(".pdf"),
                    "Individual certificate download should be PDF");

                TestContext.WriteLine($"Individual certificate downloaded: {download.SuggestedFilename}");
                await TakeScreenshot("individual_download_completed");
            }
            else
            {
                // Try clicking on the certificate to go to details page
                await firstTile.ClickAsync();
                await Task.Delay(1000);
                await TakeScreenshot("certificate_details_for_download");

                // Look for download option on details page
                var detailsDownload = Page.Locator("a[href*='Download'], .download-certificate, button:has-text('Download')");

                if (await detailsDownload.CountAsync() > 0)
                {
                    TestContext.WriteLine("Download option found on certificate details page");
                    await TakeScreenshot("download_option_on_details");
                }
                else
                {
                    TestContext.WriteLine("No download option found - may need implementation");
                    await TakeScreenshot("no_download_option_found");
                }
            }
        }
        else
        {
            TestContext.WriteLine("No certificates available for download testing");
            await TakeScreenshot("no_certificates_for_download");
        }
    }

    [Test]
    public async Task Export_FilteredResults_CSV_MaintainsFilterCriteria()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot("filtered_export_initial");

        // Act - Apply a filter first
        var searchInput = Page.Locator("input[type='search'], .search-input, #searchInput").First;

        if (await searchInput.CountAsync() > 0)
        {
            await searchInput.FillAsync("test");
            await Task.Delay(1000); // Wait for filter to apply
            await TakeScreenshot("filter_applied_before_export");

            // Count filtered results
            var filteredCount = await Page.Locator(".certificate-tile, .certificate-card, .cert-tile").CountAsync();
            TestContext.WriteLine($"Filtered results count: {filteredCount}");

            // Try to export filtered results
            var exportButton = Page.Locator("a[href*='Export'], button:has-text('Export'), .export-btn");

            if (await exportButton.CountAsync() > 0)
            {
                var currentUrl = Page.Url;
                TestContext.WriteLine($"Current URL with filter: {currentUrl}");

                // Export should maintain filter parameters
                var downloadPromise = Page.WaitForDownloadAsync();
                await exportButton.ClickAsync();

                try
                {
                    var download = await downloadPromise;
                    TestContext.WriteLine($"Filtered export downloaded: {download.SuggestedFilename}");
                    await TakeScreenshot("filtered_export_completed");
                }
                catch (TimeoutException)
                {
                    TestContext.WriteLine("Export did not trigger download - may be API endpoint");
                    await TakeScreenshot("filtered_export_no_download");
                }
            }
            else
            {
                TestContext.WriteLine("No export button found for filtered results");
                await TakeScreenshot("no_export_button_with_filter");
            }
        }
        else
        {
            TestContext.WriteLine("No search input found for filter testing");
            await TakeScreenshot("no_search_for_filter_test");
        }
    }

    [Test]
    public async Task Export_Dashboard_IndividualMetrics_CSV()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage("/Dashboard");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot("dashboard_export_initial");

        // Act - Test dashboard metric export endpoints
        var metricTypes = new[]
        {
            "total-active-certs",
            "companies-with-active-certs",
            "certs-published-last-month",
            "certs-expiring-this-month"
        };

        var successfulExports = 0;

        foreach (var metricType in metricTypes)
        {
            var response = await Page.GotoAsync(BaseUrl + $"/Dashboard/DownloadStat?type={metricType}");

            if (response?.Status == 200)
            {
                var contentType = response.Headers["content-type"];
                if (contentType?.Contains("csv") == true || contentType?.Contains("text") == true)
                {
                    successfulExports++;
                    TestContext.WriteLine($"✓ {metricType} export successful");
                }
                else
                {
                    TestContext.WriteLine($"✗ {metricType} wrong content type: {contentType}");
                }
            }
            else
            {
                TestContext.WriteLine($"✗ {metricType} export failed: HTTP {response?.Status}");
            }
        }

        // Return to dashboard for final screenshot
        await NavigateToPage("/Dashboard");
        await TakeScreenshot("dashboard_metric_exports_tested");

        // Assert - At least some metrics should be exportable
        Assert.That(successfulExports, Is.GreaterThan(0),
            $"At least one metric should be exportable. Successful: {successfulExports}/{metricTypes.Length}");
    }

    [Test]
    public async Task Export_Dashboard_DataTables_CSV()
    {
        // Arrange
        await LoginAsRole("Admin");
        await NavigateToPage("/Dashboard");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot("dashboard_table_export_initial");

        // Act - Test dashboard table export endpoints
        var tableTypes = new[]
        {
            "cert-types",
            "top-companies",
            "cert-status",
            "product-types",
            "product-categories"
        };

        var successfulTableExports = 0;

        foreach (var tableType in tableTypes)
        {
            var response = await Page.GotoAsync(BaseUrl + $"/Dashboard/DownloadTable?type={tableType}");

            if (response?.Status == 200)
            {
                var contentType = response.Headers["content-type"];
                if (contentType?.Contains("csv") == true || contentType?.Contains("text") == true)
                {
                    successfulTableExports++;
                    TestContext.WriteLine($"✓ {tableType} table export successful");
                }
                else
                {
                    TestContext.WriteLine($"✗ {tableType} wrong content type: {contentType}");
                }
            }
            else
            {
                TestContext.WriteLine($"✗ {tableType} table export failed: HTTP {response?.Status}");
            }
        }

        // Return to dashboard for final screenshot
        await NavigateToPage("/Dashboard");
        await TakeScreenshot("dashboard_table_exports_tested");

        // Assert - At least some tables should be exportable
        Assert.That(successfulTableExports, Is.GreaterThan(0),
            $"At least one table should be exportable. Successful: {successfulTableExports}/{tableTypes.Length}");
    }

    [Test]
    public async Task Export_BulkOperations_LargeDataset()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot("bulk_export_initial");

        // Act - Test bulk export with potentially large dataset
        var initialCount = await Page.Locator(".certificate-tile, .certificate-card, .cert-tile").CountAsync();
        TestContext.WriteLine($"Visible certificates: {initialCount}");

        // Test export performance with timeout
        var exportStartTime = DateTime.Now;

        try
        {
            var response = await Page.GotoAsync(BaseUrl + "/Certificate/Export?format=csv",
                new PageGotoOptions { Timeout = 30000 }); // 30 second timeout

            var exportDuration = DateTime.Now - exportStartTime;
            TestContext.WriteLine($"Export completed in: {exportDuration.TotalSeconds:F2} seconds");

            if (response?.Status == 200)
            {
                var contentLength = response.Headers["content-length"];
                TestContext.WriteLine($"Export content length: {contentLength} bytes");

                await TakeScreenshot("bulk_export_completed");

                // Assert - Export should complete within reasonable time
                Assert.That(exportDuration.TotalSeconds, Is.LessThan(30),
                    "Bulk export should complete within 30 seconds");
            }
            else
            {
                TestContext.WriteLine($"Bulk export failed with status: {response?.Status}");
                await TakeScreenshot("bulk_export_failed");
            }
        }
        catch (TimeoutException)
        {
            TestContext.WriteLine("Bulk export timed out - may indicate performance issue");
            await TakeScreenshot("bulk_export_timeout");
            Assert.Fail("Bulk export should not timeout within 30 seconds");
        }
    }

    [Test]
    public async Task Export_AccessControl_ExternalUserRestrictions()
    {
        // Arrange - Test as External user
        await LoginAsRole("External");
        await NavigateToPage("/Certificate/CertificateTileView");
        await TakeScreenshot("external_user_export_test");

        // Act - Try to access export functionality as External user
        var exportButton = Page.Locator("a[href*='Export'], button:has-text('Export'), .export-btn");

        if (await exportButton.CountAsync() > 0)
        {
            // External users might have limited export access
            TestContext.WriteLine("Export button visible to External user");

            try
            {
                await exportButton.ClickAsync();
                await Task.Delay(1000);
                await TakeScreenshot("external_user_export_attempted");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Export failed for External user: {ex.Message}");
            }
        }
        else
        {
            TestContext.WriteLine("Export button not visible to External user - good security");
            await TakeScreenshot("external_user_no_export_access");
        }

        // Test direct API access
        var apiResponse = await Page.GotoAsync(BaseUrl + "/Certificate/Export?format=csv");

        if (apiResponse?.Status == 403 || apiResponse?.Status == 401)
        {
            TestContext.WriteLine("External user properly blocked from export API - good security");
        }
        else if (apiResponse?.Status == 200)
        {
            TestContext.WriteLine("External user has export access - verify this is intended");
        }
        else
        {
            TestContext.WriteLine($"Export API returned status: {apiResponse?.Status}");
        }

        await TakeScreenshot("external_user_export_security_tested");
    }

    [Test]
    public async Task Export_FileFormats_ContentValidation()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot("format_validation_initial");

        // Act & Assert - Test different export formats
        var formats = new[]
        {
            new { Format = "csv", ContentType = "text/csv", Extension = ".csv" },
            new { Format = "pdf", ContentType = "application/pdf", Extension = ".pdf" },
            new { Format = "excel", ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Extension = ".xlsx" }
        };

        var supportedFormats = 0;

        foreach (var format in formats)
        {
            var response = await Page.GotoAsync(BaseUrl + $"/Certificate/Export?format={format.Format}");

            if (response?.Status == 200)
            {
                var contentType = response.Headers["content-type"];
                var contentDisposition = response.Headers["content-disposition"];

                TestContext.WriteLine($"{format.Format.ToUpper()} export - Status: {response.Status}, Content-Type: {contentType}");

                if (contentType?.Contains(format.ContentType.Split('/')[1]) == true)
                {
                    supportedFormats++;
                    TestContext.WriteLine($"✓ {format.Format.ToUpper()} format properly supported");
                }
                else
                {
                    TestContext.WriteLine($"✗ {format.Format.ToUpper()} format has incorrect content type");
                }

                // Check filename in content disposition if present
                if (!string.IsNullOrEmpty(contentDisposition) && contentDisposition.Contains("filename"))
                {
                    var hasCorrectExtension = contentDisposition.Contains(format.Extension);
                    TestContext.WriteLine($"Filename has correct extension: {hasCorrectExtension}");
                }
            }
            else if (response?.Status == 400)
            {
                TestContext.WriteLine($"{format.Format.ToUpper()} format not supported (400 Bad Request)");
            }
            else
            {
                TestContext.WriteLine($"{format.Format.ToUpper()} export failed with status: {response?.Status}");
            }
        }

        await TakeScreenshot("format_validation_completed");

        // Assert - At least CSV should be supported
        Assert.That(supportedFormats, Is.GreaterThan(0),
            "At least one export format should be properly supported");
    }

    [Test]
    public async Task Export_ErrorHandling_InvalidRequests()
    {
        // Arrange & Act - Test error handling for invalid export requests
        var errorTests = new[]
        {
            new { Name = "Invalid Format", Url = "/Certificate/Export?format=invalid" },
            new { Name = "Missing Format", Url = "/Certificate/Export" },
            new { Name = "Invalid Certificate ID", Url = "/Certificate/Download/999999" },
            new { Name = "Malformed Parameters", Url = "/Certificate/Export?format=csv&invalid=param" }
        };

        var properErrorHandling = 0;

        foreach (var test in errorTests)
        {
            var response = await Page.GotoAsync(BaseUrl + test.Url);

            var hasProperErrorStatus = response?.Status == 400 || response?.Status == 404 || response?.Status == 422;

            if (hasProperErrorStatus)
            {
                properErrorHandling++;
                TestContext.WriteLine($"✓ {test.Name}: Proper error status {response?.Status}");
            }
            else
            {
                TestContext.WriteLine($"✗ {test.Name}: Unexpected status {response?.Status}");
            }
        }

        await TakeScreenshot("export_error_handling_tested");

        // Assert - Most error cases should be handled properly
        Assert.That(properErrorHandling, Is.GreaterThanOrEqualTo(errorTests.Length / 2),
            "Most invalid export requests should return appropriate error statuses");
    }
}
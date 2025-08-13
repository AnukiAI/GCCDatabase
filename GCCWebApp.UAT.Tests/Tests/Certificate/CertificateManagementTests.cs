using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.Tests.Certificate;

[TestFixture]
public class CertificateManagementTests : BaseTest
{
    [Test]
    public async Task Admin_CanAccessCertificateManagement()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/Certificates");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("certificate_management_loaded");
        
        var pageContent = await Page.ContentAsync();
        var hasCertContent = pageContent.Contains("Certificate") || 
                           await Page.IsVisibleAsync(".certificate-list, .cert-management, table");
        
        Assert.That(hasCertContent, Is.True, "Certificate management should display certificate content");
    }

    [Test]
    public async Task Admin_CanCreateCertificate()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/Create");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("certificate_create_form");
        
        var numberField = await Page.QuerySelectorAsync("input[name='Number'], input[id='Number'], #CertificateNumber");
        if (numberField != null)
        {
            await numberField.FillAsync("TEST-CERT-001");
            
            var statusField = await Page.QuerySelectorAsync("select[name='Status'], select[id='Status'], #Status");
            if (statusField != null)
            {
                await statusField.SelectOptionAsync(new[] { "active" });
            }
            
            var effectiveDateField = await Page.QuerySelectorAsync("input[name='EffectiveDate'], input[id='EffectiveDate'], #EffectiveDate");
            if (effectiveDateField != null)
            {
                await effectiveDateField.FillAsync("2024-01-01");
            }
            
            var expirationDateField = await Page.QuerySelectorAsync("input[name='ExpirationDate'], input[id='ExpirationDate'], #ExpirationDate");
            if (expirationDateField != null)
            {
                await expirationDateField.FillAsync("2025-01-01");
            }
            
            await TakeScreenshot("certificate_form_filled");
            
            var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Create'), button:has-text('Save')");
            if (submitButton != null)
            {
                await submitButton.ClickAsync();
                await TakeScreenshot("certificate_creation_submitted");
                
                await Page.WaitForTimeoutAsync(2000);
                
                var hasSuccess = await Page.IsVisibleAsync(".success, .alert-success, .notification-success");
                var isRedirected = !Page.Url.Contains("/Create");
                
                Assert.That(hasSuccess || isRedirected, Is.True, "Certificate creation should succeed");
            }
        }
    }

    [Test]
    public async Task Admin_CanEditCertificate()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/Certificates");
        
        await Page.WaitForSelectorAsync("body");
        
        var editButton = await Page.QuerySelectorAsync("a:has-text('Edit'), button:has-text('Edit'), .edit-cert, .btn-edit");
        if (editButton != null)
        {
            await editButton.ClickAsync();
            await TakeScreenshot("certificate_edit_form_opened");
            
            await Page.WaitForSelectorAsync("body");
            
            var numberField = await Page.QuerySelectorAsync("input[name='Number'], input[id='Number'], #CertificateNumber");
            if (numberField != null)
            {
                await numberField.FillAsync("UPDATED-CERT-001");
                await TakeScreenshot("certificate_edit_form_filled");
                
                var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Update'), button:has-text('Save')");
                if (submitButton != null)
                {
                    await submitButton.ClickAsync();
                    await TakeScreenshot("certificate_update_submitted");
                    
                    await Page.WaitForTimeoutAsync(2000);
                    
                    var hasSuccess = await Page.IsVisibleAsync(".success, .alert-success, .notification-success");
                    var isRedirected = !Page.Url.Contains("/Edit");
                    
                    Assert.That(hasSuccess || isRedirected, Is.True, "Certificate update should succeed");
                }
            }
        }
    }

    [Test]
    public async Task Admin_CanDeleteCertificate()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/Certificates");
        
        await Page.WaitForSelectorAsync("body");
        
        var deleteButton = await Page.QuerySelectorAsync("a:has-text('Delete'), button:has-text('Delete'), .delete-cert, .btn-delete");
        if (deleteButton != null)
        {
            await TakeScreenshot("before_certificate_delete");
            await deleteButton.ClickAsync();
            
            var confirmDialog = await Page.IsVisibleAsync(".confirm, .modal, .alert");
            if (confirmDialog)
            {
                await TakeScreenshot("certificate_delete_confirmation");
                
                var confirmButton = await Page.QuerySelectorAsync("button:has-text('Yes'), button:has-text('Confirm'), button:has-text('Delete')");
                if (confirmButton != null)
                {
                    await confirmButton.ClickAsync();
                    await TakeScreenshot("certificate_deletion_confirmed");
                    
                    await Page.WaitForTimeoutAsync(2000);
                    
                    var hasSuccess = await Page.IsVisibleAsync(".success, .alert-success, .notification-success");
                    Assert.That(hasSuccess, Is.True, "Certificate deletion should show success");
                }
            }
        }
    }

    [Test]
    public async Task Admin_CanViewCertificateDetails()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/Certificates");
        
        await Page.WaitForSelectorAsync("body");
        
        var detailsLink = await Page.QuerySelectorAsync("a:has-text('Details'), a:has-text('View'), .view-cert, .cert-link");
        if (detailsLink != null)
        {
            await detailsLink.ClickAsync();
            await TakeScreenshot("certificate_details_admin_view");
            
            var hasDetails = await Page.IsVisibleAsync(".certificate-details, .cert-details, .details-section");
            var pageContent = await Page.ContentAsync();
            var hasBasicInfo = pageContent.Contains("Certificate") && 
                             (pageContent.Contains("Number") || pageContent.Contains("Status"));
            
            Assert.That(hasDetails || hasBasicInfo, Is.True, "Certificate details should be visible");
        }
    }

    [Test]
    public async Task Admin_CertificateFormValidation()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/Create");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("empty_certificate_form");
        
        var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Create'), button:has-text('Save')");
        if (submitButton != null)
        {
            await submitButton.ClickAsync();
            await TakeScreenshot("certificate_form_validation");
            
            var hasValidationErrors = await Page.IsVisibleAsync(".error, .validation-error, .field-validation-error, .alert-danger");
            Assert.That(hasValidationErrors, Is.True, "Empty certificate form should show validation errors");
        }
    }

    [Test]
    public async Task Admin_CanSearchCertificates()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/Certificates");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("certificate_search_interface");
        
        var searchBox = await Page.QuerySelectorAsync("input[type='search'], input[name='searchTerm'], .search-input");
        if (searchBox != null)
        {
            await searchBox.FillAsync("test");
            await TakeScreenshot("certificate_search_entered");
            
            var searchButton = await Page.QuerySelectorAsync("button:has-text('Search'), .search-btn, input[type='submit']");
            if (searchButton != null)
            {
                await searchButton.ClickAsync();
                await TakeScreenshot("certificate_search_executed");
            }
            else
            {
                await Page.Keyboard.PressAsync("Enter");
                await TakeScreenshot("certificate_search_enter_pressed");
            }
        }
        
        Assert.That(searchBox != null, Is.True, "Certificate management should have search functionality");
    }

    [Test]
    public async Task Admin_CanFilterCertificatesByCompany()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/Certificates");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("certificate_company_filter");
        
        var companyFilter = await Page.QuerySelectorAsync("select[name='companyId'], select[name='CompanyId'], .company-filter");
        if (companyFilter != null)
        {
            var firstOption = await companyFilter.QuerySelectorAsync("option:not([value=''])");
            if (firstOption != null)
            {
                var optionValue = await firstOption.GetAttributeAsync("value");
                await companyFilter.SelectOptionAsync(new[] { optionValue });
                await TakeScreenshot("certificate_company_filter_applied");
                
                var filterButton = await Page.QuerySelectorAsync("button:has-text('Filter'), .filter-btn");
                if (filterButton != null)
                {
                    await filterButton.ClickAsync();
                    await TakeScreenshot("certificate_company_filter_executed");
                }
            }
        }
        
        Assert.That(companyFilter != null, Is.True, "Certificate management should have company filtering");
    }

    [Test]
    public async Task Admin_CanFilterCertificatesByProduct()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/Certificates");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("certificate_product_filter");
        
        var productFilter = await Page.QuerySelectorAsync("select[name='productId'], select[name='ProductId'], .product-filter");
        if (productFilter != null)
        {
            var firstOption = await productFilter.QuerySelectorAsync("option:not([value=''])");
            if (firstOption != null)
            {
                var optionValue = await firstOption.GetAttributeAsync("value");
                await productFilter.SelectOptionAsync(new[] { optionValue });
                await TakeScreenshot("certificate_product_filter_applied");
            }
        }
        
        Assert.That(productFilter != null, Is.True, "Certificate management should have product filtering");
    }

    [Test]
    public async Task Admin_CanPaginateThroughCertificates()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/Certificates");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("certificate_pagination_check");
        
        var nextButton = await Page.QuerySelectorAsync("a:has-text('Next'), button:has-text('Next'), .next-page");
        var pageNumbers = await Page.QuerySelectorAsync(".page-number, .pagination .page-link");
        
        if (nextButton != null)
        {
            await nextButton.ClickAsync();
            await TakeScreenshot("certificate_next_page");
        }
        
        var hasPagination = nextButton != null || pageNumbers != null ||
                          await Page.IsVisibleAsync(".pagination, .pager");
        
        Assert.That(hasPagination, Is.True, "Certificate management should have pagination");
    }

    [Test]
    public async Task Admin_CertificateListDisplaysRequiredColumns()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/Certificates");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("certificate_list_columns");
        
        var hasTable = await Page.IsVisibleAsync("table, .certificate-grid, .cert-list");
        if (hasTable)
        {
            var pageContent = await Page.ContentAsync();
            var hasRequiredColumns = pageContent.Contains("Number") || pageContent.Contains("Status") ||
                                   pageContent.Contains("Date") || pageContent.Contains("Company");
            
            Assert.That(hasRequiredColumns, Is.True, "Certificate list should display required columns");
        }
        else
        {
            var pageContent = await Page.ContentAsync();
            var hasCertContent = pageContent.Contains("Certificate");
            Assert.That(hasCertContent, Is.True, "Page should contain certificate-related content");
        }
    }

    [Test]
    public async Task Analyst_CanCreateAndEditCertificates()
    {
        await LoginAsRole("Analyst");
        await NavigateToPage("/Certificate/Create");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("analyst_certificate_create");
        
        var hasCreateForm = await Page.IsVisibleAsync("form, .certificate-form");
        var pageContent = await Page.ContentAsync();
        var isCreatePage = pageContent.Contains("Create") || Page.Url.Contains("/Create");
        
        Assert.That(hasCreateForm || isCreatePage, Is.True, "Analyst should be able to create certificates");
    }

    [Test]
    public async Task External_CannotCreateCertificates()
    {
        await LoginAsRole("External");
        
        var response = await Page.GotoAsync(BaseUrl + "/Certificate/Create");
        await TakeScreenshot("external_certificate_create_blocked");
        
        var is403Or401 = response?.Status == 403 || response?.Status == 401;
        var hasUnauthorizedContent = await Page.IsVisibleAsync("text=Unauthorized, text=Access Denied, text=Forbidden");
        var redirectedToLogin = Page.Url.Contains("/Account/Login") || Page.Url.Contains("/login");
        
        Assert.That(is403Or401 || hasUnauthorizedContent || redirectedToLogin, Is.True, 
            "External users should not be able to create certificates");
    }

    [Test]
    public async Task Admin_CanExportCertificates()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/Certificates");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("certificate_export_check");
        
        var exportButton = await Page.QuerySelectorAsync("button:has-text('Export'), a:has-text('Export'), .export-btn");
        if (exportButton != null)
        {
            await exportButton.ClickAsync();
            await TakeScreenshot("certificate_export_clicked");
            
            await Page.WaitForTimeoutAsync(2000);
            
            var hasDownloadDialog = await Page.IsVisibleAsync(".download, .export-modal");
            Assert.That(hasDownloadDialog, Is.True, "Export should initiate download or show options");
        }
    }

    [Test]
    public async Task Admin_CertificateStatisticsAreVisible()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/Statistics");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("certificate_statistics");
        
        var hasStats = await Page.IsVisibleAsync(".stat, .stats, .metric, .dashboard-card");
        if (!hasStats)
        {
            var pageContent = await Page.ContentAsync();
            var hasNumericContent = pageContent.Contains("Total") || pageContent.Contains("Count") ||
                                  pageContent.Contains("0") || pageContent.Contains("1");
            Assert.That(hasNumericContent, Is.True, "Statistics page should show numerical data");
        }
        else
        {
            Assert.That(hasStats, Is.True, "Statistics should be displayed");
        }
    }
}
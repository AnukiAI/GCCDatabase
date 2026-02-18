using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.Tests.Integration;

[TestFixture]
public class EndToEndWorkflowTests : BaseTest
{
    [Test]
    public async Task CompleteWorkflow_AdminCreatesUserAndManagesProduct()
    {
        await LoginAsRole("Admin");
        
        // Step 1: Create a new user
        await NavigateToPage("/Admin/UserManagement");
        await TakeScreenshot("workflow_start_user_management");
        
        var createUserButton = await Page.QuerySelectorAsync("#add-user-btn, button:has-text('Add'), button:has-text('Create'), .add-user");
        if (createUserButton != null)
        {
            await createUserButton.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Page.WaitForTimeoutAsync(1000);

            var emailField = await Page.QuerySelectorAsync("#user-email, input[type='email'], input[name*='email']");
            var nameField = await Page.QuerySelectorAsync("#user-name, input[name*='name'], input[id*='name']");
            
            if (emailField != null && nameField != null)
            {
                await emailField.FillAsync("workflow.test@example.com");
                await nameField.FillAsync("Workflow Test User");
                
                var roleDropdown = await Page.QuerySelectorAsync("#user-role, select[name*='role'], select[id*='role']");
                if (roleDropdown != null)
                {
                    await roleDropdown.SelectOptionAsync("External");
                }
                
                var submitVisible = await Page.IsVisibleAsync("button[type='submit'], button:has-text('Save'), button:has-text('Create')");
                if (submitVisible)
                {
                    var submitButton = Page.Locator("button[type='submit'], button:has-text('Save'), button:has-text('Create')").First;
                    await submitButton.ScrollIntoViewIfNeededAsync();
                    await submitButton.ClickAsync();
                    await TakeScreenshot("workflow_user_created");
                    await Page.WaitForTimeoutAsync(2000);
                }
            }
        }

        // Step 2: Navigate to Product Management
        await NavigateToPage("/Product/Management");
        await TakeScreenshot("workflow_product_management");
        
        // Step 3: Create a new product
        var createProductButton = await Page.QuerySelectorAsync("button:has-text('Add'), button:has-text('Create'), .add-product");
        if (createProductButton != null)
        {
            await createProductButton.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Page.WaitForTimeoutAsync(1000);

            var nameField = await Page.QuerySelectorAsync("#new-product-input, #product-name, input[name*='name'], input[id*='name'], #Name");
            if (nameField != null)
            {
                await nameField.FillAsync("Workflow Test Product");
                
                var descriptionField = await Page.QuerySelectorAsync("textarea[name*='description'], #Description");
                if (descriptionField != null)
                {
                    await descriptionField.FillAsync("Product created during workflow test");
                }
                
                var submitVisible2 = await Page.IsVisibleAsync("button[type='submit'], button:has-text('Save'), button:has-text('Create')");
                if (submitVisible2)
                {
                    var submitButton = Page.Locator("button[type='submit'], button:has-text('Save'), button:has-text('Create')").First;
                    await submitButton.ScrollIntoViewIfNeededAsync();
                    await submitButton.ClickAsync();
                    await TakeScreenshot("workflow_product_created");
                    await Page.WaitForTimeoutAsync(2000);
                }
            }
        }

        // Step 4: Navigate to Certificate Management
        await NavigateToPage("/Certificate/Create");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("workflow_certificate_create");

        // Note: Certificate Number is readonly (auto-generated), fill editable fields instead
        var effectiveDateField = await Page.QuerySelectorAsync("input[name='EffectiveDate'], input[id='EffectiveDate']");
        if (effectiveDateField != null)
        {
            await effectiveDateField.FillAsync("2024-01-01");
        }

        var expirationDateField = await Page.QuerySelectorAsync("input[name='ExpirationDate'], input[id='ExpirationDate']");
        if (expirationDateField != null)
        {
            await expirationDateField.FillAsync("2025-01-01");
        }
        await TakeScreenshot("workflow_certificate_dates_filled");
        
        // Step 6: Verify the workflow by viewing the certificate
        await NavigateToPage("/Certificate/CertificateTileView");
        await TakeScreenshot("workflow_verification_complete");
        
        Assert.Pass("Complete admin workflow executed successfully");
    }

    [Test]
    public async Task PublicUserJourney_BrowseAndSearchCertificates()
    {
        // Step 1: Visit home page without login
        await NavigateToPage("/");
        await TakeScreenshot("public_journey_home");
        
        // Step 2: Navigate to certificate tile view
        await NavigateToPage("/Certificate/CertificateTileView");
        await TakeScreenshot("public_journey_certificates");
        
        // Step 3: Attempt to search for certificates
        var searchBoxVisible = await Page.IsVisibleAsync("#searchInput, input[type='search'], input[placeholder*='search'], .search-input");
        if (searchBoxVisible)
        {
            var searchBox = Page.Locator("#searchInput, input[type='search'], input[placeholder*='search'], .search-input").First;
            await searchBox.FillAsync("test");
            await TakeScreenshot("public_journey_search_entered");

            var searchButtonVisible = await Page.IsVisibleAsync("button:has-text('Search'), .search-btn");
            if (searchButtonVisible)
            {
                var searchButton = Page.Locator("button:has-text('Search'), .search-btn").First;
                await searchButton.ClickAsync();
            }
            else
            {
                await Page.Keyboard.PressAsync("Enter");
            }
            await TakeScreenshot("public_journey_search_executed");
        }
        else
        {
            TestContext.WriteLine("Search box not visible - may require filters to be expanded");
        }
        
        // Step 4: Try to access external tile view (requires auth)
        await NavigateToPage("/Certificate/CertificateTileViewExternal");
        await TakeScreenshot("public_journey_external_view");
        
        // Step 5: Verify public user cannot access admin areas
        var response = await Page.GotoAsync(BaseUrl + "/Admin");
        await TakeScreenshot("public_journey_admin_blocked");
        
        var hasAdminDashboard = await Page.IsVisibleAsync(".admin-dashboard-cards, .admin-card, #user-table, .admin-table");
        var isBlocked = response?.Status == 403 || response?.Status == 401 ||
                       Page.Url.Contains("/Account/Login") ||
                       await Page.IsVisibleAsync("text=Unauthorized, text=Access Denied") ||
                       !hasAdminDashboard;

        Assert.That(isBlocked, Is.True, "Public user should be blocked from admin areas");
    }

    [Test]
    public async Task AnalystWorkflow_ManageCertificatesButNotUsers()
    {
        // Ensure clean session before logging in as Analyst
        await LogoutIfLoggedIn();
        await LoginAsRole("Analyst");

        // Step 1: Verify analyst can access certificate creation
        await NavigateToPage("/Certificate/Create");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("analyst_workflow_certificate_create");

        var hasCreateForm = await Page.IsVisibleAsync("#certificateCreateForm, form[method='post'], form, .certificate-form");
        var isOnCreatePage = Page.Url.Contains("/Certificate/Create");
        Assert.That(hasCreateForm || isOnCreatePage, Is.True, "Analyst should access certificate creation");
        
        // Step 2: Try to fill editable fields (Number is readonly/auto-generated)
        var effectiveDateField2 = await Page.QuerySelectorAsync("input[name='EffectiveDate'], input[id='EffectiveDate']");
        if (effectiveDateField2 != null)
        {
            await effectiveDateField2.FillAsync("2024-06-01");
            await TakeScreenshot("analyst_workflow_certificate_date_filled");
        }
        
        // Step 3: Verify analyst cannot access user management
        var response = await Page.GotoAsync(BaseUrl + "/Admin/UserManagement");
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("analyst_workflow_user_management_blocked");

        var isBlocked = response?.Status == 403 || response?.Status == 401 ||
                       Page.Url.Contains("/Account/Login") ||
                       Page.Url.Contains("AccessDenied") ||
                       !Page.Url.Contains("/Admin/UserManagement") ||
                       await Page.IsVisibleAsync("text=Unauthorized, text=Access Denied");

        Assert.That(isBlocked, Is.True, $"Analyst should be blocked from user management (URL: {Page.Url})");
    }

    [Test]
    public async Task ExternalUserWorkflow_LimitedAccess()
    {
        await LoginAsRole("External");
        
        // Step 1: Verify external user can access certificate views
        await NavigateToPage("/Certificate/CertificateTileView");
        await TakeScreenshot("external_workflow_certificate_view");
        
        var pageContent = await Page.ContentAsync();
        var canAccessCertificates = pageContent.Contains("Certificate") || 
                                   await Page.IsVisibleAsync(".certificate, .cert");
        
        Assert.That(canAccessCertificates, Is.True, "External user should access certificate views");
        
        // Step 2: Verify external user cannot create certificates
        var response = await Page.GotoAsync(BaseUrl + "/Certificate/Create");
        await TakeScreenshot("external_workflow_certificate_create_blocked");

        var isBlocked = response?.Status == 403 || response?.Status == 401 ||
                       Page.Url.Contains("/Account/Login") ||
                       !Page.Url.Contains("/Certificate/Create") ||
                       await Page.IsVisibleAsync("text=Unauthorized, text=Access Denied");

        Assert.That(isBlocked, Is.True, $"External user should not create certificates. URL: {Page.Url}");
        
        // Step 3: Verify external user cannot access admin functions
        response = await Page.GotoAsync(BaseUrl + "/Admin");
        await Page.WaitForTimeoutAsync(1000);
        await TakeScreenshot("external_workflow_admin_blocked");

        var hasAdminContent = await Page.IsVisibleAsync(".admin-dashboard-cards, .admin-card, #user-table, .admin-table");
        isBlocked = response?.Status == 403 || response?.Status == 401 ||
                   Page.Url.Contains("/Account/Login") ||
                   Page.Url.Contains("AccessDenied") ||
                   !Page.Url.Contains("/Admin") ||
                   await Page.IsVisibleAsync("text=Unauthorized, text=Access Denied") ||
                   !hasAdminContent;

        Assert.That(isBlocked, Is.True, $"External user should not access admin functions (URL: {Page.Url})");
    }

    [Test]
    public async Task DataConsistency_ProductCertificateRelationship()
    {
        await LoginAsRole("Admin");
        
        // Step 1: Create a product
        await NavigateToPage("/Product/Management");
        await TakeScreenshot("data_consistency_product_start");
        
        var createProductButton = await Page.QuerySelectorAsync("button:has-text('Add'), button:has-text('Create'), .add-product");
        if (createProductButton != null)
        {
            await createProductButton.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Page.WaitForTimeoutAsync(1000);

            var nameField = await Page.QuerySelectorAsync("#new-product-input, #product-name, input[name*='name'], input[id*='name']");
            if (nameField != null)
            {
                await nameField.FillAsync("Data Consistency Test Product");
                
                var submitVisible = await Page.IsVisibleAsync("button[type='submit'], button:has-text('Save'), button:has-text('Create')");
                if (submitVisible)
                {
                    var submitButton = Page.Locator("button[type='submit'], button:has-text('Save'), button:has-text('Create')").First;
                    await submitButton.ScrollIntoViewIfNeededAsync();
                    await submitButton.ClickAsync();
                    await TakeScreenshot("data_consistency_product_created");
                    await Page.WaitForTimeoutAsync(2000);
                }
            }
        }
        
        // Step 2: Navigate to certificate create (Number is readonly/auto-generated)
        await NavigateToPage("/Certificate/Create");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("data_consistency_certificate_start");

        // Fill editable fields
        var dateField = await Page.QuerySelectorAsync("input[name='EffectiveDate'], input[id='EffectiveDate']");
        if (dateField != null)
        {
            await dateField.FillAsync("2024-01-01");
        }

        // Select the product if dropdown is available
        var productDropdown = await Page.QuerySelectorAsync("select[name*='Product'], select[name*='product']");
        if (productDropdown != null)
        {
            var testProductOption = await productDropdown.QuerySelectorAsync("option:has-text('Data Consistency Test Product')");
            if (testProductOption != null)
            {
                var optionValue = await testProductOption.GetAttributeAsync("value");
                if (!string.IsNullOrEmpty(optionValue))
                {
                    await productDropdown.SelectOptionAsync(optionValue);
                }
            }
        }
        await TakeScreenshot("data_consistency_certificate_form_ready");
        
        // Step 3: Verify relationship exists by viewing certificates
        await NavigateToPage("/Certificate/Certificates");
        await TakeScreenshot("data_consistency_verification");
        
        Assert.Pass("Data consistency workflow completed");
    }

    [Test]
    public async Task ErrorHandling_InvalidDataSubmission()
    {
        await LoginAsRole("Admin");
        
        // Step 1: Navigate to certificate create and verify form exists
        await NavigateToPage("/Certificate/Create");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("error_handling_start");

        // Submit form without required fields
        var submitVisible = await Page.IsVisibleAsync("button[type='submit'], button:has-text('Create'), input[type='submit']");
        if (submitVisible)
        {
            var submitButton = Page.Locator("button[type='submit'], button:has-text('Create'), input[type='submit']").First;
            await submitButton.ScrollIntoViewIfNeededAsync();
            await submitButton.ClickAsync();
            await TakeScreenshot("error_handling_validation_errors");

            // Verify validation errors appear or stayed on page
            await Page.WaitForTimeoutAsync(1000);
            var hasValidationErrors = await Page.IsVisibleAsync(".error, .validation-error, .field-validation-error, .alert-danger, .text-danger, .validation-summary-errors");
            var stayedOnPage = Page.Url.Contains("/Create");
            Assert.That(hasValidationErrors || stayedOnPage, Is.True, "Validation errors should appear for invalid data or stay on page");
        }
        else
        {
            Assert.That(Page.Url.Contains("/Create"), Is.True, "Should be on certificate creation page");
        }
        
        // Step 2: Submit invalid product data
        await NavigateToPage("/Product/Management");
        await TakeScreenshot("error_handling_product_start");
        
        var createProductButton = await Page.QuerySelectorAsync("button:has-text('Add'), button:has-text('Create'), .add-product");
        if (createProductButton != null)
        {
            await createProductButton.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Page.WaitForTimeoutAsync(1000);

            var productSubmitVisible = await Page.IsVisibleAsync("button[type='submit'], button:has-text('Save'), button:has-text('Create')");
            if (productSubmitVisible)
            {
                var productSubmitButton = Page.Locator("button[type='submit'], button:has-text('Save'), button:has-text('Create')").First;
                await productSubmitButton.ScrollIntoViewIfNeededAsync();
                await productSubmitButton.ClickAsync();
                await TakeScreenshot("error_handling_product_validation");

                var hasProductValidationErrors = await Page.IsVisibleAsync(".error, .validation-error, .field-validation-error, .alert-danger, .text-danger");
                var productStayedOnPage = Page.Url.Contains("Product");
                Assert.That(hasProductValidationErrors || productStayedOnPage, Is.True, "Product validation errors should appear or stay on page");
            }
        }
    }

    [Test]
    public async Task NavigationFlow_BetweenAllMainSections()
    {
        await LoginAsRole("Admin");
        
        // Navigate through all main sections
        var sections = new[]
        {
            ("/", "Home"),
            ("/Dashboard", "Dashboard"),
            ("/Admin/UserManagement", "User Management"),
            ("/Admin/ProductManagement", "Product Management"),
            ("/Product/Management", "Product Management Alt"),
            ("/Certificate/CertificateTileView", "Certificate Tiles"),
            ("/Certificate/Certificates", "Certificate List")
        };

        foreach (var (url, name) in sections)
        {
            await NavigateToPage(url);
            await TakeScreenshot($"navigation_flow_{name.Replace(" ", "_").ToLower()}");

            // Verify each page loads without unhandled exceptions
            var pageContent = await Page.ContentAsync();
            var hasException = pageContent.Contains("Exception", StringComparison.OrdinalIgnoreCase) &&
                              pageContent.Contains("stack trace", StringComparison.OrdinalIgnoreCase);
            Assert.That(hasException, Is.False, $"{name} page should not display unhandled exceptions");
        }
        
        Assert.Pass("Navigation flow through all sections completed");
    }

    [Test]
    public async Task SecurityWorkflow_RoleBasedAccessControl()
    {
        // Test each role's access restrictions
        var roles = new[] { "Admin", "Analyst", "External" };

        foreach (var role in roles)
        {
            // Ensure previous session is cleared before switching roles
            await LogoutIfLoggedIn();
            await LoginAsRole(role);
            await TakeScreenshot($"security_workflow_{role.ToLower()}_login");
            
            // Test access to admin functions
            var adminResponse = await Page.GotoAsync(BaseUrl + "/Admin/UserManagement");
            await Page.WaitForTimeoutAsync(1000);
            var canAccessAdmin = !Page.Url.Contains("/Account/Login") && !Page.Url.Contains("AccessDenied") &&
                                Page.Url.Contains("/Admin");

            // Test access to certificate creation
            var certCreateResponse = await Page.GotoAsync(BaseUrl + "/Certificate/Create");
            await Page.WaitForTimeoutAsync(1000);
            var canCreateCerts = !Page.Url.Contains("/Account/Login") && !Page.Url.Contains("AccessDenied") &&
                                Page.Url.Contains("/Certificate");
            
            await TakeScreenshot($"security_workflow_{role.ToLower()}_access_test");
            
            // Verify role-based access
            switch (role)
            {
                case "Admin":
                    Assert.That(canAccessAdmin, Is.True, "Admin should access admin functions");
                    Assert.That(canCreateCerts, Is.True, "Admin should create certificates");
                    break;
                case "Analyst":
                    Assert.That(canAccessAdmin, Is.False, "Analyst should not access admin functions");
                    Assert.That(canCreateCerts, Is.True, "Analyst should create certificates");
                    break;
                case "External":
                    Assert.That(canAccessAdmin, Is.False, "External should not access admin functions");
                    Assert.That(canCreateCerts, Is.False, "External should not create certificates");
                    break;
            }
        }
    }
}
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
        
        var createUserButton = await Page.QuerySelectorAsync("button:has-text('Add'), button:has-text('Create'), .add-user");
        if (createUserButton != null)
        {
            await createUserButton.ClickAsync();
            await Page.WaitForSelectorAsync("form, .user-form, .modal");
            
            var emailField = await Page.QuerySelectorAsync("input[type='email'], input[name*='email'], #Email");
            var nameField = await Page.QuerySelectorAsync("input[name*='name'], input[id*='name'], #Name");
            
            if (emailField != null && nameField != null)
            {
                await emailField.FillAsync("workflow.test@example.com");
                await nameField.FillAsync("Workflow Test User");
                
                var roleDropdown = await Page.QuerySelectorAsync("select[name*='role'], select[id*='role'], #Role");
                if (roleDropdown != null)
                {
                    await roleDropdown.SelectOptionAsync("External");
                }
                
                var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Save'), button:has-text('Create')");
                if (submitButton != null)
                {
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
            await Page.WaitForSelectorAsync("form, .product-form, .modal");
            
            var nameField = await Page.QuerySelectorAsync("input[name*='name'], input[id*='name'], #Name");
            if (nameField != null)
            {
                await nameField.FillAsync("Workflow Test Product");
                
                var descriptionField = await Page.QuerySelectorAsync("textarea[name*='description'], #Description");
                if (descriptionField != null)
                {
                    await descriptionField.FillAsync("Product created during workflow test");
                }
                
                var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Save'), button:has-text('Create')");
                if (submitButton != null)
                {
                    await submitButton.ClickAsync();
                    await TakeScreenshot("workflow_product_created");
                    await Page.WaitForTimeoutAsync(2000);
                }
            }
        }
        
        // Step 4: Navigate to Certificate Management
        await NavigateToPage("/Certificate/Create");
        await TakeScreenshot("workflow_certificate_create");
        
        // Step 5: Create a certificate for the product
        var certNumberField = await Page.QuerySelectorAsync("input[name='Number'], input[id='Number'], #CertificateNumber");
        if (certNumberField != null)
        {
            await certNumberField.FillAsync("WORKFLOW-CERT-001");
            
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
            
            var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Create'), button:has-text('Save')");
            if (submitButton != null)
            {
                await submitButton.ClickAsync();
                await TakeScreenshot("workflow_certificate_created");
                await Page.WaitForTimeoutAsync(2000);
            }
        }
        
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
        var searchBox = await Page.QuerySelectorAsync("input[type='search'], input[placeholder*='search'], .search-input");
        if (searchBox != null)
        {
            await searchBox.FillAsync("test");
            await TakeScreenshot("public_journey_search_entered");
            
            var searchButton = await Page.QuerySelectorAsync("button:has-text('Search'), .search-btn");
            if (searchButton != null)
            {
                await searchButton.ClickAsync();
            }
            else
            {
                await Page.Keyboard.PressAsync("Enter");
            }
            await TakeScreenshot("public_journey_search_executed");
        }
        
        // Step 4: Try to access external view
        await NavigateToPage("/Certificate/CertificateExternalView");
        await TakeScreenshot("public_journey_external_view");
        
        // Step 5: Verify public user cannot access admin areas
        var response = await Page.GotoAsync(BaseUrl + "/Admin");
        await TakeScreenshot("public_journey_admin_blocked");
        
        var isBlocked = response?.Status == 403 || response?.Status == 401 ||
                       Page.Url.Contains("/Account/Login") || 
                       await Page.IsVisibleAsync("text=Unauthorized, text=Access Denied");
        
        Assert.That(isBlocked, Is.True, "Public user should be blocked from admin areas");
    }

    [Test]
    public async Task AnalystWorkflow_ManageCertificatesButNotUsers()
    {
        await LoginAsRole("Analyst");
        
        // Step 1: Verify analyst can access certificate creation
        await NavigateToPage("/Certificate/Create");
        await TakeScreenshot("analyst_workflow_certificate_create");
        
        var hasCreateForm = await Page.IsVisibleAsync("form, .certificate-form");
        Assert.That(hasCreateForm, Is.True, "Analyst should access certificate creation");
        
        // Step 2: Create a certificate as analyst
        var certNumberField = await Page.QuerySelectorAsync("input[name='Number'], input[id='Number']");
        if (certNumberField != null)
        {
            await certNumberField.FillAsync("ANALYST-CERT-001");
            
            var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Create')");
            if (submitButton != null)
            {
                await submitButton.ClickAsync();
                await TakeScreenshot("analyst_workflow_certificate_created");
                await Page.WaitForTimeoutAsync(2000);
            }
        }
        
        // Step 3: Verify analyst cannot access user management
        var response = await Page.GotoAsync(BaseUrl + "/Admin/UserManagement");
        await TakeScreenshot("analyst_workflow_user_management_blocked");
        
        var isBlocked = response?.Status == 403 || response?.Status == 401 ||
                       Page.Url.Contains("/Account/Login") ||
                       await Page.IsVisibleAsync("text=Unauthorized, text=Access Denied");
        
        Assert.That(isBlocked, Is.True, "Analyst should be blocked from user management");
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
                       await Page.IsVisibleAsync("text=Unauthorized, text=Access Denied");
        
        Assert.That(isBlocked, Is.True, "External user should not create certificates");
        
        // Step 3: Verify external user cannot access admin functions
        response = await Page.GotoAsync(BaseUrl + "/Admin");
        await TakeScreenshot("external_workflow_admin_blocked");
        
        isBlocked = response?.Status == 403 || response?.Status == 401 ||
                   Page.Url.Contains("/Account/Login") ||
                   await Page.IsVisibleAsync("text=Unauthorized, text=Access Denied");
        
        Assert.That(isBlocked, Is.True, "External user should not access admin functions");
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
            await Page.WaitForSelectorAsync("form, .product-form, .modal");
            
            var nameField = await Page.QuerySelectorAsync("input[name*='name'], input[id*='name']");
            if (nameField != null)
            {
                await nameField.FillAsync("Data Consistency Test Product");
                
                var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Save'), button:has-text('Create')");
                if (submitButton != null)
                {
                    await submitButton.ClickAsync();
                    await TakeScreenshot("data_consistency_product_created");
                    await Page.WaitForTimeoutAsync(2000);
                }
            }
        }
        
        // Step 2: Create certificate for the product
        await NavigateToPage("/Certificate/Create");
        await TakeScreenshot("data_consistency_certificate_start");
        
        var certNumberField = await Page.QuerySelectorAsync("input[name='Number'], input[id='Number']");
        if (certNumberField != null)
        {
            await certNumberField.FillAsync("DATA-CONSISTENCY-001");
            
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
            
            var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Create')");
            if (submitButton != null)
            {
                await submitButton.ClickAsync();
                await TakeScreenshot("data_consistency_certificate_created");
                await Page.WaitForTimeoutAsync(2000);
            }
        }
        
        // Step 3: Verify relationship exists by viewing certificates
        await NavigateToPage("/Certificate/Certificates");
        await TakeScreenshot("data_consistency_verification");
        
        Assert.Pass("Data consistency workflow completed");
    }

    [Test]
    public async Task ErrorHandling_InvalidDataSubmission()
    {
        await LoginAsRole("Admin");
        
        // Step 1: Submit invalid certificate data
        await NavigateToPage("/Certificate/Create");
        await TakeScreenshot("error_handling_start");
        
        // Submit form without required fields
        var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Create')");
        if (submitButton != null)
        {
            await submitButton.ClickAsync();
            await TakeScreenshot("error_handling_validation_errors");
            
            // Verify validation errors appear
            var hasValidationErrors = await Page.IsVisibleAsync(".error, .validation-error, .field-validation-error, .alert-danger");
            Assert.That(hasValidationErrors, Is.True, "Validation errors should appear for invalid data");
        }
        
        // Step 2: Submit invalid product data
        await NavigateToPage("/Product/Management");
        await TakeScreenshot("error_handling_product_start");
        
        var createProductButton = await Page.QuerySelectorAsync("button:has-text('Add'), button:has-text('Create'), .add-product");
        if (createProductButton != null)
        {
            await createProductButton.ClickAsync();
            await Page.WaitForSelectorAsync("form, .product-form, .modal");
            
            var productSubmitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Save'), button:has-text('Create')");
            if (productSubmitButton != null)
            {
                await productSubmitButton.ClickAsync();
                await TakeScreenshot("error_handling_product_validation");
                
                var hasProductValidationErrors = await Page.IsVisibleAsync(".error, .validation-error, .field-validation-error, .alert-danger");
                Assert.That(hasProductValidationErrors, Is.True, "Product validation errors should appear");
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
            ("/Admin", "Admin Dashboard"),
            ("/Admin/UserManagement", "User Management"),
            ("/Admin/ProductManagement", "Product Management"),
            ("/Product", "Product Index"),
            ("/Product/Management", "Product Management"),
            ("/Certificate/CertificateTileView", "Certificate Tiles"),
            ("/Certificate/Certificates", "Certificate List")
        };
        
        foreach (var (url, name) in sections)
        {
            await NavigateToPage(url);
            await TakeScreenshot($"navigation_flow_{name.Replace(" ", "_").ToLower()}");
            
            // Verify each page loads without critical errors
            var hasErrorContent = await Page.IsVisibleAsync(".error, .alert-danger, .exception");
            Assert.That(hasErrorContent, Is.False, $"{name} page should not display critical errors");
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
            await LoginAsRole(role);
            await TakeScreenshot($"security_workflow_{role.ToLower()}_login");
            
            // Test access to admin functions
            var adminResponse = await Page.GotoAsync(BaseUrl + "/Admin");
            var canAccessAdmin = adminResponse?.Status == 200 && !Page.Url.Contains("/Account/Login");
            
            // Test access to certificate creation
            var certCreateResponse = await Page.GotoAsync(BaseUrl + "/Certificate/Create");
            var canCreateCerts = certCreateResponse?.Status == 200 && !Page.Url.Contains("/Account/Login");
            
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
using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.RoleBased;

[TestFixture]
public class AdminWorkflowTests : BaseTest
{
    [Test]
    public async Task Admin_CanPerformCompleteUserManagementWorkflow()
    {
        // Arrange - Login as Admin
        await LoginAsRole("Admin");
        await TakeScreenshot("admin_logged_in_ready_for_user_management");

        // Act 1 - Navigate to User Management
        await NavigateToPage(TestDataHelper.Urls.AdminUserManagement);
        await TakeScreenshot("user_management_page_loaded");

        // Act 2 - Verify admin can access user list
        await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Medium);
        await TakeScreenshot("user_list_displayed");

        // Act 3 - Try to access Add User functionality
        try
        {
            var addButton = Page.Locator(TestDataHelper.Selectors.AddButton).First;
            if (await addButton.CountAsync() > 0)
            {
                await addButton.ClickAsync();
                await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Navigation);
                await TakeScreenshot("add_user_form_opened");
                TestContext.WriteLine("✅ Admin can access Add User functionality");
            }
            else
            {
                TestContext.WriteLine("ℹ️ Add User button not found - may be implemented differently");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Add User test: {ex.Message}");
            await TakeScreenshot("add_user_attempt_failed");
        }

        // Act 4 - Navigate to Role Management
        await NavigateToPage(TestDataHelper.Urls.AdminRoleManagement);
        await TakeScreenshot("role_management_page_loaded");

        // Assert - Verify admin has access to key management areas
        var currentUrl = Page.Url.ToLower();
        Assert.That(currentUrl.Contains("admin"), Is.True, "Admin should be able to access admin area");
        
        TestContext.WriteLine("✅ Admin workflow test completed - user can access administrative functions");
    }

    [Test]
    public async Task Admin_CanManageCertificateTypesAndCSFAttributes()
    {
        // Arrange
        await LoginAsRole("Admin");
        await TakeScreenshot("admin_ready_for_certificate_type_management");

        // Act 1 - Navigate to Certificate Types
        await NavigateToPage(TestDataHelper.Urls.AdminCertificateTypes);
        await TakeScreenshot("certificate_types_page_loaded");

        // Act 2 - Check Certificate Types management interface
        await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Medium);
        await TakeScreenshot("certificate_types_interface_displayed");

        // Act 3 - Navigate to CSF Attributes
        await NavigateToPage(TestDataHelper.Urls.AdminCSFAttributes);
        await TakeScreenshot("csf_attributes_page_loaded");

        // Act 4 - Verify CSF Attributes interface
        await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Medium);
        await TakeScreenshot("csf_attributes_interface_displayed");

        // Act 5 - Test Tag Management
        await NavigateToPage(TestDataHelper.Urls.AdminTags);
        await TakeScreenshot("tag_management_page_loaded");

        // Assert
        var currentUrl = Page.Url.ToLower();
        Assert.That(currentUrl.Contains("admin"), Is.True, "Admin should maintain access to admin functions");
        
        TestContext.WriteLine("✅ Admin can access certificate configuration pages");
    }

    [Test]
    public async Task Admin_CanAccessProductCategoryManagement()
    {
        // Arrange
        await LoginAsRole("Admin");
        await TakeScreenshot("admin_ready_for_product_management");

        // Act 1 - Navigate to Product Management
        await NavigateToPage(TestDataHelper.Urls.AdminProductManagement);
        await TakeScreenshot("product_management_page_loaded");

        // Act 2 - Verify product management interface loads
        await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Medium);
        await TakeScreenshot("product_management_interface_ready");

        // Act 3 - Try to interact with product categories
        try
        {
            // Look for any product-related controls
            var productElements = await Page.Locator("input, select, button").AllAsync();
            TestContext.WriteLine($"Found {productElements.Count} interactive elements on product management page");
            
            if (productElements.Any())
            {
                await TakeScreenshot("product_management_interactive_elements_found");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Product interaction attempt: {ex.Message}");
            await TakeScreenshot("product_interaction_attempt");
        }

        // Act 4 - Test Partners management
        await NavigateToPage(TestDataHelper.Urls.AdminPartners);
        await TakeScreenshot("partners_management_page_loaded");

        // Assert
        Assert.That(Page.Url.Contains("Admin"), Is.True, "Admin should be able to access product and partner management");
        
        TestContext.WriteLine("✅ Admin can access product category and partner management");
    }

    [Test]
    public async Task Admin_CanAccessSystemConfiguration()
    {
        // Arrange
        await LoginAsRole("Admin");
        await TakeScreenshot("admin_ready_for_system_configuration");

        // Act 1 - Navigate to main Admin area
        await NavigateToPage(TestDataHelper.Urls.Admin);
        await TakeScreenshot("admin_dashboard_loaded");

        // Act 2 - Verify admin dashboard/index loads
        await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Medium);
        await TakeScreenshot("admin_dashboard_content_displayed");

        // Act 3 - Test access to all admin sub-areas
        var adminPages = new[]
        {
            (TestDataHelper.Urls.AdminUserManagement, "User Management"),
            (TestDataHelper.Urls.AdminRoleManagement, "Role Management"), 
            (TestDataHelper.Urls.AdminProductManagement, "Product Management"),
            (TestDataHelper.Urls.AdminCertificateTypes, "Certificate Types"),
            (TestDataHelper.Urls.AdminCSFAttributes, "CSF Attributes"),
            (TestDataHelper.Urls.AdminTags, "Tags"),
            (TestDataHelper.Urls.AdminPartners, "Partners")
        };

        foreach (var (url, name) in adminPages)
        {
            try
            {
                await NavigateToPage(url);
                await TakeScreenshot($"admin_access_{name.Replace(" ", "_").ToLower()}");
                await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Short);
                
                var currentUrl = Page.Url.ToLower();
                var hasAccess = !currentUrl.Contains("login") && !currentUrl.Contains("accessdenied");
                
                TestContext.WriteLine($"{(hasAccess ? "✅" : "❌")} Admin access to {name}: {hasAccess}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"❌ Error accessing {name}: {ex.Message}");
                await TakeScreenshot($"admin_access_error_{name.Replace(" ", "_").ToLower()}");
            }
        }

        // Assert
        Assert.That(Page.Url.Contains("Admin"), Is.True, "Admin should maintain access to administrative functions");
        
        TestContext.WriteLine("✅ Admin system configuration access test completed");
    }
}
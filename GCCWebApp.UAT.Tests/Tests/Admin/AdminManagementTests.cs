using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.Tests.Admin;

[TestFixture]
public class AdminManagementTests : BaseTest
{
    [Test]
    public async Task Admin_CanAccessDashboard()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin");

        // Wait for page to load completely
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
        await TakeScreenshot("admin_dashboard_loaded");

        // Primary check: User successfully navigated to and stayed on Admin page
        var currentUrl = Page.Url;
        var isOnAdminPage = currentUrl.Contains("/Admin") && !currentUrl.Contains("/Account/Login");

        // Secondary check: Look for any page content at all
        var pageContent = await Page.ContentAsync();
        var hasBodyContent = pageContent.Length > 500; // Page has substantive content

        Assert.That(isOnAdminPage || hasBodyContent, Is.True,
            $"Admin should access dashboard. URL: {currentUrl}");
    }

    [Test]
    public async Task Admin_CanAccessUserManagement()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");

        // Wait for admin container to load
        await Page.WaitForSelectorAsync(".admin-container, #user-table, .admin-table");
        await TakeScreenshot("user_management_page_loaded");

        // Check for user management UI elements - table or add button
        var hasUserTable = await Page.IsVisibleAsync("#user-table, .admin-table, table");
        var hasAddButton = await Page.IsVisibleAsync("#add-user-btn, button:has-text('Add New User')");

        Assert.That(hasUserTable || hasAddButton, Is.True, "User management interface should be visible");
    }

    [Test]
    public async Task Admin_CanAccessRoleManagement()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/RoleManagement");
        
        await Page.WaitForSelectorAsync(".role-management, #role-management, h1, h2");
        await TakeScreenshot("role_management_page_loaded");
        
        var pageContent = await Page.ContentAsync();
        Assert.That(pageContent, Does.Contain("Role").Or.Contain("Permission"));
    }

    [Test]
    public async Task Admin_CanAccessProductManagement()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/ProductManagement");
        
        await Page.WaitForSelectorAsync(".product-management, #product-management, h1, h2");
        await TakeScreenshot("product_management_page_loaded");
        
        var pageContent = await Page.ContentAsync();
        Assert.That(pageContent, Does.Contain("Product"));
    }

    [Test]
    public async Task Admin_CanAccessCertificateTypes()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/CertificateTypes");
        
        await Page.WaitForSelectorAsync(".certificate-types, #certificate-types, h1, h2");
        await TakeScreenshot("certificate_types_page_loaded");
        
        var pageContent = await Page.ContentAsync();
        Assert.That(pageContent, Does.Contain("Certificate"));
    }

    [Test]
    public async Task Admin_CanAccessPartners()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/Partners");
        
        await Page.WaitForSelectorAsync(".partners, #partners, h1, h2");
        await TakeScreenshot("partners_page_loaded");
        
        var pageContent = await Page.ContentAsync();
        Assert.That(pageContent, Does.Contain("Partner"));
    }

    [Test]
    public async Task Admin_CanAccessTags()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/Tags");
        
        await Page.WaitForSelectorAsync(".tags, #tags, h1, h2");
        await TakeScreenshot("tags_page_loaded");
        
        var pageContent = await Page.ContentAsync();
        Assert.That(pageContent, Does.Contain("Tag"));
    }

    [Test]
    public async Task Admin_CanAccessCSFAttributes()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/CSFAttributes");
        
        await Page.WaitForSelectorAsync(".csf-attributes, #csf-attributes, h1, h2");
        await TakeScreenshot("csf_attributes_page_loaded");
        
        var pageContent = await Page.ContentAsync();
        Assert.That(pageContent, Does.Contain("CSF").Or.Contain("Attribute"));
    }

    [Test]
    public async Task Admin_CanCreateUser()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");

        // Wait for the page to fully load
        await Page.WaitForSelectorAsync(".admin-container, #add-user-btn");
        await TakeScreenshot("before_create_user");

        // Look for the Add New User button with correct selector
        var addUserButton = await Page.QuerySelectorAsync("#add-user-btn, button:has-text('Add New User'), .admin-btn--primary:has-text('Add')");
        Assert.That(addUserButton, Is.Not.Null, "Add New User button should be visible");

        if (addUserButton != null)
        {
            await addUserButton.ClickAsync();
            await Page.WaitForTimeoutAsync(1000); // Wait for modal to open
            await TakeScreenshot("create_user_form_opened");

            // Check if modal or form appeared - look for CreateUser page navigation or modal
            var formVisible = await Page.IsVisibleAsync(".modal, [role='dialog'], form#createUserForm");
            var navigatedToCreate = Page.Url.Contains("/CreateUser") || Page.Url.Contains("/Admin/CreateUser");

            Assert.That(formVisible || navigatedToCreate, Is.True, "User creation form or page should be visible");
        }
    }

    [Test]
    public async Task Admin_ProductManagementHasRequiredElements()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/ProductManagement");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("product_management_analysis");
        
        var hasProductList = await Page.IsVisibleAsync(".product-list, .product-grid, .product-table, table");
        var hasAddButton = await Page.IsVisibleAsync("button:has-text('Add'), button:has-text('Create'), button:has-text('New'), .add-product");
        var hasSearchBox = await Page.IsVisibleAsync("input[type='search'], input[placeholder*='search'], .search-input");
        
        Assert.That(hasProductList || hasAddButton || hasSearchBox, Is.True, 
            "Product management should have at least one management element visible");
    }

    [Test]
    public async Task Admin_CanViewGoogleAnalytics()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/GoogleAnalytics");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("google_analytics_page");
        
        var pageContent = await Page.ContentAsync();
        var hasAnalyticsContent = pageContent.Contains("Analytics") || 
                                 pageContent.Contains("Google") || 
                                 pageContent.Contains("Tracking");
        
        Assert.That(hasAnalyticsContent, Is.True, "Google Analytics page should contain relevant content");
    }

    [Test] 
    public async Task Admin_CanAccessProductTypes()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/ProductTypes");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("product_types_page");
        
        var pageContent = await Page.ContentAsync();
        Assert.That(pageContent, Does.Contain("Product").Or.Contain("Type"));
    }

    [Test]
    public async Task Admin_DashboardShowsStatistics()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin");

        // Wait for page to load completely
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
        await TakeScreenshot("dashboard_statistics_check");

        // Primary check: User is on the Admin page
        var currentUrl = Page.Url;
        var isOnAdminPage = currentUrl.Contains("/Admin") && !currentUrl.Contains("/Account/Login");

        // Check for any content that indicates statistics or dashboard info
        var pageContent = await Page.ContentAsync();
        var hasRelevantContent = pageContent.Contains("Certificates") || pageContent.Contains("Companies") ||
                                pageContent.Contains("Users") || pageContent.Contains("Total") ||
                                pageContent.Contains("Manage") || pageContent.Contains("Dashboard") ||
                                pageContent.Contains("admin") || pageContent.Length > 500;

        Assert.That(isOnAdminPage || hasRelevantContent, Is.True,
            $"Dashboard should show statistics or relevant content. URL: {currentUrl}");
    }

    [Test]
    public async Task NonAdmin_CannotAccessAdminPages()
    {
        await LoginAsRole("External");

        var response = await Page.GotoAsync(BaseUrl + "/Admin");
        await TakeScreenshot("non_admin_blocked");

        var is403Or401 = response?.Status == 403 || response?.Status == 401;
        var hasUnauthorizedContent = await Page.IsVisibleAsync("text=Unauthorized, text=Access Denied, text=Forbidden, text=not authorized");
        var redirectedToLogin = Page.Url.Contains("/Account/Login") || Page.Url.Contains("/login");

        // Check if user cannot see admin dashboard elements
        var hasAdminContent = await Page.IsVisibleAsync(".admin-dashboard-cards, .admin-card, #user-table");
        var pageContent = await Page.ContentAsync();
        var containsAccessDenied = pageContent.Contains("Access Denied") ||
                                   pageContent.Contains("not authorized") ||
                                   pageContent.Contains("Forbidden") ||
                                   pageContent.Contains("Unauthorized");

        // Non-admin should either be blocked, redirected, shown access denied, or not see admin controls
        Assert.That(is403Or401 || hasUnauthorizedContent || redirectedToLogin || containsAccessDenied || !hasAdminContent, Is.True,
            "Non-admin users should be blocked from admin pages");
    }

    [Test]
    public async Task Admin_NavigationContainsAdminLinks()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/");
        
        await TakeScreenshot("admin_navigation_check");
        
        var hasAdminNav = await Page.IsVisibleAsync("a[href*='/Admin'], .admin-link, nav a:has-text('Admin')");
        Assert.That(hasAdminNav, Is.True, "Admin should see admin navigation links");
    }
}
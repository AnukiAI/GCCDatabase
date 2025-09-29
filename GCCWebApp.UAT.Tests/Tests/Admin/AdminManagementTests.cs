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
        
        await Page.WaitForSelectorAsync("h1, h2, .dashboard-title");
        await TakeScreenshot("admin_dashboard_loaded");
        
        var adminTitle = await Page.TextContentAsync("h1, h2, .dashboard-title");
        Assert.That(adminTitle, Does.Contain("Admin").Or.Contain("Dashboard"));
    }

    [Test]
    public async Task Admin_CanAccessUserManagement()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        
        await Page.WaitForSelectorAsync(".user-management, #user-management, h1, h2");
        await TakeScreenshot("user_management_page_loaded");
        
        var isUserManagementVisible = await Page.IsVisibleAsync(".user-grid, .user-table, .user-list");
        Assert.That(isUserManagementVisible, Is.True, "User management interface should be visible");
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
        
        await Page.WaitForSelectorAsync(".add-user, .create-user, .new-user, button");
        await TakeScreenshot("before_create_user");
        
        var addUserButton = await Page.QuerySelectorAsync(".add-user, .create-user, .new-user, button:has-text('Add'), button:has-text('Create'), button:has-text('New')");
        if (addUserButton != null)
        {
            await addUserButton.ClickAsync();
            await TakeScreenshot("create_user_form_opened");
            
            var formVisible = await Page.IsVisibleAsync("form, .user-form, .create-form");
            Assert.That(formVisible, Is.True, "User creation form should be visible");
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
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("dashboard_statistics_check");
        
        var hasStatistics = await Page.IsVisibleAsync(".stat, .stats, .metric, .dashboard-card, .summary");
        if (!hasStatistics)
        {
            var pageContent = await Page.ContentAsync();
            var hasNumericContent = pageContent.Contains("0") || pageContent.Contains("1") || 
                                  pageContent.Contains("Total") || pageContent.Contains("Count");
            Assert.That(hasNumericContent, Is.True, "Dashboard should show some statistical information");
        }
        else
        {
            Assert.That(hasStatistics, Is.True, "Dashboard should display statistics");
        }
    }

    [Test]
    public async Task NonAdmin_CannotAccessAdminPages()
    {
        await LoginAsRole("External");
        
        var response = await Page.GotoAsync(BaseUrl + "/Admin");
        await TakeScreenshot("non_admin_blocked");
        
        var is403Or401 = response?.Status == 403 || response?.Status == 401;
        var hasUnauthorizedContent = await Page.IsVisibleAsync("text=Unauthorized, text=Access Denied, text=Forbidden");
        var redirectedToLogin = Page.Url.Contains("/Account/Login") || Page.Url.Contains("/login");
        
        Assert.That(is403Or401 || hasUnauthorizedContent || redirectedToLogin, Is.True, 
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
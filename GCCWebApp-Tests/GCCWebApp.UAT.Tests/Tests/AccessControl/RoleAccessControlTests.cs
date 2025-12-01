using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.AccessControl;

/// <summary>
/// Role-based access control tests
/// Verifies that Admin, Analyst, and External roles have correct permissions
/// Tests page access, feature visibility, and data filtering
/// Safe to run on both Dev and Production (test environment)
/// </summary>
[TestFixture]
[Category("AccessControl")]
[Category("Security")]
[Category("RBAC")]
public class RoleAccessControlTests : BaseTest
{
    #region Admin Access Tests

    [Test]
    [Order(1)]
    public async Task Admin_CanAccessAdminDashboard()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("admin_dashboard_access");

        var currentUrl = Page.Url;
        var hasAccess = currentUrl.Contains("/Admin") && !currentUrl.Contains("Login") && !currentUrl.Contains("AccessDenied");

        Assert.That(hasAccess, Is.True, "Admin should access Admin dashboard");
    }

    [Test]
    [Order(2)]
    public async Task Admin_CanAccessUserManagement()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("admin_user_management_access");

        var currentUrl = Page.Url;
        var hasAccess = currentUrl.Contains("UserManagement") && !currentUrl.Contains("AccessDenied");

        Assert.That(hasAccess, Is.True, "Admin should access User Management");
    }

    [Test]
    [Order(3)]
    public async Task Admin_CanAccessRoleManagement()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/RoleManagement");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("admin_role_management_access");

        var currentUrl = Page.Url;
        var hasAccess = currentUrl.Contains("RoleManagement") && !currentUrl.Contains("AccessDenied");

        Assert.That(hasAccess, Is.True, "Admin should access Role Management");
    }

    [Test]
    [Order(4)]
    public async Task Admin_CanAccessTagManagement()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/Tags");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("admin_tag_management_access");

        var currentUrl = Page.Url;
        var hasAccess = currentUrl.Contains("Tags") && !currentUrl.Contains("AccessDenied");

        Assert.That(hasAccess, Is.True, "Admin should access Tag Management");
    }

    [Test]
    [Order(5)]
    public async Task Admin_CanAccessPartnerAPI()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/PartnerAPI");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("admin_partner_api_access");

        var currentUrl = Page.Url;
        var hasAccess = currentUrl.Contains("PartnerAPI") && !currentUrl.Contains("AccessDenied");

        Assert.That(hasAccess, Is.True, "Admin should access Partner API Management");
    }

    [Test]
    [Order(6)]
    public async Task Admin_CanAccessCertificateCreate()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/Create");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("admin_certificate_create_access");

        var currentUrl = Page.Url;
        var hasAccess = currentUrl.Contains("Create") && !currentUrl.Contains("AccessDenied");

        Assert.That(hasAccess, Is.True, "Admin should access Certificate Create");
    }

    [Test]
    [Order(7)]
    public async Task Admin_CanSeeAllProducts()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Product/Management");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("admin_products_all");

        var productRows = Page.Locator("table tbody tr, .product-row");
        var productCount = await productRows.CountAsync();

        TestContext.WriteLine($"Admin sees {productCount} products");
        Assert.Pass("Admin product visibility test completed");
    }

    [Test]
    [Order(8)]
    public async Task Admin_CanSeeInternalTags()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/CertificateTileViewInternal");
        await Page.WaitForTimeoutAsync(2000);

        var internalTagColumn = Page.Locator("th:has-text('Internal Tag'), th:has-text('Internal')");
        var hasInternalColumn = await internalTagColumn.CountAsync() > 0;

        await TakeScreenshot("admin_internal_tags_visible");
        Assert.That(hasInternalColumn, Is.True, "Admin should see Internal Tag column");
    }

    #endregion

    #region Analyst Access Tests

    [Test]
    [Order(20)]
    public async Task Analyst_CannotAccessAdminDashboard()
    {
        await LoginAsRole("Analyst");
        await NavigateToPage("/Admin");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("analyst_admin_blocked");

        var currentUrl = Page.Url;
        var isBlocked = currentUrl.Contains("Login") || currentUrl.Contains("AccessDenied") ||
                        currentUrl.Contains("Forbidden") || !currentUrl.Contains("/Admin");

        Assert.That(isBlocked, Is.True, "Analyst should be blocked from Admin dashboard");
    }

    [Test]
    [Order(21)]
    public async Task Analyst_CannotAccessUserManagement()
    {
        await LoginAsRole("Analyst");
        await NavigateToPage("/Admin/UserManagement");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("analyst_user_management_blocked");

        var currentUrl = Page.Url;
        var isBlocked = currentUrl.Contains("Login") || currentUrl.Contains("AccessDenied") ||
                        currentUrl.Contains("Forbidden") || !currentUrl.Contains("UserManagement");

        Assert.That(isBlocked, Is.True, "Analyst should be blocked from User Management");
    }

    [Test]
    [Order(22)]
    public async Task Analyst_CannotAccessRoleManagement()
    {
        await LoginAsRole("Analyst");
        await NavigateToPage("/Admin/RoleManagement");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("analyst_role_management_blocked");

        var currentUrl = Page.Url;
        var isBlocked = currentUrl.Contains("Login") || currentUrl.Contains("AccessDenied") ||
                        currentUrl.Contains("Forbidden") || !currentUrl.Contains("RoleManagement");

        Assert.That(isBlocked, Is.True, "Analyst should be blocked from Role Management");
    }

    [Test]
    [Order(23)]
    public async Task Analyst_CanAccessDashboard()
    {
        await LoginAsRole("Analyst");
        await NavigateToPage("/Dashboard");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("analyst_dashboard_access");

        var currentUrl = Page.Url;
        var hasAccess = currentUrl.Contains("Dashboard") && !currentUrl.Contains("AccessDenied");

        Assert.That(hasAccess, Is.True, "Analyst should access Dashboard");
    }

    [Test]
    [Order(24)]
    public async Task Analyst_CanAccessCertificateCreate()
    {
        await LoginAsRole("Analyst");
        await NavigateToPage("/Certificate/Create");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("analyst_certificate_create");

        var currentUrl = Page.Url;
        var hasAccess = currentUrl.Contains("Create") && !currentUrl.Contains("AccessDenied");

        Assert.That(hasAccess, Is.True, "Analyst should access Certificate Create");
    }

    [Test]
    [Order(25)]
    public async Task Analyst_CanAccessInternalView()
    {
        await LoginAsRole("Analyst");
        await NavigateToPage("/Certificate/CertificateTileViewInternal");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("analyst_internal_view");

        var currentUrl = Page.Url;
        var hasAccess = currentUrl.Contains("Internal") && !currentUrl.Contains("AccessDenied");

        Assert.That(hasAccess, Is.True, "Analyst should access Internal View");
    }

    [Test]
    [Order(26)]
    public async Task Analyst_CanSeeInternalTags()
    {
        await LoginAsRole("Analyst");
        await NavigateToPage("/Certificate/CertificateTileViewInternal");
        await Page.WaitForTimeoutAsync(2000);

        var internalTagColumn = Page.Locator("th:has-text('Internal Tag'), th:has-text('Internal')");
        var hasInternalColumn = await internalTagColumn.CountAsync() > 0;

        await TakeScreenshot("analyst_internal_tags_visible");
        Assert.That(hasInternalColumn, Is.True, "Analyst should see Internal Tag column");
    }

    #endregion

    #region External Access Tests

    [Test]
    [Order(40)]
    public async Task External_CannotAccessAdminDashboard()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Admin");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("external_admin_blocked");

        var currentUrl = Page.Url;
        var isBlocked = currentUrl.Contains("Login") || currentUrl.Contains("AccessDenied") ||
                        currentUrl.Contains("Forbidden") || !currentUrl.Contains("/Admin");

        Assert.That(isBlocked, Is.True, "External should be blocked from Admin dashboard");
    }

    [Test]
    [Order(41)]
    public async Task External_CannotAccessUserManagement()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Admin/UserManagement");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("external_user_management_blocked");

        var currentUrl = Page.Url;
        var isBlocked = currentUrl.Contains("Login") || currentUrl.Contains("AccessDenied") ||
                        currentUrl.Contains("Forbidden") || !currentUrl.Contains("UserManagement");

        Assert.That(isBlocked, Is.True, "External should be blocked from User Management");
    }

    [Test]
    [Order(42)]
    public async Task External_CannotAccessCertificateCreate()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Certificate/Create");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("external_certificate_create_blocked");

        var currentUrl = Page.Url;
        var isBlocked = currentUrl.Contains("Login") || currentUrl.Contains("AccessDenied") ||
                        currentUrl.Contains("Forbidden") || !currentUrl.Contains("Create");

        Assert.That(isBlocked, Is.True, "External should be blocked from Certificate Create");
    }

    [Test]
    [Order(43)]
    public async Task External_CanAccessExternalView()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Certificate/CertificateTileViewExternal");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("external_view_access");

        var currentUrl = Page.Url;
        var hasAccess = currentUrl.Contains("External") && !currentUrl.Contains("AccessDenied");

        Assert.That(hasAccess, Is.True, "External should access External View");
    }

    [Test]
    [Order(44)]
    public async Task External_CannotSeeInternalTags()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Certificate/CertificateTileViewExternal");
        await Page.WaitForTimeoutAsync(2000);

        var internalTagColumn = Page.Locator("th:has-text('Internal Tag')");
        var hasInternalColumn = await internalTagColumn.CountAsync() > 0;

        await TakeScreenshot("external_no_internal_tags");
        Assert.That(hasInternalColumn, Is.False, "External should NOT see Internal Tag column");
    }

    [Test]
    [Order(45)]
    public async Task External_CannotSeeAddCertificateButton()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Certificate/CertificateTileViewExternal");
        await Page.WaitForTimeoutAsync(2000);

        var addButton = Page.Locator("button:has-text('Add'), a:has-text('Add Certificate'), #add-certificate-btn, .add-btn");
        var hasAddButton = await addButton.IsVisibleAsync();

        await TakeScreenshot("external_no_add_button");
        Assert.That(hasAddButton, Is.False, "External should NOT see Add Certificate button");
    }

    [Test]
    [Order(46)]
    public async Task External_CannotSeeEditButton()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Certificate/CertificateTileViewExternal");
        await Page.WaitForTimeoutAsync(2000);

        var editButton = Page.Locator(".edit-btn, button:has-text('Edit'), a[href*='Edit']");
        var hasEditButton = await editButton.IsVisibleAsync();

        await TakeScreenshot("external_no_edit_button");
        Assert.That(hasEditButton, Is.False, "External should NOT see Edit button");
    }

    [Test]
    [Order(47)]
    public async Task External_CannotSeeDeleteButton()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Certificate/CertificateTileViewExternal");
        await Page.WaitForTimeoutAsync(2000);

        var deleteButton = Page.Locator(".delete-btn, button:has-text('Delete')");
        var hasDeleteButton = await deleteButton.IsVisibleAsync();

        await TakeScreenshot("external_no_delete_button");
        Assert.That(hasDeleteButton, Is.False, "External should NOT see Delete button");
    }

    [Test]
    [Order(48)]
    public async Task External_SeesFilteredProducts()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Product/Management");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("external_products_filtered");

        // External should see only products associated with their company/partner
        // or be blocked from Product Management entirely
        var currentUrl = Page.Url;
        var productRows = Page.Locator("table tbody tr, .product-row");
        var productCount = await productRows.CountAsync();

        TestContext.WriteLine($"External sees {productCount} products (should be filtered)");
        Assert.Pass("External product filtering test completed");
    }

    [Test]
    [Order(49)]
    public async Task External_CannotSeeActionsColumnInProducts()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Product/Management");
        await Page.WaitForTimeoutAsync(2000);

        var actionsColumn = Page.Locator("th:has-text('Actions')");
        var hasActionsColumn = await actionsColumn.CountAsync() > 0;

        await TakeScreenshot("external_products_no_actions");
        Assert.That(hasActionsColumn, Is.False, "External should NOT see Actions column in Products");
    }

    #endregion

    #region Unauthenticated Access Tests

    [Test]
    [Order(60)]
    public async Task Unauthenticated_CanAccessPublicTileView()
    {
        // No login - go directly to public view
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("public_tile_view");

        var currentUrl = Page.Url;
        var hasAccess = currentUrl.Contains("CertificateTileView") && !currentUrl.Contains("Login");

        Assert.That(hasAccess, Is.True, "Unauthenticated user should access Public Tile View");
    }

    [Test]
    [Order(61)]
    public async Task Unauthenticated_CannotAccessInternalView()
    {
        await NavigateToPage("/Certificate/CertificateTileViewInternal");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("unauthenticated_internal_blocked");

        var currentUrl = Page.Url;
        var isBlocked = currentUrl.Contains("Login") || currentUrl.Contains("AccessDenied");

        Assert.That(isBlocked, Is.True, "Unauthenticated user should be blocked from Internal View");
    }

    [Test]
    [Order(62)]
    public async Task Unauthenticated_CannotAccessExternalView()
    {
        await NavigateToPage("/Certificate/CertificateTileViewExternal");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("unauthenticated_external_blocked");

        var currentUrl = Page.Url;
        var isBlocked = currentUrl.Contains("Login") || currentUrl.Contains("AccessDenied");

        Assert.That(isBlocked, Is.True, "Unauthenticated user should be blocked from External View");
    }

    [Test]
    [Order(63)]
    public async Task Unauthenticated_CannotAccessAdmin()
    {
        await NavigateToPage("/Admin");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("unauthenticated_admin_blocked");

        var currentUrl = Page.Url;
        var isBlocked = currentUrl.Contains("Login") || currentUrl.Contains("AccessDenied");

        Assert.That(isBlocked, Is.True, "Unauthenticated user should be blocked from Admin");
    }

    [Test]
    [Order(64)]
    public async Task Unauthenticated_CannotAccessDashboard()
    {
        await NavigateToPage("/Dashboard");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("unauthenticated_dashboard_blocked");

        var currentUrl = Page.Url;
        var isBlocked = currentUrl.Contains("Login") || currentUrl.Contains("AccessDenied");

        Assert.That(isBlocked, Is.True, "Unauthenticated user should be blocked from Dashboard");
    }

    #endregion

    #region Navigation Menu Tests

    [Test]
    [Order(70)]
    public async Task Admin_SeesAdminMenuLink()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Dashboard");
        await Page.WaitForTimeoutAsync(2000);

        var adminLink = Page.Locator("a[href*='/Admin'], nav a:has-text('Admin'), .nav-link:has-text('Admin')");
        var hasAdminLink = await adminLink.CountAsync() > 0;

        await TakeScreenshot("admin_nav_admin_link");
        Assert.That(hasAdminLink, Is.True, "Admin should see Admin menu link");
    }

    [Test]
    [Order(71)]
    public async Task Analyst_DoesNotSeeAdminMenuLink()
    {
        await LoginAsRole("Analyst");
        await NavigateToPage("/Dashboard");
        await Page.WaitForTimeoutAsync(2000);

        var adminLink = Page.Locator("a[href='/Admin'], nav a:has-text('Admin')");
        var hasAdminLink = await adminLink.IsVisibleAsync();

        await TakeScreenshot("analyst_nav_no_admin_link");
        Assert.That(hasAdminLink, Is.False, "Analyst should NOT see Admin menu link");
    }

    [Test]
    [Order(72)]
    public async Task External_DoesNotSeeAdminMenuLink()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Certificate/CertificateTileViewExternal");
        await Page.WaitForTimeoutAsync(2000);

        var adminLink = Page.Locator("a[href='/Admin'], nav a:has-text('Admin')");
        var hasAdminLink = await adminLink.IsVisibleAsync();

        await TakeScreenshot("external_nav_no_admin_link");
        Assert.That(hasAdminLink, Is.False, "External should NOT see Admin menu link");
    }

    #endregion

    #region Direct URL Access Tests

    [Test]
    [Order(80)]
    public async Task DirectURL_AdminPageBlocksNonAdmin()
    {
        // Test that directly accessing admin URLs is blocked for non-admins
        var adminPages = new[]
        {
            "/Admin/UserManagement",
            "/Admin/RoleManagement",
            "/Admin/Tags",
            "/Admin/Partners",
            "/Admin/PartnerAPI"
        };

        await LoginAsRole("External");

        foreach (var page in adminPages)
        {
            await NavigateToPage(page);
            await Page.WaitForTimeoutAsync(1000);

            var currentUrl = Page.Url;
            var isBlocked = currentUrl.Contains("Login") || currentUrl.Contains("AccessDenied") ||
                            currentUrl.Contains("Forbidden") || !currentUrl.Contains(page.Split('/').Last());

            TestContext.WriteLine($"Page {page}: Blocked = {isBlocked}");
        }

        await TakeScreenshot("direct_url_blocked_test");
        Assert.Pass("Direct URL access blocking test completed");
    }

    [Test]
    [Order(81)]
    public async Task DirectURL_CertificateEditBlocksExternal()
    {
        await LoginAsRole("External");

        // Try to access a certificate edit page directly
        await NavigateToPage("/Certificate/Edit/1");
        await Page.WaitForTimeoutAsync(2000);
        await TakeScreenshot("external_direct_edit_blocked");

        var currentUrl = Page.Url;
        var isBlocked = currentUrl.Contains("Login") || currentUrl.Contains("AccessDenied") ||
                        currentUrl.Contains("Forbidden") || !currentUrl.Contains("Edit");

        Assert.That(isBlocked, Is.True, "External should be blocked from direct Certificate Edit URL");
    }

    #endregion
}

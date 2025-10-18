namespace GCCWebApp.UAT.Tests.Tests.Screenshots;

[TestFixture]
public class AdminPagesScreenshotTests : ScreenshotTestBase
{
    [Test]
    [TestCaseSource(nameof(TestViewports))]
    public async Task CaptureAdminDashboard(ViewportSize viewport)
    {
        await NavigateAndScreenshot(AdminUserPage, "/Admin/Index", "AdminDashboardPage", "Admin", viewport);
    }

    [Test]
    public async Task CaptureUserManagementPages()
    {
        var userManagementPages = new Dictionary<string, string>
        {
            { "/Admin/UserManagement", "UserManagementPage" },
            { "/Admin/CreateUser", "CreateUserPage" },
            { "/Admin/RoleManagement", "RoleManagementPage" }
        };

        foreach (var page in userManagementPages)
        {
            foreach (var viewport in TestViewports)
            {
                await NavigateAndScreenshot(AdminUserPage, page.Key, page.Value, "Admin", viewport);
            }
        }
    }

    [Test]
    public async Task CaptureSystemConfigurationPages()
    {
        var configPages = new Dictionary<string, string>
        {
            { "/Admin/GoogleAnalytics", "GoogleAnalyticsConfigPage" },
            { "/Admin/GCCAPI", "GCCAPIManagementPage" }
        };

        foreach (var page in configPages)
        {
            foreach (var viewport in TestViewports)
            {
                await NavigateAndScreenshot(AdminUserPage, page.Key, page.Value, "Admin", viewport);
            }
        }
    }

    [Test]
    public async Task CaptureDataManagementPages()
    {
        var dataManagementPages = new Dictionary<string, string>
        {
            { "/Admin/ProductTypes", "ProductTypesManagementPage" },
            { "/Admin/CertificateTypes", "CertificateTypesManagementPage" },
            { "/Admin/CertificateFields", "CertificateFieldsConfigPage" },
            { "/Admin/CSFAttributes", "CSFAttributesManagementPage" },
            { "/Admin/Tags", "TagsManagementPage" },
            { "/Admin/Partners", "PartnersManagementPage" },
            { "/Admin/ProductManagement", "AdminProductManagementPage" }
        };

        foreach (var page in dataManagementPages)
        {
            foreach (var viewport in TestViewports)
            {
                await NavigateAndScreenshot(AdminUserPage, page.Key, page.Value, "Admin", viewport);
            }
        }
    }

    [Test]
    public async Task CaptureTestPages()
    {
        var testPages = new Dictionary<string, string>
        {
            { "/Test/ProductTypes", "TestProductTypesPage" }
        };

        foreach (var page in testPages)
        {
            foreach (var viewport in TestViewports)
            {
                // Use Admin page since test pages may require authentication
                await NavigateAndScreenshot(AdminUserPage, page.Key, page.Value, "Admin", viewport);
            }
        }
    }

    [Test]
    public async Task CaptureAccessDeniedScenarios()
    {
        // Test access denied scenarios with External and Analyst users trying to access admin pages
        var restrictedPages = new[]
        {
            "/Admin/Index",
            "/Admin/UserManagement", 
            "/Admin/RoleManagement"
        };

        var restrictedUsers = new Dictionary<string, IPage>
        {
            { "External", ExternalUserPage },
            { "Analyst", AnalystUserPage }
        };

        foreach (var user in restrictedUsers)
        {
            foreach (var restrictedPage in restrictedPages.Take(1)) // Just test one page per user to avoid spam
            {
                foreach (var viewport in TestViewports)
                {
                    await NavigateAndScreenshot(user.Value, restrictedPage, 
                        $"AccessDeniedPage_{restrictedPage.Split('/').Last()}", user.Key, viewport);
                }
            }
        }
    }
}
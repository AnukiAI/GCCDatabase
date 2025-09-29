namespace GCCWebApp.UAT.Tests.Tests.Screenshots;

[TestFixture]
public class AuthenticatedPagesScreenshotTests : ScreenshotTestBase
{
    [Test]
    [TestCaseSource(nameof(TestViewports))]
    public async Task CaptureDashboard_External(ViewportSize viewport)
    {
        await NavigateAndScreenshot(ExternalUserPage, "/Dashboard/Index", "DashboardPage", "External", viewport);
    }

    [Test]
    [TestCaseSource(nameof(TestViewports))]
    public async Task CaptureDashboard_Analyst(ViewportSize viewport)
    {
        await NavigateAndScreenshot(AnalystUserPage, "/Dashboard/Index", "DashboardPage", "Analyst", viewport);
    }

    [Test]
    [TestCaseSource(nameof(TestViewports))]
    public async Task CaptureDashboard_Admin(ViewportSize viewport)
    {
        await NavigateAndScreenshot(AdminUserPage, "/Dashboard/Index", "DashboardPage", "Admin", viewport);
    }

    [Test]
    public async Task CaptureCertificatePages_AllRoles()
    {
        var certificatePages = new Dictionary<string, string>
        {
            { "/Certificate/Certificates", "CertificatesListPage" },
            { "/Certificate/CertificateTileViewExternal", "CertificateTileViewExternal" },
            { "/Certificate/CertificateTileViewInternal", "CertificateTileViewInternal" },
            { "/Certificate/CertificateInternalView", "CertificateInternalView" }
        };

        var users = new Dictionary<string, IPage>
        {
            { "External", ExternalUserPage },
            { "Analyst", AnalystUserPage },
            { "Admin", AdminUserPage }
        };

        foreach (var user in users)
        {
            foreach (var certPage in certificatePages)
            {
                foreach (var viewport in TestViewports)
                {
                    await NavigateAndScreenshot(user.Value, certPage.Key, certPage.Value, user.Key, viewport);
                }
            }
        }
    }

    [Test]
    public async Task CaptureCertificateDetails_AllRoles()
    {
        var certificateIds = await GetSampleCertificateIds();
        var users = new Dictionary<string, IPage>
        {
            { "External", ExternalUserPage },
            { "Analyst", AnalystUserPage },
            { "Admin", AdminUserPage }
        };

        foreach (var user in users)
        {
            foreach (var viewport in TestViewports)
            {
                foreach (var certId in certificateIds.Take(2)) // Test first 2 certificates per user
                {
                    await NavigateAndScreenshot(user.Value, $"/Certificate/Details/{certId}", 
                        $"CertificateDetailsPage_ID{certId}", user.Key, viewport);
                }
            }
        }
    }

    [Test]
    public async Task CaptureCompanyPages_AuthenticatedUsers()
    {
        var companyPages = new Dictionary<string, string>
        {
            { "/Company/Index", "CompanyIndexPage" },
            { "/Company/Management", "CompanyManagementPage" }
        };

        var users = new Dictionary<string, IPage>
        {
            { "External", ExternalUserPage },
            { "Analyst", AnalystUserPage },
            { "Admin", AdminUserPage }
        };

        foreach (var user in users)
        {
            foreach (var companyPage in companyPages)
            {
                foreach (var viewport in TestViewports)
                {
                    await NavigateAndScreenshot(user.Value, companyPage.Key, companyPage.Value, user.Key, viewport);
                }
            }
        }
    }

    [Test]
    public async Task CaptureProductPages_AuthenticatedUsers()
    {
        var productPages = new Dictionary<string, string>
        {
            { "/Product/Management", "ProductManagementPage" },
            { "/Product/ManagementAlt", "ProductManagementAltPage" },
            { "/Product/ManagementV2", "ProductManagementV2Page" },
            { "/Product/Categories", "ProductCategoriesPage" }
        };

        var users = new Dictionary<string, IPage>
        {
            { "External", ExternalUserPage },
            { "Analyst", AnalystUserPage },
            { "Admin", AdminUserPage }
        };

        foreach (var user in users)
        {
            foreach (var productPage in productPages)
            {
                foreach (var viewport in TestViewports)
                {
                    await NavigateAndScreenshot(user.Value, productPage.Key, productPage.Value, user.Key, viewport);
                }
            }
        }
    }
}
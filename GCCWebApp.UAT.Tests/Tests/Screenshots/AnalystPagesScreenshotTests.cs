namespace GCCWebApp.UAT.Tests.Tests.Screenshots;

[TestFixture]
public class AnalystPagesScreenshotTests : ScreenshotTestBase
{
    [Test]
    public async Task CaptureCertificateCRUDPages_Analyst()
    {
        var certificateIds = await GetSampleCertificateIds();

        // Create Certificate Page
        foreach (var viewport in TestViewports)
        {
            await NavigateAndScreenshot(AnalystUserPage, "/Certificate/Create", "CertificateCreatePage", "Analyst", viewport);
        }

        // Edit Certificate Pages
        foreach (var viewport in TestViewports)
        {
            foreach (var certId in certificateIds.Take(1))
            {
                await NavigateAndScreenshot(AnalystUserPage, $"/Certificate/Edit/{certId}", 
                    $"CertificateEditPage_ID{certId}", "Analyst", viewport);
            }
        }

        // Import Certificate Page
        foreach (var viewport in TestViewports)
        {
            await NavigateAndScreenshot(AnalystUserPage, "/Certificate/Import", "CertificateImportPage", "Analyst", viewport);
        }
    }

    [Test]
    public async Task CaptureDocumentUploadPages_Analyst()
    {
        var certificateIds = await GetSampleCertificateIds();

        foreach (var viewport in TestViewports)
        {
            foreach (var certId in certificateIds.Take(1))
            {
                await NavigateAndScreenshot(AnalystUserPage, $"/Document/Upload/{certId}", 
                    $"DocumentUploadPage_CertID{certId}", "Analyst", viewport);
            }
        }
    }

    [Test]
    public async Task CaptureCompanyCRUDPages_Analyst()
    {
        // Create Company Page
        foreach (var viewport in TestViewports)
        {
            await NavigateAndScreenshot(AnalystUserPage, "/Company/Create", "CompanyCreatePage", "Analyst", viewport);
        }
    }

    [Test]
    public async Task CaptureCertificateCRUDPages_Admin()
    {
        var certificateIds = await GetSampleCertificateIds();

        // Create Certificate Page
        foreach (var viewport in TestViewports)
        {
            await NavigateAndScreenshot(AdminUserPage, "/Certificate/Create", "CertificateCreatePage", "Admin", viewport);
        }

        // Edit Certificate Pages
        foreach (var viewport in TestViewports)
        {
            foreach (var certId in certificateIds.Take(1))
            {
                await NavigateAndScreenshot(AdminUserPage, $"/Certificate/Edit/{certId}", 
                    $"CertificateEditPage_ID{certId}", "Admin", viewport);
            }
        }

        // Import Certificate Page
        foreach (var viewport in TestViewports)
        {
            await NavigateAndScreenshot(AdminUserPage, "/Certificate/Import", "CertificateImportPage", "Admin", viewport);
        }
    }

    [Test]
    public async Task CaptureDocumentUploadPages_Admin()
    {
        var certificateIds = await GetSampleCertificateIds();

        foreach (var viewport in TestViewports)
        {
            foreach (var certId in certificateIds.Take(1))
            {
                await NavigateAndScreenshot(AdminUserPage, $"/Document/Upload/{certId}", 
                    $"DocumentUploadPage_CertID{certId}", "Admin", viewport);
            }
        }
    }

    [Test]
    public async Task CaptureCompanyCRUDPages_Admin()
    {
        // Create Company Page
        foreach (var viewport in TestViewports)
        {
            await NavigateAndScreenshot(AdminUserPage, "/Company/Create", "CompanyCreatePage", "Admin", viewport);
        }
    }
}
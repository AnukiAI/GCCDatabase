namespace GCCWebApp.UAT.Tests.Tests.Screenshots;

[TestFixture]
public class PublicPagesScreenshotTests : ScreenshotTestBase
{
    [Test]
    [TestCaseSource(nameof(TestViewports))]
    public async Task CaptureHomePage(ViewportSize viewport)
    {
        await NavigateAndScreenshot(PublicPage, "/", "HomePage", "Public", viewport);
    }

    [Test]
    [TestCaseSource(nameof(TestViewports))]
    public async Task CapturePrivacyPage(ViewportSize viewport)
    {
        await NavigateAndScreenshot(PublicPage, "/Home/Privacy", "PrivacyPage", "Public", viewport);
    }

    [Test]
    [TestCaseSource(nameof(TestViewports))]
    public async Task CaptureCertificateTileView(ViewportSize viewport)
    {
        await NavigateAndScreenshot(PublicPage, "/Certificate/CertificateTileView", "CertificateTileView", "Public", viewport);
    }

    [Test]
    [TestCaseSource(nameof(TestViewports))]
    public async Task CaptureCertificateExternalView(ViewportSize viewport)
    {
        await NavigateAndScreenshot(PublicPage, "/Certificate/CertificateExternalView", "CertificateExternalView", "Public", viewport);
    }

    [Test]
    public async Task CaptureCertificateSingleView()
    {
        var certificateIds = await GetSampleCertificateIds();
        
        foreach (var viewport in TestViewports)
        {
            foreach (var certId in certificateIds.Take(1)) // Just test first certificate for public
            {
                await NavigateAndScreenshot(PublicPage, $"/Certificate/CertificateSingleView/{certId}", 
                    $"CertificateSingleView_ID{certId}", "Public", viewport);
            }
        }
    }

    [Test]
    [TestCaseSource(nameof(TestViewports))]
    public async Task CaptureErrorPage(ViewportSize viewport)
    {
        await NavigateAndScreenshot(PublicPage, "/Home/Error", "ErrorPage", "Public", viewport);
    }
}
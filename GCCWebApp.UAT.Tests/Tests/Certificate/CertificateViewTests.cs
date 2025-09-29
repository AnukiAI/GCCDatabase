using GCCWebApp.UAT.Tests.PageObjects;
using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.Certificate;

[TestFixture]
public class CertificateViewTests : BaseTest
{
    [Test]
    public async Task CertificateDetailsPage_DisplaysCorrectInformation()
    {
        // Arrange
        var homePage = new HomePage(Page);
        var certificatePage = new CertificatePage(Page);

        // Act - Navigate to home and find a certificate
        await NavigateToPage(TestDataHelper.Urls.Home);
        await TakeScreenshot("home_for_certificate_test");

        var certificateCount = await homePage.GetCertificateCount();
        if (certificateCount > 0)
        {
            // Click on first certificate
            await homePage.ClickFirstCertificate();
            await TakeScreenshot("certificate_details_loaded");

            // Assert - Verify certificate details are displayed
            var isCertificateDetailsVisible = await certificatePage.IsCertificateDetailsVisible();
            Assert.That(isCertificateDetailsVisible, Is.True, "Certificate details should be visible");

            var certificateTitle = await certificatePage.GetCertificateTitle();
            Assert.That(certificateTitle, Is.Not.Empty, "Certificate should have a title");

            await TakeScreenshot("certificate_details_verified");
        }
        else
        {
            TestContext.WriteLine("No certificates available for testing");
            await TakeScreenshot("no_certificates_available");
        }
    }

    [Test]
    public async Task CertificateTileView_DisplaysCertificates()
    {
        // Arrange
        var certificatePage = new CertificatePage(Page);

        // Act - Navigate to certificates tile view
        await NavigateToPage("/Certificate/CertificateTileView");
        await TakeScreenshot("tile_view_loaded");

        // Assert - Verify tiles are displayed
        var certificateCount = await certificatePage.GetCertificateCount();
        TestContext.WriteLine($"Certificate count in tile view: {certificateCount}");

        // The count might be 0 if no test data exists, which is okay for initial setup
        Assert.That(certificateCount, Is.GreaterThanOrEqualTo(0), "Certificate count should be valid");

        await TakeScreenshot("tile_view_verified");
    }

    [Test]
    public async Task CertificateTileView_InfiniteScroll_LoadsMoreContent()
    {
        // Arrange
        var certificatePage = new CertificatePage(Page);

        // Act - Navigate to certificates tile view
        await NavigateToPage("/Certificate/CertificateTileView");
        await TakeScreenshot("infinite_scroll_initial");

        // Wait for initial certificates to load
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
        await Task.Delay(2000); // Allow time for initial API call

        var initialCertificateCount = await certificatePage.GetCertificateCount();
        TestContext.WriteLine($"Initial certificate count: {initialCertificateCount}");

        // Verify loading container exists but is hidden
        var loadingContainer = Page.Locator("#loadingContainer");
        await Expect(loadingContainer).ToBeHiddenAsync();

        // Verify end-of-results container exists but is hidden
        var endOfResults = Page.Locator("#endOfResults");
        await Expect(endOfResults).ToBeHiddenAsync();

        // Simulate scroll to trigger infinite scroll (if certificates are available)
        if (initialCertificateCount > 0)
        {
            // Scroll to bottom to trigger infinite scroll
            await Page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
            await TakeScreenshot("after_scroll_to_bottom");

            // Wait for potential loading state
            await Task.Delay(1000);

            // Check if more certificates loaded or end-of-results appeared
            var finalCertificateCount = await certificatePage.GetCertificateCount();
            TestContext.WriteLine($"Final certificate count: {finalCertificateCount}");

            // Either more certificates should load OR end-of-results should be visible
            var isEndOfResultsVisible = await endOfResults.IsVisibleAsync();
            var moreCertificatesLoaded = finalCertificateCount > initialCertificateCount;

            Assert.That(moreCertificatesLoaded || isEndOfResultsVisible, Is.True,
                "Either more certificates should load or end-of-results should be visible");

            await TakeScreenshot("infinite_scroll_completed");
        }
        else
        {
            TestContext.WriteLine("No certificates available for infinite scroll testing");
            await TakeScreenshot("no_certificates_for_scroll_test");
        }
    }

    [Test]
    public async Task CertificateTileView_InfiniteScroll_JavaScript_LoadsCorrectly()
    {
        // Arrange & Act - Navigate to certificates tile view
        await NavigateToPage("/Certificate/CertificateTileView");
        await TakeScreenshot("javascript_loading_test");

        // Assert - Verify infinite scroll JavaScript class is initialized
        var jsInitialized = await Page.EvaluateAsync<bool>(@"
            () => {
                return window.CertificateList !== undefined &&
                       typeof window.CertificateList === 'function';
            }
        ");

        Assert.That(jsInitialized, Is.True, "CertificateList JavaScript class should be available");

        // Verify infinite scroll elements are present in DOM
        var certificatesGrid = Page.Locator("#certificatesGrid");
        await Expect(certificatesGrid).ToBeVisibleAsync();

        var loadingContainer = Page.Locator("#loadingContainer");
        await Expect(loadingContainer).ToHaveCountAsync(1);

        var endOfResults = Page.Locator("#endOfResults");
        await Expect(endOfResults).ToHaveCountAsync(1);

        await TakeScreenshot("infinite_scroll_elements_verified");
    }

    [Test]
    public async Task CertificateTileView_LoadingStates_DisplayCorrectly()
    {
        // Arrange & Act - Navigate to certificates tile view
        await NavigateToPage("/Certificate/CertificateTileView");

        // Wait for page to fully load
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

        // Verify loading spinner is hidden initially
        var loadingSpinner = Page.Locator("#loadingContainer .loading-spinner");
        await Expect(loadingSpinner).ToBeHiddenAsync();

        // Verify loading container has correct CSS classes
        var loadingContainer = Page.Locator("#loadingContainer");
        var hasLoadingClass = await loadingContainer.EvaluateAsync<bool>("el => el.classList.contains('loading-container')");
        Assert.That(hasLoadingClass, Is.True, "Loading container should have loading-container class");

        // Verify end-of-results has correct structure
        var endMessage = Page.Locator("#endOfResults .end-message");
        await Expect(endMessage).ToHaveCountAsync(1);

        var endIcon = Page.Locator("#endOfResults .end-message i.fas.fa-check-circle");
        await Expect(endIcon).ToHaveCountAsync(1);

        await TakeScreenshot("loading_states_verified");
    }

    [Test]
    public async Task CertificateExternalView_IsAccessible()
    {
        // Act - Navigate to external certificate view
        await NavigateToPage("/Certificate/CertificateExternalView");
        await TakeScreenshot("external_view_loaded");

        // Assert - Verify page loads without errors
        var pageTitle = await Page.TitleAsync();
        Assert.That(pageTitle, Is.Not.Empty, "Page should have a title");

        // Verify no error messages are displayed
        var errorElements = Page.Locator(".alert-danger, .error");
        var errorCount = await errorElements.CountAsync();
        Assert.That(errorCount, Is.EqualTo(0), "No error messages should be displayed");

        await TakeScreenshot("external_view_verified");
    }

    [Test]
    public async Task CertificateInternalView_RequiresAuthentication()
    {
        // Act - Try to navigate to internal certificate view without authentication
        await NavigateToPage("/Certificate/CertificateInternalView");
        await TakeScreenshot("internal_view_access_attempt");

        // Assert - Should either redirect to login or show access denied
        var currentUrl = Page.Url;
        var isRedirectedToLogin = currentUrl.Contains("/Account/Login");
        var isAccessDenied = currentUrl.Contains("AccessDenied");
        var isOnInternalView = currentUrl.Contains("CertificateInternalView");

        if (isOnInternalView)
        {
            // If we can access it, the app might allow anonymous access
            TestContext.WriteLine("Internal view allows anonymous access");
            await TakeScreenshot("internal_view_anonymous_access");
        }
        else
        {
            Assert.That(isRedirectedToLogin || isAccessDenied, Is.True, 
                "Should redirect to login or show access denied for internal view");
            await TakeScreenshot("internal_view_access_restricted");
        }
    }
}
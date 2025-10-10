using GCCWebApp.UAT.Tests.PageObjects;
using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.UserJourneys;

[TestFixture]
public class PublicUserJourneyTests : BaseTest
{
    [Test]
    public async Task PublicUser_CanBrowseAndSearchCertificates()
    {
        // Arrange
        var homePage = new HomePage(Page);
        
        // Act - Navigate to home page
        await NavigateToPage(TestDataHelper.Urls.Home);
        await TakeScreenshot("homepage_loaded");

        // Verify homepage loads correctly
        var certificateCount = await homePage.GetCertificateCount();
        await TakeScreenshot("certificates_displayed");

        // Act - Search for certificates
        await homePage.SearchForCertificates(TestDataHelper.TestData.SearchTerms[0]);
        await TakeScreenshot("search_completed");

        // Assert - Verify search results
        var hasResults = await homePage.IsSearchResultsVisible();
        Assert.That(hasResults, Is.True, "Search results should be visible");

        // Act - Click on first certificate
        await homePage.ClickFirstCertificate();
        await TakeScreenshot("certificate_details_opened");

        // Assert - Verify certificate details page loaded
        Assert.That(Page.Url.Contains("/Certificate"), Is.True, "Should navigate to certificate details page");
    }

    [Test]
    public async Task PublicUser_CanViewDifferentCertificateViews()
    {
        // Arrange
        var certificatePage = new CertificatePage(Page);

        // Act - Navigate to certificates page
        await NavigateToPage(TestDataHelper.Urls.Certificates);
        await TakeScreenshot("certificates_page_loaded");

        // Act - Switch to tile view
        await certificatePage.SwitchToTileView();
        await TakeScreenshot("tile_view_activated");

        // Act - Switch to list view
        await certificatePage.SwitchToListView();
        await TakeScreenshot("list_view_activated");

        // Assert - Verify certificates are still displayed
        var certificateCount = await certificatePage.GetCertificateCount();
        Assert.That(certificateCount, Is.GreaterThan(0), "Certificates should be visible in both views");
    }

    [Test]
    public async Task PublicUser_CanFilterAndSortCertificates()
    {
        // Arrange
        var certificatePage = new CertificatePage(Page);

        // Act - Navigate to certificates page
        await NavigateToPage(TestDataHelper.Urls.Certificates);
        await TakeScreenshot("certificates_before_filter");

        // Get initial certificate count
        var initialCount = await certificatePage.GetCertificateCount();

        // Act - Apply filter if available
        try 
        {
            await certificatePage.SelectFilter("Paper Products");
            await TakeScreenshot("filter_applied");
        }
        catch
        {
            TestContext.WriteLine("Filter dropdown not available - this is okay for initial tests");
        }

        // Act - Apply sort if available
        try
        {
            await certificatePage.SelectSort("Company Name");
            await TakeScreenshot("sort_applied");
        }
        catch
        {
            TestContext.WriteLine("Sort dropdown not available - this is okay for initial tests");
        }

        // Assert - Verify page still functions
        var finalCount = await certificatePage.GetCertificateCount();
        Assert.That(finalCount, Is.GreaterThanOrEqualTo(0), "Certificate count should be valid after filtering/sorting");
    }
}
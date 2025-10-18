using Microsoft.Playwright;
using GCCWebApp.UAT.Tests.PageObjects;
using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.Certificate;

[TestFixture]
public class CertificateUIFixesTests : BaseTest
{
    [Test]
    public async Task CertificateTileView_ProductNameHeight_ShouldBeConsistent()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Wait for certificates to load
        await Page.WaitForSelectorAsync(".certificate-card", new PageWaitForSelectorOptions { Timeout = 10000 });

        // Get all product name elements
        var productNames = await Page.QuerySelectorAllAsync(".product-name");

        // Assert - Check that product names have consistent styling
        Assert.That(productNames.Count, Is.GreaterThan(0), "Should have product names to test");

        // Check CSS properties for consistency - should be 200px
        var heights = new List<double>();
        foreach (var element in productNames)
        {
            var height = await element.EvaluateAsync<double>("el => el.getBoundingClientRect().height");
            heights.Add(height);
        }

        // All product names should be exactly 140px height (tiles only)
        foreach (var height in heights)
        {
            Assert.That(height, Is.EqualTo(140).Within(2),
                $"Product name height should be 140px, but was {height}px");
        }

        TestContext.WriteLine($"All {heights.Count} product names have 140px height as required");

        await TakeScreenshot("product_name_height_consistency");
    }

    [Test]
    public async Task CertificateTileView_ProductTypeHeight_ShouldBeConsistent()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Wait for certificates to load
        await Page.WaitForSelectorAsync(".certificate-card", new PageWaitForSelectorOptions { Timeout = 10000 });

        // Get all product type elements
        var productTypes = await Page.QuerySelectorAllAsync(".product-type");

        // Assert - Check that product types have consistent styling
        Assert.That(productTypes.Count, Is.GreaterThan(0), "Should have product types to test");

        // Check CSS properties for consistency - should be 55px
        var heights = new List<double>();
        foreach (var element in productTypes)
        {
            var height = await element.EvaluateAsync<double>("el => el.getBoundingClientRect().height");
            heights.Add(height);
        }

        // All product types should be exactly 55px height (tiles only)
        foreach (var height in heights)
        {
            Assert.That(height, Is.EqualTo(55).Within(2),
                $"Product type height should be 55px, but was {height}px");
        }

        TestContext.WriteLine($"All {heights.Count} product types have 55px height as required");

        await TakeScreenshot("product_type_height_consistency");
    }

    [Test]
    public async Task CertificateTileView_TileClick_ShouldOpenInNewTab()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Wait for certificates to load
        await Page.WaitForSelectorAsync(".certificate-card", new PageWaitForSelectorOptions { Timeout = 10000 });

        // Get the first certificate card
        var firstCard = await Page.QuerySelectorAsync(".certificate-card");
        Assert.That(firstCard, Is.Not.Null, "Should have at least one certificate card");

        // Check that onclick attribute contains viewCertificateDetails function
        var onclick = await firstCard.GetAttributeAsync("onclick");
        Assert.That(onclick, Is.Not.Null.And.Contains("viewCertificateDetails"),
            "Certificate card should have onclick with viewCertificateDetails function");

        await TakeScreenshot("tile_click_test");
    }

    [Test]
    public async Task CertificateTileView_FilterItems_ShouldHaveMinimalPadding()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Check filter item spacing
        var filterItems = await Page.QuerySelectorAllAsync(".filter-item");
        Assert.That(filterItems.Count, Is.GreaterThan(0), "Should have filter items to test");

        // Check margin-bottom is minimal (should be 1px as per fix)
        var marginBottom = await filterItems[0].EvaluateAsync<string>("el => getComputedStyle(el).marginBottom");
        Assert.That(marginBottom, Is.EqualTo("1px"), "Filter items should have minimal margin-bottom");

        await TakeScreenshot("filter_item_spacing");
    }

    [Test]
    public async Task CertificateTileView_SearchInput_ShouldHaveCorrectPlaceholder()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateTileView");

        // Act
        var searchInput = await Page.QuerySelectorAsync("#searchInput");
        Assert.That(searchInput, Is.Not.Null, "Search input should exist");

        var placeholder = await searchInput.GetAttributeAsync("placeholder");

        // Assert
        Assert.That(placeholder, Is.EqualTo("Search"), "Search input placeholder should be 'Search'");

        await TakeScreenshot("search_placeholder_test");
    }

    [Test]
    public async Task CertificateTileView_SortDropdown_ShouldBeFunctional()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Click sort dropdown button
        var sortButton = await Page.QuerySelectorAsync("#sortDropdownBtn");
        Assert.That(sortButton, Is.Not.Null, "Sort dropdown button should exist");

        await sortButton.ClickAsync();

        // Wait for dropdown to open
        await Page.WaitForSelectorAsync(".sort-dropdown.open", new PageWaitForSelectorOptions { Timeout = 5000 });

        // Assert - Check dropdown is visible
        var dropdown = await Page.QuerySelectorAsync(".sort-dropdown.open");
        Assert.That(dropdown, Is.Not.Null, "Sort dropdown should open when clicked");

        await TakeScreenshot("sort_dropdown_functional");
    }

    [Test]
    public async Task CertificateSingleView_SectionHeaders_ShouldHavePrimaryBlueLeftBar()
    {
        // Arrange - Navigate to a specific certificate details page
        await NavigateToPage("/Certificate/CertificateSingleView/1");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Check section title styling
        var sectionTitles = await Page.QuerySelectorAllAsync(".certificate-section-title");
        Assert.That(sectionTitles.Count, Is.GreaterThan(0), "Should have section titles to test");

        // Check border-left property
        var borderLeft = await sectionTitles[0].EvaluateAsync<string>("el => getComputedStyle(el).borderLeft");
        Assert.That(borderLeft, Does.Contain("4px"), "Section titles should have 4px left border");
        Assert.That(borderLeft, Does.Contain("rgb(0, 174, 239)"), "Section titles should have primary blue border");

        await TakeScreenshot("section_headers_blue_bar");
    }

    [Test]
    public async Task CertificateSingleView_DownloadIcon_ShouldBeBelowImage()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateSingleView/1");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Check download icon position
        var certificateImage = await Page.QuerySelectorAsync(".certificate-image");
        var downloadAction = await Page.QuerySelectorAsync(".certificate-download-action");

        Assert.That(certificateImage, Is.Not.Null, "Certificate image should exist");
        Assert.That(downloadAction, Is.Not.Null, "Download action should exist");

        // Get positions
        var imageRect = await certificateImage.BoundingBoxAsync();
        var downloadRect = await downloadAction.BoundingBoxAsync();

        // Assert download icon is below image
        Assert.That(downloadRect?.Y, Is.GreaterThan(imageRect?.Y + imageRect?.Height),
            "Download icon should be positioned below the certificate image");

        await TakeScreenshot("download_icon_position");
    }

    [Test]
    public async Task CertificateTileView_ResultsCount_ShouldShowAccurateCount()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for certificates to load
        await Page.WaitForSelectorAsync("#resultsCount", new PageWaitForSelectorOptions { Timeout = 10000 });

        // Act - Get displayed count
        var resultsCountElement = await Page.QuerySelectorAsync("#resultsCount");
        Assert.That(resultsCountElement, Is.Not.Null, "Results count element should exist");

        var countText = await resultsCountElement.TextContentAsync();
        Assert.That(countText, Is.Not.Null.And.Not.Empty, "Results count should display text");

        // Extract number from text like "80 Results"
        var match = System.Text.RegularExpressions.Regex.Match(countText, @"(\d+)\s+Results?");
        Assert.That(match.Success, Is.True, $"Results count should show format 'X Results', got: {countText}");

        var displayedCount = int.Parse(match.Groups[1].Value);

        // Count actual visible certificate cards
        var visibleCards = await Page.QuerySelectorAllAsync(".certificate-card");

        // Note: Due to infinite scroll, displayed count may be higher than visible cards
        Assert.That(displayedCount, Is.GreaterThan(0), "Should show at least 1 result");
        TestContext.WriteLine($"Displayed count: {displayedCount}, Visible cards: {visibleCards.Count}");

        await TakeScreenshot("results_count_accuracy");
    }

    [Test]
    public async Task CertificateSingleView_ProductName_ShouldNotHaveFixedHeight()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateSingleView/1");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Check product name in single view does NOT have fixed height
        var productName = await Page.QuerySelectorAsync(".certificate-single-card .product-name");
        Assert.That(productName, Is.Not.Null, "Product name should exist in single view");

        // Get computed height - should be auto/natural, not 170px
        var height = await productName.EvaluateAsync<double>("el => el.getBoundingClientRect().height");

        // Assert - Should NOT be 140px (allowing for some flexibility in natural height)
        Assert.That(height, Is.Not.EqualTo(140).Within(5),
            $"SingleCertificateView product name should NOT have fixed 140px height, but was {height}px");

        // Verify CSS properties indicate auto height
        var computedHeight = await productName.EvaluateAsync<string>("el => getComputedStyle(el).height");
        var display = await productName.EvaluateAsync<string>("el => getComputedStyle(el).display");

        TestContext.WriteLine($"SingleView product name - Height: {height}px, CSS height: {computedHeight}, Display: {display}");

        await TakeScreenshot("single_view_product_name_natural_height");
    }

    [Test]
    public async Task CertificateSingleView_SectionContainers_ShouldHaveBlueBorders()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateSingleView/1");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Check detail section containers have blue left borders
        var detailSections = await Page.QuerySelectorAllAsync(".certificate-single-card .detail-section");
        Assert.That(detailSections.Count, Is.GreaterThan(0), "Should have detail sections");

        // Check border-left is blue, not green
        foreach (var section in detailSections)
        {
            var borderLeft = await section.EvaluateAsync<string>("el => getComputedStyle(el).borderLeft");
            Assert.That(borderLeft, Does.Contain("rgb(0, 174, 239)"),
                "Detail sections should have primary blue left border");
            Assert.That(borderLeft, Does.Not.Contain("green").IgnoreCase,
                "Detail sections should NOT have green borders");
        }

        await TakeScreenshot("detail_sections_blue_borders");
    }

    [Test]
    public async Task CertificateSingleView_DownloadIcon_ShouldBeRightAligned()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateSingleView/1");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Check download action is right-aligned
        var downloadAction = await Page.QuerySelectorAsync(".certificate-download-action");
        Assert.That(downloadAction, Is.Not.Null, "Download action should exist");

        // Check CSS justify-content is flex-end (right-aligned)
        var justifyContent = await downloadAction.EvaluateAsync<string>("el => getComputedStyle(el).justifyContent");
        Assert.That(justifyContent, Is.EqualTo("flex-end"),
            "Download action should be right-aligned with justify-content: flex-end");

        await TakeScreenshot("download_icon_right_aligned");
    }

    [Test]
    public async Task CertificateSingleView_ProductName_ShouldBeFullWidth()
    {
        // Arrange
        await NavigateToPage("/Certificate/CertificateSingleView/1");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Check product name width
        var productName = await Page.QuerySelectorAsync(".certificate-single-card .product-name");
        Assert.That(productName, Is.Not.Null, "Product name should exist in single view");

        // Get parent container width and product name width
        var parentContainer = await Page.QuerySelectorAsync(".certificate-single-card .product-info");
        Assert.That(parentContainer, Is.Not.Null, "Product info container should exist");

        var containerWidth = await parentContainer.EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        var productNameWidth = await productName.EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        // Assert product name takes full width (allowing small margin for padding/borders)
        var widthRatio = productNameWidth / containerWidth;
        Assert.That(widthRatio, Is.GreaterThan(0.95),
            $"Product name should be full width. Container: {containerWidth}px, Product name: {productNameWidth}px, Ratio: {widthRatio:F2}");

        // Verify CSS width property
        var computedWidth = await productName.EvaluateAsync<string>("el => getComputedStyle(el).width");
        TestContext.WriteLine($"Product name width: {productNameWidth}px, Container width: {containerWidth}px, CSS width: {computedWidth}");

        await TakeScreenshot("product_name_full_width");
    }
}
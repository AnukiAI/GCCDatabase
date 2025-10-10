using GCCWebApp.Accessibility.Tests.Tests.Core;
using Microsoft.Playwright;

namespace GCCWebApp.Accessibility.Tests.Tests.Pages;

public class CertificatePageAccessibilityTests : BaseAccessibilityTest
{
    [Fact]
    public async Task CertificatesPage_ShouldHaveNoWCAG21AAViolations()
    {
        // Arrange
        var url = $"{_baseUrl}/Certificate";

        // Act
        var results = await RunWCAG21AACheck(url);

        // Assert
        AssertNoViolations(results, "CertificatesPage_WCAG21AA");
    }

    [Fact]
    public async Task CertificateTileView_ShouldHaveNoWCAG21AAViolations()
    {
        // Arrange
        var url = $"{_baseUrl}/Certificate/CertificateTileView";

        // Act
        var results = await RunWCAG21AACheck(url);

        // Assert
        AssertNoViolations(results, "CertificateTileView_WCAG21AA");
    }

    [Fact]
    public async Task CertificatesPage_DataTables_ShouldBeAccessible()
    {
        // Arrange
        var url = $"{_baseUrl}/Certificate";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var results = await RunAccessibilityCheck(url, new AxeOptions
        {
            Rules = new[] { "table-header", "td-headers-attr", "th-has-data-cells", "table-duplicate-name" }
        });

        // Assert
        AssertNoViolations(results, "CertificatesPage_DataTables");
    }

    [Fact]
    public async Task CertificatesPage_FilterControls_ShouldBeAccessible()
    {
        // Arrange
        var url = $"{_baseUrl}/Certificate";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var results = await RunAccessibilityCheck(url, new AxeOptions
        {
            Rules = new[] { "label", "select-name", "autocomplete-valid" }
        });

        // Assert
        AssertNoViolations(results, "CertificatesPage_FilterControls");
    }

    [Fact]
    public async Task CertificatesPage_SearchFunction_ShouldBeAccessible()
    {
        // Arrange
        var url = $"{_baseUrl}/Certificate";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Test search input accessibility
        var searchInput = _page.Locator("input[type='search'], input[placeholder*='search'], .search-bar input");
        
        if (await searchInput.CountAsync() > 0)
        {
            // Act
            var results = await RunAccessibilityCheck(url, new AxeOptions
            {
                Rules = new[] { "label", "aria-input-field-name" }
            });

            // Assert
            AssertNoViolations(results, "CertificatesPage_SearchFunction");
        }
    }

    [Fact]
    public async Task CertificateDetails_ShouldHaveNoWCAG21AAViolations()
    {
        // Arrange - First navigate to certificates page to find a certificate
        var certificatesUrl = $"{_baseUrl}/Certificate";
        await _page!.GotoAsync(certificatesUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Look for a certificate details link
        var detailsLink = _page.Locator("a[href*='/Certificate/Details'], .certificate-item a").First;
        
        if (await detailsLink.CountAsync() > 0)
        {
            var detailsUrl = await detailsLink.GetAttributeAsync("href");
            if (!string.IsNullOrEmpty(detailsUrl))
            {
                var fullDetailsUrl = detailsUrl.StartsWith("http") ? detailsUrl : $"{_baseUrl}{detailsUrl}";

                // Act
                var results = await RunWCAG21AACheck(fullDetailsUrl);

                // Assert
                AssertNoViolations(results, "CertificateDetails_WCAG21AA");
            }
        }
    }

    [Fact]
    public async Task CertificateTiles_ShouldHaveProperARIALabels()
    {
        // Arrange
        var url = $"{_baseUrl}/Certificate/CertificateTileView";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var results = await RunAccessibilityCheck(url, new AxeOptions
        {
            Rules = new[] { "aria-label", "button-name", "link-name" }
        });

        // Assert
        AssertNoViolations(results, "CertificateTiles_ARIALabels");
    }

    [Fact]
    public async Task CertificatesPage_Pagination_ShouldBeAccessible()
    {
        // Arrange
        var url = $"{_baseUrl}/Certificate";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Check if pagination exists
        var paginationElements = _page.Locator(".pagination, .pager, [aria-label*='pagination']");
        
        if (await paginationElements.CountAsync() > 0)
        {
            // Act
            var results = await RunAccessibilityCheck(url, new AxeOptions
            {
                Rules = new[] { "link-name", "button-name", "aria-label" }
            });

            // Assert
            AssertNoViolations(results, "CertificatesPage_Pagination");
        }
    }

    [Fact]
    public async Task CertificatesPage_SortControls_ShouldBeAccessible()
    {
        // Arrange
        var url = $"{_baseUrl}/Certificate";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Look for sort controls
        var sortControls = _page.Locator("select[name*='sort'], .sort-control, th[role='columnheader']");
        
        if (await sortControls.CountAsync() > 0)
        {
            // Act
            var results = await RunAccessibilityCheck(url, new AxeOptions
            {
                Rules = new[] { "select-name", "button-name", "aria-sort" }
            });

            // Assert
            AssertNoViolations(results, "CertificatesPage_SortControls");
        }
    }
}
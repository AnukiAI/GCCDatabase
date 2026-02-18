using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.PageObjects;

public class CertificatePage
{
    private readonly IPage _page;

    public CertificatePage(IPage page)
    {
        _page = page;
    }

    // Locators
    public ILocator CertificateTitle => _page.Locator("h1, .certificate-title");
    public ILocator CompanyName => _page.Locator(".company-name, .certificate-company");
    public ILocator ProductName => _page.Locator(".product-name, .certificate-product");
    public ILocator CertificateDetails => _page.Locator(".certificate-details, .details-section").First;
    public ILocator FilterDropdown => _page.Locator("#filter-dropdown, .filter-select");
    public ILocator SortDropdown => _page.Locator("#sort-dropdown, .sort-select");
    public ILocator CertificateGrid => _page.Locator(".certificate-grid, .certificates-container");
    public ILocator TileViewButton => _page.Locator("button:has-text('Tile'), .view-toggle-tile");
    public ILocator ListViewButton => _page.Locator("button:has-text('List'), .view-toggle-list");

    // Actions
    public async Task SelectFilter(string filterValue)
    {
        await FilterDropdown.SelectOptionAsync(filterValue);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task SelectSort(string sortValue)
    {
        await SortDropdown.SelectOptionAsync(sortValue);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task SwitchToTileView()
    {
        await TileViewButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task SwitchToListView()
    {
        await ListViewButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // Assertions
    public async Task<string> GetCertificateTitle()
    {
        return await CertificateTitle.TextContentAsync() ?? "";
    }

    public async Task<string> GetCompanyName()
    {
        return await CompanyName.TextContentAsync() ?? "";
    }

    public async Task<string> GetProductName()
    {
        return await ProductName.TextContentAsync() ?? "";
    }

    public async Task<bool> IsCertificateDetailsVisible()
    {
        return await CertificateDetails.IsVisibleAsync();
    }

    public async Task<int> GetCertificateCount()
    {
        return await CertificateGrid.Locator(".certificate-item").CountAsync();
    }
}
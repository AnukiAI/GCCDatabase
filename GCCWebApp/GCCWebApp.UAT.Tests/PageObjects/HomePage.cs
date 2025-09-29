using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.PageObjects;

public class HomePage
{
    private readonly IPage _page;

    public HomePage(IPage page)
    {
        _page = page;
    }

    // Locators
    public ILocator SearchInput => _page.Locator("#search-input, input[name='searchTerm']");
    public ILocator SearchButton => _page.Locator("#search-button, button[type='submit']");
    public ILocator CertificateCards => _page.Locator(".certificate-card, .certificate-tile");
    public ILocator NavigationMenu => _page.Locator(".navbar-nav");
    public ILocator LoginLink => _page.Locator("a[href*='Login'], .nav-link:has-text('Login')");

    // Actions
    public async Task SearchForCertificates(string searchTerm)
    {
        await SearchInput.FillAsync(searchTerm);
        await SearchButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task ClickFirstCertificate()
    {
        await CertificateCards.First.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task NavigateToLogin()
    {
        await LoginLink.ClickAsync();
        await _page.WaitForURLAsync(url => url.Contains("/Account/Login"));
    }

    // Assertions
    public async Task<int> GetCertificateCount()
    {
        return await CertificateCards.CountAsync();
    }

    public async Task<bool> IsSearchResultsVisible()
    {
        return await CertificateCards.First.IsVisibleAsync();
    }
}
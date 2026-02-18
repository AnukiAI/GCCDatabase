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
    public ILocator SearchInput => _page.Locator("#searchInput, #search-input, input[name='searchTerm']");
    public ILocator SearchButton => _page.Locator("#search-button, button[type='submit']");
    public ILocator CertificateCards => _page.Locator(".certificate-card, .certificate-tile, .cert-tile");
    public ILocator NavigationMenu => _page.Locator(".navbar-nav");
    public ILocator LoginLink => _page.Locator("a[href*='Login'], .nav-link:has-text('Login')");

    // Actions
    public async Task SearchForCertificates(string searchTerm)
    {
        var isVisible = await SearchInput.First.IsVisibleAsync();
        if (isVisible)
        {
            await SearchInput.First.FillAsync(searchTerm);
            // Search may auto-trigger or need button click
            var hasSearchButton = await SearchButton.First.IsVisibleAsync();
            if (hasSearchButton)
            {
                await SearchButton.First.ClickAsync();
            }
            else
            {
                await _page.Keyboard.PressAsync("Enter");
            }
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
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
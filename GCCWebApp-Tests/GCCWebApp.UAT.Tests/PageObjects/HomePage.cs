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

    // User profile dropdown (contains login/logout links)
    public ILocator UserProfile => _page.Locator(".user-profile");
    public ILocator UserDropdown => _page.Locator(".user-dropdown");
    public ILocator LoginLink => _page.Locator(".user-dropdown a[href*='Login'], .dropdown-item:has-text('Sign In')");
    public ILocator LogoutButton => _page.Locator(".user-dropdown .logout-button, .dropdown-item:has-text('Logout')");

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
        // First, open the user profile dropdown by hovering/clicking on it
        await UserProfile.HoverAsync();
        await _page.WaitForTimeoutAsync(300); // Brief wait for dropdown animation

        // Check if dropdown is now visible
        var dropdownVisible = await UserDropdown.IsVisibleAsync();
        if (!dropdownVisible)
        {
            // Try clicking if hover didn't work
            await UserProfile.ClickAsync();
            await _page.WaitForTimeoutAsync(300);
        }

        // Now click the login link
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
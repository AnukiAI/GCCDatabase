using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.PageObjects;

public class LoginPage
{
    private readonly IPage _page;

    public LoginPage(IPage page)
    {
        _page = page;
    }

    // Locators
    public ILocator EmailInput => _page.Locator("#Email, input[name='Email']");
    public ILocator PasswordInput => _page.Locator("#Password, input[name='Password']");
    public ILocator LoginButton => _page.Locator("button[type='submit'], input[type='submit']");
    public ILocator ForgotPasswordLink => _page.Locator("a[href*='ForgotPassword']");
    public ILocator ErrorMessage => _page.Locator(".alert-danger, .text-danger");

    // Actions
    public async Task LoginAsync(string email, string password)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        await LoginButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task ClickForgotPassword()
    {
        await ForgotPasswordLink.ClickAsync();
        await _page.WaitForURLAsync(url => url.Contains("/Account/ForgotPassword"));
    }

    // Assertions
    public async Task<bool> IsErrorMessageVisible()
    {
        return await ErrorMessage.IsVisibleAsync();
    }

    public async Task<string> GetErrorMessage()
    {
        return await ErrorMessage.TextContentAsync() ?? "";
    }

    public async Task<bool> IsOnLoginPage()
    {
        return _page.Url.Contains("/Account/Login");
    }
}
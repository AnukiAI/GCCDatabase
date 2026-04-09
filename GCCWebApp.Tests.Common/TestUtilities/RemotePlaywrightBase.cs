using Microsoft.Playwright;
using Xunit;

namespace GCCWebApp.Tests.Common.TestUtilities;

/// <summary>
/// Base class for Playwright browser tests against a remote deployed site.
/// Usable as xUnit IAsyncLifetime or instantiated directly for NUnit.
/// </summary>
public class RemotePlaywrightBase : IAsyncLifetime
{
    public static string BaseUrl => RemoteTestBase.BaseUrl;

    protected IPlaywright? Playwright { get; private set; }
    protected IBrowser? Browser { get; private set; }
    protected IPage? Page { get; private set; }

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        Page = await Browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (Page != null) await Page.CloseAsync();
        if (Browser != null) await Browser.CloseAsync();
        Playwright?.Dispose();
    }

    /// <summary>
    /// Login via the browser login form.
    /// </summary>
    public async Task LoginAsync(string role = "Admin")
    {
        if (Page == null) throw new InvalidOperationException("Call InitializeAsync first");

        if (!RemoteTestBase.TestUsers.TryGetValue(role, out var creds) || string.IsNullOrEmpty(creds.Email))
            return;

        await Page.GotoAsync($"{BaseUrl}/Account/Login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.FillAsync("#Email", creds.Email);
        await Page.FillAsync("#Password", creds.Password);
        await Page.ClickAsync("button[type='submit']");

        await Page.WaitForURLAsync(url => !url.Contains("/Account/Login"),
            new() { Timeout = 15000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Navigate to a page on the remote site.
    /// </summary>
    public async Task NavigateAsync(string path)
    {
        if (Page == null) throw new InvalidOperationException("Call InitializeAsync first");
        await Page.GotoAsync($"{BaseUrl}{path}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}

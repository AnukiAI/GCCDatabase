using System.Net;
using System.Text.RegularExpressions;
using Xunit;

namespace GCCWebApp.Tests.Common.TestUtilities;

/// <summary>
/// Base class for tests that run against a remote deployed site.
/// Provides HttpClient with cookie-based session auth and API key auth.
/// Usable as xUnit IAsyncLifetime or as a static helper for NUnit.
/// </summary>
public class RemoteTestBase : IAsyncLifetime
{
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "https://gccwebapp.com";

    public static readonly Dictionary<string, (string Email, string Password)> TestUsers = new()
    {
        ["Admin"] = ("admin", "TestPass123!"),
        ["Analyst"] = ("analyst", "TestPass123!"),
        ["External"] = ("external", "TestPass123!"),
        ["Public"] = ("", "")
    };

    private HttpClientHandler? _handler;
    private HttpClient? _client;

    public HttpClient Client => _client ?? throw new InvalidOperationException("Call InitializeAsync first");

    public Task InitializeAsync()
    {
        _handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = true,
            UseCookies = true
        };
        _client = new HttpClient(_handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client?.Dispose();
        _handler?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Login via form POST with anti-forgery token extraction.
    /// </summary>
    public async Task LoginAsync(string role = "Admin")
    {
        if (!TestUsers.TryGetValue(role, out var creds))
            throw new ArgumentException($"Unknown role: {role}");

        if (string.IsNullOrEmpty(creds.Email)) return; // Public user

        await LoginAsync(creds.Email, creds.Password);
    }

    /// <summary>
    /// Login with specific credentials.
    /// </summary>
    public async Task LoginAsync(string email, string password)
    {
        // GET login page to extract anti-forgery token
        var loginPageResponse = await Client.GetAsync("/Account/Login");
        loginPageResponse.EnsureSuccessStatusCode();
        var loginPageHtml = await loginPageResponse.Content.ReadAsStringAsync();

        // Extract __RequestVerificationToken
        var token = ExtractAntiForgeryToken(loginPageHtml);

        // POST credentials
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", email),
            new KeyValuePair<string, string>("Password", password),
            new KeyValuePair<string, string>("RememberMe", "false"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var response = await Client.PostAsync("/Account/Login", formData);
        // Should redirect on success — verify we're not still on login page
        var finalUrl = response.RequestMessage?.RequestUri?.AbsolutePath ?? "";
        if (finalUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Login failed for user '{email}'. Still on login page.");
        }
    }

    /// <summary>
    /// Create a new HttpClient with API key authentication.
    /// </summary>
    public static HttpClient CreateApiKeyClient(string apiKey)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        return client;
    }

    /// <summary>
    /// Create a new unauthenticated HttpClient with cookie support.
    /// </summary>
    public static HttpClient CreateAnonymousClient()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = true,
            UseCookies = true
        };
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// Create a new authenticated HttpClient for a specific role.
    /// </summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(string role = "Admin")
    {
        var testBase = new RemoteTestBase();
        await testBase.InitializeAsync();
        await testBase.LoginAsync(role);
        return testBase.Client;
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        var match = Regex.Match(html,
            @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            // Try alternate attribute order
            match = Regex.Match(html,
                @"<input[^>]*value=""([^""]+)""[^>]*name=""__RequestVerificationToken""",
                RegexOptions.IgnoreCase);
        }

        if (!match.Success)
            throw new InvalidOperationException("Could not find __RequestVerificationToken on login page");

        return match.Groups[1].Value;
    }
}

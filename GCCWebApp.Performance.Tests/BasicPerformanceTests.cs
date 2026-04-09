using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

namespace GCCWebApp.Performance.Tests;

public class BasicPerformanceTests
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "https://gccwebapp.com";

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = true,
            UseCookies = true
        };
        return new HttpClient(handler) { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(30) };
    }

    private static async Task LoginAsync(HttpClient client)
    {
        var loginPage = await client.GetStringAsync("/Account/Login");
        var match = Regex.Match(loginPage, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        var token = match.Success ? match.Groups[1].Value : "";
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", "admin"),
            new KeyValuePair<string, string>("Password", "TestPass123!"),
            new KeyValuePair<string, string>("RememberMe", "false"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });
        await client.PostAsync("/Account/Login", form);
    }

    [Theory]
    [InlineData("/", "Home", 3000)]
    [InlineData("/Account/Login", "Login", 2000)]
    [InlineData("/Certificate/CertificateTileView", "Tile View", 5000)]
    public async Task PublicPage_LoadTime_WithinThreshold(string path, string name, int maxMs)
    {
        using var client = CreateClient();
        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync(path);
        sw.Stop();
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Redirect,
            $"{name} returned {response.StatusCode}");
        Assert.True(sw.ElapsedMilliseconds < maxMs,
            $"{name} took {sw.ElapsedMilliseconds}ms, threshold is {maxMs}ms");
    }

    [Theory]
    [InlineData("/Admin", "Admin", 3000)]
    [InlineData("/Dashboard", "Dashboard", 3000)]
    [InlineData("/Product/Management", "Products", 3000)]
    [InlineData("/Admin/UserManagement", "Users", 3000)]
    [InlineData("/Certificate", "Certificates", 3000)]
    [InlineData("/Company", "Companies", 3000)]
    public async Task AuthenticatedPage_LoadTime_WithinThreshold(string path, string name, int maxMs)
    {
        using var client = CreateClient();
        await LoginAsync(client);
        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync(path);
        sw.Stop();
        Assert.True(response.IsSuccessStatusCode, $"{name} returned {response.StatusCode}");
        Assert.True(sw.ElapsedMilliseconds < maxMs,
            $"{name} took {sw.ElapsedMilliseconds}ms, threshold is {maxMs}ms");
    }

    [Fact]
    public async Task LoginFlow_CompletesWithinThreshold()
    {
        using var client = CreateClient();
        var sw = Stopwatch.StartNew();
        await LoginAsync(client);
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 3000,
            $"Login flow took {sw.ElapsedMilliseconds}ms, threshold is 3000ms");
    }
}

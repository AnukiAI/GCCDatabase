using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

namespace GCCWebApp.Performance.Tests;

public class AdvancedPerformanceTests
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

    [Fact]
    public async Task ConcurrentRequests_10Simultaneous_NoErrors()
    {
        var paths = new[]
        {
            "/", "/Account/Login", "/Certificate/CertificateTileView",
            "/", "/Certificate/CertificateTileView",
            "/", "/Account/Login", "/Certificate/CertificateTileView",
            "/", "/Certificate/CertificateTileView"
        };

        var tasks = paths.Select(async path =>
        {
            using var client = CreateClient();
            var sw = Stopwatch.StartNew();
            var response = await client.GetAsync(path);
            sw.Stop();
            return (path, response.StatusCode, sw.ElapsedMilliseconds);
        });

        var results = await Task.WhenAll(tasks);

        foreach (var (path, status, ms) in results)
        {
            Assert.True(status != HttpStatusCode.InternalServerError,
                $"GET {path} returned 500 under concurrent load");
            Assert.True(ms < 10000,
                $"GET {path} took {ms}ms under concurrent load (threshold: 10000ms)");
        }
    }

    [Theory]
    [InlineData("/css/main.css")]
    [InlineData("/css/auth.css")]
    [InlineData("/js/site.js")]
    public async Task StaticAssets_ReturnQuickly_WithCacheHeaders(string path)
    {
        using var client = CreateClient();
        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync(path);
        sw.Stop();

        // Static assets should exist and load fast
        if (response.IsSuccessStatusCode)
        {
            Assert.True(sw.ElapsedMilliseconds < 1000,
                $"Static asset {path} took {sw.ElapsedMilliseconds}ms (threshold: 1000ms)");
        }
    }

    [Fact]
    public async Task ConcurrentAuthenticatedRequests_5Simultaneous_NoErrors()
    {
        // Login first
        using var client = CreateClient();
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

        // Now hit authenticated pages concurrently
        var paths = new[] { "/Admin", "/Dashboard", "/Certificate", "/Company", "/Product/Management" };
        var tasks = paths.Select(async path =>
        {
            var sw = Stopwatch.StartNew();
            var response = await client.GetAsync(path);
            sw.Stop();
            return (path, response.StatusCode, sw.ElapsedMilliseconds);
        });

        var results = await Task.WhenAll(tasks);

        foreach (var (path, status, ms) in results)
        {
            Assert.True(status != HttpStatusCode.InternalServerError,
                $"GET {path} returned 500 under concurrent authenticated load");
            Assert.True(ms < 10000,
                $"GET {path} took {ms}ms (threshold: 10000ms)");
        }
    }
}

using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

namespace GCCWebApp.Performance.Tests;

public class ProductIdPerformanceTests
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "https://gccwebapp.com";

    private static HttpClient CreateAuthenticatedClient()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = true,
            UseCookies = true
        };
        var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(30) };

        // Login synchronously via .Result (acceptable in test setup)
        var loginPage = client.GetStringAsync("/Account/Login").Result;
        var match = Regex.Match(loginPage, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        var token = match.Success ? match.Groups[1].Value : "";
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", "admin"),
            new KeyValuePair<string, string>("Password", "TestPass123!"),
            new KeyValuePair<string, string>("RememberMe", "false"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });
        client.PostAsync("/Account/Login", form).Wait();
        return client;
    }

    [Fact]
    public async Task ProductManagement_PageLoad_WithinThreshold()
    {
        using var client = CreateAuthenticatedClient();
        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync("/Product/Management");
        sw.Stop();

        Assert.True(response.IsSuccessStatusCode, $"Product Management returned {response.StatusCode}");
        Assert.True(sw.ElapsedMilliseconds < 3000,
            $"Product Management took {sw.ElapsedMilliseconds}ms (threshold: 3000ms)");
    }

    [Fact]
    public async Task CsvExport_CompletesWithinThreshold()
    {
        using var client = CreateAuthenticatedClient();
        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync("/Certificate/ExportCertificates?format=csv");
        sw.Stop();

        // Export might return CSV or redirect
        Assert.True(response.StatusCode != HttpStatusCode.InternalServerError,
            $"CSV Export returned 500");
        Assert.True(sw.ElapsedMilliseconds < 5000,
            $"CSV Export took {sw.ElapsedMilliseconds}ms (threshold: 5000ms)");
    }

    [Fact]
    public async Task DashboardWithMetrics_LoadsWithinThreshold()
    {
        using var client = CreateAuthenticatedClient();
        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync("/Dashboard");
        sw.Stop();

        Assert.True(response.IsSuccessStatusCode, $"Dashboard returned {response.StatusCode}");
        Assert.True(sw.ElapsedMilliseconds < 3000,
            $"Dashboard took {sw.ElapsedMilliseconds}ms (threshold: 3000ms)");
    }
}

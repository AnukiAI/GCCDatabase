using System.Diagnostics;
using System.Net;

namespace GCCWebApp.Performance.Tests;

public class InfiniteScrollPerformanceTests
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "https://gccwebapp.com";

    [Fact]
    public async Task InfiniteScroll_5Pages_EachUnder2Seconds()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(10) };

        for (int page = 1; page <= 5; page++)
        {
            var sw = Stopwatch.StartNew();
            var response = await client.GetAsync($"/api/certificate-tiles?page={page}&pageSize=20");
            sw.Stop();

            // API might return 200 or 404 if no more data
            Assert.True(response.StatusCode != HttpStatusCode.InternalServerError,
                $"Tile API page {page} returned 500");
            Assert.True(sw.ElapsedMilliseconds < 2000,
                $"Tile API page {page} took {sw.ElapsedMilliseconds}ms (threshold: 2000ms)");
        }
    }

    [Fact]
    public async Task TileView_WithSearchFilter_RespondsQuickly()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(10) };

        var searches = new[] { "Green", "Certified", "Paper", "test" };

        foreach (var term in searches)
        {
            var sw = Stopwatch.StartNew();
            var response = await client.GetAsync($"/api/certificate-tiles?search={term}&page=1&pageSize=20");
            sw.Stop();

            Assert.True(response.StatusCode != HttpStatusCode.InternalServerError,
                $"Tile search for '{term}' returned 500");
            Assert.True(sw.ElapsedMilliseconds < 3000,
                $"Tile search for '{term}' took {sw.ElapsedMilliseconds}ms (threshold: 3000ms)");
        }
    }
}

using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using GCCWebApp.RoleBasedUI.Tests.Helpers;

namespace GCCWebApp.RoleBasedUI.Tests.Tests.Performance;

[TestFixture]
public class PerformanceTests : PageTest
{
    private TestWebApplicationFactory _factory = null!;
    private string _baseUrl = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new TestWebApplicationFactory();
        _baseUrl = _factory.CreateClient().BaseAddress!.ToString().TrimEnd('/');
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _factory?.Dispose();
    }

    [Test]
    public async Task Homepage_ShouldLoadWithinAcceptableTime()
    {
        var start = DateTime.UtcNow;
        await Page.GotoAsync(_baseUrl);
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        var elapsed = DateTime.UtcNow - start;
        
        Assert.That(elapsed.TotalMilliseconds, Is.LessThan(3000), 
            "Homepage should load within 3 seconds");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(Homepage_ShouldLoadWithinAcceptableTime), "Performance");
    }

    [Test]
    public async Task LoginPage_ShouldLoadQuickly()
    {
        var start = DateTime.UtcNow;
        await Page.GotoAsync($"{_baseUrl}/Account/Login");
        await Page.WaitForSelectorAsync("#Email");
        var elapsed = DateTime.UtcNow - start;
        
        Assert.That(elapsed.TotalMilliseconds, Is.LessThan(2000), 
            "Login page should load within 2 seconds");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(LoginPage_ShouldLoadQuickly), "Performance");
    }

    [Test]
    public async Task Dashboard_ShouldLoadWithinTimeLimit()
    {
        var (email, password) = RoleTestHelper.TestUsers["Admin"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        var start = DateTime.UtcNow;
        await Page.GotoAsync($"{_baseUrl}/Dashboard");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        var elapsed = DateTime.UtcNow - start;
        
        Assert.That(elapsed.TotalMilliseconds, Is.LessThan(4000), 
            "Dashboard should load within 4 seconds");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(Dashboard_ShouldLoadWithinTimeLimit), "Performance");
    }

    [Test]
    public async Task CertificatePage_ShouldHandleLargeDataSets()
    {
        var (email, password) = RoleTestHelper.TestUsers["Admin"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        var start = DateTime.UtcNow;
        await Page.GotoAsync($"{_baseUrl}/Certificate/Certificates");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        var elapsed = DateTime.UtcNow - start;
        
        Assert.That(elapsed.TotalMilliseconds, Is.LessThan(5000), 
            "Certificate page should load within 5 seconds");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(CertificatePage_ShouldHandleLargeDataSets), "Performance");
    }

    [Test]
    public async Task PageNavigation_ShouldBeResponsive()
    {
        var (email, password) = RoleTestHelper.TestUsers["Admin"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        var pages = new[]
        {
            "/Dashboard",
            "/Certificate/Certificates",
            "/Company/Management",
            "/Admin/UserManagement"
        };

        foreach (var pageUrl in pages)
        {
            var start = DateTime.UtcNow;
            await Page.GotoAsync($"{_baseUrl}{pageUrl}");
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            var elapsed = DateTime.UtcNow - start;
            
            Assert.That(elapsed.TotalMilliseconds, Is.LessThan(3000), 
                $"Page {pageUrl} should load within 3 seconds");
        }
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(PageNavigation_ShouldBeResponsive), "Performance");
    }

    [Test]
    public async Task SearchFunctionality_ShouldReturnResultsQuickly()
    {
        var (email, password) = RoleTestHelper.TestUsers["Admin"];
        await RoleTestHelper.LoginAsync(Page, email, password, _baseUrl);
        
        await Page.GotoAsync($"{_baseUrl}/Certificate/Certificates");
        
        var start = DateTime.UtcNow;
        await Page.FillAsync("input[name='search']", "test", new() { Timeout = 1000 });
        await Page.ClickAsync("button[type='submit']", new() { Timeout = 1000 });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new() { Timeout = 5000 });
        var elapsed = DateTime.UtcNow - start;
        
        Assert.That(elapsed.TotalMilliseconds, Is.LessThan(3000), 
            "Search should return results within 3 seconds");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(SearchFunctionality_ShouldReturnResultsQuickly), "Performance");
    }

    [Test]
    public async Task ConcurrentUserSimulation_ShouldMaintainPerformance()
    {
        var tasks = new List<Task>();
        var results = new List<TimeSpan>();
        
        for (int i = 0; i < 3; i++)
        {
            var taskIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                var context = await Browser.NewContextAsync();
                var page = await context.NewPageAsync();
                
                try
                {
                    var start = DateTime.UtcNow;
                    await page.GotoAsync(_baseUrl);
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                    var elapsed = DateTime.UtcNow - start;
                    
                    lock (results)
                    {
                        results.Add(elapsed);
                    }
                }
                finally
                {
                    await context.CloseAsync();
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        
        Assert.That(results.Count, Is.EqualTo(3), "All concurrent requests should complete");
        Assert.That(results.All(r => r.TotalMilliseconds < 5000), Is.True, 
            "All concurrent requests should complete within 5 seconds");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(ConcurrentUserSimulation_ShouldMaintainPerformance), "Performance");
    }

    [Test]
    public async Task ImageAndAssetLoading_ShouldBeOptimized()
    {
        var responses = new List<IResponse>();
        
        Page.Response += (_, response) =>
        {
            if (response.Url.Contains(".css") || response.Url.Contains(".js") || 
                response.Url.Contains(".png") || response.Url.Contains(".jpg"))
            {
                responses.Add(response);
            }
        };
        
        await Page.GotoAsync(_baseUrl);
        await Page.WaitForLoadStateAsync(LoadState.Load);
        
        var slowAssets = responses.Where(r => r.Request.Timing.ResponseEnd > 2000).ToList();
        
        Assert.That(slowAssets.Count, Is.EqualTo(0), 
            $"No assets should take longer than 2 seconds to load. Slow assets: {string.Join(", ", slowAssets.Select(a => a.Url))}");
        
        await RoleTestHelper.TakeScreenshotAsync(Page, nameof(ImageAndAssetLoading_ShouldBeOptimized), "Performance");
    }
}
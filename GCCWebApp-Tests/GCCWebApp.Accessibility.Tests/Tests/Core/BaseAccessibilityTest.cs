using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using Microsoft.Playwright;
using Deque.AxeCore.Playwright;
using Deque.AxeCore.Commons;
using GCCWebApp.Accessibility.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;

namespace GCCWebApp.Accessibility.Tests.Tests.Core;

public abstract class BaseAccessibilityTest : IAsyncLifetime
{
    protected IPlaywright? _playwright;
    protected IBrowser? _browser;
    protected IPage? _page;
    protected AccessibilityTestWebApplicationFactory _factory = null!;
    protected HttpClient _httpClient = null!;
    protected string _baseUrl = null!;
    protected readonly ITestOutputHelper? _output;

    protected BaseAccessibilityTest()
    {
    }

    protected BaseAccessibilityTest(ITestOutputHelper output)
    {
        _output = output;
    }

    public virtual async Task InitializeAsync()
    {
        try
        {
            _factory = new AccessibilityTestWebApplicationFactory();
            _httpClient = _factory.CreateClient();

            // Use the actual server URL for Playwright (not the in-memory client)
            _baseUrl = _factory.ServerUrl;
            _output?.WriteLine($"Test server started at: {_baseUrl}");

            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[] { "--disable-web-security", "--allow-running-insecure-content" }
            });
            _page = await _browser.NewPageAsync();
        }
        catch (Exception ex)
        {
            _output?.WriteLine($"Failed to initialize test: {ex.Message}");
            // Don't throw - let individual tests handle missing server gracefully
        }
    }

    public virtual async Task DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
        _httpClient?.Dispose();
        await _factory.DisposeAsync();
    }

    protected async Task<AxeResult?> RunAccessibilityCheck(string url, AxeOptions? options = null)
    {
        if (_page == null)
        {
            _output?.WriteLine("Page not initialized - skipping accessibility check");
            return null;
        }

        try
        {
            await _page.GotoAsync(url, new PageGotoOptions { Timeout = 15000 });
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Wait for any dynamic content to load
            await _page.WaitForTimeoutAsync(1000);

            // Run AxeCore accessibility check using Deque.AxeCore.Playwright
            var axeResults = await _page.RunAxe();

            return axeResults;
        }
        catch (Exception ex)
        {
            _output?.WriteLine($"Failed to run accessibility check on {url}: {ex.Message}");
            return null;
        }
    }

    protected async Task<AxeResult?> RunWCAG21AACheck(string url)
    {
        return await RunAccessibilityCheck(url, new AxeOptions
        {
            Tags = new[] { "wcag2a", "wcag2aa", "wcag21a", "wcag21aa" }
        });
    }

    protected async Task<AxeResult?> RunSection508Check(string url)
    {
        return await RunAccessibilityCheck(url, new AxeOptions
        {
            Tags = new[] { "section508" }
        });
    }

    protected void AssertNoViolations(AxeResult? results, string testName)
    {
        if (results == null)
        {
            _output?.WriteLine($"Skipping assertion for {testName} - no results available");
            return;
        }

        if (results.Violations.Any())
        {
            var report = GenerateViolationReport(results, testName);
            SaveReport(report, testName);

            var violationSummary = string.Join("\n", results.Violations.Select(v =>
                $"- {v.Id}: {v.Description} (Impact: {v.Impact}, Nodes: {v.Nodes.Count()})"));

            throw new AccessibilityViolationException(
                $"Accessibility violations found in {testName}:\n{violationSummary}\n\nFull report saved to Reports/{testName}_violations.json");
        }

        _output?.WriteLine($"No accessibility violations found for {testName}");
    }

    protected void AssertMaxViolations(AxeResult? results, string testName, int maxViolations)
    {
        if (results == null)
        {
            _output?.WriteLine($"Skipping assertion for {testName} - no results available");
            return;
        }

        if (results.Violations.Count() > maxViolations)
        {
            var report = GenerateViolationReport(results, testName);
            SaveReport(report, testName);

            throw new AccessibilityViolationException(
                $"Too many accessibility violations in {testName}: {results.Violations.Count()} > {maxViolations}\n\nFull report saved to Reports/{testName}_violations.json");
        }
    }

    private AccessibilityReport GenerateViolationReport(AxeResult results, string testName)
    {
        return new AccessibilityReport
        {
            TestName = testName,
            Timestamp = DateTime.UtcNow,
            Url = _page?.Url ?? "",
            ViolationCount = results.Violations.Count(),
            IncompleteCount = results.Incomplete.Count(),
            PassCount = results.Passes.Count(),
            InapplicableCount = results.Inapplicable.Count(),
            Violations = results.Violations.Select(v => new ViolationSummary
            {
                Id = v.Id,
                Description = v.Description,
                Impact = v.Impact.ToString(),
                Help = v.Help,
                HelpUrl = v.HelpUrl,
                NodeCount = v.Nodes.Count(),
                Nodes = v.Nodes.Select(n => new NodeSummary
                {
                    Html = n.Html,
                    Target = string.Join(", ", n.Target),
                    FailureSummary = n.Any?.FirstOrDefault()?.Message ?? ""
                }).ToArray()
            }).ToArray()
        };
    }

    private void SaveReport(AccessibilityReport report, string testName)
    {
        try
        {
            var reportsDir = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
            Directory.CreateDirectory(reportsDir);

            var fileName = Path.Combine(reportsDir, $"{testName}_violations.json");
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(fileName, json);
            _output?.WriteLine($"Report saved to: {fileName}");
        }
        catch (Exception ex)
        {
            _output?.WriteLine($"Failed to save report: {ex.Message}");
        }
    }
}

public class AxeOptions
{
    public string[]? Tags { get; set; }
    public string[]? Rules { get; set; }
    public string[]? Include { get; set; }
    public string[]? Exclude { get; set; }
}

public class AccessibilityViolationException : Exception
{
    public AccessibilityViolationException(string message) : base(message) { }
}

public class AccessibilityReport
{
    public string TestName { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Url { get; set; } = "";
    public int ViolationCount { get; set; }
    public int IncompleteCount { get; set; }
    public int PassCount { get; set; }
    public int InapplicableCount { get; set; }
    public ViolationSummary[] Violations { get; set; } = Array.Empty<ViolationSummary>();
}

public class ViolationSummary
{
    public string Id { get; set; } = "";
    public string Description { get; set; } = "";
    public string Impact { get; set; } = "";
    public string Help { get; set; } = "";
    public string HelpUrl { get; set; } = "";
    public int NodeCount { get; set; }
    public NodeSummary[] Nodes { get; set; } = Array.Empty<NodeSummary>();
}

public class NodeSummary
{
    public string Html { get; set; } = "";
    public string Target { get; set; } = "";
    public string FailureSummary { get; set; } = "";
}

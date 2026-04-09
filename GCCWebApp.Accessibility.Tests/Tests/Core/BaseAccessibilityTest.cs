using Microsoft.Playwright;
using Deque.AxeCore.Playwright;
using Deque.AxeCore.Commons;
using System.Text.Json;

namespace GCCWebApp.Accessibility.Tests.Tests.Core;

public abstract class BaseAccessibilityTest : IAsyncLifetime
{
    protected IPlaywright? _playwright;
    protected IBrowser? _browser;
    protected IPage? _page;
    protected string _baseUrl = null!;

    public virtual async Task InitializeAsync()
    {
        _baseUrl = (Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "https://gccwebapp.com").TrimEnd('/');

        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        _page = await _browser.NewPageAsync();
    }

    public virtual async Task DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    protected async Task LoginAsync(string email = "admin", string password = "TestPass123!")
    {
        if (_page == null) throw new InvalidOperationException("Page not initialized");

        await _page.GotoAsync($"{_baseUrl}/Account/Login");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.FillAsync("#Email", email);
        await _page.FillAsync("#Password", password);
        await _page.ClickAsync("button[type='submit']");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    protected async Task<AxeResult> RunAccessibilityCheck(string url, AccessibilityCheckOptions? options = null)
    {
        if (_page == null) throw new InvalidOperationException("Page not initialized");

        await _page.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForTimeoutAsync(1000);

        return await _page.RunAxe();
    }

    protected async Task<AxeResult> RunWCAG21AACheck(string url)
    {
        return await RunAccessibilityCheck(url, new AccessibilityCheckOptions
        {
            Tags = new[] { "wcag2a", "wcag2aa", "wcag21a", "wcag21aa" }
        });
    }

    protected async Task<AxeResult> RunSection508Check(string url)
    {
        return await RunAccessibilityCheck(url, new AccessibilityCheckOptions
        {
            Tags = new[] { "section508" }
        });
    }

    protected void AssertNoViolations(AxeResult results, string testName)
    {
        if (results.Violations.Any())
        {
            var report = GenerateViolationReport(results, testName);
            SaveReport(report, testName);

            var violationSummary = string.Join("\n", results.Violations.Select(v =>
                $"- {v.Id}: {v.Description} (Impact: {v.Impact}, Nodes: {v.Nodes.Count()})"));

            throw new AccessibilityViolationException(
                $"Accessibility violations found in {testName}:\n{violationSummary}\n\nFull report saved to Reports/{testName}_violations.json");
        }
    }

    protected void AssertMaxViolations(AxeResult results, string testName, int maxViolations)
    {
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
                    FailureSummary = n.Any?.ToString() ?? ""
                }).ToArray()
            }).ToArray()
        };
    }

    private void SaveReport(AccessibilityReport report, string testName)
    {
        var reportsDir = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
        Directory.CreateDirectory(reportsDir);

        var fileName = Path.Combine(reportsDir, $"{testName}_violations.json");
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(fileName, json);
    }
}

public class AccessibilityCheckOptions
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

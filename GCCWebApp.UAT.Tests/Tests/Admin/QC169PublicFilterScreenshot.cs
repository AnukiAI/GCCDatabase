using System.Text;
using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.Tests.Admin;

/// QC #169 close-out evidence: capture the live public Certificate Tile View's
/// Certification Type filter pane to attach to the client reply.
[TestFixture]
public class QC169PublicFilterScreenshot : BaseTest
{
    [SetUp]
    public async Task PrimeBasicAuth()
    {
        if (BaseUrl.Contains("gccwebapp.com", StringComparison.OrdinalIgnoreCase))
        {
            var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("gcc:Cert-Test-Access-2026!"));
            await Context.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                ["Authorization"] = "Basic " + b64,
            });
        }
    }

    [Test]
    public async Task QC169_LivePublicView_CertTypeFilter()
    {
        await Page.SetViewportSizeAsync(1440, 900);

        await Page.GotoAsync(BaseUrl + "/Certificate/CertificateTileView", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30000,
        });

        // Wait for the cert-type filter list to be populated by the JS
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('#cert-type-content ul li').length > 0",
            null,
            new PageWaitForFunctionOptions { Timeout = 30000 });

        // Make sure the cert-type filter pane is expanded
        var collapsed = await Page.Locator("#cert-type-content").IsHiddenAsync();
        if (collapsed)
        {
            var header = Page.Locator("[data-target='cert-type-content'], #cert-type-header").First;
            if (await header.CountAsync() > 0)
            {
                await header.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }
        }

        // Pull the rendered cert-type list for the test record
        var renderedTypes = await Page.EvaluateAsync<string[]>(
            "() => Array.from(document.querySelectorAll('#cert-type-content ul li label')).map(l => l.textContent.trim())");
        TestContext.WriteLine($"Cert-type filter has {renderedTypes.Length} item(s):");
        foreach (var t in renderedTypes) TestContext.WriteLine($"  - {t}");

        // Also pull what the API returned to compare against what's rendered
        var apiTypes = await Page.EvaluateAsync<string>(
            "async () => { const r = await fetch('/api/certificate-tiles/filters'); const j = await r.json(); return JSON.stringify(j.certificationTypes); }");
        TestContext.WriteLine($"API certificationTypes: {apiTypes}");

        // Full-page screenshot
        var screenshotDir = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots");
        Directory.CreateDirectory(screenshotDir);
        var fullPath = Path.Combine(screenshotDir, $"QC169_live_public_full_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        await Page.ScreenshotAsync(new() { Path = fullPath, FullPage = true });
        TestContext.WriteLine($"Full-page screenshot: {fullPath}");

        // Tight crop on the cert-type filter pane
        var panelPath = Path.Combine(screenshotDir, $"QC169_live_public_certtype_pane_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        var panel = Page.Locator("#cert-type-content").Locator("xpath=..").First;
        if (await panel.CountAsync() > 0)
        {
            await panel.ScreenshotAsync(new LocatorScreenshotOptions { Path = panelPath });
            TestContext.WriteLine($"Filter pane screenshot: {panelPath}");
        }

        Assert.That(renderedTypes.Length, Is.GreaterThan(0),
            "Public cert-type filter should show at least one option");
    }
}

using System.Text;
using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.Tests.Admin;

/// 2026-05-12 — User Management: pagination right-alignment regression.
/// Shipped per Shawn: pagination should be right-aligned on /Admin/UserManagement.
/// Verifies (a) the scoped CSS rule resolves to flex-end at runtime, (b) the right
/// edge of the controls hugs the right edge of the wrapper, and saves a screenshot.
[TestFixture]
public class UserManagementPaginationAlignmentTests : BaseTest
{
    [SetUp]
    public async Task PrimeGate()
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

    private async Task GotoUserManagementAsAdmin()
    {
        await LoginAsRole("Admin");
        await Page.GotoAsync(BaseUrl + "/Admin/UserManagement", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30000,
        });
        // Pagination wrapper is only injected after setData() with totalItems > pageSize,
        // which guarantees the table is populated. GetUsers XHR is slow on dev with 279
        // users + Include chains, so allow up to 60s.
        await Page.WaitForSelectorAsync("#user-pagination .pagination-wrapper",
            new PageWaitForSelectorOptions { Timeout = 60000 });
    }

    [Test]
    public async Task Pagination_Is_Right_Aligned_On_UserManagement()
    {
        await GotoUserManagementAsAdmin();

        // 1) Computed style — scoped rule must beat shared admin-tables.css.
        var justifyContent = await Page.EvalOnSelectorAsync<string>(
            "#user-pagination .pagination-wrapper",
            "el => getComputedStyle(el).justifyContent");
        justifyContent.Should().Be("flex-end",
            "scoped '#user-pagination .pagination-wrapper { justify-content: flex-end }' must override admin-tables.css's 'space-between'");

        // 2) Geometric — controls' right edge should hug the wrapper's right edge, and
        // be far from the left edge. (Returning a simple JSON tuple as a string and
        // parsing in C# avoids Playwright's Dictionary<string,double> deserialization quirks.)
        var rectsJson = await Page.EvalOnSelectorAsync<string>(
            "#user-pagination",
            @"el => {
                const wrapper = el.querySelector('.pagination-wrapper');
                const controls = el.querySelector('.pagination-controls');
                const w = wrapper.getBoundingClientRect();
                const c = controls.getBoundingClientRect();
                return JSON.stringify({
                    wrapperLeft: w.left, wrapperRight: w.right, wrapperWidth: w.width,
                    controlsLeft: c.left, controlsRight: c.right, controlsWidth: c.width
                });
            }");
        var rects = System.Text.Json.JsonDocument.Parse(rectsJson).RootElement;
        var wrapperLeft = rects.GetProperty("wrapperLeft").GetDouble();
        var wrapperRight = rects.GetProperty("wrapperRight").GetDouble();
        var controlsLeft = rects.GetProperty("controlsLeft").GetDouble();
        var controlsRight = rects.GetProperty("controlsRight").GetDouble();
        var wrapperWidth = rects.GetProperty("wrapperWidth").GetDouble();
        var controlsWidth = rects.GetProperty("controlsWidth").GetDouble();

        var rightEdgeGap = Math.Abs(controlsRight - wrapperRight);
        var leftEdgeGap = controlsLeft - wrapperLeft;
        var emptySpaceOnLeft = wrapperWidth - controlsWidth;

        TestContext.WriteLine($"Wrapper:  [{wrapperLeft:F1}, {wrapperRight:F1}]  width {wrapperWidth:F1}");
        TestContext.WriteLine($"Controls: [{controlsLeft:F1}, {controlsRight:F1}]  width {controlsWidth:F1}");
        TestContext.WriteLine($"Right-edge gap (controls→wrapper.right): {rightEdgeGap:F1}px  [expect <5]");
        TestContext.WriteLine($"Left-edge gap (controls.left - wrapper.left): {leftEdgeGap:F1}px  [expect = empty-space-on-left for flex-end]");
        TestContext.WriteLine($"Empty space on left of controls: {emptySpaceOnLeft:F1}px");

        rightEdgeGap.Should().BeLessThan(5,
            "controls' right edge should hug the wrapper's right edge under justify-content: flex-end");
        leftEdgeGap.Should().BeGreaterThan(100,
            "controls should be pushed to the right, leaving substantial empty space on the left (wrapper is full-width, controls aren't)");

        // 3) Visual — capture screenshot for the shipped log.
        var screenshotsDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Screenshots");
        Directory.CreateDirectory(screenshotsDir);
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var shotPath = Path.Combine(screenshotsDir, $"UserManagement_Pagination_RightAligned_{stamp}.png");
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = shotPath,
            FullPage = true,
        });
        TestContext.WriteLine($"Screenshot saved: {shotPath}");
    }
}

using System.Text;
using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.Tests.Admin;

/// 2026-05-12 — User Management: pagination right-alignment regression.
/// Shipped per Shawn: pagination should be right-aligned on /Admin/UserManagement.
/// Verifies the scoped CSS rule (#user-pagination .pagination-wrapper { justify-content: flex-end })
/// resolves correctly at runtime AND captures a screenshot for visual confirmation.
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
        // GetUsers XHR is slow on local dev with 279 users + Include chains; bump to 60s.
        // Wait directly for pagination wrapper — it's only injected after setData() with totalItems > 25,
        // which guarantees the table is populated.
        await Page.WaitForSelectorAsync("#user-pagination .pagination-wrapper", new PageWaitForSelectorOptions { Timeout = 60000 });
    }

    [Test]
    public async Task Pagination_Wrapper_Is_Right_Aligned()
    {
        await GotoUserManagementAsAdmin();

        var justifyContent = await Page.EvalOnSelectorAsync<string>(
            "#user-pagination .pagination-wrapper",
            "el => getComputedStyle(el).justifyContent");

        justifyContent.Should().Be("flex-end",
            "scoped rule '#user-pagination .pagination-wrapper { justify-content: flex-end }' must take precedence over the shared admin-tables.css rule (space-between)");
    }

    [Test]
    public async Task Pagination_Controls_Sit_To_The_Right_Of_The_Table()
    {
        await GotoUserManagementAsAdmin();

        // Compare the right edge of the pagination controls block to the right edge of the table.
        // If pagination is correctly right-aligned, the right edges should be within a few pixels.
        var measurements = await Page.EvaluateAsync<Dictionary<string, double>>(@"() => {
            const controls = document.querySelector('#user-pagination .pagination-controls');
            const wrapper = document.querySelector('#user-pagination .pagination-wrapper');
            const table = document.querySelector('#user-table');
            const cRect = controls.getBoundingClientRect();
            const wRect = wrapper.getBoundingClientRect();
            const tRect = table.getBoundingClientRect();
            return {
                controlsRight: cRect.right,
                controlsLeft: cRect.left,
                wrapperRight: wRect.right,
                wrapperLeft: wRect.left,
                tableRight: tRect.right,
                tableLeft: tRect.left,
                wrapperWidth: wRect.width,
                controlsWidth: cRect.width
            };
        }");

        var controlsRight = measurements["controlsRight"];
        var wrapperRight = measurements["wrapperRight"];
        var controlsLeft = measurements["controlsLeft"];
        var wrapperLeft = measurements["wrapperLeft"];

        // The controls' right edge should be within 4px of the wrapper's right edge (flex-end).
        // The controls' left edge should be FAR from the wrapper's left edge (controls pushed right).
        var rightEdgeDelta = Math.Abs(controlsRight - wrapperRight);
        var leftEdgeDelta = controlsLeft - wrapperLeft;

        TestContext.WriteLine($"Wrapper: [{wrapperLeft:F1}, {wrapperRight:F1}] (width {measurements["wrapperWidth"]:F1})");
        TestContext.WriteLine($"Controls: [{controlsLeft:F1}, {controlsRight:F1}] (width {measurements["controlsWidth"]:F1})");
        TestContext.WriteLine($"Right-edge delta: {rightEdgeDelta:F1}px; Left-edge delta: {leftEdgeDelta:F1}px");

        rightEdgeDelta.Should().BeLessThan(5,
            "pagination controls' right edge should hug the wrapper's right edge (right-aligned)");
        leftEdgeDelta.Should().BeGreaterThan(100,
            "pagination controls should be pushed to the right, leaving a large gap on the left side of the wrapper");

        // Capture full-page screenshot for visual confirmation.
        var screenshotsDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Screenshots");
        Directory.CreateDirectory(screenshotsDir);
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var shotPath = Path.Combine(screenshotsDir, $"UserManagement_Pagination_RightAligned_{stamp}.png");
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = shotPath,
            FullPage = true,
        });
        TestContext.WriteLine($"Screenshot: {shotPath}");
    }
}

using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace GCCWebApp.UAT.Tests;

[TestFixture]
public abstract class BaseTest : PageTest
{
    protected string BaseUrl = "https://localhost:7248"; // Update with your app's URL
    protected string ScreenshotPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Screenshots");

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Ensure screenshot directory exists
        Directory.CreateDirectory(ScreenshotPath);
        
        // Install Playwright browsers if needed
        var exitCode = Microsoft.Playwright.Program.Main(new[] { "install" });
        if (exitCode != 0)
        {
            throw new Exception($"Playwright installation failed with exit code {exitCode}");
        }
    }

    [SetUp]
    public async Task SetUp()
    {
        // Configure browser options
        await Page.SetViewportSizeAsync(1920, 1080);
        
        // Set default timeout
        Page.SetDefaultTimeout(30000); // 30 seconds
        
        // Take screenshot of test start
        await TakeScreenshot("test_start");
    }

    [TearDown]
    public async Task TearDown()
    {
        // Take screenshot on test failure
        if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            await TakeScreenshot("test_failed");
        }
        else
        {
            await TakeScreenshot("test_completed");
        }
    }

    protected async Task TakeScreenshot(string name)
    {
        var testName = TestContext.CurrentContext.Test.Name;
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{testName}_{name}_{timestamp}.png";
        var filePath = Path.Combine(ScreenshotPath, fileName);
        
        await Page.ScreenshotAsync(new() { Path = filePath, FullPage = true });
        TestContext.WriteLine($"Screenshot saved: {fileName}");
    }

    protected async Task NavigateToPage(string path = "/")
    {
        var url = BaseUrl + path;
        await Page.GotoAsync(url);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot($"navigated_to_{path.Replace("/", "_")}");
    }

    protected async Task LoginAsRole(string role = "Admin")
    {
        // This method will handle login based on your authentication system
        await NavigateToPage("/Account/Login");
        
        // For cookie authentication (development mode)
        await Page.FillAsync("#Email", GetTestUserEmail(role));
        await Page.FillAsync("#Password", GetTestUserPassword(role));
        await TakeScreenshot("login_form_filled");
        
        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForURLAsync(url => !url.Contains("/Account/Login"));
        await TakeScreenshot("login_completed");
    }

    private string GetTestUserEmail(string role) => role.ToLower() switch
    {
        "admin" => "admin@test.com",
        "analyst" => "analyst@test.com", 
        "external" => "external@test.com",
        _ => "public@test.com"
    };

    private string GetTestUserPassword(string role) => "TestPass123!";
}
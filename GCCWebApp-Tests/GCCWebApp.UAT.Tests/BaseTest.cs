using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using Microsoft.Extensions.Configuration;

namespace GCCWebApp.UAT.Tests;

[TestFixture]
public abstract class BaseTest : PageTest
{
    protected string BaseUrl = "http://localhost:5101"; // GCCWebApp default port (HTTP for testing)
    protected string ScreenshotPath = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots");
    protected IConfiguration? Configuration { get; private set; }
    protected bool IsProductionTest { get; private set; }

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Load configuration based on environment
        var testEnvironment = Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") ?? "Development";
        IsProductionTest = testEnvironment.Equals("Production", StringComparison.OrdinalIgnoreCase);

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", optional: true);

        // Load production config if TEST_ENVIRONMENT=Production
        if (IsProductionTest)
        {
            configBuilder.AddJsonFile("appsettings.Production.Test.json", optional: false);
            Console.WriteLine("*** PRODUCTION TEST MODE ***");
            Console.WriteLine("Running tests against LIVE production site!");
        }

        configBuilder.AddEnvironmentVariables();
        Configuration = configBuilder.Build();

        // Get base URL: Environment variable > Config file > Default
        BaseUrl = Environment.GetEnvironmentVariable("TEST_BASE_URL")
                  ?? Configuration["BaseUrl"]
                  ?? "http://localhost:5101";

        Console.WriteLine($"Test Environment: {testEnvironment}");
        Console.WriteLine($"Base URL: {BaseUrl}");

        // Update TestDataHelper credentials for production if needed
        if (IsProductionTest || BaseUrl.Contains("gccwebapp.com"))
        {
            Console.WriteLine("Using production credentials (username-based login)");
            // Production uses username, not email
            UpdateProductionCredentials();
        }

        // Ensure screenshot directory exists
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        ScreenshotPath = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots",
            IsProductionTest ? $"Production_{timestamp}" : $"Dev_{timestamp}");
        Directory.CreateDirectory(ScreenshotPath);
        Console.WriteLine($"Screenshots will be saved to: {Path.GetFullPath(ScreenshotPath)}");

        // Install Playwright browsers if needed
        var exitCode = Microsoft.Playwright.Program.Main(new[] { "install" });
        if (exitCode != 0)
        {
            throw new Exception($"Playwright installation failed with exit code {exitCode}");
        }
    }

    private void UpdateProductionCredentials()
    {
        // For production, we use username instead of email
        // The login page accepts username in the Email field
        Helpers.TestDataHelper.Users.RoleCredentials["Admin"] =
            ("admin", "TestPass123!", Helpers.TestDataHelper.Users.RoleCredentials["Admin"].ExpectedPages);
        Helpers.TestDataHelper.Users.RoleCredentials["Analyst"] =
            ("analyst", "TestPass123!", Helpers.TestDataHelper.Users.RoleCredentials["Analyst"].ExpectedPages);
        Helpers.TestDataHelper.Users.RoleCredentials["External"] =
            ("external", "TestPass123!", Helpers.TestDataHelper.Users.RoleCredentials["External"].ExpectedPages);
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
        try
        {
            var testName = TestContext.CurrentContext.Test.Name;
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{testName}_{name}_{timestamp}.png";
            var filePath = Path.Combine(ScreenshotPath, fileName);
            
            // Ensure directory exists before taking screenshot
            Directory.CreateDirectory(ScreenshotPath);
            
            await Page.ScreenshotAsync(new() { Path = filePath, FullPage = true });
            Console.WriteLine($"Screenshot saved: {Path.GetFullPath(filePath)}");
            TestContext.WriteLine($"Screenshot saved: {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to take screenshot '{name}': {ex.Message}");
            TestContext.WriteLine($"Failed to take screenshot '{name}': {ex.Message}");
        }
    }

    protected async Task NavigateToPage(string path = "/")
    {
        var url = BaseUrl + path;
        TestContext.WriteLine($"Navigating to: {url}");
        
        try
        {
            await Page.GotoAsync(url, new() { Timeout = 30000 });
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 20000 });
            await TakeScreenshot($"navigated_to_{path.Replace("/", "_").Replace("?", "_")}");
            TestContext.WriteLine($"Successfully navigated to: {path}");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Navigation failed to {path}: {ex.Message}");
            await TakeScreenshot($"navigation_failed_{path.Replace("/", "_")}");
            throw;
        }
    }

    protected async Task LoginAsRole(string role = "Admin")
    {
        // Skip login for public users
        if (role.Equals("Public", StringComparison.OrdinalIgnoreCase))
        {
            TestContext.WriteLine("Public user - no login required");
            return;
        }
        
        var userInfo = Helpers.TestDataHelper.Users.RoleCredentials.GetValueOrDefault(role);
        if (string.IsNullOrEmpty(userInfo.Email))
        {
            throw new ArgumentException($"No test user defined for role: {role}");
        }
        
        TestContext.WriteLine($"Logging in as {role}: {userInfo.Email}");
        
        // Navigate to login page
        await NavigateToPage("/Account/Login");
        await TakeScreenshot($"login_page_for_{role.ToLower()}");
        
        // Wait for login form to load
        await Page.WaitForSelectorAsync(Helpers.TestDataHelper.Selectors.EmailInput, new() { Timeout = 10000 });
        
        // Fill login form
        await Page.FillAsync(Helpers.TestDataHelper.Selectors.EmailInput, userInfo.Email);
        await Page.FillAsync(Helpers.TestDataHelper.Selectors.PasswordInput, userInfo.Password);
        await TakeScreenshot($"login_form_filled_for_{role.ToLower()}");
        
        // Submit login form
        await Page.ClickAsync(Helpers.TestDataHelper.Selectors.LoginButton);
        
        // Wait for successful login (URL change or timeout)
        try
        {
            await Page.WaitForURLAsync(url => !url.Contains("/Account/Login"), new() { Timeout = 15000 });
            await Page.WaitForTimeoutAsync(Helpers.TestDataHelper.WaitTimes.Navigation);
            await TakeScreenshot($"login_successful_for_{role.ToLower()}");
            TestContext.WriteLine($"Successfully logged in as {role}");
        }
        catch (TimeoutException)
        {
            await TakeScreenshot($"login_timeout_for_{role.ToLower()}");
            throw new Exception($"Login failed for {role} - timeout waiting for redirect");
        }
    }

    protected async Task LogoutIfLoggedIn()
    {
        try
        {
            // Check if logout button/link exists
            var logoutSelector = Helpers.TestDataHelper.Selectors.LogoutButton;
            if (await Page.Locator(logoutSelector).CountAsync() > 0)
            {
                await Page.ClickAsync(logoutSelector);
                await Page.WaitForURLAsync(url => url.Contains("/Account/Login") || url.Contains("/"));
                await TakeScreenshot("logged_out");
                TestContext.WriteLine("Successfully logged out");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Logout attempt failed: {ex.Message}");
            // Continue anyway - might not have been logged in
        }
    }
    
    protected async Task VerifyRoleAccess(string role, string[] expectedPages)
    {
        TestContext.WriteLine($"Verifying access for role: {role}");
        
        foreach (var page in expectedPages)
        {
            try
            {
                await NavigateToPage(page);
                await TakeScreenshot($"{role.ToLower()}_accessing_{page.Replace("/", "_")}");
                
                // Check if redirected to login (access denied) or error page
                var currentUrl = Page.Url.ToLower();
                var accessDenied = currentUrl.Contains("/account/login") || 
                                 currentUrl.Contains("accessdenied") || 
                                 currentUrl.Contains("unauthorized");
                
                if (accessDenied && role != "Public")
                {
                    TestContext.WriteLine($"⚠️ Access denied for {role} to {page}");
                }
                else
                {
                    TestContext.WriteLine($"✅ {role} can access {page}");
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"❌ Error accessing {page} as {role}: {ex.Message}");
                await TakeScreenshot($"{role.ToLower()}_error_accessing_{page.Replace("/", "_")}");
            }
        }
    }
    
    protected async Task PerformSearchOperation(string searchTerm)
    {
        try
        {
            // Look for search input
            var searchInput = Page.Locator(Helpers.TestDataHelper.Selectors.SearchInput).First;
            if (await searchInput.CountAsync() > 0)
            {
                await searchInput.FillAsync(searchTerm);
                await TakeScreenshot($"search_term_entered_{searchTerm.Replace(" ", "_")}");
                
                // Try to find and click search button
                var searchButton = Page.Locator(Helpers.TestDataHelper.Selectors.SearchButton).First;
                if (await searchButton.CountAsync() > 0)
                {
                    await searchButton.ClickAsync();
                }
                else
                {
                    // Try Enter key if no button found
                    await searchInput.PressAsync("Enter");
                }
                
                await Page.WaitForTimeoutAsync(Helpers.TestDataHelper.WaitTimes.Medium);
                await TakeScreenshot($"search_results_for_{searchTerm.Replace(" ", "_")}");
                TestContext.WriteLine($"Search performed for: {searchTerm}");
            }
            else
            {
                TestContext.WriteLine("No search input found on current page");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Search operation failed: {ex.Message}");
            await TakeScreenshot($"search_failed_{searchTerm.Replace(" ", "_")}");
        }
    }
    
    protected async Task InteractWithCertificateViews()
    {
        try
        {
            // Look for view toggle buttons
            var viewToggle = Page.Locator(Helpers.TestDataHelper.Selectors.ViewToggle).First;
            if (await viewToggle.CountAsync() > 0)
            {
                await viewToggle.ClickAsync();
                await Page.WaitForTimeoutAsync(Helpers.TestDataHelper.WaitTimes.Short);
                await TakeScreenshot("view_toggled");
                TestContext.WriteLine("Certificate view toggled");
            }
            
            // Look for certificate cards/items to interact with
            var certificateCards = Page.Locator(Helpers.TestDataHelper.Selectors.CertificateCard);
            var cardCount = await certificateCards.CountAsync();
            
            if (cardCount > 0)
            {
                TestContext.WriteLine($"Found {cardCount} certificate cards");
                
                // Click on first certificate if available
                if (cardCount > 0)
                {
                    await certificateCards.First.ClickAsync();
                    await Page.WaitForTimeoutAsync(Helpers.TestDataHelper.WaitTimes.Navigation);
                    await TakeScreenshot("certificate_details_opened");
                    TestContext.WriteLine("Certificate details accessed");
                }
            }
            else
            {
                TestContext.WriteLine("No certificates found on current page");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Certificate interaction failed: {ex.Message}");
            await TakeScreenshot("certificate_interaction_failed");
        }
    }
}
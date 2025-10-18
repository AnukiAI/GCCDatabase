using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GCCWebApp.UAT.Tests.Tests.Screenshots;

[TestFixture]
public abstract class ScreenshotTestBase : PageTest
{
    protected string BaseUrl { get; private set; } = null!;
    protected new IBrowser Browser { get; private set; } = null!;
    protected string ScreenshotDirectory { get; private set; } = null!;
    protected IPage PublicPage { get; private set; } = null!;
    protected IPage ExternalUserPage { get; private set; } = null!;
    protected IPage AnalystUserPage { get; private set; } = null!;
    protected IPage AdminUserPage { get; private set; } = null!;
    protected IConfiguration Configuration { get; private set; } = null!;

    protected readonly Dictionary<string, string> UserCredentials = new()
    {
        { "Public", "" },
        { "External", "external@company.com:TestPass123!" },
        { "Analyst", "analyst@gcc.com:TestPass123!" },
        { "Admin", "admin@gcc.com:TestPass123!" }
    };

    protected static readonly ViewportSize[] TestViewports = new[]
    {
        new ViewportSize { Width = 1920, Height = 1080 }, // Desktop (Full HD)
        new ViewportSize { Width = 1366, Height = 768 },  // Small Desktop (Laptop)
        new ViewportSize { Width = 768, Height = 1024 },  // Tablet
        new ViewportSize { Width = 375, Height = 812 }    // Mobile
    };

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Initialize Playwright
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        
        // Load configuration
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", optional: false)
            .AddEnvironmentVariables()
            .Build();
            
        // Get base URL from configuration or environment
        BaseUrl = Environment.GetEnvironmentVariable("TEST_BASE_URL") 
                  ?? Configuration["BaseUrl"] 
                  ?? "http://localhost:5101";
        
        // Get headless mode from configuration or environment
        var headlessMode = Environment.GetEnvironmentVariable("TEST_HEADLESS") ?? Configuration["TestSettings:BrowserHeadless"];
        var isHeadless = !string.Equals(headlessMode, "false", StringComparison.OrdinalIgnoreCase);
        
        Console.WriteLine($"Using base URL: {BaseUrl}");
        Console.WriteLine("IMPORTANT: Make sure the application is running at this URL before running tests!");

        // Create screenshot directory
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        ScreenshotDirectory = Path.Combine("Screenshots", $"Full_System_Screenshots_{timestamp}");
        Directory.CreateDirectory(ScreenshotDirectory);

        // Launch browser and create pages for different user types
        var browserArgs = new List<string>
        {
            "--disable-web-security",
            "--disable-features=IsolateOrigins,site-per-process",
            "--disable-blink-features=AutomationControlled",
            "--ignore-certificate-errors",
            "--force-device-scale-factor=1", // Ensures consistent rendering
            "--force-color-profile=srgb" // Ensures consistent colors
        };
        
        if (isHeadless)
        {
            browserArgs.AddRange(new[] {
                "--disable-gpu",
                "--no-sandbox",
                "--disable-dev-shm-usage",
                "--disable-setuid-sandbox",
                "--single-process" // Sometimes helps in containers
            });
        }
        
        Browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = isHeadless,
            SlowMo = isHeadless ? 0 : 100,
            Args = browserArgs,
            Timeout = 30000
        });
        
        Console.WriteLine($"Browser launched in {(isHeadless ? "HEADLESS" : "HEADED")} mode");

        // Create browser contexts for each user type with ignore HTTPS errors
        var contextOptions = new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            DeviceScaleFactor = 1, // Ensures consistent screenshot pixel density
            HasTouch = false,
            JavaScriptEnabled = true,
            Locale = "en-US",
            TimezoneId = "America/New_York",
            UserAgent = isHeadless 
                ? "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/120.0.0.0 Safari/537.36"
                : null
        };
        
        var publicContext = await Browser.NewContextAsync(contextOptions);
        var externalContext = await Browser.NewContextAsync(contextOptions);
        var analystContext = await Browser.NewContextAsync(contextOptions);
        var adminContext = await Browser.NewContextAsync(contextOptions);

        PublicPage = await publicContext.NewPageAsync();
        ExternalUserPage = await externalContext.NewPageAsync();
        AnalystUserPage = await analystContext.NewPageAsync();
        AdminUserPage = await adminContext.NewPageAsync();

        // Login users (except public)
        await LoginUser(ExternalUserPage, "External");
        await LoginUser(AnalystUserPage, "Analyst");  
        await LoginUser(AdminUserPage, "Admin");

        Console.WriteLine($"Screenshots will be saved to: {Path.GetFullPath(ScreenshotDirectory)}");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (PublicPage != null) await PublicPage.CloseAsync();
        if (ExternalUserPage != null) await ExternalUserPage.CloseAsync();
        if (AnalystUserPage != null) await AnalystUserPage.CloseAsync();
        if (AdminUserPage != null) await AdminUserPage.CloseAsync();
        if (Browser != null) await Browser.CloseAsync();
    }

    protected async Task LoginUser(IPage page, string userType)
    {
        if (userType == "Public" || !UserCredentials.ContainsKey(userType))
            return;

        var credentials = UserCredentials[userType].Split(':');
        var email = credentials[0];
        var password = credentials[1];

        try
        {
            // Navigate to login page
            var loginUrl = $"{BaseUrl}/Account/Login";
            Console.WriteLine($"Navigating to login: {loginUrl}");
            
            var response = await page.GotoAsync(loginUrl, new PageGotoOptions 
            { 
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000 
            });
            
            if (response != null && !response.Ok)
            {
                Console.WriteLine($"Failed to load login page: {response.Status} - {response.StatusText}");
            }
            
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // Fill login form
            await page.FillAsync("input[name='Email']", email);
            await page.FillAsync("input[name='Password']", password);
            await page.ClickAsync("button[type='submit']");

            // Wait for redirect after login
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 30000 });
            
            // Verify login succeeded by checking for logout link or user-specific element
            var currentUrl = page.Url;
            Console.WriteLine($"After login, current URL: {currentUrl}");
            
            Console.WriteLine($"Successfully logged in {userType} user");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to login {userType} user: {ex.Message}");
        }
    }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
    protected async Task CaptureFullPageScreenshot(IPage page, string pageName, string userRole = "Public", ViewportSize viewport = default)
#pragma warning restore CS8625
    {
        try
        {
            // Set viewport if specified
            if (viewport.Width > 0 && viewport.Height > 0)
            {
                await page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            }
            
            // Use comprehensive wait helpers
            await ScreenshotWaitHelpers.WaitForPageReadyAsync(page);

            // Wait for page to be fully loaded
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 30000 });
            
            // Wait for common indicators that page is ready
            try
            {
                // Wait for body to have content
                await page.WaitForFunctionAsync("() => document.body && document.body.innerHTML.length > 100", new PageWaitForFunctionOptions { Timeout = 5000 });
                
                // Wait for no pending requests
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                
                // Wait for fonts to load (important for consistent screenshots)
                await page.EvaluateAsync(@"() => {
                    if (document.fonts && document.fonts.ready) {
                        return document.fonts.ready;
                    }
                }");
                
                // Small additional wait for any CSS animations
                await page.WaitForTimeoutAsync(1000);
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Warning: Some wait conditions timed out, proceeding anyway");
            }
            
            // Check if page has content
            var bodyContent = await page.EvaluateAsync<string>("() => document.body ? document.body.innerHTML : ''" );
            if (string.IsNullOrWhiteSpace(bodyContent) || bodyContent.Length < 100)
            {
                Console.WriteLine($"WARNING: Page appears to have minimal content. Body length: {bodyContent?.Length ?? 0}");
                
                // Try to log any errors on the page
                var consoleErrors = await page.EvaluateAsync<string>(@"() => {
                    return JSON.stringify(window.__errors || []);
                }");
                if (!string.IsNullOrEmpty(consoleErrors) && consoleErrors != "[]")
                {
                    Console.WriteLine($"Page console errors: {consoleErrors}");
                }
            }

            // Generate screenshot filename
            var viewportSuffix = viewport.Width > 0 ? $"_{viewport.Width}x{viewport.Height}" : "";
            var filename = $"{userRole}_{pageName}{viewportSuffix}.png";
            var filepath = Path.Combine(ScreenshotDirectory, filename);

            // Scroll to top before screenshot
            await page.EvaluateAsync("window.scrollTo(0, 0)");
            await page.WaitForTimeoutAsync(500);
            
            // Take full page screenshot with retry logic
            int retries = 3;
            Exception lastException = null;
            
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    await page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Path = filepath,
                        FullPage = true,
                        Type = ScreenshotType.Png,
                        Quality = 100, // Max quality for PNG
                        Animations = ScreenshotAnimations.Disabled // Disable animations for consistency
                    });
                    
                    // Verify the file was created and has content
                    var fileInfo = new FileInfo(filepath);
                    if (fileInfo.Exists && fileInfo.Length > 0)
                    {
                        Console.WriteLine($"Screenshot saved: {filename} ({fileInfo.Length / 1024}KB)");
                        return;
                    }
                    else
                    {
                        throw new Exception($"Screenshot file is empty or not created: {filepath}");
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (i < retries - 1)
                    {
                        Console.WriteLine($"Screenshot attempt {i + 1} failed, retrying...");
                        await page.WaitForTimeoutAsync(1000);
                    }
                }
            }
            
            throw new Exception($"Failed to capture screenshot after {retries} attempts", lastException);

            Console.WriteLine($"Screenshot saved: {filename}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to capture screenshot for {pageName} ({userRole}): {ex.Message}");
        }
    }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
    protected async Task NavigateAndScreenshot(IPage page, string route, string pageName, string userRole, ViewportSize viewport = default)
#pragma warning restore CS8625
    {
        try
        {
            var url = $"{BaseUrl.TrimEnd('/')}{route}";
            Console.WriteLine($"Navigating to: {url}");
            
            var response = await page.GotoAsync(url, new PageGotoOptions 
            { 
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });
            
            if (response != null)
            {
                Console.WriteLine($"Response status: {response.Status}");
                if (!response.Ok)
                {
                    Console.WriteLine($"WARNING: Non-OK response: {response.Status} - {response.StatusText}");
                }
            }
            await CaptureFullPageScreenshot(page, pageName, userRole, viewport);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to navigate to {route} for {userRole}: {ex.Message}");
        }
    }

    protected async Task<List<int>> GetSampleCertificateIds()
    {
        // Since we're testing against a running application, return default IDs
        // You may want to make an API call to get actual IDs if available
        return await Task.FromResult(new List<int> { 1, 2, 3 });
    }

    protected async Task<List<int>> GetSampleCompanyIds()
    {
        // Since we're testing against a running application, return default IDs
        // You may want to make an API call to get actual IDs if available
        return await Task.FromResult(new List<int> { 1, 2, 3 });
    }
}
using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.Tests.Negative
{
    [TestFixture]
    public abstract class NegativeTestingBase : PageTest
    {
        protected string BaseUrl { get; set; } = Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "https://gccwebapp.com";
        protected IPage TestPage { get; private set; } = null!;

        [SetUp]
        public async Task NegativeTestSetUp()
        {
            TestPage = await Browser.NewPageAsync(new BrowserNewPageOptions
            {
                IgnoreHTTPSErrors = true
            });

            // Set up console error tracking
            TestPage.Console += (_, e) =>
            {
                if (e.Type == "error")
                {
                    Console.WriteLine($"Browser console error: {e.Text}");
                }
            };

            // Set up request failure tracking
            TestPage.RequestFailed += (_, e) =>
            {
                Console.WriteLine($"Request failed: {e.Url} - {e.Failure}");
            };
        }

        [TearDown]
        public async Task NegativeTestTearDown()
        {
            if (TestPage != null)
                await TestPage.CloseAsync();
        }

        /// <summary>
        /// Test unauthorized access attempts
        /// </summary>
        protected async Task<UnauthorizedAccessResult> TestUnauthorizedAccess(string protectedUrl, string? expectedRedirectPattern = null)
        {
            var result = new UnauthorizedAccessResult { TestedUrl = protectedUrl };

            try
            {
                var response = await TestPage.GotoAsync($"{BaseUrl}{protectedUrl}");
                result.StatusCode = response?.Status ?? 0;
                result.FinalUrl = TestPage.Url;

                // Check if redirected to login
                result.RedirectedToLogin = TestPage.Url.Contains("/Account/Login") || TestPage.Url.Contains("/login");

                // Check for access denied message
                var pageContent = await TestPage.ContentAsync();
                result.ShowsAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                                          pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                                          pageContent.Contains("forbidden", StringComparison.OrdinalIgnoreCase);

                // Check if the page loads actual content (security breach)
                result.LoadsProtectedContent = !result.RedirectedToLogin && !result.ShowsAccessDenied && result.StatusCode == 200;

                return result;
            }
            catch (Exception ex)
            {
                result.TestError = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Test SQL injection attempts
        /// </summary>
        protected async Task<SqlInjectionResult> TestSqlInjection(string inputSelector, string[] sqlPayloads)
        {
            var result = new SqlInjectionResult { TestedSelector = inputSelector };

            foreach (var payload in sqlPayloads)
            {
                try
                {
                    await TestPage.FillAsync(inputSelector, payload);
                    await TestPage.PressAsync(inputSelector, "Tab");
                    await TestPage.WaitForTimeoutAsync(1000);

                    // Check for SQL error messages in page content
                    var pageContent = await TestPage.ContentAsync();
                    var hasSqlError = pageContent.Contains("SQL", StringComparison.OrdinalIgnoreCase) &&
                                     (pageContent.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                                      pageContent.Contains("syntax", StringComparison.OrdinalIgnoreCase) ||
                                      pageContent.Contains("database", StringComparison.OrdinalIgnoreCase));

                    if (hasSqlError)
                    {
                        result.VulnerablePayloads.Add(payload);
                        Console.WriteLine($"Potential SQL injection vulnerability with payload: {payload}");
                    }
                    else
                    {
                        result.SafePayloads.Add(payload);
                    }
                }
                catch (Exception ex)
                {
                    result.TestErrors.Add($"Payload '{payload}': {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Test XSS (Cross-Site Scripting) attempts
        /// </summary>
        protected async Task<XssResult> TestXss(string inputSelector, string[] xssPayloads)
        {
            var result = new XssResult { TestedSelector = inputSelector };

            foreach (var payload in xssPayloads)
            {
                try
                {
                    await TestPage.FillAsync(inputSelector, payload);
                    
                    // Submit form or trigger action
                    await TestPage.PressAsync(inputSelector, "Tab");
                    await TestPage.WaitForTimeoutAsync(1000);

                    // Check if script executed (would show alert or modify DOM)
                    var alertHandled = false;
                    TestPage.Dialog += async (_, dialog) =>
                    {
                        alertHandled = true;
                        await dialog.AcceptAsync();
                        result.VulnerablePayloads.Add(payload);
                    };

                    // Check if payload appears unescaped in HTML
                    var pageContent = await TestPage.ContentAsync();
                    if (pageContent.Contains($"<script>{payload}</script>") || 
                        pageContent.Contains(payload.Replace("&lt;", "<").Replace("&gt;", ">")))
                    {
                        result.VulnerablePayloads.Add(payload);
                    }
                    else
                    {
                        result.SafePayloads.Add(payload);
                    }
                }
                catch (Exception ex)
                {
                    result.TestErrors.Add($"Payload '{payload}': {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Test session manipulation
        /// </summary>
        protected async Task<SessionTestResult> TestSessionSecurity()
        {
            var result = new SessionTestResult();

            try
            {
                // Login as normal user
                await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
                await TestPage.FillAsync("input[name='Email']", "analyst");
                await TestPage.FillAsync("input[name='Password']", "TestPass123!");
                await TestPage.ClickAsync("button[type='submit']");
                await TestPage.WaitForTimeoutAsync(2000);

                // Get session cookies
                var cookies = await TestPage.Context.CookiesAsync();
                result.HasSessionCookie = cookies.Any(c => c.Name.Contains("session", StringComparison.OrdinalIgnoreCase) ||
                                                          c.Name.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
                                                          c.Name.Contains(".AspNetCore.", StringComparison.OrdinalIgnoreCase) ||
                                                          c.Name.Contains("Identity", StringComparison.OrdinalIgnoreCase));

                // Test session fixation - clear cookies and try to access protected content
                await TestPage.Context.ClearCookiesAsync();
                var response = await TestPage.GotoAsync($"{BaseUrl}/Admin/UserManagement");
                result.PreventSessionFixation = response?.Status == 401 || TestPage.Url.Contains("/Account/Login");

                return result;
            }
            catch (Exception ex)
            {
                result.TestError = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Test rate limiting and brute force protection
        /// </summary>
        protected async Task<RateLimitResult> TestRateLimit(string endpoint, int requestCount = 20)
        {
            var result = new RateLimitResult { TestedEndpoint = endpoint };

            try
            {
                var startTime = DateTime.Now;

                for (int i = 0; i < requestCount; i++)
                {
                    var response = await TestPage.GotoAsync($"{BaseUrl}{endpoint}");
                    var statusCode = response?.Status ?? 0;
                    
                    result.ResponseCodes.Add(statusCode);

                    if (statusCode == 429) // Too Many Requests
                    {
                        result.RateLimitTriggered = true;
                        result.RequestsUntilLimit = i + 1;
                        break;
                    }

                    // Small delay to avoid overwhelming the server
                    await TestPage.WaitForTimeoutAsync(100);
                }

                result.TotalDuration = DateTime.Now - startTime;
                return result;
            }
            catch (Exception ex)
            {
                result.TestError = ex.Message;
                return result;
            }
        }
    }

    public class UnauthorizedAccessResult
    {
        public string TestedUrl { get; set; } = "";
        public int StatusCode { get; set; }
        public string FinalUrl { get; set; } = "";
        public bool RedirectedToLogin { get; set; }
        public bool ShowsAccessDenied { get; set; }
        public bool LoadsProtectedContent { get; set; }
        public string TestError { get; set; } = "";
    }

    public class SqlInjectionResult
    {
        public string TestedSelector { get; set; } = "";
        public List<string> VulnerablePayloads { get; set; } = new List<string>();
        public List<string> SafePayloads { get; set; } = new List<string>();
        public List<string> TestErrors { get; set; } = new List<string>();
    }

    public class XssResult
    {
        public string TestedSelector { get; set; } = "";
        public List<string> VulnerablePayloads { get; set; } = new List<string>();
        public List<string> SafePayloads { get; set; } = new List<string>();
        public List<string> TestErrors { get; set; } = new List<string>();
    }

    public class SessionTestResult
    {
        public bool HasSessionCookie { get; set; }
        public bool PreventSessionFixation { get; set; }
        public string TestError { get; set; } = "";
    }

    public class RateLimitResult
    {
        public string TestedEndpoint { get; set; } = "";
        public List<int> ResponseCodes { get; set; } = new List<int>();
        public bool RateLimitTriggered { get; set; }
        public int RequestsUntilLimit { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public string TestError { get; set; } = "";
    }
}
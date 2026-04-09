using GCCWebApp.Security.Tests.Fixtures;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace GCCWebApp.Security.Tests.Tests
{
    [Trait("Category", "BasicSecurity")]
    public class BasicSecurityTests : IDisposable
    {
        private readonly RemoteSecurityTestHelper _helper;
        private readonly ITestOutputHelper _output;

        public BasicSecurityTests(ITestOutputHelper output)
        {
            _helper = new RemoteSecurityTestHelper();
            _output = output;
        }

        [Fact]
        public async Task SecurityHeaders_ShouldBePresent()
        {
            // Arrange & Act
            var response = await _helper.Client.GetAsync("/");

            // Assert - check common security headers
            var headers = response.Headers;
            var contentHeaders = response.Content.Headers;

            // Collect all header names for diagnostics
            var allHeaders = new List<string>();
            foreach (var h in headers) allHeaders.Add(h.Key);
            foreach (var h in contentHeaders) allHeaders.Add(h.Key);
            _output.WriteLine($"Response headers: {string.Join(", ", allHeaders)}");

            // X-Content-Type-Options: nosniff
            Assert.True(
                headers.Contains("X-Content-Type-Options"),
                "Missing X-Content-Type-Options header");

            // X-Frame-Options: DENY or SAMEORIGIN
            Assert.True(
                headers.Contains("X-Frame-Options"),
                "Missing X-Frame-Options header");

            // X-XSS-Protection
            Assert.True(
                headers.Contains("X-XSS-Protection"),
                "Missing X-XSS-Protection header");
        }

        [Theory]
        [InlineData("' OR 1=1 --")]
        [InlineData("'; DROP TABLE Users; --")]
        [InlineData("admin' OR '1'='1")]
        public async Task SqlInjection_OnLoginForm_ShouldNotCauseServerError(string maliciousInput)
        {
            // Arrange - get login page to obtain antiforgery token
            using var client = _helper.CreateClient(allowAutoRedirect: true);
            var loginPage = await client.GetAsync("/Account/Login");
            var html = await loginPage.Content.ReadAsStringAsync();
            var token = RemoteSecurityTestHelper.ParseAntiforgeryToken(html);

            var formData = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = maliciousInput,
                ["Password"] = "anypassword",
                ["__RequestVerificationToken"] = token ?? string.Empty
            });

            // Act
            var response = await client.PostAsync("/Account/Login", formData);

            // Assert - should NOT return 500 (server error)
            var statusCode = (int)response.StatusCode;
            _output.WriteLine($"SQL injection payload returned status: {statusCode}");
            Assert.True(statusCode < 500, $"SQL injection payload caused server error: {statusCode}");
        }

        [Theory]
        [InlineData("<script>alert('xss')</script>")]
        [InlineData("<img src=x onerror=alert('xss')>")]
        public async Task XssPayload_InLoginForm_ShouldBeSanitized(string xssPayload)
        {
            // Arrange - get login page to obtain antiforgery token
            using var client = _helper.CreateClient(allowAutoRedirect: true);
            var loginPage = await client.GetAsync("/Account/Login");
            var html = await loginPage.Content.ReadAsStringAsync();
            var token = RemoteSecurityTestHelper.ParseAntiforgeryToken(html);

            var formData = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = xssPayload,
                ["Password"] = "anypassword",
                ["__RequestVerificationToken"] = token ?? string.Empty
            });

            // Act
            var response = await client.PostAsync("/Account/Login", formData);
            var content = await response.Content.ReadAsStringAsync();

            // Assert - the raw XSS payload should NOT appear unencoded in the response
            Assert.DoesNotContain("<script>alert('xss')</script>", content);
            Assert.DoesNotContain("onerror=alert", content);
            _output.WriteLine("XSS payload was properly sanitized in response");
        }

        [Fact]
        public async Task CsrfProtection_PostWithoutToken_ShouldFail()
        {
            // Arrange - POST to login WITHOUT an antiforgery token
            using var client = _helper.CreateClient();

            var formData = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = "admin",
                ["Password"] = "TestPass123!"
                // deliberately omitting __RequestVerificationToken
            });

            // Act
            var response = await client.PostAsync("/Account/Login", formData);

            // Assert - should be rejected (400 or 302 back to login, NOT a successful login)
            var statusCode = (int)response.StatusCode;
            _output.WriteLine($"POST without CSRF token returned status: {statusCode}");

            // Without a valid token the server should reject the request
            // It may return 400, 403, or redirect. It should NOT return 200 with a valid session.
            Assert.True(
                statusCode == 400 || statusCode == 403 || statusCode == 302 || statusCode == 301,
                $"Expected 400/403/redirect without CSRF token, got {statusCode}");
        }

        [Fact]
        public async Task SessionCookie_ShouldHaveSecureFlags()
        {
            // Arrange - create a client with no auto-redirect so we can inspect Set-Cookie headers
            var cookies = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                AllowAutoRedirect = false,
                UseCookies = true
            };
            using var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(_helper.BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            // GET login page for antiforgery token
            var loginPage = await client.GetAsync("/Account/Login");
            var html = await loginPage.Content.ReadAsStringAsync();
            var token = RemoteSecurityTestHelper.ParseAntiforgeryToken(html);

            var formData = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = "admin",
                ["Password"] = "TestPass123!",
                ["__RequestVerificationToken"] = token ?? string.Empty
            });

            // POST login - do NOT follow redirect so we can see Set-Cookie headers
            var loginResponse = await client.PostAsync("/Account/Login", formData);

            // Check Set-Cookie headers for security flags
            if (loginResponse.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
            {
                var cookieHeadersList = setCookieHeaders.ToList();
                _output.WriteLine($"Set-Cookie headers found: {cookieHeadersList.Count}");

                foreach (var cookieHeader in cookieHeadersList)
                {
                    _output.WriteLine($"Cookie: {cookieHeader}");

                    // Session/auth cookies should have HttpOnly and Secure flags
                    if (cookieHeader.Contains(".AspNetCore.") || cookieHeader.Contains("session", StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.Contains("HttpOnly", cookieHeader, StringComparison.OrdinalIgnoreCase);

                        // Only check Secure flag if site is HTTPS
                        if (_helper.BaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            Assert.Contains("Secure", cookieHeader, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }
            else
            {
                _output.WriteLine("No Set-Cookie headers found on login response (may have followed redirect)");
            }
        }

        [Theory]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        public async Task HttpMethods_PutDeleteOnLogin_ShouldBeRejected(string method)
        {
            // Arrange
            using var client = _helper.CreateClient();
            var request = new HttpRequestMessage(new HttpMethod(method), "/Account/Login");

            // Act
            var response = await client.SendAsync(request);

            // Assert - PUT/DELETE on login should return 4xx or 405
            var statusCode = (int)response.StatusCode;
            _output.WriteLine($"{method} /Account/Login returned: {statusCode}");
            Assert.True(
                statusCode >= 400 && statusCode < 500,
                $"Expected 4xx for {method} on login, got {statusCode}");
        }

        public void Dispose()
        {
            _helper.Dispose();
        }
    }
}

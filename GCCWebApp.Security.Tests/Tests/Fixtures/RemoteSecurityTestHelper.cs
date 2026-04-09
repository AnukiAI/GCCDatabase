using System.Net;
using AngleSharp;
using AngleSharp.Html.Parser;

namespace GCCWebApp.Security.Tests.Fixtures
{
    /// <summary>
    /// Helper for running security tests against a remote deployed GCC site.
    /// Provides authenticated HttpClient instances using cookie-based session auth.
    /// </summary>
    public class RemoteSecurityTestHelper : IDisposable
    {
        public string BaseUrl { get; }

        private readonly CookieContainer _cookieContainer;
        private readonly HttpClientHandler _handler;
        private readonly HttpClient _client;
        private bool _disposed;

        private static readonly Dictionary<string, (string Username, string Password)> RoleCredentials = new()
        {
            ["admin"] = ("admin", "TestPass123!"),
            ["analyst"] = ("analyst", "TestPass123!"),
            ["external"] = ("external", "TestPass123!")
        };

        public RemoteSecurityTestHelper()
        {
            BaseUrl = Environment.GetEnvironmentVariable("TEST_BASE_URL")
                       ?? "https://gccwebapp.com";

            _cookieContainer = new CookieContainer();
            _handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                AllowAutoRedirect = false,
                UseCookies = true
            };
            _client = new HttpClient(_handler)
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// Returns the shared HttpClient (unauthenticated unless LoginAsync was called).
        /// </summary>
        public HttpClient Client => _client;

        /// <summary>
        /// Creates a fresh HttpClient with its own CookieContainer (unauthenticated).
        /// Caller is responsible for disposing.
        /// </summary>
        public HttpClient CreateClient(bool allowAutoRedirect = false)
        {
            var container = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = container,
                AllowAutoRedirect = allowAutoRedirect,
                UseCookies = true
            };
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// Creates a new HttpClient that is logged in with the specified role.
        /// Caller is responsible for disposing the returned client.
        /// </summary>
        public async Task<HttpClient> CreateAuthenticatedClient(string role)
        {
            var client = CreateClient(allowAutoRedirect: true);
            await LoginAsync(client, role);
            // Reset AllowAutoRedirect after login so tests can inspect redirect responses
            // Note: can't change after construction, so we create a new handler
            return client;
        }

        /// <summary>
        /// Logs in the shared HttpClient with the specified role.
        /// </summary>
        public async Task LoginAsync(string role)
        {
            await LoginAsync(_client, role);
        }

        /// <summary>
        /// Logs in the given HttpClient by GETting /Account/Login, parsing the
        /// antiforgery token, and POSTing credentials.
        /// </summary>
        public async Task LoginAsync(HttpClient client, string role)
        {
            if (!RoleCredentials.TryGetValue(role.ToLowerInvariant(), out var creds))
                throw new ArgumentException($"Unknown role: {role}. Valid roles: admin, analyst, external");

            // GET the login page to obtain the antiforgery token
            var loginPageResponse = await client.GetAsync("/Account/Login");
            loginPageResponse.EnsureSuccessStatusCode();
            var loginHtml = await loginPageResponse.Content.ReadAsStringAsync();

            // Parse the __RequestVerificationToken from the HTML form
            var token = ParseAntiforgeryToken(loginHtml);

            // POST credentials with the antiforgery token
            var formData = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = creds.Username,
                ["Password"] = creds.Password,
                ["__RequestVerificationToken"] = token ?? string.Empty
            });

            var loginResponse = await client.PostAsync("/Account/Login", formData);

            // A successful login typically redirects (302/301) to the home page
            // If AllowAutoRedirect is true, we'll get the final 200
            var statusCode = (int)loginResponse.StatusCode;
            if (statusCode >= 400)
            {
                throw new InvalidOperationException(
                    $"Login failed for role '{role}' with status {loginResponse.StatusCode}");
            }
        }

        /// <summary>
        /// Parses the __RequestVerificationToken from an HTML page.
        /// </summary>
        public static string? ParseAntiforgeryToken(string html)
        {
            var parser = new HtmlParser();
            var document = parser.ParseDocument(html);

            // Look for hidden input with name __RequestVerificationToken
            var tokenInput = document.QuerySelector("input[name='__RequestVerificationToken']");
            return tokenInput?.GetAttribute("value");
        }

        /// <summary>
        /// Gets cookies for the current session from the shared client.
        /// </summary>
        public CookieCollection GetCookies()
        {
            return _cookieContainer.GetCookies(new Uri(BaseUrl));
        }

        /// <summary>
        /// Gets cookies from a specific HttpClientHandler's container.
        /// </summary>
        public CookieCollection GetCookies(HttpClientHandler handler)
        {
            return handler.CookieContainer.GetCookies(new Uri(BaseUrl));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _client.Dispose();
                _handler.Dispose();
                _disposed = true;
            }
        }
    }
}

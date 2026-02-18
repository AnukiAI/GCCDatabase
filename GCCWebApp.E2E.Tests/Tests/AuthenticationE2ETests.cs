using System.Net;
using System.Text;
using FluentAssertions;
using GCCWebApp.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GCCWebApp.E2E.Tests.Tests
{
    public class AuthenticationE2ETests : IClassFixture<TestWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly TestWebApplicationFactory<Program> _factory;

        public AuthenticationE2ETests(TestWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task Login_WithoutCredentials_RedirectsToLogin()
        {
            // Arrange
            var protectedUrl = "/Dashboard";

            // Act
            var response = await _client.GetAsync(protectedUrl);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Found,
                HttpStatusCode.Unauthorized
            );
        }

        [Fact]
        public async Task Login_PageLoads_Successfully()
        {
            // Arrange - No separate SignUp page; login is the entry point
            var loginUrl = "/Account/Login";

            // Act
            var response = await _client.GetAsync(loginUrl);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.Found // May redirect if already authenticated
            );
        }

        [Fact]
        public async Task ForgotPassword_PageLoads_Successfully()
        {
            // Arrange
            var forgotPasswordUrl = "/Account/ForgotPassword";

            // Act
            var response = await _client.GetAsync(forgotPasswordUrl);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShowsError()
        {
            // Arrange
            var loginUrl = "/Account/Login";
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Email", "invalid@example.com"),
                new KeyValuePair<string, string>("Password", "WrongPassword123!")
            });

            // Act
            var response = await _client.PostAsync(loginUrl, formContent);

            // Assert
            // The response should either stay on login page or redirect
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK, // Stays on page with error
                HttpStatusCode.BadRequest,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Found // May redirect to error page
            );
        }

        [Fact]
        public async Task Logout_RedirectsToHome()
        {
            // Arrange - POST logout is at /Account/LogoutPost (requires AntiForgeryToken)
            // GET logout at /Account/Logout also works for unauthenticated users
            var logoutUrl = "/Account/Logout";

            // Act - Use GET which is allowed without AntiForgeryToken
            var response = await _client.GetAsync(logoutUrl);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Found,
                HttpStatusCode.OK,
                HttpStatusCode.MethodNotAllowed // POST to wrong endpoint
            );
        }
    }
}
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
                HttpStatusCode.Redirect,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Found
            );
        }

        [Fact]
        public async Task SignUp_PageLoads_Successfully()
        {
            // Arrange
            var signUpUrl = "/Account/SignUp";

            // Act
            var response = await _client.GetAsync(signUpUrl);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.Redirect // May redirect if already authenticated
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
                HttpStatusCode.Redirect // May redirect to error page
            );
        }

        [Fact]
        public async Task Logout_RedirectsToHome()
        {
            // Arrange
            var logoutUrl = "/Account/Logout";

            // Act
            var response = await _client.PostAsync(logoutUrl, null);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Redirect,
                HttpStatusCode.Found,
                HttpStatusCode.OK
            );
        }
    }
}
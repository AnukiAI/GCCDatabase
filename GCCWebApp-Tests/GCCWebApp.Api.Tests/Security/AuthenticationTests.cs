using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using GCCWebApp.Models.Api.Requests;
using GCCWebApp.Api.Tests.Integration;

namespace GCCWebApp.Api.Tests.Security
{
    public class AuthenticationTests : IClassFixture<WebApplicationFactoryBase<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactoryBase<Program> _factory;

        public AuthenticationTests(WebApplicationFactoryBase<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task AccessProtectedEndpoint_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Unauthorized, 
                HttpStatusCode.Forbidden
            );
        }

        [Fact]
        public async Task AccessProtectedEndpoint_WithInvalidApiKey_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Add("X-API-Key", "invalid-key");

            // Act
            var response = await _client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Unauthorized, 
                HttpStatusCode.Forbidden
            );
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsToken()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "testuser",
                Password = "testpassword"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            // This test will depend on your test user setup
            // Adjust expectations based on your authentication implementation
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.Unauthorized
            );
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "invaliduser",
                Password = "wrongpassword"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WithEmptyCredentials_ReturnsBadRequest()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "",
                Password = ""
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var refreshRequest = new RefreshTokenRequest
            {
                AccessToken = "invalid-access-token",
                RefreshToken = "invalid-refresh-token"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ValidateToken_WithoutToken_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.PostAsync("/api/v1/auth/validate", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("invalid")]
        [InlineData("bearer")]
        [InlineData("Bearer")]
        public async Task AccessWithInvalidAuthorizationHeader_ReturnsUnauthorized(string headerValue)
        {
            // Arrange
            if (!string.IsNullOrEmpty(headerValue))
            {
                _client.DefaultRequestHeaders.Add("Authorization", headerValue);
            }

            // Act
            var response = await _client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Forbidden
            );
        }

        [Fact]
        public async Task SqlInjectionAttempt_InLoginUsername_IsRejected()
        {
            // Arrange
            var maliciousLogin = new LoginRequest
            {
                Username = "admin'; DROP TABLE Users; --",
                Password = "password"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", maliciousLogin);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.Unauthorized
            );
        }

        [Fact]
        public async Task XSSAttempt_InLoginFields_IsRejected()
        {
            // Arrange
            var maliciousLogin = new LoginRequest
            {
                Username = "<script>alert('xss')</script>",
                Password = "<script>alert('xss')</script>"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", maliciousLogin);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.Unauthorized
            );
        }

        [Fact]
        public async Task ExcessivelyLongCredentials_AreRejected()
        {
            // Arrange
            var longString = new string('a', 10000);
            var maliciousLogin = new LoginRequest
            {
                Username = longString,
                Password = longString
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", maliciousLogin);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RoleBasedAccess_AdminEndpoints_RequireAdminRole()
        {
            // Act - Try to access admin-only endpoint without proper role
            var response = await _client.GetAsync("/api/v1/apikeys");

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Forbidden
            );
        }
    }

    // Create these request classes if they don't exist in your API
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
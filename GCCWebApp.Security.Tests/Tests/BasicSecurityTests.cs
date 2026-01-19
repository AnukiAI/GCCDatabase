using FluentAssertions;
using GCCWebApp.Security.Tests.Fixtures;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace GCCWebApp.Security.Tests.Tests
{
    [Trait("Category", "BasicSecurity")]
    public class BasicSecurityTests : IClassFixture<SecurityTestWebApplicationFactory<Program>>
    {
        private readonly SecurityTestWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public BasicSecurityTests(SecurityTestWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Authentication_WithValidCredentials_ShouldSucceed()
        {
            // Arrange
            var loginRequest = new
            {
                Username = "securitytestadmin",
                Password = "SecurePassword123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("accessToken");
        }

        [Fact]
        public async Task Authentication_WithInvalidCredentials_ShouldFail()
        {
            // Arrange
            var loginRequest = new
            {
                Username = "invalid",
                Password = "invalid"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData("'; DROP TABLE Users; --")]
        [InlineData("' OR '1'='1")]
        [InlineData("admin'; DELETE FROM Users WHERE 'a'='a")]
        public async Task SqlInjection_InLogin_ShouldBeBlocked(string maliciousInput)
        {
            // Arrange
            var loginRequest = new
            {
                Username = maliciousInput,
                Password = "anypassword"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ApiEndpoint_WithoutAuthentication_ShouldRequireAuth()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ApiEndpoint_WithValidApiKey_ShouldSucceed()
        {
            // Arrange
            _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key-123");

            // Act
            var response = await _client.GetAsync("/api/v1/certificates");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task SecurityHeaders_ShouldBePresent()
        {
            // Act
            var response = await _client.GetAsync("/");

            // Assert
            response.Headers.Should().ContainKey("X-Content-Type-Options");
        }

        [Theory]
        [InlineData("<script>alert('xss')</script>", "script")]
        [InlineData("<img src=x onerror=alert('xss')>", "onerror")]
        public async Task XssPayload_ShouldBeSanitized(string xssPayload, string expectedPattern)
        {
            // This test verifies that the InputSanitizationAttribute properly detects XSS patterns
            // The attribute uses regex patterns to catch:
            // - <script> tags
            // - on* event handlers (onerror, onclick, onload, etc.)
            // - javascript: and vbscript: protocols
            // - alert(), eval(), confirm(), prompt() calls

            var sanitizationAttribute = new GCCWebApp.Validation.InputSanitizationAttribute();
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(xssPayload);

            // The payload should be detected as containing unsafe content
            var result = sanitizationAttribute.GetValidationResult(xssPayload, validationContext);

            // Assert that validation fails (returns an error message, not Success)
            result.Should().NotBe(System.ComponentModel.DataAnnotations.ValidationResult.Success,
                $"XSS payload containing '{expectedPattern}' should be blocked by InputSanitizationAttribute");
        }

        [Fact]
        public async Task PasswordHashing_ShouldNotReturnPlaintext()
        {
            // Arrange
            var loginRequest = new
            {
                Username = "securitytestuser",
                Password = "UserPassword123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Assert
                content.Should().NotContain("UserPassword123!");
                content.Should().NotContain("password");
            }
        }

        [Fact]
        public async Task ApiKey_Validation_ShouldRejectInvalidKeys()
        {
            // Arrange
            _client.DefaultRequestHeaders.Add("X-API-Key", "invalid-key");

            // Act
            var response = await _client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }
    }
}
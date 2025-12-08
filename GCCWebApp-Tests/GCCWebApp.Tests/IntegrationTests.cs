using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text;
using FluentAssertions;

namespace GCCWebApp.Tests
{
    public class SecurityIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public SecurityIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        #region Authentication Tests

        [Fact]
        public async Task AccessProtectedEndpoint_WithoutAuthentication_ShouldRedirectToLogin()
        {
            // Act
            var response = await _client.GetAsync("/Admin");

            // Assert
            // Should redirect to login or return 401/403
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Found, HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task LoginPage_ShouldLoadSuccessfully()
        {
            // Act
            var response = await _client.GetAsync("/Account/Login");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Input Validation Tests

        [Theory]
        [InlineData("'; DROP TABLE Certificates; --")]
        [InlineData("1' OR '1'='1")]
        [InlineData("admin'--")]
        [InlineData("' UNION SELECT * FROM Users --")]
        public async Task SearchEndpoint_SqlInjectionAttempts_ShouldReturnSafeResponse(string maliciousInput)
        {
            // Act
            var response = await _client.GetAsync($"/Certificate/Certificates?searchTerm={Uri.EscapeDataString(maliciousInput)}");

            // Assert - Security middleware should block SQL injection attempts with BadRequest
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                "malicious SQL injection attempts should be blocked by security middleware");
        }

        [Theory]
        [InlineData("<script>alert('XSS')</script>")]
        [InlineData("<img src=x onerror=alert('XSS')>")]
        [InlineData("javascript:alert('XSS')")]
        [InlineData("<svg onload=alert('XSS')>")]
        public async Task SearchEndpoint_XssAttempts_ShouldReturnSafeResponse(string maliciousInput)
        {
            // Act
            var response = await _client.GetAsync($"/Certificate/Certificates?searchTerm={Uri.EscapeDataString(maliciousInput)}");

            // Assert - Security middleware should block XSS attempts with BadRequest
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                "malicious XSS attempts should be blocked by security middleware");
        }

        #endregion

        #region HTTPS and Security Headers Tests

        [Fact]
        public async Task HomePage_ShouldHaveSecurityHeaders()
        {
            // Act
            var response = await _client.GetAsync("/");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Check for important security headers
            response.Headers.Should().ContainKey("X-Content-Type-Options");
            response.Headers.Should().ContainKey("X-Frame-Options");
        }

        [Fact]
        public async Task StaticFiles_ShouldHaveAppropriateHeaders()
        {
            // Act
            var response = await _client.GetAsync("/css/main.css");

            // Assert
            if (response.StatusCode == HttpStatusCode.OK)
            {
                response.Content.Headers.ContentType?.MediaType.Should().Be("text/css");
            }
        }

        #endregion

        #region File Upload Security Tests

        [Fact]
        public async Task FileUpload_MaliciousFileType_ShouldBeRejected()
        {
            // This test would need to be adapted based on your actual file upload implementation
            // Arrange
            var maliciousContent = Encoding.UTF8.GetBytes("<?php system($_GET['cmd']); ?>");
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(maliciousContent), "file", "malicious.php");

            // Act
            var response = await _client.PostAsync("/Document/Upload", content);

            // Assert
            // Should reject PHP files or other executable types
            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        #endregion

        #region Rate Limiting Tests

        [Fact]
        public async Task MultipleRequests_ShouldNotCauseServerOverload()
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();

            // Act - Send 10 concurrent requests
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_client.GetAsync("/"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            responses.Should().NotBeEmpty();
            responses.Should().OnlyContain(r => r.StatusCode != HttpStatusCode.InternalServerError);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task InvalidRoute_ShouldReturn404WithoutRevealingInternalInfo()
        {
            // Act
            var response = await _client.GetAsync("/NonExistent/Route/That/Does/Not/Exist");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("stack trace");
            content.Should().NotContain("exception");
            content.Should().NotContain("debug");
        }

        [Fact]
        public async Task MalformedRequest_ShouldReturnGracefulError()
        {
            // Act
            var response = await _client.GetAsync("/Certificate/Details/not-a-number");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
        }

        #endregion

        #region Data Exposure Tests

        [Fact]
        public async Task PublicEndpoints_ShouldNotExposeInternalSystemInfo()
        {
            // Arrange
            var publicEndpoints = new[]
            {
                "/",
                "/Certificate/Certificates",
                "/Account/Login",
                "/Account/Register"
            };

            foreach (var endpoint in publicEndpoints)
            {
                // Act
                var response = await _client.GetAsync(endpoint);

                // Assert
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    content.Should().NotContain("connection string");
                    content.Should().NotContain("password");
                    content.Should().NotContain("secret");
                    content.Should().NotContain("api key");
                    content.Should().NotContain("token");
                }
            }
        }

        #endregion

        #region Browser Security Tests

        [Fact]
        public async Task HomePage_ShouldPreventClickjacking()
        {
            // Act
            var response = await _client.GetAsync("/");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Check for X-Frame-Options header
            if (response.Headers.Contains("X-Frame-Options"))
            {
                var frameOptions = response.Headers.GetValues("X-Frame-Options").FirstOrDefault();
                frameOptions.Should().BeOneOf("DENY", "SAMEORIGIN");
            }
        }

        #endregion

        #region Performance Security Tests

        [Fact]
        public async Task LargePayload_ShouldNotCauseMemoryExhaustion()
        {
            // Arrange
            var largeContent = new string('A', 10 * 1024 * 1024); // 10MB string
            var content = new StringContent(largeContent, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/Certificate/Certificates", content);

            // Assert
            // Should handle large payloads gracefully without crashing
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        }

        #endregion

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
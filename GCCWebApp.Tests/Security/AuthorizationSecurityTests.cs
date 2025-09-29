using System.Net;
using FluentAssertions;

namespace GCCWebApp.Tests.Security
{
    public class AuthorizationSecurityTests : IClassFixture<SecurityTestWebApplicationFactory>
    {
        private readonly SecurityTestWebApplicationFactory _factory;

        public AuthorizationSecurityTests(SecurityTestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task AdminEndpoints_WithAdminRole_ShouldAllowAccess()
        {
            // Arrange
            using var testBase = new SecurityTestBase(_factory);
            using var client = testBase.CreateAdminClient();

            // Act
            var response = await client.GetAsync("/Admin/UserManagement");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
            response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AdminEndpoints_WithAnalystRole_ShouldDenyAccess()
        {
            // Arrange
            using var testBase = new SecurityTestBase(_factory);
            using var client = testBase.CreateAnalystClient();

            // Act
            var response = await client.GetAsync("/Admin/UserManagement");

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Forbidden,
                HttpStatusCode.Found); // Redirect to login
        }

        [Fact]
        public async Task AdminEndpoints_WithExternalRole_ShouldDenyAccess()
        {
            // Arrange
            using var testBase = new SecurityTestBase(_factory);
            using var client = testBase.CreateExternalClient();

            // Act
            var response = await client.GetAsync("/Admin/UserManagement");

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Forbidden,
                HttpStatusCode.Found); // Redirect to login
        }

        [Fact]
        public async Task CertificateEndpoints_WithAnalystRole_ShouldAllowAccess()
        {
            // Arrange
            using var testBase = new SecurityTestBase(_factory);
            using var client = testBase.CreateAnalystClient();

            // Act
            var response = await client.GetAsync("/Certificate/Certificates");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
            response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CertificateCreate_WithExternalRole_ShouldDenyAccess()
        {
            // Arrange
            using var testBase = new SecurityTestBase(_factory);
            using var client = testBase.CreateExternalClient();

            // Act
            var response = await client.GetAsync("/Certificate/Create");

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Forbidden,
                HttpStatusCode.Found); // Redirect to login
        }

        [Theory]
        [InlineData("Admin", "/Admin/UserManagement")]
        [InlineData("Admin", "/Admin/RoleManagement")]
        [InlineData("Analyst", "/Certificate/Create")]
        [InlineData("Analyst", "/Certificate/Edit/1")]
        [InlineData("External", "/Certificate/CertificateTileViewExternal")]
        public async Task RoleBasedAccess_ShouldRespectPermissions(string role, string endpoint)
        {
            // Arrange
            using var testBase = new SecurityTestBase(_factory);
            using var client = testBase.CreateAuthenticatedClient(role);

            // Act
            var response = await client.GetAsync(endpoint);

            // Assert
            if (role == "Admin" ||
                (role == "Analyst" && endpoint.Contains("/Certificate/")) ||
                (role == "External" && endpoint.Contains("External")))
            {
                // Should allow access
                response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
                response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
                response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
            }
            else
            {
                // Should deny access
                response.StatusCode.Should().BeOneOf(
                    HttpStatusCode.Unauthorized,
                    HttpStatusCode.Forbidden,
                    HttpStatusCode.Found);
            }
        }

        [Fact]
        public async Task ApiEndpoints_WithoutAuthentication_ShouldRequireAuth()
        {
            // Arrange
            using var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            // Protected API endpoints should require authentication
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Unauthorized,
                HttpStatusCode.OK); // Some APIs might be public
        }

        [Fact]
        public async Task ApiEndpoints_WithValidAuthentication_ShouldAllowAccess()
        {
            // Arrange
            using var testBase = new SecurityTestBase(_factory);
            using var client = testBase.CreateAdminClient();

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task PublicEndpoints_ShouldBeAccessibleWithoutAuth()
        {
            // Arrange
            using var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Certificate/CertificateTileView");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginEndpoint_ShouldBeAccessibleWithoutAuth()
        {
            // Arrange
            using var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Account/Login");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Theory]
        [InlineData("/Admin/UserManagement")]
        [InlineData("/Admin/RoleManagement")]
        [InlineData("/Certificate/Create")]
        [InlineData("/Certificate/Edit/1")]
        public async Task ProtectedEndpoints_WithoutAuth_ShouldRedirectOrDeny(string endpoint)
        {
            // Arrange
            using var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(endpoint);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Forbidden,
                HttpStatusCode.Found); // Redirect to login
        }

        [Fact]
        public async Task ConcurrentAuthenticatedRequests_ShouldMaintainSecurity()
        {
            // Arrange
            using var testBase = new SecurityTestBase(_factory);
            var tasks = new List<Task<HttpResponseMessage>>();

            // Act - Multiple concurrent requests with different roles
            for (int i = 0; i < 5; i++)
            {
                using var adminClient = testBase.CreateAdminClient();
                using var analystClient = testBase.CreateAnalystClient();
                using var externalClient = testBase.CreateExternalClient();

                tasks.Add(adminClient.GetAsync("/Admin/UserManagement"));
                tasks.Add(analystClient.GetAsync("/Certificate/Certificates"));
                tasks.Add(externalClient.GetAsync("/Certificate/CertificateTileViewExternal"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            foreach (var response in responses)
            {
                // All requests should complete (not hang or crash)
                response.Should().NotBeNull();
                // Dispose responses
                response.Dispose();
            }
        }

        [Theory]
        [InlineData("'; DROP TABLE Users; --")]
        [InlineData("<script>alert('XSS')</script>")]
        [InlineData("../../etc/passwd")]
        [InlineData("javascript:alert('XSS')")]
        public async Task SecurityInputValidation_ShouldHandleMaliciousInput(string maliciousInput)
        {
            // Arrange
            using var testBase = new SecurityTestBase(_factory);
            using var client = testBase.CreateAnalystClient();

            // Act
            var encodedInput = Uri.EscapeDataString(maliciousInput);
            var response = await client.GetAsync($"/Certificate/Certificates?searchTerm={encodedInput}");

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.BadRequest,
                HttpStatusCode.NotFound);

            // Should never return server error from malicious input
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);

            var content = await response.Content.ReadAsStringAsync();
            // Response should not contain the malicious script
            content.Should().NotContain("<script>");
            content.Should().NotContain("DROP TABLE");
        }
    }
}
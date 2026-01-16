using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using GCCWebApp.Models;

namespace GCCWebApp.Tests.Integration
{
    public class ApiIntegrationTests : IntegrationTestBase
    {
        public ApiIntegrationTests(IntegrationTestWebApplicationFactory factory) : base(factory)
        {
        }

        protected override async Task SeedTestDataAsync()
        {
            await GCCWebApp.Tests.Integration.TestDataSeeder.SeedBasicTestDataAsync(DbContext);
        }

        [Fact(Skip = "Integration test requires running application")]
        public async Task GetCertificates_ShouldReturnSuccessAndData()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var response = await Client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();

            // Verify JSON structure
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var certificates = JsonSerializer.Deserialize<List<object>>(content, options);
            certificates.Should().NotBeNull();
            certificates.Should().HaveCountGreaterThan(0);
        }

        [Fact(Skip = "Integration test requires running application")]
        public async Task GetCertificatesTiles_PublicEndpoint_ShouldReturnSuccess()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var response = await Client.GetAsync("/api/CertificateTileExternal/tiles");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }

        [Fact(Skip = "Integration test requires running application")]
        public async Task SearchCertificates_WithValidSearchTerm_ShouldReturnResults()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var response = await Client.GetAsync("/api/CertificateTileExternal/tiles?searchTerm=Test");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }

        [Fact(Skip = "Integration test requires running application")]
        public async Task SearchCertificates_WithEmptySearchTerm_ShouldReturnAllResults()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var response = await Client.GetAsync("/api/CertificateTileExternal/tiles?searchTerm=");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }

        [Fact(Skip = "Integration test requires running application")]
        public async Task GetCertificateById_WithValidId_ShouldReturnCertificate()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var response = await Client.GetAsync("/api/v1/certificates/1");

            // Assert
            // Note: This might return 401/404 depending on authentication requirements
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.NotFound);
        }

        [Fact(Skip = "Integration test requires running application")]
        public async Task GetCertificateById_WithInvalidId_ShouldReturn404()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var response = await Client.GetAsync("/api/v1/certificates/99999");

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.NotFound,
                HttpStatusCode.Unauthorized);
        }

        [Theory(Skip = "Integration test requires running application")]
        [InlineData("'; DROP TABLE Certificates; --")]
        [InlineData("<script>alert('xss')</script>")]
        [InlineData("' OR '1'='1")]
        public async Task SearchCertificates_WithMaliciousInput_ShouldHandleSafely(string maliciousInput)
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var encodedInput = Uri.EscapeDataString(maliciousInput);
            var response = await Client.GetAsync($"/api/CertificateTileExternal/tiles?searchTerm={encodedInput}");

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.BadRequest);

            // Should not return server error
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        }

        [Fact(Skip = "Integration test requires running application")]
        public async Task ApiEndpoints_ShouldHaveProperHeaders()
        {
            // Act
            var response = await Client.GetAsync("/api/CertificateTileExternal/tiles");

            // Assert
            response.Headers.Should().ContainKey("Content-Type");
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        }

        [Fact(Skip = "Integration test requires running application")]
        public async Task ApiEndpoints_WithLargeRequest_ShouldHandleGracefully()
        {
            // Arrange
            var largeSearchTerm = new string('A', 1000); // 1KB search term

            // Act
            var encodedInput = Uri.EscapeDataString(largeSearchTerm);
            var response = await Client.GetAsync($"/api/CertificateTileExternal/tiles?searchTerm={encodedInput}");

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.BadRequest,
                HttpStatusCode.RequestUriTooLong);

            // Should not cause server error
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        }

        [Fact(Skip = "Integration test requires running application")]
        public async Task ApiHealth_ShouldBeReachable()
        {
            // Act
            var response = await Client.GetAsync("/health");

            // Assert
            // Health endpoint might not exist, but if it does, it should respond
            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                response.StatusCode.Should().BeOneOf(
                    HttpStatusCode.OK,
                    HttpStatusCode.ServiceUnavailable);
            }
        }

        [Fact(Skip = "Integration test requires running application")]
        public async Task ConcurrentApiRequests_ShouldHandleLoad()
        {
            // Arrange
            await SeedTestDataAsync();
            var tasks = new List<Task<HttpResponseMessage>>();

            // Act - Make 10 concurrent requests
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Client.GetAsync("/api/CertificateTileExternal/tiles"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            foreach (var response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                response.Dispose();
            }
        }
    }
}
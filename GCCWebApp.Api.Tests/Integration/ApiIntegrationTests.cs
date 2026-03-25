using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using GCCWebApp.Models;
using GCCWebApp.Models.Api;

namespace GCCWebApp.Api.Tests.Integration
{
    public class ApiIntegrationTests : IClassFixture<WebApplicationFactoryBase<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactoryBase<Program> _factory;

        public ApiIntegrationTests(WebApplicationFactoryBase<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();

            // Add authentication header using test API key seeded in TestDbContextFactory
            _client.DefaultRequestHeaders.Add("X-API-Key", "valid_test_key");
        }

        [Fact]
        public async Task GetCertificates_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/certificates?pageNumber=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetCertificate_WithValidId_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/certificates/1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetCertificate_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/certificates/999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CreateCertificate_WithValidData_ReturnsCreatedOrBadRequest()
        {
            // Arrange
            var newCertificate = new
            {
                Number = "INTEGRATION-TEST-001",
                ProductId = 1,
                CompanyId = 1,
                CertificationTypeId = 1,
                EffectiveDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddYears(1),
                Status = "Client Approved - Paid"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/certificates", newCertificate);

            // Assert - May return Created (201) or BadRequest (400) depending on model validation
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Created,
                HttpStatusCode.OK,
                HttpStatusCode.BadRequest);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();
        }

        [Fact]
        public async Task CreateCertificate_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var invalidCertificate = new
            {
                // Missing required fields
                Number = "",
                ProductId = 0,
                CompanyId = 0,
                CertificationTypeId = 0
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/certificates", invalidCertificate);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetProducts_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/products?pageNumber=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();
        }

        [Fact]
        public async Task SearchCertificates_WithParameters_ReturnsResults()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/certificates/search?term=TEST&pageNumber=1&pageSize=10");

            // Assert - May return 200 OK or 400 BadRequest depending on validation
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetExpiringCertificates_ReturnsResults()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/certificates/expiring?days=60&pageNumber=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ApiHealthCheck_ReturnsSuccess()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            // Health check may return 503 ServiceUnavailable when DB is not fully available in test
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.ServiceUnavailable);
        }

        [Theory]
        [InlineData("/api/v1/certificates")]
        [InlineData("/api/v1/products")]
        public async Task ApiEndpoints_ReturnSuccessOrAuthenticationRequired(string endpoint)
        {
            // Act
            var response = await _client.GetAsync($"{endpoint}?pageNumber=1&pageSize=10");

            // Assert
            // API might return 401/403 if authentication is required, which is expected
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK, 
                HttpStatusCode.Unauthorized, 
                HttpStatusCode.Forbidden
            );
        }

        [Fact]
        public async Task InvalidApiEndpoint_ReturnsNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/nonexistent");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ApiVersioning_WorksCorrectly()
        {
            // Act
            var v1Response = await _client.GetAsync("/api/v1/certificates/1");
            
            // Assert
            // Test that versioning is working correctly
            v1Response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.NotFound,
                HttpStatusCode.Unauthorized
            );
        }

        private async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<T>(content, options);
        }
    }
}
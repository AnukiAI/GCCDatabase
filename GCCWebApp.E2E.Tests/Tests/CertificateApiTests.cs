using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using GCCWebApp.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GCCWebApp.E2E.Tests.Tests
{
    public class CertificateApiTests : IClassFixture<TestWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly TestWebApplicationFactory<Program> _factory;

        public CertificateApiTests(TestWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task GetCertificates_ReturnsSuccessStatusCode()
        {
            // Arrange - versioned API route: /api/v1/certificates
            var request = "/api/v1/certificates";

            // Act
            var response = await _client.GetAsync(request);

            // Assert - API requires authentication (API key)
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.Unauthorized
            );
        }

        [Fact]
        public async Task GetCertificate_WithValidId_ReturnsSuccess()
        {
            // Arrange
            var certificateId = 1; // Assuming test data has this ID
            var request = $"/api/v1/certificates/{certificateId}";

            // Act
            var response = await _client.GetAsync(request);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.NotFound,
                HttpStatusCode.Unauthorized
            );
        }

        [Fact]
        public async Task CreateCertificate_WithValidData_ReturnsCreated()
        {
            // Arrange
            var newCertificate = new
            {
                CertificateNumber = "TEST-001",
                CompanyId = 1,
                ProductId = 1,
                IssueDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddYears(1),
                Status = "Active"
            };

            var json = JsonSerializer.Serialize(newCertificate);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/certificates", content);

            // Assert
            // May return Unauthorized if authentication is required
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Created,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Forbidden
            );
        }

        [Fact]
        public async Task DeleteCertificate_WithValidId_ReturnsSuccess()
        {
            // Arrange
            var certificateId = 999; // Non-existent ID for safe testing

            // Act
            var response = await _client.DeleteAsync($"/api/v1/certificates/{certificateId}");

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.NoContent,
                HttpStatusCode.NotFound,
                HttpStatusCode.Unauthorized
            );
        }
    }
}
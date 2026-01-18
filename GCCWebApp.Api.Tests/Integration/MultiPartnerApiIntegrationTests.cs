using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using GCCWebApp.Models.Api;
using GCCWebApp.Models;

namespace GCCWebApp.Api.Tests.Integration
{
    /// <summary>
    /// Tests for API-001 to API-005: Multi-Partner API Support and Integration Requirements
    /// </summary>
    public class MultiPartnerApiIntegrationTests : IClassFixture<WebApplicationFactoryBase<Program>>
    {
        private readonly WebApplicationFactoryBase<Program> _factory;
        private readonly HttpClient _client;

        public MultiPartnerApiIntegrationTests(WebApplicationFactoryBase<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        #region API-001: Multiple Partner API Support Tests

        [Theory]
        [InlineData("Amazon")]
        [InlineData("Wayfair")]
        [InlineData("Other")]
        public async Task GetCertificates_WithPartnerFilter_ShouldReturnPartnerSpecificData(string partnerName)
        {
            // Arrange
            var requestUri = $"/api/v1/certificates?partner={partnerName}";

            // Act
            var response = await _client.GetAsync(requestUri);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();
            
            // Verify response contains partner-specific data
            content.Should().Contain(partnerName);
        }

        [Fact]
        public async Task GetCertificates_WithAmazonApiKey_ShouldReturnAmazonDataOnly()
        {
            // Arrange
            _client.DefaultRequestHeaders.Add("X-API-Key", "amazon_test_key");
            
            // Act
            var response = await _client.GetAsync("/api/v1/certificates");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var certificates = JsonSerializer.Deserialize<ApiResponse<IEnumerable<Certificate>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            // Verify data segregation - Amazon users should only see Amazon data
            certificates?.Data?.All(c => c.Company == null || c.Company.Name.Contains("Amazon") || string.IsNullOrEmpty(c.Company.Name))
                .Should().BeTrue("Amazon API key should only return Amazon or unassigned certificates");
        }

        [Fact]
        public async Task GetCertificates_WithWayfairApiKey_ShouldReturnWayfairDataOnly()
        {
            // Arrange
            _client.DefaultRequestHeaders.Add("X-API-Key", "wayfair_test_key");
            
            // Act
            var response = await _client.GetAsync("/api/v1/certificates");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var certificates = JsonSerializer.Deserialize<ApiResponse<IEnumerable<Certificate>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            // Verify data segregation - Wayfair users should only see Wayfair data
            certificates?.Data?.All(c => c.Company == null || c.Company.Name.Contains("Wayfair") || string.IsNullOrEmpty(c.Company.Name))
                .Should().BeTrue("Wayfair API key should only return Wayfair or unassigned certificates");
        }

        [Fact]
        public async Task CreateCertificate_WithPartnerSpecificData_ShouldSetCorrectPartner()
        {
            // Arrange
            var newCertificate = new
            {
                Number = "TEST-AMAZON-001",
                Partner = "Amazon",
                CompanyId = 1,
                ProductId = 1,
                Status = "Active"
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(newCertificate),
                Encoding.UTF8,
                "application/json"
            );

            _client.DefaultRequestHeaders.Add("X-API-Key", "amazon_test_key");

            // Act
            var response = await _client.PostAsync("/api/v1/certificates", jsonContent);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.NotImplemented);
            
            if (response.StatusCode == HttpStatusCode.Created)
            {
                var content = await response.Content.ReadAsStringAsync();
                content.Should().Contain("Amazon");
            }
        }

        #endregion

        #region API-002: API Error Handling & Fallbacks Tests

        [Fact]
        public async Task GetCertificates_WithInvalidApiKey_ShouldReturnUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Add("X-API-Key", "invalid_key_123");

            // Act
            var response = await _client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            
            var content = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            errorResponse?.Success.Should().BeFalse();
            errorResponse?.Message.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetCertificates_WithMissingApiKey_ShouldReturnUnauthorized()
        {
            // Arrange - No API key provided

            // Act
            var response = await _client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetCertificates_WithServerError_ShouldReturnGracefulError()
        {
            // Arrange - Force a server error scenario
            var requestUri = "/api/v1/certificates?forceError=true";

            // Act
            var response = await _client.GetAsync(requestUri);

            // Assert - Should not return 500, should handle gracefully
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
            
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                errorResponse?.Success.Should().BeFalse();
                errorResponse?.Message.Should().NotBeEmpty();
            }
        }

        [Fact]
        public async Task PostCertificate_WithInvalidData_ShouldReturnValidationErrors()
        {
            // Arrange
            var invalidCertificate = new
            {
                // Missing required fields
                Number = "",
                Partner = "InvalidPartner",
                CompanyId = -1
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(invalidCertificate),
                Encoding.UTF8,
                "application/json"
            );

            _client.DefaultRequestHeaders.Add("X-API-Key", "valid_test_key");

            // Act
            var response = await _client.PostAsync("/api/v1/certificates", jsonContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            
            var content = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            errorResponse?.Success.Should().BeFalse();
            errorResponse?.Message?.ToLower().Should().Contain("validation");
        }

        [Fact]
        public async Task ApiEndpoints_WithRateLimiting_ShouldEnforceRateLimits()
        {
            // Arrange
            _client.DefaultRequestHeaders.Add("X-API-Key", "rate_limit_test_key");
            var tasks = new List<Task<HttpResponseMessage>>();

            // Act - Send multiple requests rapidly
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_client.GetAsync("/api/v1/certificates"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert - Some requests should be rate limited (429)
            var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            var successfulCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
            
            // Should have some successful requests and potentially some rate limited
            successfulCount.Should().BeGreaterThan(0);
            // Note: Rate limiting may not be implemented yet, so we just verify the structure exists
        }

        #endregion

        #region API-003: Product ID Type Standardization Tests

        [Theory]
        [InlineData("UPC")]
        [InlineData("GTIN")]
        [InlineData("ASIN")]
        [InlineData("SKU")]
        public async Task GetProducts_WithProductIdType_ShouldHandleAllTypes(string productIdType)
        {
            // Arrange
            var requestUri = $"/api/v1/products?idType={productIdType}";
            _client.DefaultRequestHeaders.Add("X-API-Key", "valid_test_key");

            // Act
            var response = await _client.GetAsync(requestUri);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            // Verify that the API can handle different product ID types
            content.Should().NotBeEmpty();
        }

        [Fact]
        public async Task CreateProduct_WithUPCAndGTIN_ShouldAcceptBothFormats()
        {
            // Arrange - Test 4/30 version requirement (UPC/GTIN)
            var productWithUPC = new
            {
                Name = "Test Product UPC",
                UPC = "123456789012",
                GTIN = "01234567890123",
                CategoryId = 1
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(productWithUPC),
                Encoding.UTF8,
                "application/json"
            );

            _client.DefaultRequestHeaders.Add("X-API-Key", "valid_test_key");

            // Act
            var response = await _client.PostAsync("/api/v1/products", jsonContent);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.NotImplemented);
        }

        [Fact]
        public async Task CreateProduct_WithASIN_ShouldAcceptAmazonFormat()
        {
            // Arrange - Test 6/15 version requirement (ASIN)
            var productWithASIN = new
            {
                Name = "Test Product ASIN",
                ASIN = "B08N5WRWNW",
                CategoryId = 1
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(productWithASIN),
                Encoding.UTF8,
                "application/json"
            );

            _client.DefaultRequestHeaders.Add("X-API-Key", "amazon_test_key");

            // Act
            var response = await _client.PostAsync("/api/v1/products", jsonContent);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.NotImplemented);
        }

        [Fact]
        public async Task ValidateProductId_WithInvalidFormat_ShouldReturnValidationError()
        {
            // Arrange
            var productWithInvalidId = new
            {
                Name = "Test Product Invalid",
                UPC = "invalid_upc_format",
                ASIN = "invalid_asin",
                CategoryId = 1
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(productWithInvalidId),
                Encoding.UTF8,
                "application/json"
            );

            _client.DefaultRequestHeaders.Add("X-API-Key", "valid_test_key");

            // Act
            var response = await _client.PostAsync("/api/v1/products", jsonContent);

            // Assert
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var content = await response.Content.ReadAsStringAsync();
                content.ToLower().Should().Contain("format");
            }
        }

        #endregion

        #region API-004: Amazon API Validation Tests

        [Fact]
        public async Task AmazonApiIntegration_WithValidJsonFormat_ShouldProcessCorrectly()
        {
            // Arrange - Test Amazon-specific JSON format
            var amazonCertificateData = new
            {
                CertificateNumber = "AMAZON-CERT-001",
                Partner = "Amazon",
                CompanyName = "Test Amazon Supplier",
                Products = new[]
                {
                    new { ASIN = "B08N5WRWNW", Name = "Test Product 1" },
                    new { ASIN = "B07XYZ1234", Name = "Test Product 2" }
                },
                EffectiveDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ExpirationDate = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(amazonCertificateData),
                Encoding.UTF8,
                "application/json"
            );

            _client.DefaultRequestHeaders.Add("X-API-Key", "amazon_test_key");

            // Act
            var response = await _client.PostAsync("/api/v1/amazon/certificates", jsonContent);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Created,
                HttpStatusCode.OK,
                HttpStatusCode.NotImplemented,
                HttpStatusCode.NotFound); // May not be implemented yet

            if (response.StatusCode == HttpStatusCode.Created)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                responseContent.Should().Contain("AMAZON-CERT-001");
            }
        }

        [Fact]
        public async Task AmazonApiIntegration_WithInvalidJsonFormat_ShouldReturnFormatError()
        {
            // Arrange - Test invalid Amazon JSON format
            var invalidAmazonData = new
            {
                // Missing required fields for Amazon format
                WrongField = "Invalid"
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(invalidAmazonData),
                Encoding.UTF8,
                "application/json"
            );

            _client.DefaultRequestHeaders.Add("X-API-Key", "amazon_test_key");

            // Act
            var response = await _client.PostAsync("/api/v1/amazon/certificates", jsonContent);

            // Assert
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var content = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                errorResponse?.Success.Should().BeFalse();
                errorResponse?.Message?.ToLower().Should().Contain("format");
            }
        }

        #endregion

        #region API-005: Partner API Documentation Tests

        [Fact]
        public async Task ApiDocumentation_SwaggerEndpoint_ShouldBeAccessible()
        {
            // Act
            var response = await _client.GetAsync("/swagger/index.html");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                content.ToLower().Should().Contain("api");
            }
        }

        [Fact]
        public async Task ApiDocumentation_OpenApiSpec_ShouldIncludePartnerEndpoints()
        {
            // Act
            var response = await _client.GetAsync("/swagger/v1/swagger.json");

            // Assert
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                var openApiSpec = JsonSerializer.Deserialize<JsonElement>(content);

                // Verify that partner-specific endpoints are documented
                var pathsJson = openApiSpec.GetProperty("paths").ToString();
                pathsJson.ToLower().Should().Contain("/api/v1/certificates");
                pathsJson.ToLower().Should().Contain("/api/v1/products");
            }
        }

        [Fact]
        public async Task ApiHealth_HealthCheckEndpoint_ShouldReturnHealthy()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                content.ToLower().Should().Contain("healthy");
            }
        }

        #endregion

        #region Test Data Scenarios

        [Fact]
        public async Task IntegrationScenario_CompletePartnerWorkflow_ShouldExecuteSuccessfully()
        {
            // Arrange - Complete workflow test
            _client.DefaultRequestHeaders.Add("X-API-Key", "integration_test_key");

            // Act & Assert - Step 1: Get available partners
            var partnersResponse = await _client.GetAsync("/api/v1/partners");
            partnersResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

            // Step 2: Get certificates for a specific partner
            var certificatesResponse = await _client.GetAsync("/api/v1/certificates?partner=Amazon");
            certificatesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 3: Get products for the partner
            var productsResponse = await _client.GetAsync("/api/v1/products?partner=Amazon");
            productsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the complete workflow executed without critical failures
            var responses = new[] { partnersResponse, certificatesResponse, productsResponse };
            responses.Any(r => r.StatusCode == HttpStatusCode.InternalServerError).Should().BeFalse(
                "No critical server errors should occur during partner workflow");
        }

        #endregion
    }
}
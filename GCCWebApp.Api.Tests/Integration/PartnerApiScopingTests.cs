using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace GCCWebApp.Api.Tests.Integration
{
    /// <summary>
    /// Tests for partner API data scoping: verifies that each partner (Amazon, Novi, Other)
    /// only sees their own certificates through the /api/v1/certificates endpoints.
    ///
    /// Covers:
    /// - Data isolation per partner API key
    /// - Standard response fields per partner
    /// - includeProductIds response shape
    /// - GS1 JSON-LD format (Amazon-specific)
    /// - Product ID search scoping
    /// - Cross-partner data leak prevention
    /// </summary>
    public class PartnerApiScopingTests : IClassFixture<PartnerScopingFactory>
    {
        private readonly PartnerScopingFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        // Partner API keys (gcc_partner_ prefix) — must match seed data in TestDbContextFactory
        private const string AmazonApiKey = "gcc_partner_amazon_test_key_0000000000000000000000000000";
        private const string NoviApiKey = "gcc_partner_novi_test_key_00000000000000000000000000000000";
        private const string OtherApiKey = "gcc_partner_other_test_key_000000000000000000000000000000";

        public PartnerApiScopingTests(PartnerScopingFactory factory)
        {
            _factory = factory;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        private HttpClient CreateClientWithApiKey(string apiKey)
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            return client;
        }

        #region Amazon Partner Scoping

        [Fact]
        public async Task Amazon_GetCertificates_ReturnsOnlyAmazonCertificates()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("success").GetBoolean().Should().BeTrue();

            var data = root.GetProperty("data");
            data.GetArrayLength().Should().BeGreaterThan(0, "Amazon should have seeded certificates");

            // Every certificate returned must have partner == "Amazon"
            foreach (var cert in data.EnumerateArray())
            {
                cert.GetProperty("partner").GetString().Should().Be("Amazon",
                    "Amazon API key should only return Amazon-scoped certificates");
            }
        }

        [Fact]
        public async Task Amazon_GetCertificates_DoesNotReturnNoviOrOtherCertificates()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            foreach (var cert in data.EnumerateArray())
            {
                var partner = cert.GetProperty("partner").GetString();
                partner.Should().NotBe("Novi", "Amazon should not see Novi certificates");
                partner.Should().NotBe("Other", "Amazon should not see Other certificates");
                partner.Should().NotBe("None", "Amazon should not see unassigned certificates");
            }
        }

        [Fact]
        public async Task Amazon_GetCertificates_StandardResponseFields()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            data.GetArrayLength().Should().BeGreaterThan(0);

            var cert = data[0];

            // Verify all standard response fields are present
            cert.TryGetProperty("id", out _).Should().BeTrue();
            cert.TryGetProperty("certificateNumber", out _).Should().BeTrue();
            cert.TryGetProperty("status", out _).Should().BeTrue();
            cert.TryGetProperty("issueDate", out _).Should().BeTrue();
            cert.TryGetProperty("expirationDate", out _).Should().BeTrue();
            cert.TryGetProperty("location", out _).Should().BeTrue();
            cert.TryGetProperty("certificationCountries", out _).Should().BeTrue();
            cert.TryGetProperty("partner", out _).Should().BeTrue();
            cert.TryGetProperty("certificationResults", out _).Should().BeTrue();
            cert.TryGetProperty("pathway", out _).Should().BeTrue();
            cert.TryGetProperty("productName", out _).Should().BeTrue();
            cert.TryGetProperty("company", out _).Should().BeTrue();
            cert.TryGetProperty("certificationType", out _).Should().BeTrue();
            cert.TryGetProperty("amazonCode", out _).Should().BeTrue();
            cert.TryGetProperty("createdAt", out _).Should().BeTrue();
            cert.TryGetProperty("updatedAt", out _).Should().BeTrue();
        }

        [Fact]
        public async Task Amazon_GetCertificates_AmazonCodeFieldPopulated()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            // At least one Amazon cert should have an amazonCode
            var hasAmazonCode = false;
            foreach (var cert in data.EnumerateArray())
            {
                if (cert.TryGetProperty("amazonCode", out var amazonCode) &&
                    amazonCode.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrEmpty(amazonCode.GetString()))
                {
                    hasAmazonCode = true;
                    amazonCode.GetString().Should().StartWith("greencircle_",
                        "Amazon code should follow the greencircle_ naming convention");
                }
            }

            hasAmazonCode.Should().BeTrue("Amazon certificates should have amazonCode mapped from CertificationType");
        }

        [Fact]
        public async Task Amazon_GetCertificates_CertificationResultsFieldPopulated()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            // At least one Amazon cert should have certificationResults
            var hasResults = false;
            foreach (var cert in data.EnumerateArray())
            {
                if (cert.TryGetProperty("certificationResults", out var results) &&
                    results.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrEmpty(results.GetString()))
                {
                    hasResults = true;
                }
            }

            hasResults.Should().BeTrue("Amazon certificates should have certificationResults for threshold verification");
        }

        [Fact]
        public async Task Amazon_GetCertificatesWithProductIds_IncludesProductIdentifiers()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates?includeProductIds=true");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            data.GetArrayLength().Should().BeGreaterThan(0);

            // Check that at least one certificate has productIds
            var hasProductIds = false;
            foreach (var cert in data.EnumerateArray())
            {
                if (cert.TryGetProperty("productIds", out var productIds) &&
                    productIds.GetArrayLength() > 0)
                {
                    hasProductIds = true;

                    var firstProductId = productIds[0];
                    firstProductId.TryGetProperty("type", out _).Should().BeTrue();
                    firstProductId.TryGetProperty("value", out _).Should().BeTrue();
                    firstProductId.TryGetProperty("status", out _).Should().BeTrue();
                    firstProductId.TryGetProperty("partner", out _).Should().BeTrue();
                }

                // includeProductIds=true should also include issuer and pdfDownloadUrl
                cert.TryGetProperty("issuer", out _).Should().BeTrue();
                cert.TryGetProperty("pdfDownloadUrl", out _).Should().BeTrue();
            }

            hasProductIds.Should().BeTrue("Amazon certificates should have ASIN/UPC/GTIN product IDs");
        }

        [Fact]
        public async Task Amazon_GetCertificatesWithProductIds_IncludesAsinType()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates?includeProductIds=true");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            // At least one Amazon product ID should be of type ASIN
            var hasAsin = false;
            foreach (var cert in data.EnumerateArray())
            {
                if (cert.TryGetProperty("productIds", out var productIds))
                {
                    foreach (var pid in productIds.EnumerateArray())
                    {
                        if (pid.GetProperty("type").GetString() == "ASIN")
                        {
                            hasAsin = true;
                            pid.GetProperty("value").GetString().Should().NotBeNullOrEmpty();
                        }
                    }
                }
            }

            hasAsin.Should().BeTrue("Amazon certificates should include ASIN product identifiers");
        }

        [Fact]
        public async Task Amazon_GetCertificates_ReturnsTwoCertificates()
        {
            // Arrange — seed data has two Amazon certs (IDs 100, 101)
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            data.GetArrayLength().Should().Be(2, "Seed data contains exactly 2 Amazon certificates");
        }

        [Fact]
        public async Task Amazon_GetCertificates_PaginationMetadataPresent()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.TryGetProperty("pagination", out var pagination).Should().BeTrue();
            pagination.TryGetProperty("totalCount", out _).Should().BeTrue();
            pagination.TryGetProperty("pageNumber", out _).Should().BeTrue();
            pagination.TryGetProperty("pageSize", out _).Should().BeTrue();
        }

        #endregion

        #region Amazon GS1 Format

        [Fact]
        public async Task Amazon_GetCertificatesGS1_ReturnsGS1JsonLdFormat()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates/gs1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("success").GetBoolean().Should().BeTrue();

            var data = root.GetProperty("data");
            data.GetArrayLength().Should().BeGreaterThan(0);

            var report = data[0];

            // Verify GS1 JSON-LD structure
            report.GetProperty("@context").GetString()
                .Should().Contain("gs1us-technology.github.io");
            report.GetProperty("$schema").GetString()
                .Should().Contain("gs1us-technology.github.io");
            report.GetProperty("type").GetString().Should().Be("CertificationReport");
            report.GetProperty("issuer").GetString().Should().Be("GreenCircle Certified");
            report.TryGetProperty("issueDate", out _).Should().BeTrue();
        }

        [Fact]
        public async Task Amazon_GetCertificatesGS1_ProductsArrayContainsCertificationInfo()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates/gs1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            var report = data[0];
            var products = report.GetProperty("products");
            products.GetArrayLength().Should().BeGreaterThan(0);

            var product = products[0];
            product.GetProperty("type").GetString().Should().Be("Product");

            var certInfo = product.GetProperty("certificationInfo");
            certInfo.GetProperty("type").GetString().Should().Be("CertificationDetails");
            certInfo.GetProperty("agency").GetString().Should().Be("GreenCircle Certified");
            certInfo.TryGetProperty("program", out _).Should().BeTrue();
            certInfo.TryGetProperty("certificateId", out _).Should().BeTrue();
            certInfo.TryGetProperty("status", out _).Should().BeTrue();
            certInfo.TryGetProperty("startDate", out _).Should().BeTrue();
            certInfo.TryGetProperty("endDate", out _).Should().BeTrue();
        }

        [Fact]
        public async Task Amazon_GetCertificatesGS1_StatusMappedCorrectly()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates/gs1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            foreach (var report in data.EnumerateArray())
            {
                var certInfo = report.GetProperty("products")[0].GetProperty("certificationInfo");
                var status = certInfo.GetProperty("status").GetString();

                // GS1 status should be ACTIVE or INACTIVE (uppercase)
                status.Should().BeOneOf("ACTIVE", "INACTIVE");
            }
        }

        [Fact]
        public async Task Amazon_GetCertificatesGS1_AmazonCodeAndPathwayPresent()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates/gs1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            // At least one GS1 report should include amazonCode and pathway
            var hasAmazonFields = false;
            foreach (var report in data.EnumerateArray())
            {
                var certInfo = report.GetProperty("products")[0].GetProperty("certificationInfo");

                if (certInfo.TryGetProperty("amazonCode", out var code) && code.ValueKind == JsonValueKind.String)
                {
                    hasAmazonFields = true;
                    code.GetString().Should().StartWith("greencircle_");
                }
            }

            hasAmazonFields.Should().BeTrue("GS1 format should include Amazon-specific fields");
        }

        [Fact]
        public async Task Amazon_GetCertificatesGS1_GtinPaddedTo14Digits()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates/gs1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            foreach (var report in data.EnumerateArray())
            {
                var product = report.GetProperty("products")[0];
                if (product.TryGetProperty("gtin", out var gtin) && gtin.ValueKind == JsonValueKind.String)
                {
                    gtin.GetString()!.Length.Should().Be(14, "GTIN should be padded to 14 digits");
                }
            }
        }

        [Fact]
        public async Task Amazon_GetCertificatesGS1_AdditionalTradeItemIdsIncludeAsin()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates/gs1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            var hasAsinInAdditionalIds = false;
            foreach (var report in data.EnumerateArray())
            {
                var product = report.GetProperty("products")[0];
                if (product.TryGetProperty("additionalTradeItemId", out var additionalIds) &&
                    additionalIds.ValueKind == JsonValueKind.Array)
                {
                    foreach (var addId in additionalIds.EnumerateArray())
                    {
                        if (addId.GetProperty("type").GetString() == "ASIN")
                        {
                            hasAsinInAdditionalIds = true;
                            addId.GetProperty("value").GetString().Should().NotBeNullOrEmpty();
                        }
                    }
                }
            }

            hasAsinInAdditionalIds.Should().BeTrue("Amazon GS1 format should include ASIN in additionalTradeItemId");
        }

        [Fact]
        public async Task Amazon_GetSingleCertificateGS1_ReturnsSingleReport()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates/gs1/24-0100");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("type").GetString().Should().Be("CertificationReport");
            root.GetProperty("issuer").GetString().Should().Be("GreenCircle Certified");
        }

        [Fact]
        public async Task Amazon_GetCertificatesGS1_CertificationResultsMappedToValue()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates/gs1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            // certificationResults maps to GS1 certificationInfo.value
            var hasValue = false;
            foreach (var report in data.EnumerateArray())
            {
                var certInfo = report.GetProperty("products")[0].GetProperty("certificationInfo");
                if (certInfo.TryGetProperty("value", out var val) && val.ValueKind == JsonValueKind.String)
                {
                    hasValue = true;
                    val.GetString().Should().NotBeNullOrEmpty();
                }
            }

            hasValue.Should().BeTrue("CertificationResults should map to GS1 certificationInfo.value");
        }

        #endregion

        #region Novi Partner Scoping

        [Fact]
        public async Task Novi_GetCertificates_ReturnsOnlyNoviCertificates()
        {
            // Arrange
            using var client = CreateClientWithApiKey(NoviApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            data.GetArrayLength().Should().BeGreaterThan(0, "Novi should have seeded certificates");

            foreach (var cert in data.EnumerateArray())
            {
                cert.GetProperty("partner").GetString().Should().Be("Novi",
                    "Novi API key should only return Novi-scoped certificates");
            }
        }

        [Fact]
        public async Task Novi_GetCertificates_DoesNotReturnAmazonCertificates()
        {
            // Arrange
            using var client = CreateClientWithApiKey(NoviApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            foreach (var cert in data.EnumerateArray())
            {
                cert.GetProperty("partner").GetString().Should().NotBe("Amazon",
                    "Novi should not see Amazon certificates");
            }
        }

        [Fact]
        public async Task Novi_GetCertificates_ReturnsOneCertificate()
        {
            // Arrange — seed data has one Novi cert (ID 200)
            using var client = CreateClientWithApiKey(NoviApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            data.GetArrayLength().Should().Be(1, "Seed data contains exactly 1 Novi certificate");
        }

        [Fact]
        public async Task Novi_GetCertificates_StandardResponseFieldsPresent()
        {
            // Arrange
            using var client = CreateClientWithApiKey(NoviApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var cert = doc.RootElement.GetProperty("data")[0];

            // Novi gets the same field set as Amazon
            cert.TryGetProperty("id", out _).Should().BeTrue();
            cert.TryGetProperty("certificateNumber", out _).Should().BeTrue();
            cert.TryGetProperty("status", out _).Should().BeTrue();
            cert.TryGetProperty("issueDate", out _).Should().BeTrue();
            cert.TryGetProperty("expirationDate", out _).Should().BeTrue();
            cert.TryGetProperty("partner", out _).Should().BeTrue();
            cert.TryGetProperty("certificationResults", out _).Should().BeTrue();
            cert.TryGetProperty("amazonCode", out _).Should().BeTrue(); // Field exists for all partners
            cert.TryGetProperty("certificationType", out _).Should().BeTrue();
        }

        [Fact]
        public async Task Novi_GetCertificatesWithProductIds_IncludesSkuAndMpnTypes()
        {
            // Arrange
            using var client = CreateClientWithApiKey(NoviApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates?includeProductIds=true");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            var productIdTypes = new List<string>();
            foreach (var cert in data.EnumerateArray())
            {
                if (cert.TryGetProperty("productIds", out var productIds))
                {
                    foreach (var pid in productIds.EnumerateArray())
                    {
                        productIdTypes.Add(pid.GetProperty("type").GetString()!);
                    }
                }
            }

            productIdTypes.Should().Contain("SKU", "Novi should have SKU product IDs");
            productIdTypes.Should().Contain("MPN", "Novi should have MPN product IDs");
        }

        #endregion

        #region Other Partner Scoping

        [Fact]
        public async Task Other_GetCertificates_ReturnsOnlyOtherCertificates()
        {
            // Arrange
            using var client = CreateClientWithApiKey(OtherApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            data.GetArrayLength().Should().BeGreaterThan(0, "Other partner should have seeded certificates");

            foreach (var cert in data.EnumerateArray())
            {
                cert.GetProperty("partner").GetString().Should().Be("Other",
                    "Other API key should only return Other-scoped certificates");
            }
        }

        [Fact]
        public async Task Other_GetCertificates_ReturnsOneCertificate()
        {
            // Arrange — seed data has one Other cert (ID 300)
            using var client = CreateClientWithApiKey(OtherApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            data.GetArrayLength().Should().Be(1, "Seed data contains exactly 1 Other certificate");
        }

        [Fact]
        public async Task Other_GetCertificates_DoesNotReturnAmazonOrNoviCertificates()
        {
            // Arrange
            using var client = CreateClientWithApiKey(OtherApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            foreach (var cert in data.EnumerateArray())
            {
                var partner = cert.GetProperty("partner").GetString();
                partner.Should().NotBe("Amazon");
                partner.Should().NotBe("Novi");
            }
        }

        #endregion

        #region Product ID Search Scoping

        [Fact]
        public async Task Amazon_SearchByAsin_ReturnsMatchingAmazonCertificate()
        {
            // Arrange — ASIN B08XQRT456 belongs to Amazon cert 24-0100
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates/search/productid?productId=B08XQRT456&type=ASIN");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("success").GetBoolean().Should().BeTrue();

            var data = root.GetProperty("data");
            data.GetArrayLength().Should().BeGreaterThan(0);

            var cert = data[0];
            cert.GetProperty("partner").GetString().Should().Be("Amazon");
            cert.GetProperty("certificateNumber").GetString().Should().Be("24-0100");
        }

        [Fact]
        public async Task Amazon_SearchByAsin_ResponseIncludesAllProductIds()
        {
            // Arrange — Cert 24-0100 has ASIN, UPC, GTIN
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates/search/productid?productId=B08XQRT456&type=ASIN");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");
            var cert = data[0];

            var productIds = cert.GetProperty("productIds");
            productIds.GetArrayLength().Should().BeGreaterThanOrEqualTo(3,
                "Should return ALL product IDs for the certificate, not just the matched one");

            var types = new List<string>();
            foreach (var pid in productIds.EnumerateArray())
            {
                types.Add(pid.GetProperty("type").GetString()!);
            }

            types.Should().Contain("ASIN");
            types.Should().Contain("UPC");
            types.Should().Contain("GTIN");
        }

        [Fact]
        public async Task Amazon_SearchByAsin_MatchedIdFlaggedCorrectly()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates/search/productid?productId=B08XQRT456&type=ASIN");

            // Assert
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var productIds = doc.RootElement.GetProperty("data")[0].GetProperty("productIds");

            var hasMatchedFlag = false;
            foreach (var pid in productIds.EnumerateArray())
            {
                if (pid.TryGetProperty("isMatchedId", out var isMatched) && isMatched.GetBoolean())
                {
                    hasMatchedFlag = true;
                    pid.GetProperty("type").GetString().Should().Be("ASIN");
                    pid.GetProperty("value").GetString().Should().Be("B08XQRT456");
                }
            }

            hasMatchedFlag.Should().BeTrue("The searched product ID should be flagged with isMatchedId=true");
        }

        [Fact]
        public async Task Novi_SearchByNoviSku_ReturnsNoviCertificate()
        {
            // Arrange — SKU NOVI-ECS-001 belongs to Novi cert 24-0200
            using var client = CreateClientWithApiKey(NoviApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates/search/productid?productId=NOVI-ECS-001&type=SKU");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            data.GetArrayLength().Should().BeGreaterThan(0);
            data[0].GetProperty("partner").GetString().Should().Be("Novi");
        }

        [Fact]
        public async Task Amazon_SearchByNoviSku_ReturnsEmptyDueToScoping()
        {
            // Arrange — SKU NOVI-ECS-001 belongs to Novi cert, Amazon should NOT see it
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates/search/productid?productId=NOVI-ECS-001&type=SKU");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            data.GetArrayLength().Should().Be(0,
                "Amazon should not see Novi's product IDs due to partner scoping");
        }

        [Fact]
        public async Task Novi_SearchByAmazonAsin_ReturnsEmptyDueToScoping()
        {
            // Arrange — ASIN B08XQRT456 belongs to Amazon cert, Novi should NOT see it
            using var client = CreateClientWithApiKey(NoviApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates/search/productid?productId=B08XQRT456&type=ASIN");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            data.GetArrayLength().Should().Be(0,
                "Novi should not see Amazon's ASIN product IDs due to partner scoping");
        }

        [Fact]
        public async Task SearchByProductId_InvalidType_ReturnsBadRequest()
        {
            // Arrange
            using var client = CreateClientWithApiKey(AmazonApiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates/search/productid?productId=test&type=INVALID");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        }

        #endregion

        #region Authentication & Error Handling

        [Fact]
        public async Task NoApiKey_GetCertificates_ReturnsUnauthorized()
        {
            // Arrange — no X-API-Key header
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task InvalidPartnerApiKey_GetCertificates_ReturnsUnauthorized()
        {
            // Arrange — invalid gcc_partner_ key
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Add("X-API-Key", "gcc_partner_invalid_key_000000000000000000000000000");

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Cross-Partner Data Isolation (All Three Partners)

        [Theory]
        [InlineData("Amazon", 2)]
        [InlineData("Novi", 1)]
        [InlineData("Other", 1)]
        public async Task Partner_GetCertificates_ReturnsExpectedCount(string partnerName, int expectedCount)
        {
            // Arrange
            var apiKey = partnerName switch
            {
                "Amazon" => AmazonApiKey,
                "Novi" => NoviApiKey,
                "Other" => OtherApiKey,
                _ => throw new ArgumentException($"Unknown partner: {partnerName}")
            };
            using var client = CreateClientWithApiKey(apiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            data.GetArrayLength().Should().Be(expectedCount,
                $"{partnerName} should see exactly {expectedCount} certificate(s)");
        }

        [Theory]
        [InlineData("Amazon")]
        [InlineData("Novi")]
        [InlineData("Other")]
        public async Task Partner_GetCertificates_AllReturnedCertsMatchPartner(string partnerName)
        {
            // Arrange
            var apiKey = partnerName switch
            {
                "Amazon" => AmazonApiKey,
                "Novi" => NoviApiKey,
                "Other" => OtherApiKey,
                _ => throw new ArgumentException($"Unknown partner: {partnerName}")
            };
            using var client = CreateClientWithApiKey(apiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            foreach (var cert in data.EnumerateArray())
            {
                cert.GetProperty("partner").GetString().Should().Be(partnerName,
                    $"All certificates should have partner={partnerName}");
            }
        }

        [Theory]
        [InlineData("Amazon")]
        [InlineData("Novi")]
        [InlineData("Other")]
        public async Task Partner_GetCertificates_NeverReturnsNonePartner(string partnerName)
        {
            // Arrange — cert ID 400 has Partner="None" and should never appear
            var apiKey = partnerName switch
            {
                "Amazon" => AmazonApiKey,
                "Novi" => NoviApiKey,
                "Other" => OtherApiKey,
                _ => throw new ArgumentException($"Unknown partner: {partnerName}")
            };
            using var client = CreateClientWithApiKey(apiKey);

            // Act
            var response = await client.GetAsync("/api/v1/certificates");

            // Assert
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            foreach (var cert in data.EnumerateArray())
            {
                cert.GetProperty("partner").GetString().Should().NotBe("None",
                    "Partner-scoped API should never return certificates with Partner=None");
            }
        }

        #endregion

        #region Response Shape Comparison

        [Fact]
        public async Task AllPartners_StandardResponse_HaveSameFieldSet()
        {
            // Arrange — get first cert from each partner
            using var amazonClient = CreateClientWithApiKey(AmazonApiKey);
            using var noviClient = CreateClientWithApiKey(NoviApiKey);
            using var otherClient = CreateClientWithApiKey(OtherApiKey);

            // Act
            var amazonResponse = await amazonClient.GetAsync("/api/v1/certificates");
            var noviResponse = await noviClient.GetAsync("/api/v1/certificates");
            var otherResponse = await otherClient.GetAsync("/api/v1/certificates");

            // Assert — all should return 200
            amazonResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            noviResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            otherResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Extract field names from first certificate of each partner
            var amazonFields = await GetCertificateFieldNames(amazonResponse);
            var noviFields = await GetCertificateFieldNames(noviResponse);
            var otherFields = await GetCertificateFieldNames(otherResponse);

            // All partners should receive the same field structure
            amazonFields.Should().BeEquivalentTo(noviFields,
                "Amazon and Novi should have the same response field structure");
            amazonFields.Should().BeEquivalentTo(otherFields,
                "Amazon and Other should have the same response field structure");
        }

        private async Task<List<string>> GetCertificateFieldNames(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            if (data.GetArrayLength() == 0)
                return new List<string>();

            return data[0].EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToList();
        }

        #endregion
    }
}

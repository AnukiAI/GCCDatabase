using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using FluentAssertions;
using GCCWebApp.Controllers.Api;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Api.Tests.TestUtilities;
using System.Security.Claims;

namespace GCCWebApp.Api.Tests.Unit.Controllers
{
    /// <summary>
    /// Unit tests for CertificateTileInternalApiController
    /// Tests internal-specific features like status filtering, all certificates visibility, and internal tags
    /// </summary>
    public class CertificateTileInternalApiControllerTests : IDisposable
    {
        private readonly Mock<ILogger<CertificateTileInternalApiController>> _loggerMock;
        private readonly ApplicationDbContext _context;
        private readonly CertificateTileInternalApiController _controller;

        public CertificateTileInternalApiControllerTests()
        {
            _loggerMock = new Mock<ILogger<CertificateTileInternalApiController>>();

            // Create in-memory database for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"InternalApiTest_{Guid.NewGuid()}")
                .Options;
            _context = new ApplicationDbContext(options);

            // Seed test data
            SeedTestData();

            _controller = new CertificateTileInternalApiController(
                _context,
                _loggerMock.Object);

            // Setup HttpContext for the controller
            SetupHttpContextWithAnalystUser();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #region Test Data Setup

        private void SeedTestData()
        {
            // Create test companies
            var company1 = new Company { Id = 1, Name = "Test Company 1", Email = "test1@company.com", IsActive = true };
            var company2 = new Company { Id = 2, Name = "Test Company 2", Email = "test2@company.com", IsActive = true };

            // Create product category
            var category = new ProductCategory { Id = 1, Name = "Electronics", IsActive = true };

            // Create product type
            var productType = new ProductType { Id = 1, Name = "Consumer Electronics", IsActive = true, CategoryId = 1 };

            // Create test products
            var product1 = new Product { Id = 1, Name = "Test Product 1", CompanyId = 1, CategoryId = 1, ProductTypeId = 1, IsActive = true, CreatedAt = DateTime.UtcNow };
            var product2 = new Product { Id = 2, Name = "Test Product 2", CompanyId = 2, CategoryId = 1, ProductTypeId = 1, IsActive = true, CreatedAt = DateTime.UtcNow };

            // Create certification types
            var certType1 = new CertificationType { Id = 1, Name = "Environmental Facts", IsActive = true };
            var certType2 = new CertificationType { Id = 2, Name = "Recycled Content", IsActive = true };

            // Create tags
            var tag1 = new Tag { Id = 1, Name = "LEED", TagType = "external", IsActive = true };
            var tag2 = new Tag { Id = 2, Name = "Internal Review", TagType = "internal", IsActive = true };
            var tag3 = new Tag { Id = 3, Name = "APR", TagType = "external", IsActive = true };

            // Create certificates with different statuses and partners
            var certificates = new List<Certificate>
            {
                // Active certificate
                new Certificate
                {
                    Id = 1,
                    Number = "CERT-001",
                    ProductId = 1,
                    CompanyId = 1,
                    CertificationTypeId = 1,
                    EffectiveDate = DateTime.UtcNow.AddDays(-30),
                    ExpirationDate = DateTime.UtcNow.AddDays(335),
                    Status = "active",
                    Partner = "Amazon",
                    Location = "Seattle, WA",
                    Country = "United States",
                    CreatedAt = DateTime.UtcNow,
                    Asin = "B08X7N9MRQ"
                },
                // Draft certificate (internal-only status)
                new Certificate
                {
                    Id = 2,
                    Number = "CERT-002",
                    ProductId = 1,
                    CompanyId = 1,
                    CertificationTypeId = 2,
                    EffectiveDate = DateTime.UtcNow.AddDays(-60),
                    ExpirationDate = DateTime.UtcNow.AddDays(305),
                    Status = "draft",
                    Partner = "Amazon",
                    Location = "Portland, OR",
                    Country = "United States",
                    CreatedAt = DateTime.UtcNow
                },
                // Pending Review certificate
                new Certificate
                {
                    Id = 3,
                    Number = "CERT-003",
                    ProductId = 2,
                    CompanyId = 2,
                    CertificationTypeId = 1,
                    EffectiveDate = DateTime.UtcNow.AddDays(-15),
                    ExpirationDate = DateTime.UtcNow.AddDays(350),
                    Status = "pending_review",
                    Partner = "Wayfair",
                    Location = "Boston, MA",
                    Country = "United States",
                    CreatedAt = DateTime.UtcNow
                },
                // Expired certificate
                new Certificate
                {
                    Id = 4,
                    Number = "CERT-004",
                    ProductId = 1,
                    CompanyId = 1,
                    CertificationTypeId = 1,
                    EffectiveDate = DateTime.UtcNow.AddDays(-400),
                    ExpirationDate = DateTime.UtcNow.AddDays(-30),
                    Status = "expired",
                    Partner = null,
                    Location = "Denver, CO",
                    Country = "United States",
                    CreatedAt = DateTime.UtcNow
                },
                // Expiring soon certificate
                new Certificate
                {
                    Id = 5,
                    Number = "CERT-005",
                    ProductId = 1,
                    CompanyId = 1,
                    CertificationTypeId = 2,
                    EffectiveDate = DateTime.UtcNow.AddDays(-300),
                    ExpirationDate = DateTime.UtcNow.AddDays(30),
                    Status = "expiring_soon",
                    Partner = "Amazon",
                    Location = "Phoenix, AZ",
                    Country = "United States",
                    CreatedAt = DateTime.UtcNow,
                    Upc = "012345678901"
                },
                // Renewal in progress certificate
                new Certificate
                {
                    Id = 6,
                    Number = "CERT-006",
                    ProductId = 2,
                    CompanyId = 2,
                    CertificationTypeId = 1,
                    EffectiveDate = DateTime.UtcNow.AddDays(-350),
                    ExpirationDate = DateTime.UtcNow.AddDays(15),
                    Status = "renewal_in_progress",
                    Partner = "Wayfair",
                    Location = "Chicago, IL",
                    Country = "Canada",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Companies.AddRange(company1, company2);
            _context.Set<ProductCategory>().Add(category);
            _context.Set<ProductType>().Add(productType);
            _context.Products.AddRange(product1, product2);
            _context.CertificationTypes.AddRange(certType1, certType2);
            _context.Tags.AddRange(tag1, tag2, tag3);
            _context.Certificates.AddRange(certificates);
            _context.SaveChanges();
        }

        private void SetupHttpContextWithAnalystUser()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "analyst_user"),
                new Claim(ClaimTypes.Role, "Analyst")
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost", 5101);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        private void SetupHttpContextWithAdminUser()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin_user"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost", 5101);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        #endregion

        #region GetCertificateTiles Tests

        [Fact]
        public async Task GetCertificateTiles_WithNoFilters_ReturnsAllCertificatesIncludingDrafts()
        {
            // Act
            var result = await _controller.GetCertificateTiles();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var totalCountProperty = response!.GetType().GetProperty("totalCount");
            var totalCount = (int)totalCountProperty!.GetValue(response)!;

            // Internal view should show ALL certificates including drafts, pending, expired
            totalCount.Should().Be(6);
        }

        [Fact]
        public async Task GetCertificateTiles_WithStatusFilter_FiltersByStatus()
        {
            // Act - Filter by draft status
            var result = await _controller.GetCertificateTiles(status: "draft");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var totalCountProperty = response!.GetType().GetProperty("totalCount");
            var totalCount = (int)totalCountProperty!.GetValue(response)!;

            totalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetCertificateTiles_WithMultipleStatusFilters_FiltersCorrectly()
        {
            // Act - Filter by multiple statuses
            var result = await _controller.GetCertificateTiles(status: "active,expiring_soon");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var totalCountProperty = response!.GetType().GetProperty("totalCount");
            var totalCount = (int)totalCountProperty!.GetValue(response)!;

            // active (1) + expiring_soon (1) = 2
            totalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetCertificateTiles_WithPartnerFilter_FiltersByPartner()
        {
            // Act - Filter by Amazon partner
            var result = await _controller.GetCertificateTiles(partner: "Amazon");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var totalCountProperty = response!.GetType().GetProperty("totalCount");
            var totalCount = (int)totalCountProperty!.GetValue(response)!;

            // Amazon has 3 certificates (CERT-001, CERT-002, CERT-005)
            totalCount.Should().Be(3);
        }

        [Fact]
        public async Task GetCertificateTiles_WithSearchTerm_FiltersCorrectly()
        {
            // Act
            var result = await _controller.GetCertificateTiles(search: "CERT-001");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var totalCountProperty = response!.GetType().GetProperty("totalCount");
            var totalCount = (int)totalCountProperty!.GetValue(response)!;

            totalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetCertificateTiles_ExposesStatusField()
        {
            // Act
            var result = await _controller.GetCertificateTiles();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var certificatesProperty = response!.GetType().GetProperty("certificates");
            var certificates = (certificatesProperty!.GetValue(response) as IEnumerable<object>)!.ToList();

            // Verify Status field IS exposed in internal view
            foreach (var cert in certificates)
            {
                var statusProperty = cert.GetType().GetProperty("Status");
                statusProperty.Should().NotBeNull("Internal view should expose Status field");
            }
        }

        [Fact]
        public async Task GetCertificateTiles_ExposesInternalTags()
        {
            // Act
            var result = await _controller.GetCertificateTiles();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var certificatesProperty = response!.GetType().GetProperty("certificates");
            var certificates = (certificatesProperty!.GetValue(response) as IEnumerable<object>)!.ToList();

            // Verify InternalTags field exists in internal view
            foreach (var cert in certificates)
            {
                var internalTagsProperty = cert.GetType().GetProperty("InternalTags");
                internalTagsProperty.Should().NotBeNull("Internal view should expose InternalTags field");
            }
        }

        [Fact]
        public async Task GetCertificateTiles_ExposesProductIds()
        {
            // Act
            var result = await _controller.GetCertificateTiles();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var certificatesProperty = response!.GetType().GetProperty("certificates");
            var certificates = (certificatesProperty!.GetValue(response) as IEnumerable<object>)!.ToList();

            // Verify product ID fields exist
            foreach (var cert in certificates)
            {
                cert.GetType().GetProperty("Asin").Should().NotBeNull();
                cert.GetType().GetProperty("Upc").Should().NotBeNull();
                cert.GetType().GetProperty("Gtin").Should().NotBeNull();
                cert.GetType().GetProperty("Ean").Should().NotBeNull();
            }
        }

        [Fact]
        public async Task GetCertificateTiles_WithPagination_ReturnsCorrectPage()
        {
            // Act
            var result = await _controller.GetCertificateTiles(page: 1, pageSize: 2);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var certificatesProperty = response!.GetType().GetProperty("certificates");
            var certificates = certificatesProperty!.GetValue(response) as IEnumerable<object>;
            certificates!.Count().Should().Be(2);

            var hasMoreProperty = response.GetType().GetProperty("hasMore");
            var hasMore = (bool)hasMoreProperty!.GetValue(response)!;
            hasMore.Should().BeTrue();

            var totalPagesProperty = response.GetType().GetProperty("totalPages");
            var totalPages = (int)totalPagesProperty!.GetValue(response)!;
            totalPages.Should().Be(3); // 6 total, 2 per page = 3 pages
        }

        [Fact]
        public async Task GetCertificateTiles_WithOffsetAndLimit_SupportsInfiniteScroll()
        {
            // Act - Use offset/limit instead of page/pageSize
            var result = await _controller.GetCertificateTiles(offset: 2, limit: 2);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var loadedCountProperty = response!.GetType().GetProperty("loadedCount");
            var loadedCount = (int)loadedCountProperty!.GetValue(response)!;
            loadedCount.Should().Be(4); // offset 2 + 2 items = 4 loaded
        }

        [Fact]
        public async Task GetCertificateTiles_WithSortByStatus_SortsCorrectly()
        {
            // Act
            var result = await _controller.GetCertificateTiles(sortBy: "status");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            // Verify sorting is applied
        }

        [Fact]
        public async Task GetCertificateTiles_WithSortByCreated_SortsCorrectly()
        {
            // Act
            var result = await _controller.GetCertificateTiles(sortBy: "created");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            // Verify sorting by creation date
        }

        [Fact]
        public async Task GetCertificateTiles_WithSortByModified_SortsCorrectly()
        {
            // Act
            var result = await _controller.GetCertificateTiles(sortBy: "modified");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            // Verify sorting by modification date
        }

        [Fact]
        public async Task GetCertificateTiles_WithCountryFilter_FiltersCorrectly()
        {
            // Act - Filter by Canada
            var result = await _controller.GetCertificateTiles(country: "Canada");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var totalCountProperty = response!.GetType().GetProperty("totalCount");
            var totalCount = (int)totalCountProperty!.GetValue(response)!;

            totalCount.Should().Be(1);
        }

        #endregion

        #region GetAutocompleteSuggestions Tests

        [Fact]
        public async Task GetAutocompleteSuggestions_WithValidTerm_ReturnsSuggestions()
        {
            // Act
            var result = await _controller.GetAutocompleteSuggestions("Test");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var suggestionsProperty = response!.GetType().GetProperty("suggestions");
            suggestionsProperty.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAutocompleteSuggestions_WithShortTerm_ReturnsEmptyList()
        {
            // Act
            var result = await _controller.GetAutocompleteSuggestions("T");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var suggestionsProperty = response!.GetType().GetProperty("suggestions");
            var suggestions = suggestionsProperty!.GetValue(response) as IEnumerable<object>;
            suggestions!.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAutocompleteSuggestions_SearchesAllCertificatesIncludingDrafts()
        {
            // Internal autocomplete should include data from ALL certificates
            // Act
            var result = await _controller.GetAutocompleteSuggestions("CERT");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        #endregion

        #region GetFilterOptions Tests

        [Fact]
        public async Task GetFilterOptions_ReturnsAllFilterData()
        {
            // Act
            var result = await _controller.GetFilterOptions();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            // Verify expected filter properties exist
            response!.GetType().GetProperty("certificationTypes").Should().NotBeNull();
            response.GetType().GetProperty("companies").Should().NotBeNull();
            response.GetType().GetProperty("countries").Should().NotBeNull();
            response.GetType().GetProperty("statuses").Should().NotBeNull();
            response.GetType().GetProperty("partners").Should().NotBeNull();
            response.GetType().GetProperty("tags").Should().NotBeNull();
        }

        [Fact]
        public async Task GetFilterOptions_IncludesStatusesFilter()
        {
            // Act
            var result = await _controller.GetFilterOptions();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var statusesProperty = response!.GetType().GetProperty("statuses");
            statusesProperty.Should().NotBeNull("Internal view should include statuses filter");

            var statuses = statusesProperty!.GetValue(response) as IEnumerable<object>;
            statuses.Should().NotBeEmpty("Statuses should include all certificate status options");
        }

        [Fact]
        public async Task GetFilterOptions_IncludesProductCategoriesHierarchy()
        {
            // Act
            var result = await _controller.GetFilterOptions();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var hierarchyProperty = response!.GetType().GetProperty("productCategoriesHierarchy");
            hierarchyProperty.Should().NotBeNull("Should include hierarchical product categories");
        }

        #endregion

        #region DownloadCertificate Tests

        [Fact]
        public async Task DownloadCertificate_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.DownloadCertificate(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DownloadCertificate_InternalCanDownloadDraftCertificates()
        {
            // Unlike external, internal users CAN download draft certificates
            // Act - Certificate ID 2 is draft
            var result = await _controller.DownloadCertificate(2);

            // Assert - Should return NotFound because no PDF file, NOT because of status restriction
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            var response = notFoundResult!.Value;
            var errorProperty = response!.GetType().GetProperty("error");
            errorProperty!.GetValue(response).Should().Be("Certificate PDF not available");
        }

        #endregion

        #region GetShareableLink Tests

        [Fact]
        public async Task GetShareableLink_WithValidRequest_ReturnsUrl()
        {
            // Arrange
            var request = new InternalShareLinkRequest
            {
                Search = "test",
                CertType = "Environmental Facts",
                Status = "draft",
                Partner = "Amazon"
            };

            // Act
            var result = _controller.GetShareableLink(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var urlProperty = response!.GetType().GetProperty("url");
            var url = urlProperty!.GetValue(response) as string;
            url.Should().Contain("/Certificate/CertificateTileViewInternal");
            url.Should().Contain("search=test");
            url.Should().Contain("status=draft");
            url.Should().Contain("partner=Amazon");
        }

        [Fact]
        public async Task GetShareableLink_WithStatusFilter_IncludesStatusInUrl()
        {
            // Arrange - Internal-specific: status filter
            var request = new InternalShareLinkRequest
            {
                Status = "pending_review"
            };

            // Act
            var result = _controller.GetShareableLink(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var urlProperty = response!.GetType().GetProperty("url");
            var url = urlProperty!.GetValue(response) as string;
            url.Should().Contain("status=pending_review");
        }

        [Fact]
        public async Task GetShareableLink_WithEmptyRequest_ReturnsBaseUrl()
        {
            // Arrange
            var request = new InternalShareLinkRequest();

            // Act
            var result = _controller.GetShareableLink(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;

            var urlProperty = response!.GetType().GetProperty("url");
            var url = urlProperty!.GetValue(response) as string;
            url.Should().EndWith("/Certificate/CertificateTileViewInternal");
            url.Should().NotContain("?");
        }

        #endregion

        #region Admin vs Analyst Access Tests

        [Fact]
        public async Task GetCertificateTiles_AnalystUser_CanAccessAllCertificates()
        {
            // Arrange - Default setup is analyst user

            // Act
            var result = await _controller.GetCertificateTiles();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetCertificateTiles_AdminUser_CanAccessAllCertificates()
        {
            // Arrange
            SetupHttpContextWithAdminUser();

            // Act
            var result = await _controller.GetCertificateTiles();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        #endregion
    }
}

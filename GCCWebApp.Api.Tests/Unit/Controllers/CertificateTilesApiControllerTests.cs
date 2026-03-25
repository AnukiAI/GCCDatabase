using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GCCWebApp.Controllers.Api;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Repositories;
using GCCWebApp.Services;
using GCCWebApp.Api.Tests.TestUtilities;

namespace GCCWebApp.Api.Tests.Unit.Controllers
{
    public class CertificateTilesApiControllerTests : IDisposable
    {
        private readonly Mock<ILogger<CertificateTilesApiController>> _loggerMock;
        private readonly Mock<ICertificateRepository> _certificateRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ICertificatePdfService> _pdfServiceMock;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly CertificateTilesApiController _controller;

        public CertificateTilesApiControllerTests()
        {
            _loggerMock = new Mock<ILogger<CertificateTilesApiController>>();
            _certificateRepositoryMock = new Mock<ICertificateRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _pdfServiceMock = new Mock<ICertificatePdfService>();

            // Build real configuration instead of mocking extension methods
            var configValues = new Dictionary<string, string?>
            {
                { "FeatureFlags:ShowDetailedApiErrors", "false" }
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            // Create in-memory database for testing
            _context = TestDbContextFactory.CreateInMemoryContext();

            _controller = new CertificateTilesApiController(
                _loggerMock.Object,
                _certificateRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _pdfServiceMock.Object,
                _context,
                _configuration);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task GetCertificates_WithValidPagination_ReturnsSuccessfulInfiniteScrollResponse()
        {
            // Arrange
            var certificates = CreateTestCertificates(20);
            var totalCount = 100;

            _certificateRepositoryMock
                .Setup(r => r.GetAdvancedFilteredCertificatesAsync(It.IsAny<CertificateSearchFilter>()))
                .ReturnsAsync((certificates, totalCount));

            // Act
            var result = await _controller.GetCertificates(
                page: 1,
                pageSize: 20);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;

            var response = okResult!.Value;
            response.Should().NotBeNull();

            // Verify the response has the infinite scroll structure
            var successProperty = response!.GetType().GetProperty("success");
            successProperty.Should().NotBeNull();
            successProperty!.GetValue(response).Should().Be(true);

            var certificatesProperty = response.GetType().GetProperty("certificates");
            certificatesProperty.Should().NotBeNull();

            var totalCountProperty = response.GetType().GetProperty("totalCount");
            totalCountProperty.Should().NotBeNull();
            totalCountProperty!.GetValue(response).Should().Be(totalCount);

            var hasMoreProperty = response.GetType().GetProperty("hasMore");
            hasMoreProperty.Should().NotBeNull();
            hasMoreProperty!.GetValue(response).Should().Be(true); // Should have more since 20 < 100
        }

        [Fact]
        public async Task GetCertificates_WithLastPage_ReturnsHasMoreFalse()
        {
            // Arrange
            var certificates = CreateTestCertificates(10); // Less than page size
            var totalCount = 90; // 5th page of 20-item pages

            _certificateRepositoryMock
                .Setup(r => r.GetAdvancedFilteredCertificatesAsync(It.IsAny<CertificateSearchFilter>()))
                .ReturnsAsync((certificates, totalCount));

            // Act
            var result = await _controller.GetCertificates(
                page: 5,
                pageSize: 20);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;

            var response = okResult!.Value;
            var hasMoreProperty = response!.GetType().GetProperty("hasMore");
            hasMoreProperty!.GetValue(response).Should().Be(false);
        }

        [Fact]
        public async Task GetCertificates_WithSearchTerm_FiltersCorrectly()
        {
            // Arrange
            var certificates = CreateTestCertificates(5);
            var totalCount = 5;

            _certificateRepositoryMock
                .Setup(r => r.GetAdvancedFilteredCertificatesAsync(It.Is<CertificateSearchFilter>(
                    f => f.SearchTerm == "test search")))
                .ReturnsAsync((certificates, totalCount));

            // Act
            var result = await _controller.GetCertificates(
                searchTerm: "test search",
                page: 1,
                pageSize: 20);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _certificateRepositoryMock.Verify(r =>
                r.GetAdvancedFilteredCertificatesAsync(It.Is<CertificateSearchFilter>(
                    f => f.SearchTerm == "test search")), Times.Once);
        }

        [Fact]
        public async Task GetCertificates_WithFilters_AppliesFiltersCorrectly()
        {
            // Arrange
            var certificates = CreateTestCertificates(10);
            var totalCount = 10;

            _certificateRepositoryMock
                .Setup(r => r.GetAdvancedFilteredCertificatesAsync(It.IsAny<CertificateSearchFilter>()))
                .ReturnsAsync((certificates, totalCount));

            // Act
            var result = await _controller.GetCertificates(
                certType: "Environmental Facts",
                country: "US",
                company: "1,2,3",
                page: 1,
                pageSize: 20);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _certificateRepositoryMock.Verify(r =>
                r.GetAdvancedFilteredCertificatesAsync(It.Is<CertificateSearchFilter>(f =>
                    f.CertificationTypeNames != null && f.CertificationTypeNames.Contains("Environmental Facts") &&
                    f.Countries != null && f.Countries.Contains("US") &&
                    f.CompanyIds != null && f.CompanyIds.Contains(1))), Times.Once);
        }

        [Fact]
        public async Task GetCertificates_WithSortParameter_AppliesSortCorrectly()
        {
            // Arrange
            var certificates = CreateTestCertificates(10);
            var totalCount = 10;

            _certificateRepositoryMock
                .Setup(r => r.GetAdvancedFilteredCertificatesAsync(It.IsAny<CertificateSearchFilter>()))
                .ReturnsAsync((certificates, totalCount));

            // Act
            var result = await _controller.GetCertificates(
                sort: "company-az",
                page: 1,
                pageSize: 20);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _certificateRepositoryMock.Verify(r =>
                r.GetAdvancedFilteredCertificatesAsync(It.Is<CertificateSearchFilter>(f =>
                    f.SortBy == "Company" && f.SortDescending == false)), Times.Once);
        }

        [Fact]
        public async Task GetCertificates_WithError_ReturnsErrorResponse()
        {
            // Arrange
            _certificateRepositoryMock
                .Setup(r => r.GetAdvancedFilteredCertificatesAsync(It.IsAny<CertificateSearchFilter>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetCertificates(page: 1, pageSize: 20);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;

            var response = badRequestResult!.Value;
            var successProperty = response!.GetType().GetProperty("success");
            successProperty!.GetValue(response).Should().Be(false);

            var messageProperty = response.GetType().GetProperty("message");
            messageProperty!.GetValue(response).Should().Be("Failed to retrieve certificates");
        }

        [Fact]
        public async Task GetFilterOptions_ReturnsFilterData()
        {
            // Arrange
            var certTypes = new List<CertificationType>
            {
                new CertificationType { Name = "Environmental Facts", IsActive = true },
                new CertificationType { Name = "Recycled Content", IsActive = true }
            };
            var companies = new List<Company>
            {
                new Company { Id = 1, Name = "Test Company 1", IsActive = true },
                new Company { Id = 2, Name = "Test Company 2", IsActive = true }
            };
            var countries = new List<string> { "US", "CA", "MX" };

            var mockCertTypesRepo = new Mock<IRepository<CertificationType>>();
            mockCertTypesRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(certTypes);

            var mockCompaniesRepo = new Mock<IRepository<Company>>();
            mockCompaniesRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(companies);

            _unitOfWorkMock.Setup(u => u.CertificationTypes).Returns(mockCertTypesRepo.Object);
            _unitOfWorkMock.Setup(u => u.Companies).Returns(mockCompaniesRepo.Object);

            _certificateRepositoryMock
                .Setup(r => r.GetDistinctCountriesAsync())
                .ReturnsAsync(countries);

            // Act
            var result = await _controller.GetFilterOptions();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAutocompleteSuggestions_WithValidTerm_ReturnsSuggestions()
        {
            // Arrange
            var companies = new List<Company>
            {
                new Company { Name = "Test Company", IsActive = true }
            };
            var products = new List<Product>
            {
                new Product { Name = "Test Product", IsActive = true }
            };

            var mockCompaniesRepo = new Mock<IRepository<Company>>();
            mockCompaniesRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Company, bool>>>()))
                .ReturnsAsync(companies);

            var mockProductsRepo = new Mock<IRepository<Product>>();
            mockProductsRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Product, bool>>>()))
                .ReturnsAsync(products);

            _unitOfWorkMock.Setup(u => u.Companies).Returns(mockCompaniesRepo.Object);
            _unitOfWorkMock.Setup(u => u.Products).Returns(mockProductsRepo.Object);

            _certificateRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Certificate, bool>>>()))
                .ReturnsAsync(new List<Certificate>());

            var mockCertTypesRepo = new Mock<IRepository<CertificationType>>();
            mockCertTypesRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<CertificationType, bool>>>()))
                .ReturnsAsync(new List<CertificationType>());
            _unitOfWorkMock.Setup(u => u.CertificationTypes).Returns(mockCertTypesRepo.Object);

            // Act
            var result = await _controller.GetAutocompleteSuggestions("test");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("t")]
        public async Task GetAutocompleteSuggestions_WithInvalidTerm_ReturnsEmptySuggestions(string term)
        {
            // Act
            var result = await _controller.GetAutocompleteSuggestions(term);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;

            var response = okResult!.Value;
            var suggestionsProperty = response!.GetType().GetProperty("suggestions");
            var suggestions = suggestionsProperty!.GetValue(response) as System.Collections.IEnumerable;
            suggestions.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAutocompleteSuggestions_WithNullTerm_ReturnsEmptySuggestions()
        {
            // Act
            var result = await _controller.GetAutocompleteSuggestions(null!);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;

            var response = okResult!.Value;
            var suggestionsProperty = response!.GetType().GetProperty("suggestions");
            var suggestions = suggestionsProperty!.GetValue(response) as System.Collections.IEnumerable;
            suggestions.Should().NotBeNull();
        }

        private static IEnumerable<Certificate> CreateTestCertificates(int count)
        {
            var certificates = new List<Certificate>();
            for (int i = 1; i <= count; i++)
            {
                certificates.Add(new Certificate
                {
                    Id = i,
                    Number = $"CERT-TEST-{i:D3}",
                    CompanyId = 1,
                    ProductId = 1,
                    CertificationTypeId = 1,
                    EffectiveDate = DateTime.UtcNow.AddDays(-30),
                    ExpirationDate = DateTime.UtcNow.AddDays(330),
                    Status = "Client Approved - Paid",
                    Location = "Test Location",
                    Country = "US",
                    Company = new Company { Id = 1, Name = $"Test Company {i}" },
                    Product = new Product { Id = 1, Name = $"Test Product {i}" },
                    CertificationType = new CertificationType { Id = 1, Name = "Environmental Facts" }
                });
            }
            return certificates;
        }
    }
}
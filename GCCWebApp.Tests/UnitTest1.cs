using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using GCCWebApp.Services;
using GCCWebApp.Models;
using GCCWebApp.Data;

namespace GCCWebApp.Tests
{
    public class CertificateServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<CertificateService>> _mockLogger;
        private readonly CertificateService _service;

        public CertificateServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mockLogger = new Mock<ILogger<CertificateService>>();
            _service = new CertificateService(_mockLogger.Object, _context);

            // Seed test data
            SeedTestData();
        }

        [Fact]
        public async Task GetCertificatesAsync_WithSearchTerm_ShouldFilterResults()
        {
            // Arrange
            var request = new CertificateListRequest
            {
                Filters = new CertificateSearchFilters { SearchTerm = "Test Company" },
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCountGreaterThan(0);
            result.Certificates.Should().OnlyContain(c => c.CompanyName.Contains("Test Company"));
        }

        [Fact]
        public async Task GetCertificatesAsync_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var request = new CertificateListRequest
            {
                Page = 1,
                PageSize = 2,
                Filters = new CertificateSearchFilters()
            };

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCount(2);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(2);
        }

        [Theory]
        [InlineData("newest")]
        [InlineData("expiring")]
        [InlineData("company-az")]
        [InlineData("product-az")]
        public async Task GetCertificatesAsync_WithDifferentSorts_ShouldApplyCorrectSorting(string sortType)
        {
            // Arrange
            var request = new CertificateListRequest
            {
                Sort = sortType,
                Page = 1,
                PageSize = 10,
                Filters = new CertificateSearchFilters()
            };

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CurrentSort.Should().Be(sortType);
        }

        [Fact]
        public async Task GetCertificateDetailsAsync_WithValidId_ShouldReturnDetails()
        {
            // Arrange
            var certificate = _context.Certificates.First();

            // Act
            var result = await _service.GetCertificateDetailsAsync(certificate.Id, "Public");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(certificate.Id);
            result.CertificateNumber.Should().Be(certificate.Number);
        }

        [Fact]
        public async Task GetCertificateDetailsAsync_WithInvalidId_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetCertificateDetailsAsync(999, "Public");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task ValidateCertificateAsync_WithValidCertificateNumber_ShouldReturnValid()
        {
            // Arrange
            var certificate = _context.Certificates.First();

            // Act
            var result = await _service.ValidateCertificateAsync(certificate.Number);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateCertificateAsync_WithInvalidCertificateNumber_ShouldReturnInvalid()
        {
            // Act
            var result = await _service.ValidateCertificateAsync("INVALID123");

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Message.Should().Be("Certificate not found");
        }

        [Fact]
        public async Task GetCertificateStatisticsAsync_ShouldReturnStatistics()
        {
            // Act
            var result = await _service.GetCertificateStatisticsAsync("Admin");

            // Assert
            result.Should().NotBeNull();
            result.TotalCertificates.Should().BeGreaterThan(0);
            result.ActiveCertificates.Should().BeGreaterThanOrEqualTo(0);
            result.ExpiredCertificates.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task CreateCertificateAsync_WithValidData_ShouldCreateCertificate()
        {
            // Arrange
            var company = _context.Companies.First();
            var product = _context.Products.First();
            var certificationType = _context.CertificationTypes.First();

            var certificateModel = new CertificateEditViewModel
            {
                Number = "NEW001",
                Status = "active",
                EffectiveDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddYears(1),
                CompanyId = company.Id,
                ProductId = product.Id,
                CertificationTypeId = certificationType.Id
            };

            // Act
            var result = await _service.CreateCertificateAsync(certificateModel, "Admin");

            // Assert
            result.Should().BeGreaterThan(0);
            
            var createdCertificate = await _context.Certificates.FindAsync(result);
            createdCertificate.Should().NotBeNull();
            createdCertificate!.Number.Should().Be("NEW001");
        }

        [Fact]
        public async Task UpdateCertificateAsync_WithValidData_ShouldUpdateCertificate()
        {
            // Arrange
            var certificate = _context.Certificates.First();
            var updateModel = new CertificateEditViewModel
            {
                Number = certificate.Number,
                Status = "expired",
                EffectiveDate = certificate.EffectiveDate,
                ExpirationDate = certificate.ExpirationDate,
                CompanyId = certificate.CompanyId,
                ProductId = certificate.ProductId,
                CertificationTypeId = certificate.CertificationTypeId
            };

            // Act
            var result = await _service.UpdateCertificateAsync(certificate.Id, updateModel, "Admin");

            // Assert
            result.Should().BeTrue();
            
            var updatedCertificate = await _context.Certificates.FindAsync(certificate.Id);
            updatedCertificate!.Status.Should().Be("expired");
        }

        [Fact]
        public async Task DeleteCertificateAsync_WithValidId_ShouldDeleteCertificate()
        {
            // Arrange
            var certificate = _context.Certificates.First();
            var certificateId = certificate.Id;

            // Act
            var result = await _service.DeleteCertificateAsync(certificateId, "Admin");

            // Assert
            result.Should().BeTrue();
            
            var deletedCertificate = await _context.Certificates.FindAsync(certificateId);
            deletedCertificate.Should().BeNull();
        }

        [Fact]
        public async Task GetSearchSuggestionsAsync_WithCompanyType_ShouldReturnCompanySuggestions()
        {
            // Act
            var result = await _service.GetSearchSuggestionsAsync("Test", "company", "Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(s => s.Contains("Test"));
        }

        private void SeedTestData()
        {
            var category = new ProductCategory { Name = "Test Category" };
            _context.ProductCategories.Add(category);

            var companies = new[]
            {
                new Company { Name = "Test Company 1", ContactInfo = "test1@company.com", Location = "Location 1" },
                new Company { Name = "Test Company 2", ContactInfo = "test2@company.com", Location = "Location 2" },
                new Company { Name = "Another Company", ContactInfo = "other@company.com", Location = "Location 3" }
            };
            _context.Companies.AddRange(companies);

            var products = new[]
            {
                new Product { Name = "Test Product 1", CategoryId = 1 },
                new Product { Name = "Test Product 2", CategoryId = 1 },
                new Product { Name = "Another Product", CategoryId = 1 }
            };
            _context.Products.AddRange(products);

            var certificationType = new CertificationType { Name = "Test Certificate Type" };
            _context.CertificationTypes.Add(certificationType);

            _context.SaveChanges();

            var certificates = new[]
            {
                new Certificate
                {
                    Number = "GCC001",
                    Status = "active",
                    EffectiveDate = DateTime.Now.AddDays(-30),
                    ExpirationDate = DateTime.Now.AddDays(330),
                    CompanyId = companies[0].Id,
                    ProductId = products[0].Id,
                    CertificationTypeId = certificationType.Id,
                    Country = "USA"
                },
                new Certificate
                {
                    Number = "GCC002",
                    Status = "active",
                    EffectiveDate = DateTime.Now.AddDays(-60),
                    ExpirationDate = DateTime.Now.AddDays(300),
                    CompanyId = companies[1].Id,
                    ProductId = products[1].Id,
                    CertificationTypeId = certificationType.Id,
                    Country = "Canada"
                },
                new Certificate
                {
                    Number = "GCC003",
                    Status = "expired",
                    EffectiveDate = DateTime.Now.AddDays(-400),
                    ExpirationDate = DateTime.Now.AddDays(-30),
                    CompanyId = companies[2].Id,
                    ProductId = products[2].Id,
                    CertificationTypeId = certificationType.Id,
                    Country = "Mexico"
                }
            };
            _context.Certificates.AddRange(certificates);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

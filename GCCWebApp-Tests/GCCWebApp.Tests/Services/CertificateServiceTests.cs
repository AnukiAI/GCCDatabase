using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using GCCWebApp.Services;
using GCCWebApp.Models;
using GCCWebApp.Data;
using GreenCircleCertified.ViewModels;

namespace GCCWebApp.Tests.Services
{
    public class CertificateServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<CertificateService>> _loggerMock;
        private readonly CertificateService _service;

        public CertificateServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<CertificateService>>();
            _service = new CertificateService(_loggerMock.Object, _context);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var companies = new List<Company>
            {
                new Company { Id = 1, Name = "Test Company 1", IsActive = true },
                new Company { Id = 2, Name = "Test Company 2", IsActive = true }
            };

            var categories = new List<ProductCategory>
            {
                new ProductCategory { Id = 1, Name = "Category 1", IsActive = true },
                new ProductCategory { Id = 2, Name = "Category 2", IsActive = true }
            };

            var products = new List<Product>
            {
                new Product { Id = 1, Name = "Product 1", CategoryId = 1, Category = categories[0], IsActive = true },
                new Product { Id = 2, Name = "Product 2", CategoryId = 2, Category = categories[1], IsActive = true }
            };

            var certificationTypes = new List<CertificationType>
            {
                new CertificationType { Id = 1, Name = "Recycled Content", IsActive = true },
                new CertificationType { Id = 2, Name = "Carbon Footprint", IsActive = true }
            };

            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    Number = "CERT-001",
                    CompanyId = 1,
                    Company = companies[0],
                    ProductId = 1,
                    Product = products[0],
                    CertificationTypeId = 1,
                    CertificationType = certificationTypes[0],
                    Status = "active",
                    EffectiveDate = DateTime.Now.AddDays(-30),
                    ExpirationDate = DateTime.Now.AddDays(60),
                    Location = "USA",
                    Country = "USA"
                },
                new Certificate
                {
                    Id = 2,
                    Number = "CERT-002",
                    CompanyId = 2,
                    Company = companies[1],
                    ProductId = 2,
                    Product = products[1],
                    CertificationTypeId = 2,
                    CertificationType = certificationTypes[1],
                    Status = "expired",
                    EffectiveDate = DateTime.Now.AddDays(-365),
                    ExpirationDate = DateTime.Now.AddDays(-30),
                    Location = "Canada",
                    Country = "Canada"
                },
                new Certificate
                {
                    Id = 3,
                    Number = "CERT-003",
                    CompanyId = 1,
                    Company = companies[0],
                    ProductId = 1,
                    Product = products[0],
                    CertificationTypeId = 1,
                    CertificationType = certificationTypes[0],
                    Status = "active",
                    EffectiveDate = DateTime.Now.AddDays(-15),
                    ExpirationDate = DateTime.Now.AddDays(25), // Expiring soon
                    Location = "Mexico",
                    Country = "Mexico"
                }
            };

            _context.Companies.AddRange(companies);
            _context.ProductCategories.AddRange(categories);
            _context.Products.AddRange(products);
            _context.CertificationTypes.AddRange(certificationTypes);
            _context.Certificates.AddRange(certificates);
            _context.SaveChanges();
        }

        #region GetCertificatesAsync Tests

        [Fact]
        public async Task GetCertificatesAsync_WithNoFilters_ShouldReturnAllCertificates()
        {
            // Arrange
            var request = new CertificateListRequest
            {
                Page = 1,
                PageSize = 10,
                UserRole = "Admin",
                Filters = new CertificateSearchFilters()
            };

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCount(3);
            result.TotalCount.Should().Be(3);
        }

        [Fact]
        public async Task GetCertificatesAsync_WithSearchTerm_ShouldFilterResults()
        {
            // Arrange
            var request = new CertificateListRequest
            {
                Page = 1,
                PageSize = 10,
                UserRole = "Admin",
                Filters = new CertificateSearchFilters
                {
                    SearchTerm = "Company 2"
                }
            };

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Certificates.Should().HaveCount(1);
            result.Certificates.First().CompanyName.Should().Be("Test Company 2");
        }

        [Fact]
        public async Task GetCertificatesAsync_WithStatusFilter_ShouldReturnFilteredCertificates()
        {
            // Arrange
            var request = new CertificateListRequest
            {
                Page = 1,
                PageSize = 10,
                UserRole = "Admin",
                Filters = new CertificateSearchFilters
                {
                    Statuses = new List<string> { "active" }
                }
            };

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Certificates.Should().HaveCount(2);
            result.Certificates.Should().OnlyContain(c => c.Status == "active");
        }

        [Fact]
        public async Task GetCertificatesAsync_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var request = new CertificateListRequest
            {
                Page = 2,
                PageSize = 2,
                UserRole = "Admin",
                Filters = new CertificateSearchFilters()
            };

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Certificates.Should().HaveCount(1);
            result.CurrentPage.Should().Be(2);
            result.PageSize.Should().Be(2);
            result.TotalCount.Should().Be(3);
        }

        [Fact]
        public async Task GetCertificatesAsync_WithSortingByNewest_ShouldOrderByEffectiveDateDesc()
        {
            // Arrange
            var request = new CertificateListRequest
            {
                Page = 1,
                PageSize = 10,
                Sort = "newest",
                UserRole = "Admin",
                Filters = new CertificateSearchFilters()
            };

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Certificates.First().CertificateNumber.Should().Be("CERT-003");
            result.Certificates.Last().CertificateNumber.Should().Be("CERT-002");
        }

        [Fact]
        public async Task GetCertificatesAsync_WithDateFilter_ShouldReturnFilteredResults()
        {
            // Arrange
            var request = new CertificateListRequest
            {
                Page = 1,
                PageSize = 10,
                UserRole = "Admin",
                Filters = new CertificateSearchFilters
                {
                    EffectiveDateStart = DateTime.Now.AddDays(-20),
                    EffectiveDateEnd = DateTime.Now
                }
            };

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Certificates.Should().HaveCount(1);
            result.Certificates.First().CertificateNumber.Should().Be("CERT-003");
        }

        #endregion

        #region GetCertificateDetailsAsync Tests

        [Fact]
        public async Task GetCertificateDetailsAsync_WithValidId_ShouldReturnCertificate()
        {
            // Arrange
            var certificateId = 1;
            var userRole = "Admin";

            // Act
            var result = await _service.GetCertificateDetailsAsync(certificateId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.CertificateNumber.Should().Be("CERT-001");
            result.CompanyName.Should().Be("Test Company 1");
        }

        [Fact]
        public async Task GetCertificateDetailsAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var certificateId = 999;
            var userRole = "Admin";

            // Act
            var result = await _service.GetCertificateDetailsAsync(certificateId, userRole);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetCertificateStatisticsAsync Tests

        [Fact]
        public async Task GetCertificateStatisticsAsync_ShouldReturnCorrectStatistics()
        {
            // Arrange
            var userRole = "Admin";

            // Act
            var result = await _service.GetCertificateStatisticsAsync(userRole);

            // Assert
            result.Should().NotBeNull();
            result.TotalCertificates.Should().Be(3);
            result.ActiveCertificates.Should().Be(2);
            result.ExpiredCertificates.Should().Be(1);
            result.ExpiringSoonCertificates.Should().Be(1); // CERT-003 expires in 25 days
            result.CertificatesByType.Should().ContainKey("Recycled Content");
            result.CertificatesByType["Recycled Content"].Should().Be(2);
        }

        #endregion

        #region ValidateCertificateAsync Tests

        [Fact]
        public async Task ValidateCertificateAsync_WithValidCertificate_ShouldReturnValid()
        {
            // Arrange
            var certificateNumber = "CERT-001";

            // Act
            var result = await _service.ValidateCertificateAsync(certificateNumber);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.Status.Should().Be("active");
            result.IsExpired.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateCertificateAsync_WithExpiredCertificate_ShouldReturnExpired()
        {
            // Arrange
            var certificateNumber = "CERT-002";

            // Act
            var result = await _service.ValidateCertificateAsync(certificateNumber);

            // Assert
            result.IsValid.Should().BeTrue();
            result.IsExpired.Should().BeTrue();
            result.Message.Should().Be("Certificate has expired");
        }

        [Fact]
        public async Task ValidateCertificateAsync_WithExpiringSoonCertificate_ShouldReturnExpiringSoon()
        {
            // Arrange
            var certificateNumber = "CERT-003";

            // Act
            var result = await _service.ValidateCertificateAsync(certificateNumber);

            // Assert
            result.IsValid.Should().BeTrue();
            result.IsExpiringSoon.Should().BeTrue();
            result.Message.Should().Be("Certificate is expiring soon");
        }

        [Fact]
        public async Task ValidateCertificateAsync_WithInvalidCertificate_ShouldReturnInvalid()
        {
            // Arrange
            var certificateNumber = "INVALID-001";

            // Act
            var result = await _service.ValidateCertificateAsync(certificateNumber);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Message.Should().Be("Certificate not found");
        }

        #endregion

        #region CreateCertificateAsync Tests

        [Fact]
        public async Task CreateCertificateAsync_WithValidData_ShouldCreateCertificate()
        {
            // Arrange
            var certificate = new CertificateEditViewModel
            {
                Number = "CERT-004",
                Status = "active",
                EffectiveDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddDays(365),
                Location = "Brazil",
                Country = "Brazil",
                CompanyId = 1,
                ProductId = 1,
                CertificationTypeId = 1
            };

            // Act
            var result = await _service.CreateCertificateAsync(certificate, "Admin");

            // Assert
            result.Should().BeGreaterThan(0);
            var created = await _context.Certificates.FindAsync(result);
            created.Should().NotBeNull();
            created.Number.Should().Be("CERT-004");
        }

        #endregion

        #region UpdateCertificateAsync Tests

        [Fact]
        public async Task UpdateCertificateAsync_WithValidData_ShouldUpdateCertificate()
        {
            // Arrange
            var certificate = new CertificateEditViewModel
            {
                Number = "CERT-001-UPDATED",
                Status = "pending",
                EffectiveDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddDays(180),
                Location = "Updated Location",
                Country = "USA",
                CompanyId = 1,
                ProductId = 1,
                CertificationTypeId = 1
            };

            // Act
            var result = await _service.UpdateCertificateAsync(1, certificate, "Admin");

            // Assert
            result.Should().BeTrue();
            var updated = await _context.Certificates.FindAsync(1);
            updated.Number.Should().Be("CERT-001-UPDATED");
            updated.Status.Should().Be("pending");
        }

        [Fact]
        public async Task UpdateCertificateAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var certificate = new CertificateEditViewModel
            {
                Number = "CERT-999",
                Status = "active",
                CompanyId = 1,
                ProductId = 1,
                CertificationTypeId = 1
            };

            // Act
            var result = await _service.UpdateCertificateAsync(999, certificate, "Admin");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region DeleteCertificateAsync Tests

        [Fact]
        public async Task DeleteCertificateAsync_WithValidId_ShouldDeleteCertificate()
        {
            // Act
            var result = await _service.DeleteCertificateAsync(1, "Admin");

            // Assert
            result.Should().BeTrue();
            var deleted = await _context.Certificates.FindAsync(1);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteCertificateAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Act
            var result = await _service.DeleteCertificateAsync(999, "Admin");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region BulkActionAsync Tests

        [Fact]
        public async Task PerformBulkActionAsync_Delete_ShouldDeleteMultipleCertificates()
        {
            // Arrange
            var request = new BulkActionRequest
            {
                Action = "delete",
                CertificateIds = new List<int> { 1, 2 },
                UserRole = "Admin"
            };

            // Act
            var result = await _service.PerformBulkActionAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.AffectedCount.Should().Be(2);
            var remainingCertificates = await _context.Certificates.CountAsync();
            remainingCertificates.Should().Be(1);
        }

        [Fact]
        public async Task PerformBulkActionAsync_UpdateStatus_ShouldUpdateMultipleCertificates()
        {
            // Arrange
            var request = new BulkActionRequest
            {
                Action = "update_status",
                CertificateIds = new List<int> { 1, 3 },
                Parameters = new Dictionary<string, string> { { "status", "pending" } },
                UserRole = "Admin"
            };

            // Act
            var result = await _service.PerformBulkActionAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.AffectedCount.Should().Be(2);
            
            var updated = await _context.Certificates.Where(c => c.Id == 1 || c.Id == 3).ToListAsync();
            updated.Should().OnlyContain(c => c.Status == "pending");
        }

        [Fact]
        public async Task PerformBulkActionAsync_InvalidAction_ShouldReturnError()
        {
            // Arrange
            var request = new BulkActionRequest
            {
                Action = "invalid_action",
                CertificateIds = new List<int> { 1 },
                UserRole = "Admin"
            };

            // Act
            var result = await _service.PerformBulkActionAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Unknown bulk action");
        }

        #endregion

        #region GetSearchSuggestionsAsync Tests

        [Fact]
        public async Task GetSearchSuggestionsAsync_ForCompany_ShouldReturnCompanySuggestions()
        {
            // Act
            var result = await _service.GetSearchSuggestionsAsync("Test", "company", "Admin");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().Contain("Test Company 1");
            result.Should().Contain("Test Company 2");
        }

        [Fact]
        public async Task GetSearchSuggestionsAsync_ForProduct_ShouldReturnProductSuggestions()
        {
            // Act
            var result = await _service.GetSearchSuggestionsAsync("Product", "product", "Admin");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().Contain("Product 1");
            result.Should().Contain("Product 2");
        }

        [Fact]
        public async Task GetSearchSuggestionsAsync_WithEmptyTerm_ShouldReturnEmpty()
        {
            // Act
            var result = await _service.GetSearchSuggestionsAsync("", "company", "Admin");

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region ExportCertificatesAsync Tests

        [Fact]
        public async Task ExportCertificatesAsync_AsCsv_ShouldReturnCsvContent()
        {
            // Arrange
            var request = new CertificateListRequest
            {
                Page = 1,
                PageSize = 10,
                UserRole = "Admin",
                Filters = new CertificateSearchFilters()
            };

            // Act
            var result = await _service.ExportCertificatesAsync(request, "csv");

            // Assert
            result.Should().NotBeNull();
            var content = System.Text.Encoding.UTF8.GetString(result);
            content.Should().Contain("Certificate Number");
            content.Should().Contain("CERT-001");
        }

        #endregion

        #region GetCertificateByCertificateNumberAsync Tests

        [Fact]
        public async Task GetCertificateByCertificateNumberAsync_WithValidNumber_ShouldReturnCertificate()
        {
            // Act
            var result = await _service.GetCertificateByCertificateNumberAsync("CERT-001", "Admin");

            // Assert
            result.Should().NotBeNull();
            result.CertificateNumber.Should().Be("CERT-001");
            result.CompanyName.Should().Be("Test Company 1");
        }

        [Fact]
        public async Task GetCertificateByCertificateNumberAsync_WithInvalidNumber_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetCertificateByCertificateNumberAsync("INVALID", "Admin");

            // Assert
            result.Should().BeNull();
        }

        #endregion

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
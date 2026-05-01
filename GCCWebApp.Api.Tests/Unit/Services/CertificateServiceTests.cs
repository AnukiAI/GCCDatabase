using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;
using GCCWebApp.Api.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Api.Tests.Unit.Services
{
    public class CertificateServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<CertificateService>> _loggerMock;
        private readonly CertificateService _service;

        public CertificateServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _loggerMock = new Mock<ILogger<CertificateService>>();
            var productIdServiceMock = new Mock<IProductIdService>();
            _service = new CertificateService(_loggerMock.Object, _context, productIdServiceMock.Object);

            SeedTestData();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedTestData()
        {
            // Companies
            var company1 = new Company { Id = 100, Name = "Acme Corp", Email = "info@acme.com" };
            var company2 = new Company { Id = 101, Name = "GreenTech Inc", Email = "info@greentech.com" };
            _context.Companies.AddRange(company1, company2);

            // Product categories
            var category = new ProductCategory
            {
                Id = 100,
                Name = "Packaging",
                Description = "Packaging products",
                IsActive = true,
                SortOrder = 0
            };
            _context.ProductCategories.Add(category);

            // Products
            var product1 = new Product
            {
                Id = 100,
                Name = "Paper Towels",
                CompanyId = 100,
                CategoryId = 100,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            var product2 = new Product
            {
                Id = 101,
                Name = "Cleaning Spray",
                CompanyId = 101,
                CategoryId = 100,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.Products.AddRange(product1, product2);

            // Certification types
            var certType1 = new CertificationType
            {
                Id = 100,
                Name = "Recycled Content",
                Description = "Recycled content certification",
                IsActive = true
            };
            var certType2 = new CertificationType
            {
                Id = 101,
                Name = "Certified Energy Savings",
                Description = "Energy savings certification",
                IsActive = true
            };
            _context.CertificationTypes.AddRange(certType1, certType2);

            // Certificates with various statuses and partners
            var certs = new List<Certificate>
            {
                new Certificate
                {
                    Id = 100,
                    Number = "24-0001",
                    Status = "Active",
                    Partner = "Amazon",
                    CompanyId = 100,
                    ProductId = 100,
                    CertificationTypeId = 100,
                    EffectiveDate = DateTime.UtcNow.AddDays(-90),
                    ExpirationDate = DateTime.UtcNow.AddDays(275),
                    Location = "Philadelphia, PA",
                    Country = "USA",
                    CreatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Id = 101,
                    Number = "24-0002",
                    Status = "Active",
                    Partner = "Novi",
                    CompanyId = 101,
                    ProductId = 101,
                    CertificationTypeId = 101,
                    EffectiveDate = DateTime.UtcNow.AddDays(-60),
                    ExpirationDate = DateTime.UtcNow.AddDays(305),
                    Location = "Portland, OR",
                    Country = "USA",
                    CreatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Id = 102,
                    Number = "24-0003",
                    Status = "Expired",
                    Partner = "None",
                    CompanyId = 100,
                    ProductId = 100,
                    CertificationTypeId = 100,
                    EffectiveDate = DateTime.UtcNow.AddDays(-400),
                    ExpirationDate = DateTime.UtcNow.AddDays(-35),
                    Location = "Austin, TX",
                    Country = "USA",
                    CreatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Id = 103,
                    Number = "24-0004",
                    Status = "Draft",
                    Partner = "None",
                    CompanyId = 101,
                    ProductId = 101,
                    CertificationTypeId = 101,
                    EffectiveDate = DateTime.UtcNow.AddDays(-10),
                    ExpirationDate = DateTime.UtcNow.AddDays(355),
                    Location = "Toronto, ON",
                    Country = "Canada",
                    CreatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Id = 104,
                    Number = "24-0005",
                    Status = "Active",
                    Partner = "Amazon",
                    CompanyId = 100,
                    ProductId = 100,
                    CertificationTypeId = 101,
                    EffectiveDate = DateTime.UtcNow.AddDays(-30),
                    ExpirationDate = DateTime.UtcNow.AddDays(15), // Expiring soon
                    Location = "Chicago, IL",
                    Country = "USA",
                    CreatedAt = DateTime.UtcNow
                }
            };
            _context.Certificates.AddRange(certs);
            _context.SaveChanges();
        }

        private CertificateListRequest CreateDefaultRequest(int page = 1, int pageSize = 20)
        {
            return new CertificateListRequest
            {
                Page = page,
                PageSize = pageSize,
                Sort = "",
                UserRole = "Admin",
                Filters = new CertificateSearchFilters()
            };
        }

        #region GetCertificatesAsync Tests

        [Fact]
        public async Task GetCertificatesAsync_NoFilters_ReturnsAllCertificates()
        {
            // Arrange
            var request = CreateDefaultRequest();

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCount(5);
            result.TotalCount.Should().Be(5);
        }

        [Fact]
        public async Task GetCertificatesAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var request = CreateDefaultRequest(page: 1, pageSize: 2);

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCount(2);
            result.TotalCount.Should().Be(5);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(2);
        }

        [Fact]
        public async Task GetCertificatesAsync_SecondPage_ReturnsRemainingCertificates()
        {
            // Arrange
            var request = CreateDefaultRequest(page: 2, pageSize: 3);

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCount(2); // 5 total, page 2 of 3 = 2 remaining
            result.TotalCount.Should().Be(5);
        }

        [Fact]
        public async Task GetCertificatesAsync_SearchByCompanyName_FiltersCorrectly()
        {
            // Arrange
            var request = CreateDefaultRequest();
            request.Filters.SearchTerm = "Acme";

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCount(3); // Certs 100, 102, 104 belong to Acme Corp
            result.Certificates.Should().OnlyContain(c => c.CompanyName == "Acme Corp");
        }

        [Fact]
        public async Task GetCertificatesAsync_SearchByProductName_FiltersCorrectly()
        {
            // Arrange
            var request = CreateDefaultRequest();
            request.Filters.SearchTerm = "Cleaning";

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCount(2); // Certs 101, 103
            result.Certificates.Should().OnlyContain(c => c.ProductName == "Cleaning Spray");
        }

        [Fact]
        public async Task GetCertificatesAsync_SearchByCertificateNumber_FiltersCorrectly()
        {
            // Arrange
            var request = CreateDefaultRequest();
            request.Filters.SearchTerm = "24-0001";

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCount(1);
            result.Certificates.First().CertificateNumber.Should().Be("24-0001");
        }

        [Fact]
        public async Task GetCertificatesAsync_FilterByCertificationType_FiltersCorrectly()
        {
            // Arrange
            var request = CreateDefaultRequest();
            request.Filters.CertificationTypes = new List<string> { "Recycled Content" };

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCount(2); // Certs 100, 102
            result.Certificates.Should().OnlyContain(c => c.CertificationType == "Recycled Content");
        }

        [Fact]
        public async Task GetCertificatesAsync_FilterByCountry_FiltersCorrectly()
        {
            // Arrange
            var request = CreateDefaultRequest();
            request.Filters.Countries = new List<string> { "Canada" };

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCount(1);
            result.Certificates.First().Country.Should().Be("Canada");
        }

        [Fact]
        public async Task GetCertificatesAsync_FilterByStatus_FiltersCorrectly()
        {
            // Arrange
            var request = CreateDefaultRequest();
            request.Filters.Statuses = new List<string> { "Active" };

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCount(3); // Certs 100, 101, 104
            result.Certificates.Should().OnlyContain(c => c.Status == "Active");
        }

        [Fact]
        public async Task GetCertificatesAsync_FilterByEffectiveDateRange_FiltersCorrectly()
        {
            // Arrange
            var request = CreateDefaultRequest();
            request.Filters.EffectiveDateStart = DateTime.UtcNow.AddDays(-70);
            request.Filters.EffectiveDateEnd = DateTime.UtcNow.AddDays(-20);

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCountGreaterThan(0);
            result.Certificates.Should().OnlyContain(c =>
                c.EffectiveDate >= request.Filters.EffectiveDateStart &&
                c.EffectiveDate <= request.Filters.EffectiveDateEnd);
        }

        [Fact]
        public async Task GetCertificatesAsync_SortByNewest_OrdersCorrectly()
        {
            // Arrange
            var request = CreateDefaultRequest();
            request.Sort = "newest";

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            var dates = result.Certificates.Select(c => c.EffectiveDate).ToList();
            dates.Should().BeInDescendingOrder();
        }

        [Fact]
        public async Task GetCertificatesAsync_SortByExpiring_OrdersCorrectly()
        {
            // Arrange
            var request = CreateDefaultRequest();
            request.Sort = "expiring";

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            var dates = result.Certificates.Select(c => c.ExpirationDate).ToList();
            dates.Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task GetCertificatesAsync_SortByCompany_OrdersCorrectly()
        {
            // Arrange
            var request = CreateDefaultRequest();
            request.Sort = "company-az";

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            var names = result.Certificates.Select(c => c.CompanyName).ToList();
            names.Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task GetCertificatesAsync_MultipleFilters_CombinesCorrectly()
        {
            // Arrange - Active certs in USA
            var request = CreateDefaultRequest();
            request.Filters.Statuses = new List<string> { "Active" };
            request.Filters.Countries = new List<string> { "USA" };

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().OnlyContain(c =>
                c.Status == "Active" && c.Country == "USA");
        }

        [Fact]
        public async Task GetCertificatesAsync_NoMatchingFilters_ReturnsEmpty()
        {
            // Arrange
            var request = CreateDefaultRequest();
            request.Filters.SearchTerm = "NonExistentCompanyXYZ";

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetCertificatesAsync_ReturnsCertificateNumberNotId()
        {
            // Arrange - Key requirement: show Certificate Number, not database ID
            var request = CreateDefaultRequest();

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().OnlyContain(c =>
                c.CertificateNumber.StartsWith("24-"));
        }

        #endregion

        #region GetCertificatesByPartnerAsync Tests

        [Fact]
        public async Task GetCertificatesByPartnerAsync_Amazon_ReturnsOnlyAmazonCerts()
        {
            // Arrange
            var request = CreateDefaultRequest();

            // Act
            var result = await _service.GetCertificatesByPartnerAsync("Amazon", request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCount(2); // Certs 100, 104
        }

        [Fact]
        public async Task GetCertificatesByPartnerAsync_Novi_ReturnsOnlyNoviCerts()
        {
            // Arrange
            var request = CreateDefaultRequest();

            // Act
            var result = await _service.GetCertificatesByPartnerAsync("Novi", request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCount(1); // Cert 101
        }

        [Fact]
        public async Task GetCertificatesByPartnerAsync_None_ReturnsNonPartnerCerts()
        {
            // Arrange
            var request = CreateDefaultRequest();

            // Act
            var result = await _service.GetCertificatesByPartnerAsync("None", request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCount(2); // Certs 102, 103
        }

        [Fact]
        public async Task GetCertificatesByPartnerAsync_UnknownPartner_ReturnsEmpty()
        {
            // Arrange
            var request = CreateDefaultRequest();

            // Act
            var result = await _service.GetCertificatesByPartnerAsync("NonExistentPartner", request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().BeEmpty();
        }

        [Fact]
        public async Task GetCertificatesByPartnerAsync_WithSearchFilter_CombinesWithPartnerFilter()
        {
            // Arrange - Amazon certs for Acme only
            var request = CreateDefaultRequest();
            request.Filters.SearchTerm = "Acme";

            // Act
            var result = await _service.GetCertificatesByPartnerAsync("Amazon", request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().HaveCount(2); // Certs 100, 104 both Amazon + Acme
        }

        #endregion

        #region GetCertificateDetailsAsync Tests

        [Fact]
        public async Task GetCertificateDetailsAsync_ValidId_ReturnsCertificateDetails()
        {
            // Act
            var result = await _service.GetCertificateDetailsAsync(100, "Admin");

            // Assert
            result.Should().NotBeNull();
            result.CertificateNumber.Should().Be("24-0001");
            result.CompanyName.Should().Be("Acme Corp");
            result.ProductName.Should().Be("Paper Towels");
            result.CertificationType.Should().Be("Recycled Content");
            result.Location.Should().Be("Philadelphia, PA");
            result.Country.Should().Be("USA");
        }

        [Fact]
        public async Task GetCertificateDetailsAsync_InvalidId_ReturnsNull()
        {
            // Act
            var result = await _service.GetCertificateDetailsAsync(9999, "Admin");

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CreateCertificateAsync Tests

        [Fact]
        public async Task CreateCertificateAsync_ValidData_ReturnsCertificateId()
        {
            // Arrange
            var viewModel = new CertificateEditViewModel
            {
                Number = "24-9999",
                Status = "Draft",
                EffectiveDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddDays(365),
                Location = "Denver, CO",
                Country = "USA",
                CompanyId = 100,
                ProductId = 100,
                CertificationTypeId = 100
            };

            // Act
            var result = await _service.CreateCertificateAsync(viewModel, "Admin");

            // Assert
            result.Should().BeGreaterThan(0);

            // Verify in database
            var created = await _context.Certificates.FindAsync(result);
            created.Should().NotBeNull();
            created!.Number.Should().Be("24-9999");
            created.Status.Should().Be("Draft");
            created.CompanyId.Should().Be(100);
        }

        [Fact]
        public async Task CreateCertificateAsync_SetsAuditTimestamps()
        {
            // Arrange
            var viewModel = new CertificateEditViewModel
            {
                Number = "24-8888",
                Status = "Draft",
                EffectiveDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddDays(365),
                CompanyId = 100,
                ProductId = 100,
                CertificationTypeId = 100
            };

            // Act
            var id = await _service.CreateCertificateAsync(viewModel, "Analyst");

            // Assert
            var created = await _context.Certificates.FindAsync(id);
            created.Should().NotBeNull();
            created!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            created.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task CreateCertificateAsync_WithOptionalFields_PersistsAllFields()
        {
            // Arrange
            var viewModel = new CertificateEditViewModel
            {
                Number = "24-7777",
                Status = "Draft",
                EffectiveDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddDays(365),
                Location = "Seattle, WA",
                Country = "USA",
                Asin = "B08TEST123",
                Upc = "012345678901",
                Gtin = "00012345678901",
                Ean = "1234567890123",
                CompanyId = 100,
                ProductId = 100,
                CertificationTypeId = 100
            };

            // Act
            var id = await _service.CreateCertificateAsync(viewModel, "Admin");

            // Assert
            var created = await _context.Certificates.FindAsync(id);
            created.Should().NotBeNull();
            created!.Asin.Should().Be("B08TEST123");
            created.Upc.Should().Be("012345678901");
            created.Gtin.Should().Be("00012345678901");
            created.Ean.Should().Be("1234567890123");
            created.Location.Should().Be("Seattle, WA");
        }

        #endregion

        #region UpdateCertificateAsync Tests

        [Fact]
        public async Task UpdateCertificateAsync_ValidId_UpdatesFields()
        {
            // Arrange
            var viewModel = new CertificateEditViewModel
            {
                Number = "24-0001-UPDATED",
                Status = "Active",
                EffectiveDate = DateTime.UtcNow.AddDays(-90),
                ExpirationDate = DateTime.UtcNow.AddDays(275),
                Location = "New York, NY",
                Country = "USA",
                CompanyId = 100,
                ProductId = 100,
                CertificationTypeId = 100
            };

            // Act
            var result = await _service.UpdateCertificateAsync(100, viewModel, "Admin");

            // Assert
            result.Should().BeTrue();
            var updated = await _context.Certificates.FindAsync(100);
            updated!.Number.Should().Be("24-0001-UPDATED");
            updated.Location.Should().Be("New York, NY");
            updated.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task UpdateCertificateAsync_NonExistentId_ReturnsFalse()
        {
            // Arrange
            var viewModel = new CertificateEditViewModel
            {
                Number = "XX-0000",
                Status = "Draft",
                EffectiveDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddDays(365),
                CompanyId = 100,
                ProductId = 100,
                CertificationTypeId = 100
            };

            // Act
            var result = await _service.UpdateCertificateAsync(9999, viewModel, "Admin");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateCertificateAsync_ChangesStatus()
        {
            // Arrange
            var viewModel = new CertificateEditViewModel
            {
                Number = "24-0004",
                Status = "PendingReview",
                EffectiveDate = DateTime.UtcNow.AddDays(-10),
                ExpirationDate = DateTime.UtcNow.AddDays(355),
                CompanyId = 101,
                ProductId = 101,
                CertificationTypeId = 101
            };

            // Act
            var result = await _service.UpdateCertificateAsync(103, viewModel, "Admin");

            // Assert
            result.Should().BeTrue();
            var updated = await _context.Certificates.FindAsync(103);
            updated!.Status.Should().Be("PendingReview");
        }

        #endregion

        #region DeleteCertificateAsync Tests

        [Fact]
        public async Task DeleteCertificateAsync_ValidId_DeletesCertificate()
        {
            // Act
            var result = await _service.DeleteCertificateAsync(103, "Admin");

            // Assert
            result.Should().BeTrue();
            var deleted = await _context.Certificates.FindAsync(103);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteCertificateAsync_NonExistentId_ReturnsFalse()
        {
            // Act
            var result = await _service.DeleteCertificateAsync(9999, "Admin");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteCertificateAsync_ReducesCount()
        {
            // Arrange
            var countBefore = await _context.Certificates.CountAsync();

            // Act
            await _service.DeleteCertificateAsync(103, "Admin");

            // Assert
            var countAfter = await _context.Certificates.CountAsync();
            countAfter.Should().Be(countBefore - 1);
        }

        #endregion

        #region ValidateCertificateAsync Tests

        [Fact]
        public async Task ValidateCertificateAsync_ValidActiveCert_ReturnsValid()
        {
            // Act
            var result = await _service.ValidateCertificateAsync("24-0001");

            // Assert
            result.IsValid.Should().BeTrue();
            result.Status.Should().Be("Active");
            result.IsExpired.Should().BeFalse();
            result.Message.Should().Be("Certificate is valid");
        }

        [Fact]
        public async Task ValidateCertificateAsync_ExpiredCert_ReturnsExpiredMessage()
        {
            // Act - Cert 102 has ExpirationDate in the past
            var result = await _service.ValidateCertificateAsync("24-0003");

            // Assert
            result.IsValid.Should().BeTrue(); // Still valid (found in DB), just expired
            result.IsExpired.Should().BeTrue();
            result.Message.Should().Be("Certificate has expired");
        }

        [Fact]
        public async Task ValidateCertificateAsync_ExpiringSoonCert_ReturnsExpiringSoonMessage()
        {
            // Act - Cert 104 expires in 15 days
            var result = await _service.ValidateCertificateAsync("24-0005");

            // Assert
            result.IsValid.Should().BeTrue();
            result.IsExpiringSoon.Should().BeTrue();
            result.IsExpired.Should().BeFalse();
            result.Message.Should().Be("Certificate is expiring soon");
        }

        [Fact]
        public async Task ValidateCertificateAsync_NonExistentCert_ReturnsNotFound()
        {
            // Act
            var result = await _service.ValidateCertificateAsync("XX-9999");

            // Assert
            result.IsValid.Should().BeFalse();
            result.Message.Should().Be("Certificate not found");
        }

        #endregion

        #region GetCertificateStatisticsAsync Tests

        [Fact]
        public async Task GetCertificateStatisticsAsync_ReturnsTotalCount()
        {
            // Act
            var result = await _service.GetCertificateStatisticsAsync("Admin");

            // Assert
            result.TotalCertificates.Should().Be(5);
        }

        [Fact]
        public async Task GetCertificateStatisticsAsync_CountsByType()
        {
            // Act
            var result = await _service.GetCertificateStatisticsAsync("Admin");

            // Assert
            result.CertificatesByType.Should().ContainKey("Recycled Content");
            result.CertificatesByType.Should().ContainKey("Certified Energy Savings");
        }

        [Fact]
        public async Task GetCertificateStatisticsAsync_CountsByCountry()
        {
            // Act
            var result = await _service.GetCertificateStatisticsAsync("Admin");

            // Assert
            result.CertificatesByCountry.Should().ContainKey("USA");
            result.CertificatesByCountry.Should().ContainKey("Canada");
            result.CertificatesByCountry["USA"].Should().Be(4);
            result.CertificatesByCountry["Canada"].Should().Be(1);
        }

        #endregion

        #region PerformBulkActionAsync Tests

        [Fact]
        public async Task PerformBulkActionAsync_Delete_RemovesCertificates()
        {
            // Arrange
            var request = new BulkActionRequest
            {
                Action = "delete",
                CertificateIds = new List<int> { 102, 103 },
                UserRole = "Admin"
            };

            // Act
            var result = await _service.PerformBulkActionAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.AffectedCount.Should().Be(2);
            var remaining = await _context.Certificates.CountAsync();
            remaining.Should().Be(3); // 5 - 2 deleted
        }

        [Fact]
        public async Task PerformBulkActionAsync_UpdateStatus_UpdatesAllTargeted()
        {
            // Arrange
            var request = new BulkActionRequest
            {
                Action = "update_status",
                CertificateIds = new List<int> { 100, 101 },
                UserRole = "Admin",
                Parameters = new Dictionary<string, string> { { "status", "Suspended" } }
            };

            // Act
            var result = await _service.PerformBulkActionAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.AffectedCount.Should().Be(2);

            var cert100 = await _context.Certificates.FindAsync(100);
            var cert101 = await _context.Certificates.FindAsync(101);
            cert100!.Status.Should().Be("Suspended");
            cert101!.Status.Should().Be("Suspended");
        }

        [Fact]
        public async Task PerformBulkActionAsync_UpdateStatusMissingParam_ReturnsFalse()
        {
            // Arrange - Missing "status" parameter
            var request = new BulkActionRequest
            {
                Action = "update_status",
                CertificateIds = new List<int> { 100 },
                UserRole = "Admin",
                Parameters = new Dictionary<string, string>() // Empty
            };

            // Act
            var result = await _service.PerformBulkActionAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Status parameter is required");
        }

        [Fact]
        public async Task PerformBulkActionAsync_UnknownAction_ReturnsFalse()
        {
            // Arrange
            var request = new BulkActionRequest
            {
                Action = "invalid_action",
                CertificateIds = new List<int> { 100 },
                UserRole = "Admin"
            };

            // Act
            var result = await _service.PerformBulkActionAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Unknown bulk action");
        }

        [Fact]
        public async Task PerformBulkActionAsync_DeleteNonExistentIds_SucceedsWithZeroAffected()
        {
            // Arrange
            var request = new BulkActionRequest
            {
                Action = "delete",
                CertificateIds = new List<int> { 9998, 9999 },
                UserRole = "Admin"
            };

            // Act
            var result = await _service.PerformBulkActionAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.AffectedCount.Should().Be(0);
        }

        #endregion

        #region GetSearchSuggestionsAsync Tests

        [Fact]
        public async Task GetSearchSuggestionsAsync_CompanyType_ReturnsSuggestions()
        {
            // Act
            var result = await _service.GetSearchSuggestionsAsync("Acme", "company", "Admin");

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain("Acme Corp");
        }

        [Fact]
        public async Task GetSearchSuggestionsAsync_ProductType_ReturnsSuggestions()
        {
            // Act
            var result = await _service.GetSearchSuggestionsAsync("Paper", "product", "Admin");

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain("Paper Towels");
        }

        [Fact]
        public async Task GetSearchSuggestionsAsync_DefaultType_SearchesCertificationTypes()
        {
            // Act
            var result = await _service.GetSearchSuggestionsAsync("Recycled", "certtype", "Admin");

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain("Recycled Content");
        }

        [Fact]
        public async Task GetSearchSuggestionsAsync_EmptyTerm_ReturnsEmpty()
        {
            // Act
            var result = await _service.GetSearchSuggestionsAsync("", "company", "Admin");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetSearchSuggestionsAsync_NoMatch_ReturnsEmpty()
        {
            // Act
            var result = await _service.GetSearchSuggestionsAsync("ZZZNonExistent", "company", "Admin");

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region ExportCertificatesAsync Tests

        [Fact]
        public async Task ExportCertificatesAsync_CsvFormat_ReturnsBytes()
        {
            // Arrange
            var request = CreateDefaultRequest();

            // Act
            var result = await _service.ExportCertificatesAsync(request, "csv");

            // Assert
            result.Should().NotBeNull();
            result!.Length.Should().BeGreaterThan(0);

            // Verify CSV content
            var csvContent = System.Text.Encoding.UTF8.GetString(result);
            csvContent.Should().Contain("Certificate Number");
            csvContent.Should().Contain("24-0001");
        }

        [Fact]
        public async Task ExportCertificatesAsync_CsvContainsAllCertificates()
        {
            // Arrange
            var request = CreateDefaultRequest();

            // Act
            var result = await _service.ExportCertificatesAsync(request, "csv");

            // Assert
            var csvContent = System.Text.Encoding.UTF8.GetString(result!);
            csvContent.Should().Contain("24-0001");
            csvContent.Should().Contain("24-0002");
            csvContent.Should().Contain("24-0003");
            csvContent.Should().Contain("24-0004");
            csvContent.Should().Contain("24-0005");
        }

        [Fact]
        public async Task ExportCertificatesAsync_DefaultFormat_ReturnsCsv()
        {
            // Arrange - "unknown" format should default to CSV
            var request = CreateDefaultRequest();

            // Act
            var result = await _service.ExportCertificatesAsync(request, "unknown_format");

            // Assert
            result.Should().NotBeNull();
            var csvContent = System.Text.Encoding.UTF8.GetString(result!);
            csvContent.Should().Contain("Certificate Number"); // CSV header
        }

        [Fact]
        public async Task ExportCertificatesAsync_WithStatusFilter_ExportsOnlyFiltered()
        {
            // Arrange - Only active certs
            var request = CreateDefaultRequest();
            request.Filters.Statuses = new List<string> { "Active" };

            // Act
            var result = await _service.ExportCertificatesAsync(request, "csv");

            // Assert
            var csvContent = System.Text.Encoding.UTF8.GetString(result!);
            csvContent.Should().Contain("24-0001"); // Active
            csvContent.Should().NotContain("24-0003"); // Expired - should be excluded
            csvContent.Should().NotContain("24-0004"); // Draft - should be excluded
        }

        #endregion

        #region ExportCertificatesByIdsAsync Tests

        [Fact]
        public async Task ExportCertificatesByIdsAsync_ValidIds_ExportsOnlyThoseCerts()
        {
            // Arrange
            var ids = new List<string> { "100", "101" };

            // Act
            var result = await _service.ExportCertificatesByIdsAsync(ids, "csv");

            // Assert
            result.Should().NotBeNull();
            var csvContent = System.Text.Encoding.UTF8.GetString(result!);
            csvContent.Should().Contain("24-0001");
            csvContent.Should().Contain("24-0002");
            csvContent.Should().NotContain("24-0003");
        }

        [Fact]
        public async Task ExportCertificatesByIdsAsync_ByCertificateNumber_Works()
        {
            // Arrange - Using certificate numbers instead of IDs
            var numbers = new List<string> { "24-0001", "24-0005" };

            // Act
            var result = await _service.ExportCertificatesByIdsAsync(numbers, "csv");

            // Assert
            result.Should().NotBeNull();
            var csvContent = System.Text.Encoding.UTF8.GetString(result!);
            csvContent.Should().Contain("24-0001");
            csvContent.Should().Contain("24-0005");
        }

        [Fact]
        public async Task ExportCertificatesByIdsAsync_EmptyList_ReturnsNull()
        {
            // Act
            var result = await _service.ExportCertificatesByIdsAsync(new List<string>(), "csv");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task ExportCertificatesByIdsAsync_NullList_ReturnsNull()
        {
            // Act
            var result = await _service.ExportCertificatesByIdsAsync(null!, "csv");

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetCertificateByCertificateNumberAsync Tests

        [Fact]
        public async Task GetCertificateByCertificateNumberAsync_ValidNumber_ReturnsCertificate()
        {
            // Act
            var result = await _service.GetCertificateByCertificateNumberAsync("24-0001", "Admin");

            // Assert
            result.Should().NotBeNull();
            result!.CertificateNumber.Should().Be("24-0001");
            result.CompanyName.Should().Be("Acme Corp");
        }

        [Fact]
        public async Task GetCertificateByCertificateNumberAsync_NonExistent_ReturnsNull()
        {
            // Act
            var result = await _service.GetCertificateByCertificateNumberAsync("XX-9999", "Admin");

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region TagType and BadgeText Tests

        [Fact]
        public async Task GetCertificatesAsync_AdminRole_SetsInternalTagType()
        {
            // Arrange
            var request = CreateDefaultRequest();
            request.UserRole = "Admin";

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().OnlyContain(c => c.TagType == "Internal");
        }

        [Fact]
        public async Task GetCertificatesAsync_ExternalRole_SetsExternalTagType()
        {
            // Arrange
            var request = CreateDefaultRequest();
            request.UserRole = "External";

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificates.Should().OnlyContain(c => c.TagType == "External");
        }

        [Fact]
        public async Task GetCertificatesAsync_RecycledContent_GetsBadgeRC()
        {
            // Arrange
            var request = CreateDefaultRequest();
            request.Filters.CertificationTypes = new List<string> { "Recycled Content" };

            // Act
            var result = await _service.GetCertificatesAsync(request);

            // Assert
            result.Certificates.Should().OnlyContain(c => c.BadgeText == "RC");
        }

        #endregion
    }
}

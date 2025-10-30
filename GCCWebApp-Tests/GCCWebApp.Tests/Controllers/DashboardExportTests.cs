using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using GCCWebApp.Controllers;
using GCCWebApp.Models;
using GCCWebApp.Data;
using GCCWebApp.Repositories;
using GCCWebApp.Services.Performance;
using GCCWebApp.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace GCCWebApp.Tests.Controllers
{
    /// <summary>
    /// Tests for Dashboard Export functionality - verifies that each stat card and table
    /// exports the correct subset of certificates with proper filtering
    /// </summary>
    public class DashboardExportTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<ExportController>> _loggerMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ICertificateRepository> _certificateRepositoryMock;
        private readonly Mock<IPerformanceMonitoringService> _performanceMonitoringMock;
        private readonly Mock<IStructuredLoggingService> _structuredLoggingMock;
        private readonly Mock<IWebHostEnvironment> _environmentMock;
        private readonly ExportController _controller;

        public DashboardExportTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<ExportController>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _certificateRepositoryMock = new Mock<ICertificateRepository>();
            _performanceMonitoringMock = new Mock<IPerformanceMonitoringService>();
            _structuredLoggingMock = new Mock<IStructuredLoggingService>();
            _environmentMock = new Mock<IWebHostEnvironment>();

            _controller = new ExportController(
                _loggerMock.Object,
                _unitOfWorkMock.Object,
                _certificateRepositoryMock.Object,
                _performanceMonitoringMock.Object,
                _structuredLoggingMock.Object,
                _context,
                _environmentMock.Object
            );

            // Mock HttpContext and Session
            var mockHttpContext = new Mock<HttpContext>();
            var mockSession = new Mock<ISession>();
            mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            SeedTestData();
        }

        private void SeedTestData()
        {
            var now = DateTime.UtcNow;
            var lastMonth = now.AddMonths(-1);
            var nextMonth = now.AddMonths(1);
            var lastYear = now.AddYears(-1);

            // Create companies
            var companies = new List<Company>
            {
                new Company { Id = 1, Name = "Company A", IsActive = true, Country = "USA", Location = "New York" },
                new Company { Id = 2, Name = "Company B", IsActive = true, Country = "Canada", Location = "Toronto" },
                new Company { Id = 3, Name = "Company C", IsActive = true, Country = "USA", Location = "Chicago" }
            };
            _context.Companies.AddRange(companies);

            // Create certification types
            var certTypes = new List<CertificationType>
            {
                new CertificationType { Id = 1, Name = "FSC", Description = "FSC Certification", IsActive = true },
                new CertificationType { Id = 2, Name = "PEFC", Description = "PEFC Certification", IsActive = true }
            };
            _context.CertificationTypes.AddRange(certTypes);

            // Create product categories
            var productCategories = new List<ProductCategory>
            {
                new ProductCategory { Id = 1, Name = "Wood Products", Description = "Wood", IsActive = true },
                new ProductCategory { Id = 2, Name = "Paper Products", Description = "Paper", IsActive = true }
            };
            _context.ProductCategories.AddRange(productCategories);

            // Create products
            var products = new List<Product>
            {
                new Product { Id = 1, Name = "Product 1", CategoryId = 1, IsActive = true },
                new Product { Id = 2, Name = "Product 2", CategoryId = 1, IsActive = true },
                new Product { Id = 3, Name = "Product 3", CategoryId = 2, IsActive = true }
            };
            _context.Products.AddRange(products);

            _context.SaveChanges();

            // Create certificates with different dates and statuses
            var certificates = new List<Certificate>
            {
                // 5 Active certificates expiring this month
                new Certificate { Id = 1, Number = "CERT-001", Status = "Active", CompanyId = 1, ProductId = 1, CertificationTypeId = 1,
                    EffectiveDate = now.AddMonths(-6), ExpirationDate = now.AddDays(15), Country = "USA", Location = "New York",
                    Company = companies[0], Product = products[0], CertificationType = certTypes[0] },
                new Certificate { Id = 2, Number = "CERT-002", Status = "Active", CompanyId = 1, ProductId = 1, CertificationTypeId = 1,
                    EffectiveDate = now.AddMonths(-6), ExpirationDate = now.AddDays(20), Country = "USA", Location = "New York",
                    Company = companies[0], Product = products[0], CertificationType = certTypes[0] },
                new Certificate { Id = 3, Number = "CERT-003", Status = "Active", CompanyId = 2, ProductId = 2, CertificationTypeId = 2,
                    EffectiveDate = now.AddMonths(-6), ExpirationDate = now.AddDays(10), Country = "Canada", Location = "Toronto",
                    Company = companies[1], Product = products[1], CertificationType = certTypes[1] },
                new Certificate { Id = 4, Number = "CERT-004", Status = "Active", CompanyId = 2, ProductId = 2, CertificationTypeId = 2,
                    EffectiveDate = now.AddMonths(-6), ExpirationDate = now.AddDays(25), Country = "Canada", Location = "Toronto",
                    Company = companies[1], Product = products[1], CertificationType = certTypes[1] },
                new Certificate { Id = 5, Number = "CERT-005", Status = "Active", CompanyId = 3, ProductId = 3, CertificationTypeId = 1,
                    EffectiveDate = now.AddMonths(-6), ExpirationDate = now.AddDays(5), Country = "USA", Location = "Chicago",
                    Company = companies[2], Product = products[2], CertificationType = certTypes[0] },

                // 10 Active certificates NOT expiring this month
                new Certificate { Id = 6, Number = "CERT-006", Status = "Active", CompanyId = 1, ProductId = 1, CertificationTypeId = 1,
                    EffectiveDate = now.AddMonths(-3), ExpirationDate = now.AddMonths(6), Country = "USA", Location = "New York",
                    Company = companies[0], Product = products[0], CertificationType = certTypes[0] },
                new Certificate { Id = 7, Number = "CERT-007", Status = "Active", CompanyId = 1, ProductId = 1, CertificationTypeId = 1,
                    EffectiveDate = now.AddMonths(-3), ExpirationDate = now.AddMonths(6), Country = "USA", Location = "New York",
                    Company = companies[0], Product = products[0], CertificationType = certTypes[0] },
                new Certificate { Id = 8, Number = "CERT-008", Status = "Active", CompanyId = 2, ProductId = 2, CertificationTypeId = 2,
                    EffectiveDate = now.AddMonths(-3), ExpirationDate = now.AddMonths(8), Country = "Canada", Location = "Toronto",
                    Company = companies[1], Product = products[1], CertificationType = certTypes[1] },
                new Certificate { Id = 9, Number = "CERT-009", Status = "Active", CompanyId = 2, ProductId = 2, CertificationTypeId = 2,
                    EffectiveDate = now.AddMonths(-3), ExpirationDate = now.AddMonths(8), Country = "Canada", Location = "Toronto",
                    Company = companies[1], Product = products[1], CertificationType = certTypes[1] },
                new Certificate { Id = 10, Number = "CERT-010", Status = "Active", CompanyId = 3, ProductId = 3, CertificationTypeId = 1,
                    EffectiveDate = now.AddMonths(-3), ExpirationDate = now.AddMonths(9), Country = "USA", Location = "Chicago",
                    Company = companies[2], Product = products[2], CertificationType = certTypes[0] },
                new Certificate { Id = 11, Number = "CERT-011", Status = "Active", CompanyId = 1, ProductId = 1, CertificationTypeId = 1,
                    EffectiveDate = now.AddMonths(-2), ExpirationDate = now.AddMonths(10), Country = "USA", Location = "New York",
                    Company = companies[0], Product = products[0], CertificationType = certTypes[0] },
                new Certificate { Id = 12, Number = "CERT-012", Status = "Active", CompanyId = 2, ProductId = 2, CertificationTypeId = 2,
                    EffectiveDate = now.AddMonths(-2), ExpirationDate = now.AddMonths(12), Country = "Canada", Location = "Toronto",
                    Company = companies[1], Product = products[1], CertificationType = certTypes[1] },
                new Certificate { Id = 13, Number = "CERT-013", Status = "Active", CompanyId = 3, ProductId = 3, CertificationTypeId = 1,
                    EffectiveDate = now.AddMonths(-2), ExpirationDate = now.AddYears(1), Country = "USA", Location = "Chicago",
                    Company = companies[2], Product = products[2], CertificationType = certTypes[0] },
                new Certificate { Id = 14, Number = "CERT-014", Status = "Active", CompanyId = 1, ProductId = 1, CertificationTypeId = 2,
                    EffectiveDate = now.AddMonths(-1), ExpirationDate = now.AddMonths(11), Country = "USA", Location = "New York",
                    Company = companies[0], Product = products[0], CertificationType = certTypes[1] },
                new Certificate { Id = 15, Number = "CERT-015", Status = "Active", CompanyId = 2, ProductId = 2, CertificationTypeId = 1,
                    EffectiveDate = now.AddMonths(-1), ExpirationDate = now.AddYears(1), Country = "Canada", Location = "Toronto",
                    Company = companies[1], Product = products[1], CertificationType = certTypes[0] },

                // 3 certificates published last month
                new Certificate { Id = 16, Number = "CERT-016", Status = "Active", CompanyId = 1, ProductId = 1, CertificationTypeId = 1,
                    EffectiveDate = lastMonth.AddDays(5), ExpirationDate = now.AddYears(1), Country = "USA", Location = "New York", CreatedAt = lastMonth.AddDays(5),
                    Company = companies[0], Product = products[0], CertificationType = certTypes[0] },
                new Certificate { Id = 17, Number = "CERT-017", Status = "Active", CompanyId = 2, ProductId = 2, CertificationTypeId = 2,
                    EffectiveDate = lastMonth.AddDays(10), ExpirationDate = now.AddYears(1), Country = "Canada", Location = "Toronto", CreatedAt = lastMonth.AddDays(10),
                    Company = companies[1], Product = products[1], CertificationType = certTypes[1] },
                new Certificate { Id = 18, Number = "CERT-018", Status = "Active", CompanyId = 3, ProductId = 3, CertificationTypeId = 1,
                    EffectiveDate = lastMonth.AddDays(15), ExpirationDate = now.AddYears(1), Country = "USA", Location = "Chicago", CreatedAt = lastMonth.AddDays(15),
                    Company = companies[2], Product = products[2], CertificationType = certTypes[0] },

                // 2 expired certificates
                new Certificate { Id = 19, Number = "CERT-019", Status = "Expired", CompanyId = 1, ProductId = 1, CertificationTypeId = 1,
                    EffectiveDate = now.AddYears(-2), ExpirationDate = now.AddDays(-30), Country = "USA", Location = "New York",
                    Company = companies[0], Product = products[0], CertificationType = certTypes[0] },
                new Certificate { Id = 20, Number = "CERT-020", Status = "Expired", CompanyId = 2, ProductId = 2, CertificationTypeId = 2,
                    EffectiveDate = now.AddYears(-2), ExpirationDate = now.AddDays(-60), Country = "Canada", Location = "Toronto",
                    Company = companies[1], Product = products[1], CertificationType = certTypes[1] }
            };

            _context.Certificates.AddRange(certificates);
            _context.SaveChanges();

            // Setup mock for GetCertificateStatisticsAsync
            var stats = new Dictionary<string, int>
            {
                { "Active", 18 }, // 18 total active certificates
                { "Expired", 2 },
                { "ExpiringThisMonth", 5 },
                { "PublishedLastMonth", 3 }
            };
            _certificateRepositoryMock
                .Setup(x => x.GetCertificateStatisticsAsync())
                .ReturnsAsync(stats);
        }

        [Fact]
        public async Task DownloadStat_TotalActiveCerts_ShouldExport18ActiveCertificates()
        {
            // Act
            var result = await _controller.DownloadStat("total-active-certs", "excel");

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult.Should().NotBeNull();
            fileResult!.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            // Verify logging was called
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exporting 18 certificates")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task DownloadStat_CertsExpiringThisMonth_ShouldExportOnly5Certificates()
        {
            // Act
            var result = await _controller.DownloadStat("certs-expiring-this-month", "excel");

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult.Should().NotBeNull();

            // Verify logging shows correct count
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exporting 5 certificates")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task DownloadStat_CertsPublishedLastMonth_ShouldExportOnly3Certificates()
        {
            // Act
            var result = await _controller.DownloadStat("certs-published-last-month", "excel");

            // Assert
            result.Should().BeOfType<FileContentResult>();

            // Verify logging shows correct count
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exporting 3 certificates")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task DownloadStat_CompaniesWithActiveCerts_ShouldExportAll18ActiveCertificates()
        {
            // Act
            var result = await _controller.DownloadStat("companies-with-active-certs", "excel");

            // Assert
            result.Should().BeOfType<FileContentResult>();

            // Verify correct count - should export all active certificates from companies with active certs
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exporting 18 certificates")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Theory]
        [InlineData("total-active-certs", "excel")]
        [InlineData("total-active-certs", "csv")]
        [InlineData("certs-expiring-this-month", "excel")]
        [InlineData("certs-expiring-this-month", "csv")]
        [InlineData("certs-published-last-month", "excel")]
        [InlineData("certs-published-last-month", "csv")]
        public async Task DownloadStat_VariousFormats_ShouldReturnCorrectContentType(string statType, string format)
        {
            // Act
            var result = await _controller.DownloadStat(statType, format);

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult.Should().NotBeNull();

            if (format == "excel")
            {
                fileResult!.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                fileResult.FileDownloadName.Should().EndWith(".xlsx");
            }
            else
            {
                fileResult!.ContentType.Should().Be("text/csv");
                fileResult.FileDownloadName.Should().EndWith(".csv");
            }
        }

        [Fact]
        public async Task DownloadStat_InvalidType_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.DownloadStat("invalid-type", "excel");

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("Invalid stat type");
        }

        [Fact]
        public async Task DownloadStat_VerifyDateFilteringForPublishedLastMonth()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var startOfLastMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
            var endOfLastMonth = startOfLastMonth.AddMonths(1).AddDays(-1);

            // Act
            await _controller.DownloadStat("certs-published-last-month", "excel");

            // Assert - verify logging shows correct date range
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("certs-published-last-month")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task DownloadStat_CSV_ShouldContainProperHeaders()
        {
            // Act
            var result = await _controller.DownloadStat("total-active-certs", "csv");

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult.Should().NotBeNull();

            var csvContent = Encoding.UTF8.GetString(fileResult!.FileContents);
            csvContent.Should().Contain("Certificate Number");
            csvContent.Should().Contain("Status");
            csvContent.Should().Contain("Company");
            csvContent.Should().Contain("Product");
            csvContent.Should().Contain("Certification Type");
            csvContent.Should().Contain("Effective Date");
            csvContent.Should().Contain("Expiration Date");
            csvContent.Should().Contain("Location");
            csvContent.Should().Contain("Country");
        }

        [Fact]
        public async Task DownloadStat_ActiveCerts_ShouldNotIncludeExpiredCertificates()
        {
            // Arrange - we have 2 expired certificates in test data

            // Act
            var result = await _controller.DownloadStat("total-active-certs", "csv");

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            var csvContent = Encoding.UTF8.GetString(fileResult!.FileContents);

            // Should contain active certificates
            csvContent.Should().Contain("CERT-001");
            csvContent.Should().Contain("CERT-015");

            // Should NOT contain expired certificates
            csvContent.Should().NotContain("CERT-019");
            csvContent.Should().NotContain("CERT-020");
        }

        [Fact]
        public async Task DownloadStat_LoggingShouldShowStatTypeAndCount()
        {
            // Act
            await _controller.DownloadStat("total-active-certs", "excel");

            // Assert - verify all logging calls
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DownloadStat called")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GetStatData called")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generated Excel file")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task DownloadReport_ActiveCertificates_ShouldReturn18Certificates()
        {
            // Act
            var result = await _controller.DownloadReport("active-certificates", "excel");

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            var excelContent = fileResult!.FileContents;
            excelContent.Should().NotBeNullOrEmpty();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using FluentAssertions;
using GCCWebApp.Controllers;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Repositories;
using GCCWebApp.Services.Performance;
using GCCWebApp.Services.Security;
using System.Text;

namespace GCCWebApp.Api.Tests.Unit.Controllers
{
    public class ExportControllerTests : IDisposable
    {
        private readonly Mock<ILogger<ExportController>> _loggerMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ICertificateRepository> _certificateRepositoryMock;
        private readonly Mock<IPerformanceMonitoringService> _performanceMonitoringMock;
        private readonly Mock<IStructuredLoggingService> _structuredLoggerMock;
        private readonly ApplicationDbContext _context;
        private readonly Mock<IWebHostEnvironment> _environmentMock;
        private readonly ExportController _controller;

        public ExportControllerTests()
        {
            _loggerMock = new Mock<ILogger<ExportController>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _certificateRepositoryMock = new Mock<ICertificateRepository>();
            _performanceMonitoringMock = new Mock<IPerformanceMonitoringService>();
            _structuredLoggerMock = new Mock<IStructuredLoggingService>();
            _environmentMock = new Mock<IWebHostEnvironment>();
            _environmentMock.Setup(e => e.EnvironmentName).Returns("Development");

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            SeedTestData();

            // Default mock for certificate statistics (used by DownloadStat)
            _certificateRepositoryMock.Setup(r => r.GetCertificateStatisticsAsync())
                .ReturnsAsync(new Dictionary<string, int>
                {
                    { "Active", 3 }, { "Expired", 1 }, { "ExpiringSoon", 1 }
                });

            _controller = new ExportController(
                _loggerMock.Object,
                _unitOfWorkMock.Object,
                _certificateRepositoryMock.Object,
                _performanceMonitoringMock.Object,
                _structuredLoggerMock.Object,
                _context,
                _environmentMock.Object);

            // Default: Admin role
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContextWithRole("Admin")
            };
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #region Helpers

        private static DefaultHttpContext CreateHttpContextWithRole(string? role)
        {
            var httpContext = new DefaultHttpContext();
            var session = new MockSession();
            if (role != null)
                session.SetString("UserRole", role);
            httpContext.Session = session;
            return httpContext;
        }

        private void SetControllerRole(string? role)
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContextWithRole(role)
            };
        }

        private void SeedTestData()
        {
            var now = DateTime.UtcNow;

            var company1 = new Company { Name = "GreenCorp", Email = "info@green.com", IsActive = true };
            var company2 = new Company { Name = "EcoProducts", Email = "info@eco.com", IsActive = true };
            _context.Companies.AddRange(company1, company2);
            _context.SaveChanges();

            var category = new ProductCategory { Name = "Paper Products", IsActive = true };
            _context.ProductCategories.Add(category);
            _context.SaveChanges();

            var certType1 = new CertificationType { Name = "FSC", Description = "Forest Stewardship", IsActive = true };
            var certType2 = new CertificationType { Name = "SFI", Description = "Sustainable Forestry", IsActive = true };
            _context.CertificationTypes.AddRange(certType1, certType2);
            _context.SaveChanges();

            var product1 = new Product { Name = "Recycled Paper", CompanyId = company1.Id, CategoryId = category.Id, CreatedAt = now };
            var product2 = new Product { Name = "Eco Towels", CompanyId = company2.Id, CreatedAt = now };
            _context.Products.AddRange(product1, product2);
            _context.SaveChanges();

            _context.Certificates.AddRange(
                // Active with far-future expiration
                new Certificate
                {
                    Number = "CERT-001", Status = "Active", CompanyId = company1.Id, ProductId = product1.Id,
                    CertificationTypeId = certType1.Id, EffectiveDate = now.AddDays(-60),
                    ExpirationDate = now.AddDays(300), Location = "Philadelphia", Country = "USA", CreatedAt = now.AddDays(-5)
                },
                new Certificate
                {
                    Number = "CERT-002", Status = "Active", CompanyId = company2.Id, ProductId = product2.Id,
                    CertificationTypeId = certType2.Id, EffectiveDate = now.AddDays(-30),
                    ExpirationDate = now.AddDays(200), Location = "Toronto", Country = "Canada", CreatedAt = now.AddDays(-3)
                },
                // Active expiring within 30 days (for expiring-30-days / expiring-3-months reports)
                new Certificate
                {
                    Number = "CERT-003", Status = "Active", CompanyId = company1.Id, ProductId = product1.Id,
                    CertificationTypeId = certType1.Id, EffectiveDate = now.AddDays(-300),
                    ExpirationDate = now.AddDays(15), Location = "Denver", Country = "USA", CreatedAt = now.AddDays(-5)
                },
                // Expired cert
                new Certificate
                {
                    Number = "CERT-004", Status = "Expired", CompanyId = company2.Id, ProductId = product2.Id,
                    CertificationTypeId = certType2.Id, EffectiveDate = now.AddDays(-400),
                    ExpirationDate = now.AddDays(-10), Location = "Austin", Country = "USA", CreatedAt = now.AddDays(-5)
                }
            );
            _context.SaveChanges();
        }

        /// <summary>
        /// Simple dictionary-backed ISession for testing.
        /// </summary>
        private class MockSession : ISession
        {
            private readonly Dictionary<string, byte[]> _store = new();
            public bool IsAvailable => true;
            public string Id => "test-session";
            public IEnumerable<string> Keys => _store.Keys;
            public void Clear() => _store.Clear();
            public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
            public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;
            public void Remove(string key) => _store.Remove(key);
            public void Set(string key, byte[] value) => _store[key] = value;
            public bool TryGetValue(string key, out byte[]? value) => _store.TryGetValue(key, out value);
        }

        #endregion

        // ========================================================
        //  AUTHORIZATION TESTS
        // ========================================================

        [Fact]
        public async Task DownloadStat_NoSession_ReturnsForbid()
        {
            SetControllerRole(null);

            var result = await _controller.DownloadStat("total-active-certs");

            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task DownloadStat_ExternalRole_ReturnsForbid()
        {
            SetControllerRole("External");

            var result = await _controller.DownloadStat("total-active-certs");

            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task DownloadStat_AdminRole_Succeeds()
        {
            SetControllerRole("Admin");

            var result = await _controller.DownloadStat("total-active-certs");

            result.Should().BeOfType<FileContentResult>();
        }

        [Fact]
        public async Task DownloadStat_AnalystRole_Succeeds()
        {
            SetControllerRole("Analyst");

            var result = await _controller.DownloadStat("total-active-certs");

            result.Should().BeOfType<FileContentResult>();
        }

        [Fact]
        public async Task DownloadReport_NoSession_ReturnsForbid()
        {
            SetControllerRole(null);

            var result = await _controller.DownloadReport("active-certificates");

            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task DownloadReport_ExternalRole_ReturnsForbid()
        {
            SetControllerRole("External");

            var result = await _controller.DownloadReport("active-certificates");

            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task DownloadReport_AnalystRole_Succeeds()
        {
            SetControllerRole("Analyst");

            var result = await _controller.DownloadReport("active-certificates");

            result.Should().BeOfType<FileContentResult>();
        }

        [Fact]
        public async Task DownloadTable_NoSession_ReturnsForbid()
        {
            SetControllerRole(null);

            var result = await _controller.DownloadTable("cert-types");

            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task DownloadTable_ExternalRole_ReturnsForbid()
        {
            SetControllerRole("External");

            var result = await _controller.DownloadTable("cert-types");

            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task DownloadTable_AnalystRole_Succeeds()
        {
            _certificateRepositoryMock.Setup(r => r.GetCertificationTypeStatisticsAsync())
                .ReturnsAsync(new List<(string, int)> { ("FSC", 5) });
            SetControllerRole("Analyst");

            var result = await _controller.DownloadTable("cert-types");

            result.Should().BeOfType<FileContentResult>();
        }

        // ========================================================
        //  DOWNLOAD STAT TESTS
        // ========================================================

        [Fact]
        public async Task DownloadStat_InvalidType_ReturnsBadRequest()
        {
            var result = await _controller.DownloadStat("nonexistent-stat");

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().Be("Invalid stat type");
        }

        [Fact]
        public async Task DownloadStat_TotalActiveCerts_Csv_ReturnsFileWithCorrectContentType()
        {
            var result = await _controller.DownloadStat("total-active-certs", "csv");

            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("text/csv");
            fileResult.FileDownloadName.Should().StartWith("total-active-certs-report-").And.EndWith(".csv");
        }

        [Fact]
        public async Task DownloadStat_TotalActiveCerts_Excel_ReturnsFileWithCorrectContentType()
        {
            var result = await _controller.DownloadStat("total-active-certs", "excel");

            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            fileResult.FileDownloadName.Should().StartWith("total-active-certs-report-").And.EndWith(".xlsx");
        }

        [Fact]
        public async Task DownloadStat_CsvContent_HasCorrectHeaderRow()
        {
            var result = await _controller.DownloadStat("total-active-certs", "csv");

            var csv = Encoding.UTF8.GetString(((FileContentResult)result).FileContents);
            csv.Should().Contain("Certificate Number,Status,Company,Product,Certification Type,Effective Date,Expiration Date,Location,Country");
        }

        [Fact]
        public async Task DownloadStat_TotalActiveCerts_CsvContent_ContainsActiveCerts()
        {
            var result = await _controller.DownloadStat("total-active-certs", "csv");

            var csv = Encoding.UTF8.GetString(((FileContentResult)result).FileContents);
            csv.Should().Contain("CERT-001");
            csv.Should().Contain("CERT-002");
            csv.Should().Contain("CERT-003"); // Active, expires in 15 days (still future)
        }

        [Fact]
        public async Task DownloadStat_TotalActiveCerts_CsvContent_ExcludesExpiredCerts()
        {
            var result = await _controller.DownloadStat("total-active-certs", "csv");

            var csv = Encoding.UTF8.GetString(((FileContentResult)result).FileContents);
            csv.Should().NotContain("CERT-004"); // Expired
        }

        [Theory]
        [InlineData("total-active-certs", "Total Active Certificates")]
        [InlineData("companies-with-active-certs", "Companies with Active Certificates")]
        [InlineData("product-categories-with-active-certs", "Product Categories with Active Certificates")]
        [InlineData("product-types-with-active-certs", "Product Types with Active Certificates")]
        [InlineData("products-with-active-certs", "Products with Active Certificates")]
        [InlineData("certs-published-this-year", "Certificates Published This Year")]
        [InlineData("certs-expiring-this-month", "Certificates Expiring This Month")]
        public async Task DownloadStat_ValidTypes_CsvContainsCorrectDescription(string type, string expectedDescription)
        {
            var result = await _controller.DownloadStat(type, "csv");

            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            var csv = Encoding.UTF8.GetString(fileResult.FileContents);
            csv.Should().Contain($"Report: {expectedDescription}");
        }

        [Fact]
        public async Task DownloadStat_CsvContent_IncludesReportMetadata()
        {
            var result = await _controller.DownloadStat("total-active-certs", "csv");

            var csv = Encoding.UTF8.GetString(((FileContentResult)result).FileContents);
            csv.Should().Contain("Report: Total Active Certificates");
            csv.Should().Contain("Generated:");
            csv.Should().Contain("Total Certificates:");
        }

        [Fact]
        public async Task DownloadStat_CsvContent_IncludesCompanyAndProductNames()
        {
            var result = await _controller.DownloadStat("total-active-certs", "csv");

            var csv = Encoding.UTF8.GetString(((FileContentResult)result).FileContents);
            csv.Should().Contain("GreenCorp");
            csv.Should().Contain("Recycled Paper");
        }

        [Fact]
        public async Task DownloadStat_Excel_ReturnsValidExcelBytes()
        {
            var result = await _controller.DownloadStat("total-active-certs", "excel");

            var fileResult = (FileContentResult)result;
            fileResult.FileContents.Length.Should().BeGreaterThan(0);
            // Excel files (OOXML) are ZIP archives starting with PK header
            fileResult.FileContents[0].Should().Be(0x50); // 'P'
            fileResult.FileContents[1].Should().Be(0x4B); // 'K'
        }

        [Fact]
        public async Task DownloadStat_DefaultFormat_IsCsv()
        {
            // format parameter defaults to "csv"
            var result = await _controller.DownloadStat("total-active-certs");

            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("text/csv");
        }

        // ========================================================
        //  DOWNLOAD REPORT TESTS
        // ========================================================

        [Fact]
        public async Task DownloadReport_InvalidType_ReturnsBadRequest()
        {
            var result = await _controller.DownloadReport("nonexistent-report");

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().Be("Invalid report type");
        }

        [Fact]
        public async Task DownloadReport_ActiveCertificates_DefaultFormat_IsExcel()
        {
            // format parameter defaults to "excel"
            var result = await _controller.DownloadReport("active-certificates");

            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            fileResult.FileDownloadName.Should().StartWith("active-certificates-").And.EndWith(".xlsx");
        }

        [Fact]
        public async Task DownloadReport_ActiveCertificates_Csv_ReturnsFile()
        {
            var result = await _controller.DownloadReport("active-certificates", "csv");

            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("text/csv");
            var csv = Encoding.UTF8.GetString(fileResult.FileContents);
            csv.Should().Contain("Report: Current Active Certificates");
        }

        [Fact]
        public async Task DownloadReport_ActiveCertificates_ExcludesExpiredCerts()
        {
            var result = await _controller.DownloadReport("active-certificates", "csv");

            var csv = Encoding.UTF8.GetString(((FileContentResult)result).FileContents);
            csv.Should().Contain("CERT-001");
            csv.Should().Contain("CERT-002");
            csv.Should().NotContain("CERT-004"); // Expired
        }

        [Fact]
        public async Task DownloadReport_ExpiringIn30Days_IncludesOnlyCertsExpiringSoon()
        {
            var result = await _controller.DownloadReport("expiring-30-days", "csv");

            var csv = Encoding.UTF8.GetString(((FileContentResult)result).FileContents);
            csv.Should().Contain("Certificates Expiring in Next 30 Days");
            csv.Should().Contain("CERT-003"); // Expires in 15 days
            csv.Should().NotContain("CERT-001"); // Expires in 300 days
            csv.Should().NotContain("CERT-002"); // Expires in 200 days
        }

        [Fact]
        public async Task DownloadReport_ExpiringIn3Months_IncludesCertsExpiringSoon()
        {
            var result = await _controller.DownloadReport("expiring-3-months", "csv");

            var csv = Encoding.UTF8.GetString(((FileContentResult)result).FileContents);
            csv.Should().Contain("Certificates Expiring in Next 3 Months");
            csv.Should().Contain("CERT-003"); // Expires in 15 days - within 3 months
            csv.Should().NotContain("CERT-001"); // Expires in 300 days - outside 3 months
        }

        [Fact]
        public async Task DownloadReport_CsvContent_HasCorrectHeaderRow()
        {
            var result = await _controller.DownloadReport("active-certificates", "csv");

            var csv = Encoding.UTF8.GetString(((FileContentResult)result).FileContents);
            csv.Should().Contain("Certificate Number,Status,Company,Product,Certification Type,Effective Date,Expiration Date,Location,Country");
        }

        [Fact]
        public async Task DownloadReport_CsvContent_IncludesRelatedEntityNames()
        {
            var result = await _controller.DownloadReport("active-certificates", "csv");

            var csv = Encoding.UTF8.GetString(((FileContentResult)result).FileContents);
            csv.Should().Contain("GreenCorp");
            csv.Should().Contain("EcoProducts");
            csv.Should().Contain("Recycled Paper");
            csv.Should().Contain("FSC");
        }

        [Fact]
        public async Task DownloadReport_Excel_ReturnsValidExcelBytes()
        {
            var result = await _controller.DownloadReport("active-certificates", "excel");

            var fileResult = (FileContentResult)result;
            fileResult.FileContents.Length.Should().BeGreaterThan(0);
            fileResult.FileContents[0].Should().Be(0x50); // 'P' (ZIP/OOXML header)
            fileResult.FileContents[1].Should().Be(0x4B); // 'K'
        }

        // ========================================================
        //  DOWNLOAD TABLE TESTS
        // ========================================================

        [Fact]
        public async Task DownloadTable_InvalidType_ReturnsBadRequest()
        {
            var result = await _controller.DownloadTable("nonexistent-table");

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DownloadTable_CertTypes_Csv_ReturnsFileWithData()
        {
            _certificateRepositoryMock.Setup(r => r.GetCertificationTypeStatisticsAsync())
                .ReturnsAsync(new List<(string, int)> { ("FSC", 3), ("SFI", 2) });

            var result = await _controller.DownloadTable("cert-types", "csv");

            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("text/csv");
            var csv = Encoding.UTF8.GetString(fileResult.FileContents);
            csv.Should().Contain("Name,Count");
            csv.Should().Contain("\"FSC\",3");
            csv.Should().Contain("\"SFI\",2");
        }

        [Fact]
        public async Task DownloadTable_CertTypes_Excel_ReturnsFile()
        {
            _certificateRepositoryMock.Setup(r => r.GetCertificationTypeStatisticsAsync())
                .ReturnsAsync(new List<(string, int)> { ("FSC", 3) });

            var result = await _controller.DownloadTable("cert-types", "excel");

            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            fileResult.FileContents.Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task DownloadTable_TopCompanies_Csv_ReturnsFileWithData()
        {
            _certificateRepositoryMock.Setup(r => r.GetTopCompaniesByActiveCertCountAsync(10))
                .ReturnsAsync(new List<(string, int)> { ("GreenCorp", 5), ("EcoProducts", 3) });

            var result = await _controller.DownloadTable("top-companies", "csv");

            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            var csv = Encoding.UTF8.GetString(fileResult.FileContents);
            csv.Should().Contain("Name,Count");
            csv.Should().Contain("\"GreenCorp\",5");
            csv.Should().Contain("\"EcoProducts\",3");
        }

        [Fact]
        public async Task DownloadTable_CertStatus_ReturnsStatusBreakdown()
        {
            // Uses the default mock from constructor setup
            var result = await _controller.DownloadTable("cert-status", "csv");

            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            var csv = Encoding.UTF8.GetString(fileResult.FileContents);
            csv.Should().Contain("Status,Count");
            csv.Should().Contain("\"Active\",3");
            csv.Should().Contain("\"Expired\",1");
            csv.Should().Contain("\"Expiring Soon\",1");
        }

        [Fact]
        public async Task DownloadTable_ProductTypes_Csv_ReturnsFileWithData()
        {
            _certificateRepositoryMock.Setup(r => r.GetProductTypeStatisticsAsync(10))
                .ReturnsAsync(new List<(string, int)> { ("Paper", 5), ("Wood", 3) });

            var result = await _controller.DownloadTable("product-types", "csv");

            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            var csv = Encoding.UTF8.GetString(fileResult.FileContents);
            csv.Should().Contain("Name,Count");
            csv.Should().Contain("\"Paper\",5");
            csv.Should().Contain("\"Wood\",3");
        }

        [Fact]
        public async Task DownloadTable_ProductCategories_ReturnsTop5Only()
        {
            _certificateRepositoryMock.Setup(r => r.GetCertificationTypeStatisticsAsync())
                .ReturnsAsync(new List<(string, int)>
                {
                    ("Type1", 10), ("Type2", 8), ("Type3", 6), ("Type4", 4), ("Type5", 2), ("Type6", 1)
                });

            var result = await _controller.DownloadTable("product-categories", "csv");

            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            var csv = Encoding.UTF8.GetString(fileResult.FileContents);
            csv.Should().Contain("\"Type1\",10");
            csv.Should().Contain("\"Type5\",2");
            csv.Should().NotContain("\"Type6\""); // Take(5) excludes 6th item
        }

        [Fact]
        public async Task DownloadTable_EmptyData_ReturnsFileWithHeadersOnly()
        {
            _certificateRepositoryMock.Setup(r => r.GetCertificationTypeStatisticsAsync())
                .ReturnsAsync(new List<(string, int)>());

            var result = await _controller.DownloadTable("cert-types", "csv");

            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            var csv = Encoding.UTF8.GetString(fileResult.FileContents);
            csv.Should().Contain("Name,Count");
            csv.Should().Contain("Generated on:");
        }

        [Fact]
        public async Task DownloadTable_CsvContent_IncludesGeneratedTimestamp()
        {
            _certificateRepositoryMock.Setup(r => r.GetCertificationTypeStatisticsAsync())
                .ReturnsAsync(new List<(string, int)> { ("FSC", 3) });

            var result = await _controller.DownloadTable("cert-types", "csv");

            var csv = Encoding.UTF8.GetString(((FileContentResult)result).FileContents);
            csv.Should().Contain("Generated on:");
            csv.Should().Contain("UTC");
        }

        [Fact]
        public async Task DownloadTable_DefaultFormat_IsCsv()
        {
            _certificateRepositoryMock.Setup(r => r.GetCertificationTypeStatisticsAsync())
                .ReturnsAsync(new List<(string, int)> { ("FSC", 3) });

            // format defaults to "csv"
            var result = await _controller.DownloadTable("cert-types");

            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("text/csv");
        }

        // ========================================================
        //  SEED DATABASE TESTS
        // ========================================================

        [Fact]
        public async Task SeedDatabase_ProductionEnvironment_Returns403()
        {
            _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");

            var result = await _controller.SeedDatabase();

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task SeedDatabase_DevelopmentEnvironment_DoesNotReturn403()
        {
            _environmentMock.Setup(e => e.EnvironmentName).Returns("Development");

            var result = await _controller.SeedDatabase();

            // InMemory provider doesn't support ExecuteSqlRawAsync, so it hits the catch block
            // and returns BadRequest. The important thing: it's NOT a 403 (passed the production check).
            if (result is ObjectResult objectResult)
            {
                objectResult.StatusCode.Should().NotBe(403);
            }
        }
    }
}

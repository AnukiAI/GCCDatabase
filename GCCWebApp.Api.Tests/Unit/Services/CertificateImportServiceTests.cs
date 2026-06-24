using FluentAssertions;
using GCCWebApp.Api.Tests.TestUtilities;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Models.Import;
using GCCWebApp.Services.Audit;
using GCCWebApp.Services.Import;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using GCCWebApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GCCWebApp.Api.Tests.Unit.Services
{
    public class CertificateImportServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly IMemoryCache _cache;
        private readonly Mock<ILogger<CertificateImportService>> _mockLogger;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly Mock<IFileUploadService> _mockFileUploadService;
        private readonly GCCWebApp.Services.IUploadPathResolver _uploadPathsStub;
        private readonly CertificateImportService _service;

        public CertificateImportServiceTests()
        {
            // Use custom context that suppresses transaction warnings
            // (ApplyImportAsync uses BeginTransactionAsync which InMemory
            // doesn't support - we need to ignore this warning)
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(
                    InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _context = new ApplicationDbContext(options);

            _mockAuditService = new Mock<IAuditService>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _mockLogger = new Mock<ILogger<CertificateImportService>>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockEnvironment.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
            _mockFileUploadService = new Mock<IFileUploadService>();
            var mockProductIdService = new Mock<IProductIdService>();
            _uploadPathsStub = Moq.Mock.Of<GCCWebApp.Services.IUploadPathResolver>(r =>
                r.CertificatesDir == Path.GetTempPath() && r.ImagesDir == Path.GetTempPath());
            _service = new CertificateImportService(
                _context, _mockAuditService.Object, _cache, _mockLogger.Object,
                _mockEnvironment.Object, _mockFileUploadService.Object, mockProductIdService.Object,
                _uploadPathsStub);

            SeedData();
        }

        public void Dispose()
        {
            _cache.Dispose();
            _context.Dispose();
        }

        private void SeedData()
        {
            var category = new ProductCategory
            {
                Id = 1, Name = "General", IsActive = true
            };
            _context.ProductCategories.Add(category);

            var company = new Company
            {
                Id = 1, Name = "Test Company", Email = "test@test.com",
                IsActive = true, IsDeleted = false
            };
            _context.Companies.Add(company);

            var productType = new ProductType
            {
                Id = 1, Name = "Cleaning", CategoryId = 1,
                Level = 0, SortOrder = 0, IsActive = true
            };
            _context.ProductTypes.Add(productType);

            var product = new Product
            {
                Id = 1, Name = "Test Product", CompanyId = 1,
                CategoryId = 1, IsActive = true, IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Products.Add(product);

            var certType1 = new CertificationType
            {
                Id = 1, Name = "UL 2809",
                Description = "Recycled Content", IsActive = true
            };
            var certType2 = new CertificationType
            {
                Id = 2, Name = "Certified Energy Savings",
                Description = "Energy", IsActive = true
            };
            _context.CertificationTypes.AddRange(certType1, certType2);

            var tag1 = new Tag
            {
                Id = 1, Name = "eco-friendly",
                TagType = "external", IsActive = true
            };
            var tag2 = new Tag
            {
                Id = 2, Name = "recycled",
                TagType = "external", IsActive = true
            };
            _context.Tags.AddRange(tag1, tag2);

            var cert = new Certificate
            {
                Id = 1, Number = "25-0001", CompanyId = 1, ProductId = 1,
                CertificationTypeId = 1, Status = "Client Approved - Paid",
                EffectiveDate = new DateTime(2025, 1, 1),
                ExpirationDate = new DateTime(2026, 1, 1),
                Location = "Philadelphia PA", Country = "USA",
                Partner = "Amazon", CreatedAt = DateTime.UtcNow,
                Tags = new List<Tag>()
            };
            _context.Certificates.Add(cert);
            _context.SaveChanges();
        }

        #region Helpers

        private static IFormFile CreateCsvFormFile(
            string csvContent, string fileName = "import.csv")
        {
            var bytes = Encoding.UTF8.GetBytes(csvContent);
            var formFile = new Mock<IFormFile>();
            formFile.Setup(f => f.FileName).Returns(fileName);
            formFile.Setup(f => f.Length).Returns(bytes.Length);
            formFile.Setup(f => f.OpenReadStream())
                .Returns(() => new MemoryStream(bytes));
            return formFile.Object;
        }

        private static string BuildCsv(string[] headers, params string[][] rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers));
            foreach (var row in rows)
            {
                // Quote fields that contain commas for valid CSV
                sb.AppendLine(string.Join(",", row.Select(f =>
                    f.Contains(',') ? $"\"{f}\"" : f)));
            }
            return sb.ToString();
        }

        private static readonly string[] StdHeaders =
        {
            "CERTIFICATE_NUMBER", "COMPANY_NAME", "PRODUCT_NAME",
            "CERTIFICATION_TYPE", "STATUS", "EFFECTIVE_DATE",
            "EXPIRATION_DATE", "LOCATION", "COUNTRY", "PARTNER"
        };

        private static readonly string[] ExtHeaders =
        {
            "CERTIFICATE_NUMBER", "COMPANY_NAME", "PRODUCT_NAME",
            "CERTIFICATION_TYPE", "STATUS", "EFFECTIVE_DATE",
            "EXPIRATION_DATE", "LOCATION", "COUNTRY", "PARTNER",
            "COMPANY_EMAIL", "COMPANY_LOCATION", "COMPANY_COUNTRY",
            "COMPANY_WEBSITE", "PRODUCT_TYPE", "CERTIFICATION_RESULTS",
            "TAGS", "ASIN", "UPC", "GTIN", "EAN"
        };

        private static string[] StdRow(
            string certNum = "25-9999", string company = "Test Company",
            string product = "Test Product", string certType = "UL 2809",
            string status = "Client Approved - Paid", string effDate = "2025-01-01",
            string expDate = "2026-01-01", string location = "NYC",
            string country = "USA", string partner = "Amazon")
        {
            return new[] { certNum, company, product, certType, status,
                effDate, expDate, location, country, partner };
        }

        private CertificateImportPreviewResult CreatePreviewWithNewRow(
            string certNum, int companyId = 1, int productId = 1,
            int certTypeId = 1, string status = "Client Approved - Paid",
            string partner = "Amazon",
            string? certResults = null,
            List<int>? tagIds = null,
            List<string>? tagsToCreate = null)
        {
            return new CertificateImportPreviewResult
            {
                ImportId = Guid.NewGuid().ToString(),
                NewCompanyNames = new List<string>(),
                NewProductNames = new List<string>(),
                NewProductTypeNames = new List<string>(),
                NewTagNames = new List<string>(),
                NewRows = new List<CertificateImportRow>
                {
                    new CertificateImportRow
                    {
                        RowNumber = 2,
                        CertificateNumber = certNum,
                        CompanyName = "Test Company",
                        CompanyId = companyId,
                        ProductName = "Test Product",
                        ProductId = productId,
                        CertificationType = "UL 2809",
                        CertificationTypeId = certTypeId,
                        Status = status,
                        Partner = partner,
                        CertificationResults = certResults,
                        RowStatus = ImportRowStatus.New,
                        TagIds = tagIds ?? new List<int>(),
                        TagsToCreate = tagsToCreate ?? new List<string>()
                    }
                },
                ModifiedRows = new List<CertificateImportRow>()
            };
        }

        #endregion

        // ==================== PreviewImportAsync Tests ====================

        #region PreviewImportAsync - New Certificates

        [Fact]
        public async Task PreviewImportAsync_NewCertificate_CategorizesAsNew()
        {
            var csv = BuildCsv(StdHeaders, StdRow());
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.TotalRows.Should().Be(1);
            result.NewCertificates.Should().Be(1);
            result.NewRows.Should().HaveCount(1);
            result.NewRows[0].CertificateNumber.Should().Be("25-9999");
            result.NewRows[0].RowStatus.Should().Be(ImportRowStatus.New);
        }

        [Fact]
        public async Task PreviewImportAsync_MultipleNewCerts_CountsCorrectly()
        {
            var csv = BuildCsv(StdHeaders,
                StdRow("25-9998"),
                StdRow("25-9997", partner: "Novi"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.TotalRows.Should().Be(2);
            result.NewCertificates.Should().Be(2);
            result.ValidRows.Should().Be(2);
            result.InvalidRows.Should().Be(0);
        }

        #endregion

        #region PreviewImportAsync - Existing Certificates

        [Fact]
        public async Task PreviewImportAsync_ExistingCert_RecognizedAsExisting()
        {
            var csv = BuildCsv(StdHeaders,
                StdRow("25-0001", location: "Philadelphia PA"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            // Cert should be found by number and categorized as
            // existing (Unchanged or Modified), not New or Error
            result.NewCertificates.Should().Be(0);
            result.InvalidRows.Should().Be(0);
            (result.UnchangedCertificates + result.ModifiedCertificates)
                .Should().Be(1);
        }

        [Fact]
        public async Task PreviewImportAsync_ExistingCertWithChanges_CategorizesAsModified()
        {
            var csv = BuildCsv(StdHeaders,
                StdRow("25-0001", location: "Chicago IL"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.ModifiedCertificates.Should().Be(1);
            result.ModifiedRows.Should().HaveCount(1);
            result.ModifiedRows[0].Changes
                .Should().Contain(c => c.FieldName == "Location");
        }

        [Fact]
        public async Task PreviewImportAsync_StatusChange_DetectsChange()
        {
            var csv = BuildCsv(StdHeaders,
                StdRow("25-0001", status: "Expired",
                    location: "Philadelphia PA"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.ModifiedCertificates.Should().Be(1);
            result.ModifiedRows[0].Changes
                .Should().Contain(c => c.FieldName == "Status");
        }

        #endregion

        #region PreviewImportAsync - Validation Errors

        [Fact]
        public async Task PreviewImportAsync_MissingCertNumber_ReturnsError()
        {
            var csv = BuildCsv(StdHeaders,
                StdRow(certNum: "", company: "Test Company"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.InvalidRows.Should().Be(1);
            result.ErrorRows[0].ErrorMessages
                .Should().Contain(m => m.Contains("Certificate Number"));
        }

        [Fact]
        public async Task PreviewImportAsync_MissingCompanyName_ReturnsError()
        {
            var csv = BuildCsv(StdHeaders, StdRow(company: ""));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.InvalidRows.Should().Be(1);
            result.ErrorRows[0].ErrorMessages
                .Should().Contain(m => m.Contains("Company Name"));
        }

        [Fact]
        public async Task PreviewImportAsync_MissingProductName_ReturnsError()
        {
            var csv = BuildCsv(StdHeaders, StdRow(product: ""));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.InvalidRows.Should().Be(1);
            result.ErrorRows[0].ErrorMessages
                .Should().Contain(m => m.Contains("Product Name"));
        }

        [Fact]
        public async Task PreviewImportAsync_MissingCertType_ReturnsError()
        {
            var csv = BuildCsv(StdHeaders, StdRow(certType: ""));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.InvalidRows.Should().Be(1);
            result.ErrorRows[0].ErrorMessages
                .Should().Contain(m => m.Contains("Certification Type"));
        }

        [Fact]
        public async Task PreviewImportAsync_CertTypeNotFound_ReturnsError()
        {
            var csv = BuildCsv(StdHeaders,
                StdRow(certType: "NonExistent Type"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.InvalidRows.Should().Be(1);
            result.ErrorRows[0].ErrorMessages
                .Should().Contain(m => m.Contains("Certification Type not found"));
        }

        [Fact]
        public async Task PreviewImportAsync_InvalidEffectiveDate_ReturnsError()
        {
            var csv = BuildCsv(StdHeaders, StdRow(effDate: "not-a-date"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.InvalidRows.Should().Be(1);
            result.ErrorRows[0].ErrorMessages
                .Should().Contain(m => m.Contains("Invalid Effective Date"));
        }

        [Fact]
        public async Task PreviewImportAsync_InvalidExpirationDate_ReturnsError()
        {
            var csv = BuildCsv(StdHeaders, StdRow(expDate: "bad-date"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.InvalidRows.Should().Be(1);
            result.ErrorRows[0].ErrorMessages
                .Should().Contain(m => m.Contains("Invalid Expiration Date"));
        }

        #endregion

        #region PreviewImportAsync - Email Validation

        [Fact]
        public async Task PreviewImportAsync_InvalidEmail_ReturnsError()
        {
            var csv = BuildCsv(ExtHeaders, new[]
            {
                "25-9999", "Test Company", "Test Product", "UL 2809",
                "Active", "2025-01-01", "2026-01-01", "NYC", "USA",
                "Amazon", "not-an-email", "", "", "", "", "", "",
                "", "", "", ""
            });
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.InvalidRows.Should().Be(1);
            result.ErrorRows[0].ErrorMessages
                .Should().Contain(m => m.Contains("Invalid Company Email"));
        }

        [Fact]
        public async Task PreviewImportAsync_ValidEmail_NoEmailError()
        {
            var csv = BuildCsv(ExtHeaders, new[]
            {
                "25-9999", "Test Company", "Test Product", "UL 2809",
                "Active", "2025-01-01", "2026-01-01", "NYC", "USA",
                "Amazon", "valid@email.com", "", "", "", "", "", "",
                "", "", "", ""
            });
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.ErrorRows.Should().NotContain(e =>
                e.ErrorMessages.Any(m => m.Contains("Invalid Company Email")));
        }

        #endregion

        #region PreviewImportAsync - CertificationResults

        [Fact]
        public async Task PreviewImportAsync_CertResults_ParsedCorrectly()
        {
            var csv = BuildCsv(ExtHeaders, new[]
            {
                "25-9999", "Test Company", "Test Product", "UL 2809",
                "Active", "2025-01-01", "2026-01-01", "NYC", "USA",
                "Amazon", "", "", "", "", "", "95.5% recycled", "",
                "", "", "", ""
            });
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.NewRows.Should().HaveCount(1);
            result.NewRows[0].CertificationResults
                .Should().Be("95.5% recycled");
        }

        [Fact]
        public async Task PreviewImportAsync_CertResultsTooLong_ReturnsError()
        {
            var longResult = new string('x', 1001);
            var csv = BuildCsv(ExtHeaders, new[]
            {
                "25-9999", "Test Company", "Test Product", "UL 2809",
                "Active", "2025-01-01", "2026-01-01", "NYC", "USA",
                "Amazon", "", "", "", "", "", longResult, "",
                "", "", "", ""
            });
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.InvalidRows.Should().Be(1);
            result.ErrorRows[0].ErrorMessages.Should().Contain(
                m => m.Contains("Certification Results exceeds maximum length"));
        }

        #endregion

        #region PreviewImportAsync - Auto-Creation Tracking

        [Fact]
        public async Task PreviewImportAsync_NewCompany_TracksForCreation()
        {
            var csv = BuildCsv(StdHeaders,
                StdRow(company: "Brand New Company"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.CompaniesToCreate.Should().Be(1);
            result.NewCompanyNames.Should().Contain("Brand New Company");
        }

        [Fact]
        public async Task PreviewImportAsync_NewProduct_TracksForCreation()
        {
            var csv = BuildCsv(StdHeaders,
                StdRow(product: "Brand New Product"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.ProductsToCreate.Should().Be(1);
            result.NewProductNames.Should().Contain("Brand New Product");
        }

        [Fact]
        public async Task PreviewImportAsync_NewProductType_TracksForCreation()
        {
            var csv = BuildCsv(ExtHeaders, new[]
            {
                "25-9999", "Test Company", "Test Product", "UL 2809",
                "Active", "2025-01-01", "2026-01-01", "NYC", "USA",
                "Amazon", "", "", "", "", "New Product Type", "", "",
                "", "", "", ""
            });
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.ProductTypesToCreate.Should().Be(1);
            result.NewProductTypeNames
                .Should().Contain("New Product Type");
        }

        [Fact]
        public async Task PreviewImportAsync_NewTags_TracksForCreation()
        {
            var csv = BuildCsv(ExtHeaders, new[]
            {
                "25-9999", "Test Company", "Test Product", "UL 2809",
                "Active", "2025-01-01", "2026-01-01", "NYC", "USA",
                "Amazon", "", "", "", "", "", "",
                "new-tag-1,new-tag-2", "", "", "", ""
            });
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.TagsToCreate.Should().Be(2);
            result.NewTagNames.Should().Contain("new-tag-1");
            result.NewTagNames.Should().Contain("new-tag-2");
        }

        [Fact]
        public async Task PreviewImportAsync_ExistingTag_ResolvedCorrectly()
        {
            var csv = BuildCsv(ExtHeaders, new[]
            {
                "25-9999", "Test Company", "Test Product", "UL 2809",
                "Active", "2025-01-01", "2026-01-01", "NYC", "USA",
                "Amazon", "", "", "", "", "", "", "eco-friendly",
                "", "", "", ""
            });
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.TagsToCreate.Should().Be(0);
            result.NewRows[0].TagIds.Should().Contain(1);
        }

        [Fact]
        public async Task PreviewImportAsync_MixedTags_CorrectlyCategorized()
        {
            var csv = BuildCsv(ExtHeaders, new[]
            {
                "25-9999", "Test Company", "Test Product", "UL 2809",
                "Active", "2025-01-01", "2026-01-01", "NYC", "USA",
                "Amazon", "", "", "", "", "", "",
                "eco-friendly,brand-new-tag", "", "", "", ""
            });
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.TagsToCreate.Should().Be(1);
            result.NewTagNames.Should().Contain("brand-new-tag");
            result.NewRows[0].TagIds.Should().Contain(1);
            result.NewRows[0].TagsToCreate
                .Should().Contain("brand-new-tag");
        }

        [Fact]
        public async Task PreviewImportAsync_ExistingProductType_ResolvedCorrectly()
        {
            var csv = BuildCsv(ExtHeaders, new[]
            {
                "25-9999", "Test Company", "Test Product", "UL 2809",
                "Active", "2025-01-01", "2026-01-01", "NYC", "USA",
                "Amazon", "", "", "", "", "Cleaning", "", "",
                "", "", "", ""
            });
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.ProductTypesToCreate.Should().Be(0);
            result.NewRows[0].ProductTypeId.Should().Be(1);
        }

        #endregion

        #region PreviewImportAsync - Empty/Skip Handling

        [Fact]
        public async Task PreviewImportAsync_AllRequiredFieldsEmpty_SkipsRow()
        {
            var csv = BuildCsv(StdHeaders,
                new[] { "", "", "", "", "Active", "2025-01-01",
                    "2026-01-01", "NYC", "USA", "Amazon" });
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.TotalRows.Should().Be(1);
            result.ValidRows.Should().Be(0);
            result.InvalidRows.Should().Be(0);
        }

        #endregion

        #region PreviewImportAsync - Critical Errors

        [Fact]
        public async Task PreviewImportAsync_NoCategories_ReturnsCriticalError()
        {
            // Use a fresh context with no categories seeded
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(
                    InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            using var freshContext = new ApplicationDbContext(options);

            // Seed only minimal data - NO ProductCategories
            freshContext.Companies.Add(new Company
            {
                Id = 1, Name = "Test", IsActive = true, IsDeleted = false
            });
            freshContext.CertificationTypes.Add(new CertificationType
            {
                Id = 1, Name = "UL 2809", IsActive = true
            });
            await freshContext.SaveChangesAsync();

            var freshService = new CertificateImportService(
                freshContext, _mockAuditService.Object, _cache,
                _mockLogger.Object, _mockEnvironment.Object, _mockFileUploadService.Object,
                new Mock<IProductIdService>().Object, _uploadPathsStub);

            var csv = BuildCsv(StdHeaders, StdRow());
            var file = CreateCsvFormFile(csv);

            var result = await freshService.PreviewImportAsync(
                file, 1, "admin");

            result.HasCriticalErrors.Should().BeTrue();
            result.ErrorRows.Should().Contain(e =>
                e.ErrorMessages.Any(
                    m => m.Contains("No product categories exist")));
        }

        [Fact]
        public async Task PreviewImportAsync_AllRowsError_SetsCriticalErrors()
        {
            var csv = BuildCsv(StdHeaders,
                StdRow(certType: "Nonexistent Type"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.HasCriticalErrors.Should().BeTrue();
        }

        [Fact]
        public async Task PreviewImportAsync_SomeRowsValid_NoCriticalErrors()
        {
            var csv = BuildCsv(StdHeaders,
                StdRow(),
                StdRow("25-9998", certType: "Nonexistent Type"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.HasCriticalErrors.Should().BeFalse();
            result.HasErrors.Should().BeTrue();
        }

        #endregion

        #region PreviewImportAsync - Defaults

        [Fact]
        public async Task PreviewImportAsync_EmptyStatus_DefaultsToActive()
        {
            var csv = BuildCsv(StdHeaders, StdRow(status: ""));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.NewRows[0].Status.Should().Be("Draft");
        }

        [Fact]
        public async Task PreviewImportAsync_EmptyPartner_DefaultsToNone()
        {
            var csv = BuildCsv(StdHeaders, StdRow(partner: ""));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.NewRows[0].Partner.Should().Be("None");
        }

        #endregion

        #region PreviewImportAsync - Column Name Flexibility

        [Fact]
        public async Task PreviewImportAsync_PascalCaseHeaders_ParsesCorrectly()
        {
            var headers = new[]
            {
                "CertificateNumber", "CompanyName", "ProductName",
                "CertificationType", "Status", "EffectiveDate",
                "ExpirationDate", "Location", "Country", "Partner"
            };
            var csv = BuildCsv(headers, StdRow());
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.NewRows.Should().HaveCount(1);
            result.NewRows[0].CertificateNumber.Should().Be("25-9999");
        }

        [Fact]
        public async Task PreviewImportAsync_LowercaseHeaders_ParsesCorrectly()
        {
            var headers = new[]
            {
                "certificate_number", "company_name", "product_name",
                "certification_type", "status", "effective_date",
                "expiration_date", "location", "country", "partner"
            };
            var csv = BuildCsv(headers, StdRow());
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.NewRows.Should().HaveCount(1);
            result.NewRows[0].CertificateNumber.Should().Be("25-9999");
        }

        #endregion

        #region PreviewImportAsync - Caching

        [Fact]
        public async Task PreviewImportAsync_CachesResult()
        {
            var csv = BuildCsv(StdHeaders, StdRow());
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            var cached = await _service.GetCachedPreviewAsync(result.ImportId);
            cached.Should().NotBeNull();
            cached!.ImportId.Should().Be(result.ImportId);
        }

        #endregion

        #region PreviewImportAsync - Metadata

        [Fact]
        public async Task PreviewImportAsync_SetsMetadata()
        {
            var csv = BuildCsv(StdHeaders, StdRow());
            var file = CreateCsvFormFile(csv, "my-import.csv");

            var result = await _service.PreviewImportAsync(
                file, 42, "testuser");

            result.FileName.Should().Be("my-import.csv");
            result.UploadedByUserId.Should().Be(42);
            result.UploadedByUsername.Should().Be("testuser");
            result.ImportId.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region PreviewImportAsync - Mixed Results

        [Fact]
        public async Task PreviewImportAsync_MixedRows_CorrectlyCategorized()
        {
            var csv = BuildCsv(StdHeaders,
                StdRow(),
                StdRow("25-0001", location: "Philadelphia PA"),
                StdRow("25-9998", certType: ""));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.TotalRows.Should().Be(3);
            result.NewCertificates.Should().Be(1);
            // Existing cert may show as Modified or Unchanged depending
            // on field comparison; at minimum it's not New or Error
            (result.UnchangedCertificates + result.ModifiedCertificates)
                .Should().Be(1);
            result.InvalidRows.Should().Be(1);
        }

        #endregion

        #region PreviewImportAsync - Change Detection

        [Fact]
        public async Task PreviewImportAsync_PartnerChange_DetectsChange()
        {
            var csv = BuildCsv(StdHeaders,
                StdRow("25-0001", partner: "Novi",
                    location: "Philadelphia PA"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.ModifiedCertificates.Should().Be(1);
            result.ModifiedRows[0].Changes
                .Should().Contain(c => c.FieldName == "Partner");
        }

        [Fact]
        public async Task PreviewImportAsync_DateChange_DetectsChange()
        {
            var csv = BuildCsv(StdHeaders,
                StdRow("25-0001", effDate: "2025-06-01",
                    location: "Philadelphia PA"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.ModifiedCertificates.Should().Be(1);
            result.ModifiedRows[0].Changes
                .Should().Contain(c => c.FieldName == "EffectiveDate");
        }

        [Fact]
        public async Task PreviewImportAsync_CertResultsChange_DetectsChange()
        {
            var existingCert = _context.Certificates.First(c => c.Id == 1);
            existingCert.CertificationResults = "Old results";
            await _context.SaveChangesAsync();

            var csv = BuildCsv(ExtHeaders, new[]
            {
                "25-0001", "Test Company", "Test Product", "UL 2809",
                "Client Approved - Paid", "2025-01-01", "2026-01-01", "Philadelphia PA",
                "USA", "Amazon", "", "", "", "", "", "New results", "",
                "", "", "", ""
            });
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.ModifiedCertificates.Should().Be(1);
            result.ModifiedRows[0].Changes
                .Should().Contain(c => c.FieldName == "CertificationResults");
        }

        #endregion

        #region PreviewImportAsync - Product Identifiers

        [Fact]
        public async Task PreviewImportAsync_ProductIds_ParsedCorrectly()
        {
            var csv = BuildCsv(ExtHeaders, new[]
            {
                "25-9999", "Test Company", "Test Product", "UL 2809",
                "Active", "2025-01-01", "2026-01-01", "NYC", "USA",
                "Amazon", "", "", "", "", "", "", "", "B08XQRT456",
                "012345678901", "00012345678901", "1234567890123"
            });
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.NewRows.Should().HaveCount(1);
            result.NewRows[0].Asin.Should().Be("B08XQRT456");
            result.NewRows[0].Upc.Should().Be("012345678901");
            result.NewRows[0].Gtin.Should().Be("00012345678901");
            result.NewRows[0].Ean.Should().Be("1234567890123");
        }

        #endregion

        #region PreviewImportAsync - Case Insensitive Lookups

        [Fact]
        public async Task PreviewImportAsync_CompanyNameCaseInsensitive()
        {
            var csv = BuildCsv(StdHeaders,
                StdRow(company: "test company"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.CompaniesToCreate.Should().Be(0);
            result.NewRows[0].CompanyId.Should().Be(1);
        }

        [Fact]
        public async Task PreviewImportAsync_CertTypeCaseInsensitive()
        {
            var csv = BuildCsv(StdHeaders,
                StdRow(certType: "ul 2809"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.NewRows.Should().HaveCount(1);
            result.NewRows[0].CertificationTypeId.Should().Be(1);
        }

        #endregion

        #region PreviewImportAsync - Duplicate Company Deduplication

        [Fact]
        public async Task PreviewImportAsync_SameNewCompanyTwice_CountsOnce()
        {
            var csv = BuildCsv(StdHeaders,
                StdRow("25-9999", company: "New Corp"),
                StdRow("25-9998", company: "New Corp"));
            var file = CreateCsvFormFile(csv);

            var result = await _service.PreviewImportAsync(file, 1, "admin");

            result.CompaniesToCreate.Should().Be(1);
            result.NewCompanyNames.Should().HaveCount(1);
        }

        #endregion

        // ==================== GetCachedPreviewAsync Tests ====================

        #region GetCachedPreviewAsync

        [Fact]
        public async Task GetCachedPreviewAsync_NonExistent_ReturnsNull()
        {
            var result = await _service.GetCachedPreviewAsync("nonexistent");
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetCachedPreviewAsync_AfterPreview_ReturnsCached()
        {
            var csv = BuildCsv(StdHeaders, StdRow());
            var file = CreateCsvFormFile(csv);
            var preview = await _service.PreviewImportAsync(file, 1, "admin");

            var cached = await _service.GetCachedPreviewAsync(
                preview.ImportId);

            cached.Should().NotBeNull();
            cached!.TotalRows.Should().Be(1);
        }

        #endregion

        // ==================== ApplyImportAsync Tests ====================

        #region ApplyImportAsync - Create New Certificates

        [Fact]
        public async Task ApplyImportAsync_NewCert_CreatesInDatabase()
        {
            var preview = CreatePreviewWithNewRow("25-5000");

            var result = await _service.ApplyImportAsync(
                preview, 1, "admin", "Test import");

            result.Success.Should().BeTrue();
            result.CertificatesCreated.Should().Be(1);

            var cert = _context.Certificates
                .FirstOrDefault(c => c.Number == "25-5000");
            cert.Should().NotBeNull();
            cert!.CompanyId.Should().Be(1);
            cert.Status.Should().Be("Client Approved - Paid");
        }

        [Fact]
        public async Task ApplyImportAsync_NewCertWithResults_SavesResults()
        {
            var preview = CreatePreviewWithNewRow(
                "25-5001", certResults: "95.5% recycled content");

            await _service.ApplyImportAsync(preview, 1, "admin");

            var cert = _context.Certificates
                .FirstOrDefault(c => c.Number == "25-5001");
            cert.Should().NotBeNull();
            cert!.CertificationResults
                .Should().Be("95.5% recycled content");
        }

        [Fact]
        public async Task ApplyImportAsync_NoDates_UsesDefaults()
        {
            var preview = CreatePreviewWithNewRow("25-5002");
            preview.NewRows[0].EffectiveDate = null;
            preview.NewRows[0].ExpirationDate = null;

            await _service.ApplyImportAsync(preview, 1, "admin");

            var cert = _context.Certificates
                .FirstOrDefault(c => c.Number == "25-5002");
            cert.Should().NotBeNull();
            cert!.EffectiveDate.Date.Should().Be(DateTime.UtcNow.Date);
            cert.ExpirationDate.Date
                .Should().Be(DateTime.UtcNow.AddYears(1).Date);
        }

        [Fact]
        public async Task ApplyImportAsync_NullPartner_DefaultsToNone()
        {
            var preview = CreatePreviewWithNewRow(
                "25-5003", partner: null!);

            await _service.ApplyImportAsync(preview, 1, "admin");

            var cert = _context.Certificates
                .FirstOrDefault(c => c.Number == "25-5003");
            cert.Should().NotBeNull();
            cert!.Partner.Should().Be("None");
        }

        #endregion

        #region ApplyImportAsync - Update Existing Certificates

        [Fact]
        public async Task ApplyImportAsync_ModifiedCert_UpdatesInDatabase()
        {
            var preview = new CertificateImportPreviewResult
            {
                ImportId = Guid.NewGuid().ToString(),
                NewCompanyNames = new List<string>(),
                NewProductNames = new List<string>(),
                NewProductTypeNames = new List<string>(),
                NewTagNames = new List<string>(),
                NewRows = new List<CertificateImportRow>(),
                ModifiedRows = new List<CertificateImportRow>
                {
                    new CertificateImportRow
                    {
                        RowNumber = 2,
                        CertificateNumber = "25-0001",
                        ExistingCertificateId = 1,
                        CompanyName = "Test Company",
                        CompanyId = 1,
                        ProductName = "Test Product",
                        ProductId = 1,
                        CertificationType = "UL 2809",
                        CertificationTypeId = 1,
                        Status = "Expired",
                        Location = "Chicago, IL",
                        Country = "USA",
                        Partner = "Amazon",
                        RowStatus = ImportRowStatus.Modified,
                        TagIds = new List<int>(),
                        TagsToCreate = new List<string>()
                    }
                }
            };

            var result = await _service.ApplyImportAsync(
                preview, 1, "admin", "Update test");

            result.Success.Should().BeTrue();
            result.CertificatesUpdated.Should().Be(1);

            var cert = _context.Certificates.First(c => c.Id == 1);
            cert.Status.Should().Be("Expired");
            cert.Location.Should().Be("Chicago, IL");
        }

        #endregion

        #region ApplyImportAsync - Auto-Create Entities

        [Fact]
        public async Task ApplyImportAsync_AutoCreatesCompany()
        {
            var preview = CreatePreviewWithNewRow("25-6000");
            preview.NewCompanyNames = new List<string> { "Auto Company" };
            preview.NewRows[0].CompanyId = null;
            preview.NewRows[0].CompanyName = "Auto Company";
            preview.NewRows[0].CompanyEmail = "auto@company.com";
            preview.NewRows[0].CompanyLocation = "Denver";
            preview.NewRows[0].CompanyCountry = "USA";

            var result = await _service.ApplyImportAsync(
                preview, 1, "admin");

            result.Success.Should().BeTrue();
            var company = _context.Companies
                .FirstOrDefault(c => c.Name == "Auto Company");
            company.Should().NotBeNull();
            company!.Email.Should().Be("auto@company.com");
            company.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task ApplyImportAsync_AutoCreatesProductType()
        {
            var preview = CreatePreviewWithNewRow("25-6001");
            preview.NewProductTypeNames =
                new List<string> { "New Type" };

            var result = await _service.ApplyImportAsync(
                preview, 1, "admin");

            result.Success.Should().BeTrue();
            var pt = _context.ProductTypes
                .FirstOrDefault(p => p.Name == "New Type");
            pt.Should().NotBeNull();
            pt!.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task ApplyImportAsync_AutoCreatesTags()
        {
            var preview = CreatePreviewWithNewRow(
                "25-6002",
                tagsToCreate: new List<string> { "auto-tag-1", "auto-tag-2" });
            preview.NewTagNames =
                new List<string> { "auto-tag-1", "auto-tag-2" };

            var result = await _service.ApplyImportAsync(
                preview, 1, "admin");

            result.Success.Should().BeTrue();
            var tag1 = _context.Tags
                .FirstOrDefault(t => t.Name == "auto-tag-1");
            tag1.Should().NotBeNull();
            tag1!.TagType.Should().Be("external");
        }

        [Fact]
        public async Task ApplyImportAsync_AutoCreatesProduct()
        {
            var preview = CreatePreviewWithNewRow("25-6003");
            preview.NewProductNames =
                new List<string> { "Auto Product" };
            preview.NewRows[0].ProductId = null;
            preview.NewRows[0].ProductName = "Auto Product";

            var result = await _service.ApplyImportAsync(
                preview, 1, "admin");

            result.Success.Should().BeTrue();
            var product = _context.Products
                .FirstOrDefault(p => p.Name == "Auto Product");
            product.Should().NotBeNull();
            product!.IsActive.Should().BeTrue();
        }

        #endregion

        #region ApplyImportAsync - Audit Logging

        [Fact]
        public async Task ApplyImportAsync_NewCert_LogsCreateAudit()
        {
            var preview = CreatePreviewWithNewRow("25-7000");

            await _service.ApplyImportAsync(
                preview, 1, "admin", "Audit test");

            _mockAuditService.Verify(a => a.LogEntityChangeAsync(
                It.IsAny<string>(),
                "25-7000",
                AuditAction.Create,
                It.IsAny<object>(),
                It.IsAny<Certificate>(),
                1,
                "admin",
                "Audit test",
                AuditChangeSource.Import
            ), Times.Once);
        }

        [Fact]
        public async Task ApplyImportAsync_UpdateCert_LogsUpdateAudit()
        {
            var preview = new CertificateImportPreviewResult
            {
                ImportId = Guid.NewGuid().ToString(),
                NewCompanyNames = new List<string>(),
                NewProductNames = new List<string>(),
                NewProductTypeNames = new List<string>(),
                NewTagNames = new List<string>(),
                NewRows = new List<CertificateImportRow>(),
                ModifiedRows = new List<CertificateImportRow>
                {
                    new CertificateImportRow
                    {
                        RowNumber = 2,
                        CertificateNumber = "25-0001",
                        ExistingCertificateId = 1,
                        CompanyName = "Test Company",
                        CompanyId = 1,
                        ProductName = "Test Product",
                        ProductId = 1,
                        CertificationType = "UL 2809",
                        CertificationTypeId = 1,
                        Status = "Client Approved - Paid",
                        Location = "Updated City",
                        Country = "USA",
                        Partner = "Amazon",
                        RowStatus = ImportRowStatus.Modified,
                        TagIds = new List<int>(),
                        TagsToCreate = new List<string>()
                    }
                }
            };

            await _service.ApplyImportAsync(preview, 1, "admin");

            _mockAuditService.Verify(a => a.LogEntityChangeAsync(
                It.IsAny<string>(),
                "25-0001",
                AuditAction.Update,
                It.IsAny<Certificate>(),
                It.IsAny<Certificate>(),
                1,
                "admin",
                It.IsAny<string>(),
                AuditChangeSource.Import
            ), Times.Once);
        }

        #endregion

        #region ApplyImportAsync - Result Messages

        [Fact]
        public async Task ApplyImportAsync_Success_ContainsCounts()
        {
            var preview = CreatePreviewWithNewRow("25-8001");

            var result = await _service.ApplyImportAsync(
                preview, 1, "admin");

            result.Success.Should().BeTrue();
            result.Message.Should().Contain("1 certificates created");
            result.Message.Should().Contain("0 updated");
        }

        #endregion

        #region ApplyImportAsync - Cache Removal

        [Fact]
        public async Task ApplyImportAsync_RemovesCacheAfterApply()
        {
            var csv = BuildCsv(StdHeaders, StdRow());
            var file = CreateCsvFormFile(csv);
            var preview = await _service.PreviewImportAsync(
                file, 1, "admin");

            var cached = await _service.GetCachedPreviewAsync(
                preview.ImportId);
            cached.Should().NotBeNull();

            await _service.ApplyImportAsync(preview, 1, "admin");

            var cachedAfter = await _service.GetCachedPreviewAsync(
                preview.ImportId);
            cachedAfter.Should().BeNull();
        }

        #endregion

        #region ApplyImportAsync - Error Handling

        [Fact]
        public async Task ApplyImportAsync_NoCategories_FailsGracefully()
        {
            // Use a fresh context with no categories
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(
                    InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            using var freshContext = new ApplicationDbContext(options);
            var freshService = new CertificateImportService(
                freshContext, _mockAuditService.Object,
                new MemoryCache(new MemoryCacheOptions()),
                _mockLogger.Object, _mockEnvironment.Object, _mockFileUploadService.Object,
                new Mock<IProductIdService>().Object, _uploadPathsStub);

            var preview = CreatePreviewWithNewRow("25-8000");
            preview.NewCompanyNames = new List<string> { "NewCo" };
            preview.NewRows[0].CompanyName = "NewCo";
            preview.NewRows[0].CompanyId = null;

            var result = await freshService.ApplyImportAsync(
                preview, 1, "admin");

            result.Success.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        #endregion

        #region PreviewImportAsync - Product Category Mapping (QC #189/#195, DEF-278)

        // Proves the importer honors the template's "Product Category" column:
        //   exact match / different-case match -> sets the cert's ProductCategoryId;
        //   blank -> left null (no error, cert shows "—"); unmatched/typo -> row REJECTED
        //   in preview (never silently defaulted to "Chemical and Raw Materials").
        // Writes a readable result table + the exact template CSV to the OS temp dir so the
        // outcome can be eyeballed without standing up the app or touching a real DB.
        [Fact]
        public async Task PreviewImportAsync_ProductCategory_MappingScenarios_DEF278()
        {
            // Seed two more categories alongside the seeded "General" (Id=1).
            _context.ProductCategories.AddRange(
                new ProductCategory { Id = 2, Name = "Paper and Packaging", IsActive = true },
                new ProductCategory { Id = 3, Name = "Cleaning Products", IsActive = true });
            _context.SaveChanges();

            var headers = new[]
            {
                "Certificate #", "Company", "Product Name", "Certification Type",
                "Product Category", "Status", "Effective Date", "Expiration Date",
                "Location", "Country", "Partner"
            };
            string[] Row(string cert, string category) => new[]
            {
                cert, "Test Company", "Test Product", "UL 2809",
                category, "Client Approved - Paid", "2025-01-01", "2026-01-01",
                "NYC", "USA", "Amazon"
            };

            var csv = BuildCsv(headers,
                Row("26-9001", "Paper and Packaging"),   // exact match     -> Id 2
                Row("26-9002", "paper AND packaging"),    // different case  -> Id 2
                Row("26-9003", ""),                       // blank           -> null, no error
                Row("26-9004", "Totally Made Up Cat"));   // unmatched/typo  -> REJECTED

            // Save the exact template that produced this result set, for reuse in the real UI.
            var csvPath = Path.Combine(Path.GetTempPath(), "gcc-import-category-template.csv");
            File.WriteAllText(csvPath, csv, new UTF8Encoding(true));

            var file = CreateCsvFormFile(csv);
            var result = await _service.PreviewImportAsync(file, 1, "admin");

            // Build a human-readable result table and write it to disk.
            string CatName(int? id) => id == null
                ? "(null / shows \"—\")"
                : _context.ProductCategories.FirstOrDefault(c => c.Id == id)?.Name ?? $"#{id}";
            var sb = new StringBuilder();
            sb.AppendLine("=== Import \"Product Category\" mapping — RESULT SET (DEF-278, QC #189/#195) ===");
            sb.AppendLine($"Reusable template written to: {csvPath}");
            sb.AppendLine("Existing categories in DB: General(1), Paper and Packaging(2), Cleaning Products(3)");
            sb.AppendLine($"Totals -> rows={result.TotalRows}  valid={result.ValidRows}  invalid={result.InvalidRows}  new={result.NewCertificates}  rejected={result.ErrorRows.Count}");
            sb.AppendLine();
            sb.AppendLine($"{"Cert#",-9} {"Category in template",-22} {"Outcome",-10} Resolved / message");
            sb.AppendLine(new string('-', 92));
            foreach (var r in result.NewRows.OrderBy(r => r.CertificateNumber))
                sb.AppendLine($"{r.CertificateNumber,-9} {(string.IsNullOrEmpty(r.ProductCategoryName) ? "(blank)" : r.ProductCategoryName),-22} {"ACCEPTED",-10} {CatName(r.ProductCategoryId)}");
            foreach (var e in result.ErrorRows.OrderBy(e => e.CertificateNumber))
                sb.AppendLine($"{e.CertificateNumber,-9} {"Totally Made Up Cat",-22} {"REJECTED",-10} {string.Join("; ", e.ErrorMessages)}");

            var outPath = Path.Combine(Path.GetTempPath(), "gcc-import-category-dryrun.txt");
            File.WriteAllText(outPath, sb.ToString());

            // ---- Assertions: lock in the DEF-278 behavior ----
            var r1 = result.NewRows.FirstOrDefault(r => r.CertificateNumber == "26-9001");
            r1.Should().NotBeNull("an exact-match category row should be accepted as New");
            r1!.ProductCategoryId.Should().Be(2, "exact match resolves to Paper and Packaging");

            var r2 = result.NewRows.FirstOrDefault(r => r.CertificateNumber == "26-9002");
            r2.Should().NotBeNull("a different-case category should still match");
            r2!.ProductCategoryId.Should().Be(2, "match is case-insensitive");

            var r3 = result.NewRows.FirstOrDefault(r => r.CertificateNumber == "26-9003");
            r3.Should().NotBeNull("a blank category is allowed");
            r3!.ProductCategoryId.Should().BeNull("blank leaves the cert category null (shows \"—\"), no error");

            result.ErrorRows.Should().Contain(e => e.CertificateNumber == "26-9004",
                "an unmatched/typo category must REJECT the row, not silently default it");
            result.ErrorRows.First(e => e.CertificateNumber == "26-9004")
                .ErrorMessages.Should().Contain(m => m.Contains("Product Category not found"));
        }

        #endregion
    }
}

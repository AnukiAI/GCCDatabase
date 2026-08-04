using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;
using GCCWebApp.Api.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace GCCWebApp.Api.Tests.Unit.Services
{
    public class ProductIdServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<ProductIdService>> _loggerMock;
        private readonly ProductIdService _service;

        public ProductIdServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _loggerMock = new Mock<ILogger<ProductIdService>>();
            _service = new ProductIdService(_context, _loggerMock.Object,
                new Mock<GCCWebApp.Services.Audit.IAuditService>().Object);

            SeedTestData();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedTestData()
        {
            // Minimal data needed for product ID tests
            var company = new Company { Id = 200, Name = "Test Co", Email = "test@test.com" };
            _context.Companies.Add(company);

            var product = new Product
            {
                Id = 200,
                Name = "Test Product",
                CompanyId = 200,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.Products.Add(product);

            var certType = new CertificationType
            {
                Id = 200,
                Name = "Test Cert Type",
                Description = "Testing",
                IsActive = true
            };
            _context.CertificationTypes.Add(certType);

            var certificate = new Certificate
            {
                Id = 200,
                Number = "24-TEST",
                Status = "Active",
                CompanyId = 200,
                ProductId = 200,
                CertificationTypeId = 200,
                EffectiveDate = DateTime.UtcNow.AddDays(-30),
                ExpirationDate = DateTime.UtcNow.AddDays(335),
                CreatedAt = DateTime.UtcNow
            };
            _context.Certificates.Add(certificate);

            // Seed some existing product IDs
            var productIds = new List<CertificateProductId>
            {
                new CertificateProductId
                {
                    Id = 200,
                    CertificateId = 200,
                    ProductIdType = "ASIN",
                    ProductIdValue = "B08XQRT456",
                    Status = "Active",
                    Partner = "Amazon",
                    ProductName = "Test Product 12-Pack",
                    IsActive = true,
                    IsPublic = true,
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new CertificateProductId
                {
                    Id = 201,
                    CertificateId = 200,
                    ProductIdType = "UPC",
                    ProductIdValue = "036000291452",
                    Status = "Active",
                    Partner = "Amazon",
                    IsActive = true,
                    IsPublic = true,
                    DisplayOrder = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new CertificateProductId
                {
                    Id = 202,
                    CertificateId = 200,
                    ProductIdType = "GTIN",
                    ProductIdValue = "00036000291452",
                    Status = "Active",
                    IsActive = true,
                    IsPublic = true,
                    DisplayOrder = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new CertificateProductId
                {
                    Id = 203,
                    CertificateId = 200,
                    ProductIdType = "SKU",
                    ProductIdValue = "TEST-SKU-001",
                    Status = "Inactive",
                    IsActive = false, // Inactive - should not appear in active queries
                    IsPublic = true,
                    DisplayOrder = 4,
                    CreatedAt = DateTime.UtcNow
                }
            };
            _context.CertificateProductIds.AddRange(productIds);
            _context.SaveChanges();
        }

        #region GetProductIdsByCertificateAsync Tests

        [Fact]
        public async Task GetProductIdsByCertificateAsync_ReturnsOnlyActiveIds()
        {
            // Act
            var result = await _service.GetProductIdsByCertificateAsync(200);

            // Assert - Should exclude ID 203 (IsActive=false)
            result.Should().HaveCount(3);
            result.Should().NotContain(p => p.Id == 203);
        }

        [Fact]
        public async Task GetProductIdsByCertificateAsync_OrderedByDisplayOrder()
        {
            // Act
            var result = await _service.GetProductIdsByCertificateAsync(200);

            // Assert
            var orders = result.Select(p => p.DisplayOrder).ToList();
            orders.Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task GetProductIdsByCertificateAsync_NonExistentCert_ReturnsEmpty()
        {
            // Act
            var result = await _service.GetProductIdsByCertificateAsync(9999);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetProductIdAsync Tests

        [Fact]
        public async Task GetProductIdAsync_ValidId_ReturnsProductId()
        {
            // Act
            var result = await _service.GetProductIdAsync(200);

            // Assert
            result.Should().NotBeNull();
            result!.ProductIdType.Should().Be("ASIN");
            result.ProductIdValue.Should().Be("B08XQRT456");
        }

        [Fact]
        public async Task GetProductIdAsync_NonExistentId_ReturnsNull()
        {
            // Act
            var result = await _service.GetProductIdAsync(9999);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CreateProductIdAsync Tests

        [Fact]
        public async Task CreateProductIdAsync_ValidAsin_Creates()
        {
            // Arrange
            var productId = new CertificateProductId
            {
                CertificateId = 200,
                ProductIdType = "ASIN",
                ProductIdValue = "B07XYZ1234",
                Status = "Active",
                Partner = "Amazon",
                IsActive = true
            };

            // Act
            var result = await _service.CreateProductIdAsync(productId);

            // Assert
            result.Id.Should().BeGreaterThan(0);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task CreateProductIdAsync_InvalidFormat_ThrowsArgumentException()
        {
            // Arrange - ASIN must be 10 alphanumeric chars
            var productId = new CertificateProductId
            {
                CertificateId = 200,
                ProductIdType = "ASIN",
                ProductIdValue = "TOOSHORT",
                Status = "Active",
                IsActive = true
            };

            // Act & Assert
            var act = () => _service.CreateProductIdAsync(productId);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Invalid ASIN format*");
        }

        [Fact]
        public async Task CreateProductIdAsync_DuplicateValue_ThrowsInvalidOperationException()
        {
            // Arrange - B08XQRT456 already exists in seed data
            var productId = new CertificateProductId
            {
                CertificateId = 200,
                ProductIdType = "ASIN",
                ProductIdValue = "B08XQRT456",
                Status = "Active",
                IsActive = true
            };

            // Act & Assert
            var act = () => _service.CreateProductIdAsync(productId);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task CreateProductIdAsync_AutoSetsDisplayOrder()
        {
            // Arrange
            var productId = new CertificateProductId
            {
                CertificateId = 200,
                ProductIdType = "ASIN",
                ProductIdValue = "B07NEWIT01",
                Status = "Active",
                IsActive = true
                // DisplayOrder not set
            };

            // Act
            var result = await _service.CreateProductIdAsync(productId);

            // Assert - Should be max existing order + 1
            result.DisplayOrder.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task CreateProductIdAsync_WithExplicitDisplayOrder_UsesProvided()
        {
            // Arrange
            var productId = new CertificateProductId
            {
                CertificateId = 200,
                ProductIdType = "ASIN",
                ProductIdValue = "B07NEWIT02",
                Status = "Active",
                IsActive = true,
                DisplayOrder = 99
            };

            // Act
            var result = await _service.CreateProductIdAsync(productId);

            // Assert
            result.DisplayOrder.Should().Be(99);
        }

        #endregion

        #region UpdateProductIdAsync Tests

        [Fact]
        public async Task UpdateProductIdAsync_ValidUpdate_UpdatesFields()
        {
            // Arrange
            var update = new CertificateProductId
            {
                Id = 200,
                CertificateId = 200,
                ProductIdType = "ASIN",
                ProductIdValue = "B08XQRT456", // Same value, no re-validation needed
                Status = "Inactive",
                Partner = "Novi",
                ProductName = "Updated Name",
                ProductSku = "UPDATED-SKU",
                Description = "Updated description",
                IsActive = true,
                IsPublic = false,
                DisplayOrder = 10
            };

            // Act
            var result = await _service.UpdateProductIdAsync(update);

            // Assert
            result.Status.Should().Be("Inactive");
            result.Partner.Should().Be("Novi");
            result.ProductName.Should().Be("Updated Name");
            result.IsPublic.Should().BeFalse();
            result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task UpdateProductIdAsync_ChangeValue_ValidatesNewFormat()
        {
            // Arrange - Change to invalid ASIN
            var update = new CertificateProductId
            {
                Id = 200,
                CertificateId = 200,
                ProductIdType = "ASIN",
                ProductIdValue = "BAD", // Invalid
                Status = "Active",
                IsActive = true
            };

            // Act & Assert
            var act = () => _service.UpdateProductIdAsync(update);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Invalid ASIN format*");
        }

        [Fact]
        public async Task UpdateProductIdAsync_NonExistentId_ThrowsArgumentException()
        {
            // Arrange
            var update = new CertificateProductId
            {
                Id = 9999,
                ProductIdType = "ASIN",
                ProductIdValue = "B07XYZ1234",
                Status = "Active",
                IsActive = true
            };

            // Act & Assert
            var act = () => _service.UpdateProductIdAsync(update);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task UpdateProductIdAsync_ChangeToExistingDuplicate_ThrowsException()
        {
            // Arrange - Try to change ID 202 (GTIN) to have the same value as ID 201 (UPC)
            var update = new CertificateProductId
            {
                Id = 202,
                CertificateId = 200,
                ProductIdType = "UPC",
                ProductIdValue = "036000291452", // Already exists as ID 201
                Status = "Active",
                IsActive = true
            };

            // Act & Assert
            var act = () => _service.UpdateProductIdAsync(update);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        #endregion

        #region DeleteProductIdAsync Tests

        [Fact]
        public async Task DeleteProductIdAsync_ValidId_DeletesAndReturnsTrue()
        {
            // Act
            var result = await _service.DeleteProductIdAsync(203); // Inactive one

            // Assert
            result.Should().BeTrue();
            var deleted = await _context.CertificateProductIds.FindAsync(203);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteProductIdAsync_NonExistentId_ReturnsFalse()
        {
            // Act
            var result = await _service.DeleteProductIdAsync(9999);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region ValidateProductIdAsync - ASIN Tests

        [Theory]
        [InlineData("B08XQRT456", true)]   // Standard ASIN
        [InlineData("0123456789", true)]    // All numeric ASIN
        [InlineData("B000000001", true)]    // Mixed
        [InlineData("B08XQRT45", false)]    // Too short (9 chars)
        [InlineData("B08XQRT4567", false)]  // Too long (11 chars)
        [InlineData("b08xqrt456", false)]   // Lowercase
        [InlineData("B08XQR!456", false)]   // Special char
        [InlineData("", false)]             // Empty
        public async Task ValidateProductIdAsync_ASIN_ValidatesCorrectly(string value, bool expected)
        {
            var result = await _service.ValidateProductIdAsync("ASIN", value);
            result.Should().Be(expected, $"ASIN '{value}' should be {(expected ? "valid" : "invalid")}");
        }

        #endregion

        #region ValidateProductIdAsync - UPC Tests

        [Theory]
        [InlineData("036000291452", true)]  // Valid UPC with correct check digit
        [InlineData("012345678905", true)]  // Valid UPC
        [InlineData("036000291451", false)] // Invalid check digit
        [InlineData("03600029145", false)]  // Too short (11 digits)
        [InlineData("0360002914521", false)] // Too long (13 digits)
        [InlineData("03600029145A", false)] // Contains letter
        [InlineData("", false)]             // Empty
        public async Task ValidateProductIdAsync_UPC_ValidatesWithCheckDigit(string value, bool expected)
        {
            var result = await _service.ValidateProductIdAsync("UPC", value);
            result.Should().Be(expected, $"UPC '{value}' should be {(expected ? "valid" : "invalid")}");
        }

        #endregion

        #region ValidateProductIdAsync - GTIN Tests

        [Theory]
        [InlineData("12345678", true)]          // GTIN-8
        [InlineData("036000291452", true)]      // GTIN-12
        [InlineData("0036000291452", true)]     // GTIN-13
        [InlineData("00036000291452", true)]    // GTIN-14
        [InlineData("1234567", false)]          // 7 digits - invalid
        [InlineData("123456789", false)]        // 9 digits - invalid
        [InlineData("123456789012345", false)]  // 15 digits - invalid
        [InlineData("1234567A", false)]         // Contains letter
        [InlineData("", false)]                 // Empty
        public async Task ValidateProductIdAsync_GTIN_AcceptsMultipleLengths(string value, bool expected)
        {
            var result = await _service.ValidateProductIdAsync("GTIN", value);
            result.Should().Be(expected, $"GTIN '{value}' should be {(expected ? "valid" : "invalid")}");
        }

        #endregion

        #region ValidateProductIdAsync - EAN Tests

        [Theory]
        [InlineData("4006381333931", true)]  // Valid EAN-13 with correct check digit
        [InlineData("5901234123457", true)]  // Valid EAN-13
        [InlineData("4006381333932", false)] // Invalid check digit
        [InlineData("400638133393", false)]  // Too short (12 digits)
        [InlineData("40063813339312", false)] // Too long (14 digits)
        [InlineData("400638133393A", false)] // Contains letter
        [InlineData("", false)]              // Empty
        public async Task ValidateProductIdAsync_EAN_ValidatesWithCheckDigit(string value, bool expected)
        {
            var result = await _service.ValidateProductIdAsync("EAN", value);
            result.Should().Be(expected, $"EAN '{value}' should be {(expected ? "valid" : "invalid")}");
        }

        #endregion

        #region ValidateProductIdAsync - ISBN Tests

        [Theory]
        [InlineData("0306406152", true)]     // Valid ISBN-10
        [InlineData("080442957X", true)]     // Valid ISBN-10 with X check digit
        [InlineData("9780306406157", true)]  // Valid ISBN-13
        [InlineData("9780140328721", true)]  // Valid ISBN-13
        [InlineData("0306406151", false)]    // Invalid ISBN-10 checksum
        [InlineData("9780306406158", false)] // Invalid ISBN-13 checksum
        [InlineData("12345", false)]         // Too short
        [InlineData("", false)]              // Empty
        public async Task ValidateProductIdAsync_ISBN_ValidatesWithChecksum(string value, bool expected)
        {
            var result = await _service.ValidateProductIdAsync("ISBN", value);
            result.Should().Be(expected, $"ISBN '{value}' should be {(expected ? "valid" : "invalid")}");
        }

        #endregion

        #region ValidateProductIdAsync - SKU/MPN Tests

        [Theory]
        [InlineData("SKU", "TEST-SKU-001", true)]
        [InlineData("SKU", "ABC", true)]              // Min 3 chars
        [InlineData("SKU", "AB", true)]                // 2 chars accepted (no min length in code)
        [InlineData("SKU", "", false)]                 // Empty
        [InlineData("MPN", "MPN-123-ABC", true)]
        [InlineData("MPN", "A", true)]                 // Min 1 char
        [InlineData("MPN", "", false)]                 // Empty
        [InlineData("OTHER", "CUSTOM-123", true)]
        [InlineData("OTHER", "", false)]               // Empty
        public async Task ValidateProductIdAsync_SKU_MPN_OTHER_ValidatesCorrectly(string type, string value, bool expected)
        {
            var result = await _service.ValidateProductIdAsync(type, value);
            result.Should().Be(expected);
        }

        [Fact]
        public async Task ValidateProductIdAsync_NullType_ReturnsFalse()
        {
            var result = await _service.ValidateProductIdAsync(null!, "somevalue");
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateProductIdAsync_NullValue_ReturnsFalse()
        {
            var result = await _service.ValidateProductIdAsync("ASIN", null!);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateProductIdAsync_CaseInsensitiveType()
        {
            // Type should be uppercased internally
            var result = await _service.ValidateProductIdAsync("asin", "B08XQRT456");
            result.Should().BeTrue();
        }

        #endregion

        #region CheckDuplicateAsync Tests

        [Fact]
        public async Task CheckDuplicateAsync_ExistingValue_ReturnsTrue()
        {
            // B08XQRT456 exists in seed data
            var result = await _service.CheckDuplicateAsync("ASIN", "B08XQRT456");
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CheckDuplicateAsync_NewValue_ReturnsFalse()
        {
            var result = await _service.CheckDuplicateAsync("ASIN", "B99NEWVAL0");
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CheckDuplicateAsync_ExcludingSelf_ReturnsFalse()
        {
            // Excluding ID 200 (which owns B08XQRT456) should return false
            var result = await _service.CheckDuplicateAsync("ASIN", "B08XQRT456", excludeId: 200);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CheckDuplicateAsync_ExcludingOther_ReturnsTrue()
        {
            // Excluding a different ID should still find the duplicate
            var result = await _service.CheckDuplicateAsync("ASIN", "B08XQRT456", excludeId: 999);
            result.Should().BeTrue();
        }

        #endregion

        #region GetValidationErrorsAsync Tests

        [Fact]
        public async Task GetValidationErrorsAsync_ValidItem_ReturnsNoErrors()
        {
            var item = new ProductIdImportItem
            {
                Type = "ASIN",
                Value = "B99NODUPL1" // New valid ASIN
            };

            var errors = await _service.GetValidationErrorsAsync(item);
            errors.Should().BeEmpty();
        }

        [Fact]
        public async Task GetValidationErrorsAsync_EmptyType_ReturnsError()
        {
            var item = new ProductIdImportItem { Type = "", Value = "B08XQRT456" };

            var errors = await _service.GetValidationErrorsAsync(item);
            errors.Should().Contain("Type is required");
        }

        [Fact]
        public async Task GetValidationErrorsAsync_EmptyValue_ReturnsError()
        {
            var item = new ProductIdImportItem { Type = "ASIN", Value = "" };

            var errors = await _service.GetValidationErrorsAsync(item);
            errors.Should().Contain("Value is required");
        }

        [Fact]
        public async Task GetValidationErrorsAsync_InvalidFormat_ReturnsFormatError()
        {
            var item = new ProductIdImportItem { Type = "ASIN", Value = "BAD" };

            var errors = await _service.GetValidationErrorsAsync(item);
            errors.Should().Contain("Invalid ASIN format");
        }

        [Fact]
        public async Task GetValidationErrorsAsync_Duplicate_ReturnsDuplicateError()
        {
            var item = new ProductIdImportItem { Type = "ASIN", Value = "B08XQRT456" };

            var errors = await _service.GetValidationErrorsAsync(item);
            errors.Should().Contain("Product ID already exists");
        }

        #endregion

        #region ImportProductIdsAsync Tests

        [Fact]
        public async Task ImportProductIdsAsync_ValidItems_ImportsAll()
        {
            // Arrange
            var items = new List<ProductIdImportItem>
            {
                new ProductIdImportItem { Type = "ASIN", Value = "B07IMPRT01" },
                new ProductIdImportItem { Type = "ASIN", Value = "B07IMPRT02" }
            };

            // Act
            var result = await _service.ImportProductIdsAsync(200, items);

            // Assert
            result.TotalProcessed.Should().Be(2);
            result.SuccessCount.Should().Be(2);
            result.ErrorCount.Should().Be(0);
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ImportProductIdsAsync_MixedValidInvalid_ReportsErrors()
        {
            // Arrange
            var items = new List<ProductIdImportItem>
            {
                new ProductIdImportItem { Type = "ASIN", Value = "B07IMPRT03" }, // Valid
                new ProductIdImportItem { Type = "ASIN", Value = "BAD" },           // Invalid format
                new ProductIdImportItem { Type = "", Value = "whatever" }            // Missing type
            };

            // Act
            var result = await _service.ImportProductIdsAsync(200, items);

            // Assert
            result.TotalProcessed.Should().Be(3);
            result.SuccessCount.Should().Be(1);
            result.ErrorCount.Should().Be(2);
            result.Errors.Should().HaveCount(2);
        }

        [Fact]
        public async Task ImportProductIdsAsync_DuplicateInBatch_SecondFails()
        {
            // Arrange - Two items with same value; first succeeds, second is duplicate
            var items = new List<ProductIdImportItem>
            {
                new ProductIdImportItem { Type = "ASIN", Value = "B07DOUBL01" },
                new ProductIdImportItem { Type = "ASIN", Value = "B07DOUBL01" }
            };

            // Act
            var result = await _service.ImportProductIdsAsync(200, items);

            // Assert
            result.SuccessCount.Should().Be(1);
            result.ErrorCount.Should().Be(1);
        }

        [Fact]
        public async Task ImportProductIdsAsync_SetsDefaultStatus()
        {
            // Arrange
            var items = new List<ProductIdImportItem>
            {
                new ProductIdImportItem { Type = "ASIN", Value = "B07DEFSTA1" }
                // No Status set
            };

            // Act
            await _service.ImportProductIdsAsync(200, items);

            // Assert
            var imported = await _context.CertificateProductIds
                .FirstOrDefaultAsync(p => p.ProductIdValue == "B07DEFSTA1");
            imported.Should().NotBeNull();
            imported!.Status.Should().Be("Active");
        }

        #endregion

        #region ImportFromCsvAsync Tests

        [Fact]
        public async Task ImportFromCsvAsync_ValidCsv_ImportsAll()
        {
            // Arrange
            var csv = "Type,Value,Status,Partner\nASIN,B07CSVIMP1,Active,Amazon\nASIN,B07CSVIMP2,Active,Amazon";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

            // Act
            var result = await _service.ImportFromCsvAsync(200, stream);

            // Assert
            result.SuccessCount.Should().Be(2);
            result.ErrorCount.Should().Be(0);
        }

        [Fact]
        public async Task ImportFromCsvAsync_EmptyFile_ThrowsArgumentException()
        {
            // Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));

            // Act & Assert
            var act = () => _service.ImportFromCsvAsync(200, stream);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*empty*");
        }

        [Fact]
        public async Task ImportFromCsvAsync_MissingRequiredHeaders_ThrowsArgumentException()
        {
            // Arrange - Missing "Value" header
            var csv = "Type,Status\nASIN,Active";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

            // Act & Assert
            var act = () => _service.ImportFromCsvAsync(200, stream);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Missing required headers*Value*");
        }

        [Fact]
        public async Task ImportFromCsvAsync_WithOptionalFields_ParsesAll()
        {
            // Arrange
            var csv = "Type,Value,Status,Partner,ProductName,ProductSku,Description\nASIN,B07CSVOP01,Active,Amazon,My Product,SKU-001,Test description";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

            // Act
            var result = await _service.ImportFromCsvAsync(200, stream);

            // Assert
            result.SuccessCount.Should().Be(1);

            var imported = await _context.CertificateProductIds
                .FirstOrDefaultAsync(p => p.ProductIdValue == "B07CSVOP01");
            imported.Should().NotBeNull();
            imported!.ProductName.Should().Be("My Product");
            imported.ProductSku.Should().Be("SKU-001");
            imported.Description.Should().Be("Test description");
        }

        #endregion

        #region ExportToCsvAsync Tests

        [Fact]
        public async Task ExportToCsvAsync_ReturnsHeaderRow()
        {
            // Act
            var result = await _service.ExportToCsvAsync(200);

            // Assert
            var csvContent = Encoding.UTF8.GetString(result);
            csvContent.Should().StartWith("Type,Value,Status,Partner,ProductName,ProductSku,Description,CreatedAt,CreatedBy");
        }

        [Fact]
        public async Task ExportToCsvAsync_IncludesActiveProductIds()
        {
            // Act
            var result = await _service.ExportToCsvAsync(200);

            // Assert
            var csvContent = Encoding.UTF8.GetString(result);
            csvContent.Should().Contain("B08XQRT456"); // ASIN
            csvContent.Should().Contain("036000291452"); // UPC
            csvContent.Should().Contain("00036000291452"); // GTIN
        }

        [Fact]
        public async Task ExportToCsvAsync_ExcludesInactiveProductIds()
        {
            // Act
            var result = await _service.ExportToCsvAsync(200);

            // Assert - ID 203 (SKU TEST-SKU-001) is inactive
            var csvContent = Encoding.UTF8.GetString(result);
            csvContent.Should().NotContain("TEST-SKU-001");
        }

        [Fact]
        public async Task ExportToCsvAsync_NonExistentCert_ReturnsHeaderOnly()
        {
            // Act
            var result = await _service.ExportToCsvAsync(9999);

            // Assert
            var csvContent = Encoding.UTF8.GetString(result);
            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            lines.Should().HaveCount(1); // Header only
        }

        #endregion

        #region GetProductIdCountByCertificateAsync Tests

        [Fact]
        public async Task GetProductIdCountByCertificateAsync_ReturnsActiveCount()
        {
            // Act
            var count = await _service.GetProductIdCountByCertificateAsync(200);

            // Assert - 3 active (excludes 1 inactive)
            count.Should().Be(3);
        }

        [Fact]
        public async Task GetProductIdCountByCertificateAsync_NonExistentCert_ReturnsZero()
        {
            // Act
            var count = await _service.GetProductIdCountByCertificateAsync(9999);

            // Assert
            count.Should().Be(0);
        }

        #endregion

        #region GetProductIdTypeDescriptions Tests

        [Fact]
        public void GetProductIdTypeDescriptions_ReturnsAll8Types()
        {
            var descriptions = _service.GetProductIdTypeDescriptions();

            descriptions.Should().HaveCount(8);
            descriptions.Should().ContainKey("ASIN");
            descriptions.Should().ContainKey("UPC");
            descriptions.Should().ContainKey("GTIN");
            descriptions.Should().ContainKey("EAN");
            descriptions.Should().ContainKey("ISBN");
            descriptions.Should().ContainKey("SKU");
            descriptions.Should().ContainKey("MPN");
            descriptions.Should().ContainKey("OTHER");
        }

        [Fact]
        public void GetProductIdTypeDescriptions_DescriptionsAreNotEmpty()
        {
            var descriptions = _service.GetProductIdTypeDescriptions();

            foreach (var desc in descriptions)
            {
                desc.Value.Should().NotBeNullOrWhiteSpace();
            }
        }

        #endregion

        #region FormatProductId Tests

        [Theory]
        [InlineData("asin", "B08XQRT456", "ASIN: B08XQRT456")]
        [InlineData("UPC", "036000291452", "UPC: 036000291452")]
        [InlineData("sku", "TEST-001", "SKU: TEST-001")]
        public void FormatProductId_FormatsCorrectly(string type, string value, string expected)
        {
            var result = _service.FormatProductId(type, value);
            result.Should().Be(expected);
        }

        #endregion
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using GCCWebApp.Services;
using GCCWebApp.Models;
using GCCWebApp.Data;

namespace GCCWebApp.Tests
{
    public class ValidationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<ValidationService>> _mockLogger;
        private readonly ValidationService _service;

        public ValidationServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mockLogger = new Mock<ILogger<ValidationService>>();
            _service = new ValidationService(_mockLogger.Object, _context);

            SeedTestData();
        }

        #region Certificate Validation Tests

        [Fact]
        public async Task ValidateCertificateAsync_WithValidCertificate_ShouldReturnValid()
        {
            // Arrange
            var company = _context.Companies.First();
            var product = _context.Products.First();
            var certificationType = _context.CertificationTypes.First();

            var certificate = new Certificate
            {
                Number = "VALID001",
                Status = "active",
                EffectiveDate = DateTime.Today,
                ExpirationDate = DateTime.Today.AddYears(1),
                CompanyId = company.Id,
                ProductId = product.Id,
                CertificationTypeId = certificationType.Id
            };

            // Act
            var result = await _service.ValidateCertificateAsync(certificate);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateCertificateAsync_WithDuplicateNumber_ShouldReturnInvalid()
        {
            // Arrange
            var existingCert = _context.Certificates.First();
            
            var certificate = new Certificate
            {
                Number = existingCert.Number, // Duplicate number
                Status = "active",
                EffectiveDate = DateTime.Today,
                ExpirationDate = DateTime.Today.AddYears(1),
                CompanyId = existingCert.CompanyId,
                ProductId = existingCert.ProductId,
                CertificationTypeId = existingCert.CertificationTypeId
            };

            // Act
            var result = await _service.ValidateCertificateAsync(certificate);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("already in use"));
        }

        [Fact]
        public async Task ValidateCertificateAsync_WithInvalidDateRange_ShouldReturnInvalid()
        {
            // Arrange
            var company = _context.Companies.First();
            var product = _context.Products.First();
            var certificationType = _context.CertificationTypes.First();

            var certificate = new Certificate
            {
                Number = "INVALID001",
                Status = "active",
                EffectiveDate = DateTime.Today,
                ExpirationDate = DateTime.Today.AddDays(-1), // Invalid: expiration before effective
                CompanyId = company.Id,
                ProductId = product.Id,
                CertificationTypeId = certificationType.Id
            };

            // Act
            var result = await _service.ValidateCertificateAsync(certificate);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("after the effective date"));
        }

        [Fact]
        public async Task ValidateCertificateAsync_WithTooLongValidityPeriod_ShouldReturnInvalid()
        {
            // Arrange
            var company = _context.Companies.First();
            var product = _context.Products.First();
            var certificationType = _context.CertificationTypes.First();

            var certificate = new Certificate
            {
                Number = "INVALID002",
                Status = "active",
                EffectiveDate = DateTime.Today,
                ExpirationDate = DateTime.Today.AddYears(6), // Invalid: more than 5 years
                CompanyId = company.Id,
                ProductId = product.Id,
                CertificationTypeId = certificationType.Id
            };

            // Act
            var result = await _service.ValidateCertificateAsync(certificate);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("cannot exceed 5 years"));
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("unknown")]
        [InlineData("")]
        public async Task ValidateCertificateAsync_WithInvalidStatus_ShouldReturnInvalid(string status)
        {
            // Arrange
            var company = _context.Companies.First();
            var product = _context.Products.First();
            var certificationType = _context.CertificationTypes.First();

            var certificate = new Certificate
            {
                Number = "INVALID003",
                Status = status,
                EffectiveDate = DateTime.Today,
                ExpirationDate = DateTime.Today.AddYears(1),
                CompanyId = company.Id,
                ProductId = product.Id,
                CertificationTypeId = certificationType.Id
            };

            // Act
            var result = await _service.ValidateCertificateAsync(certificate);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("ASINTEST01")] // Valid 10-character ASIN
        [InlineData("123456789A")] // Valid ASIN with numbers and letter
        public async Task ValidateCertificateAsync_WithValidAsin_ShouldReturnValid(string asin)
        {
            // Arrange
            var company = _context.Companies.First();
            var product = _context.Products.First();
            var certificationType = _context.CertificationTypes.First();

            var certificate = new Certificate
            {
                Number = "VALID002",
                Status = "active",
                EffectiveDate = DateTime.Today,
                ExpirationDate = DateTime.Today.AddYears(1),
                CompanyId = company.Id,
                ProductId = product.Id,
                CertificationTypeId = certificationType.Id,
                Asin = asin
            };

            // Act
            var result = await _service.ValidateCertificateAsync(certificate);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("123")] // Too short
        [InlineData("12345678901")] // Too long
        [InlineData("ASIN-TEST1")] // Contains special characters
        public async Task ValidateCertificateAsync_WithInvalidAsin_ShouldReturnInvalid(string asin)
        {
            // Arrange
            var company = _context.Companies.First();
            var product = _context.Products.First();
            var certificationType = _context.CertificationTypes.First();

            var certificate = new Certificate
            {
                Number = "INVALID004",
                Status = "active",
                EffectiveDate = DateTime.Today,
                ExpirationDate = DateTime.Today.AddYears(1),
                CompanyId = company.Id,
                ProductId = product.Id,
                CertificationTypeId = certificationType.Id,
                Asin = asin
            };

            // Act
            var result = await _service.ValidateCertificateAsync(certificate);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("ASIN format is invalid"));
        }

        [Theory]
        [InlineData("123456789012")] // Valid 12-digit UPC
        public async Task ValidateCertificateAsync_WithValidUpc_ShouldReturnValid(string upc)
        {
            // Arrange
            var company = _context.Companies.First();
            var product = _context.Products.First();
            var certificationType = _context.CertificationTypes.First();

            var certificate = new Certificate
            {
                Number = "VALID003",
                Status = "active",
                EffectiveDate = DateTime.Today,
                ExpirationDate = DateTime.Today.AddYears(1),
                CompanyId = company.Id,
                ProductId = product.Id,
                CertificationTypeId = certificationType.Id,
                Upc = upc
            };

            // Act
            var result = await _service.ValidateCertificateAsync(certificate);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("12345678901")] // Too short
        [InlineData("1234567890123")] // Too long
        [InlineData("12345678901A")] // Contains letter
        public async Task ValidateCertificateAsync_WithInvalidUpc_ShouldReturnInvalid(string upc)
        {
            // Arrange
            var company = _context.Companies.First();
            var product = _context.Products.First();
            var certificationType = _context.CertificationTypes.First();

            var certificate = new Certificate
            {
                Number = "INVALID005",
                Status = "active",
                EffectiveDate = DateTime.Today,
                ExpirationDate = DateTime.Today.AddYears(1),
                CompanyId = company.Id,
                ProductId = product.Id,
                CertificationTypeId = certificationType.Id,
                Upc = upc
            };

            // Act
            var result = await _service.ValidateCertificateAsync(certificate);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("UPC format is invalid"));
        }

        #endregion

        #region Company Validation Tests

        [Fact]
        public async Task ValidateCompanyAsync_WithValidCompany_ShouldReturnValid()
        {
            // Arrange
            var company = new Company
            {
                Name = "New Valid Company",
                ContactInfo = "contact@newcompany.com",
                Location = "New Location"
            };

            // Act
            var result = await _service.ValidateCompanyAsync(company);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateCompanyAsync_WithDuplicateName_ShouldReturnInvalid()
        {
            // Arrange
            var existingCompany = _context.Companies.First();
            var company = new Company
            {
                Name = existingCompany.Name, // Duplicate name
                ContactInfo = "different@email.com",
                Location = "Different Location"
            };

            // Act
            var result = await _service.ValidateCompanyAsync(company);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("already exists"));
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("@domain.com")]
        [InlineData("user@")]
        public async Task ValidateCompanyAsync_WithInvalidEmail_ShouldReturnInvalid(string invalidEmail)
        {
            // Arrange
            var company = new Company
            {
                Name = "Test Company With Invalid Email",
                ContactInfo = invalidEmail,
                Location = "Test Location"
            };

            // Act
            var result = await _service.ValidateCompanyAsync(company);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("valid email address"));
        }

        #endregion

        #region Product Validation Tests

        [Fact]
        public async Task ValidateProductAsync_WithValidProduct_ShouldReturnValid()
        {
            // Arrange
            var category = _context.ProductCategories.First();
            var product = new Product
            {
                Name = "Valid Test Product",
                Description = "A valid test product",
                CategoryId = category.Id
            };

            // Act
            var result = await _service.ValidateProductAsync(product);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateProductAsync_WithInvalidCategory_ShouldReturnInvalid()
        {
            // Arrange
            var product = new Product
            {
                Name = "Test Product",
                Description = "Test Description",
                CategoryId = 999 // Non-existent category
            };

            // Act
            var result = await _service.ValidateProductAsync(product);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("category does not exist"));
        }

        [Theory]
        [InlineData("Product <script>")]
        [InlineData("Product \"quote\"")]
        [InlineData("Product & ampersand")]
        public async Task ValidateProductAsync_WithInvalidCharacters_ShouldReturnInvalid(string productName)
        {
            // Arrange
            var category = _context.ProductCategories.First();
            var product = new Product
            {
                Name = productName,
                Description = "Test Description",
                CategoryId = category.Id
            };

            // Act
            var result = await _service.ValidateProductAsync(product);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("invalid characters"));
        }

        #endregion

        #region Model Validation Tests

        [Fact]
        public void ValidateModel_WithValidModel_ShouldReturnValid()
        {
            // Arrange
            var certificate = new Certificate
            {
                Number = "TEST001",
                Status = "active",
                EffectiveDate = DateTime.Today,
                ExpirationDate = DateTime.Today.AddYears(1)
            };

            // Act
            var result = _service.ValidateModel(certificate);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateModel_WithEmptyRequiredField_ShouldReturnInvalid()
        {
            // Arrange
            var certificate = new Certificate
            {
                Number = "", // Required field is empty
                Status = "active",
                EffectiveDate = DateTime.Today,
                ExpirationDate = DateTime.Today.AddYears(1)
            };

            // Act
            var result = _service.ValidateModel(certificate);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        #endregion

        private void SeedTestData()
        {
            var category = new ProductCategory { Name = "Test Category" };
            _context.ProductCategories.Add(category);

            var company = new Company
            {
                Name = "Test Company",
                ContactInfo = "test@company.com",
                Location = "Test Location"
            };
            _context.Companies.Add(company);

            var product = new Product
            {
                Name = "Test Product",
                CategoryId = 1
            };
            _context.Products.Add(product);

            var certificationType = new CertificationType
            {
                Name = "Test Certificate Type"
            };
            _context.CertificationTypes.Add(certificationType);

            _context.SaveChanges();

            var certificate = new Certificate
            {
                Number = "EXISTING001",
                Status = "active",
                EffectiveDate = DateTime.Today.AddDays(-30),
                ExpirationDate = DateTime.Today.AddDays(330),
                CompanyId = company.Id,
                ProductId = product.Id,
                CertificationTypeId = certificationType.Id
            };
            _context.Certificates.Add(certificate);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
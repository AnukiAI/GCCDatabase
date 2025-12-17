using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;

namespace GCCWebApp.Tests
{
    public class SimpleModelTests
    {
        [Fact]
        public void Certificate_ShouldInitializeWithDefaults()
        {
            var certificate = new Certificate();

            Assert.Equal(string.Empty, certificate.Number);
            Assert.Equal("Draft", certificate.Status); // Certificate workflow starts as Draft
            Assert.True(certificate.CreatedAt <= DateTime.UtcNow);
            Assert.True(certificate.UpdatedAt <= DateTime.UtcNow);
            Assert.NotNull(certificate.Tags);
            Assert.NotNull(certificate.CustomFieldValues);
        }

        [Fact]
        public void Certificate_ShouldValidateRequiredFields()
        {
            var certificate = new Certificate
            {
                Number = "GCC001",
                CompanyId = 1,
                ProductId = 1,
                CertificationTypeId = 1,
                EffectiveDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddYears(1)
            };

            Assert.NotNull(certificate.Number);
            Assert.True(certificate.Number.Length <= 7);
            Assert.True(certificate.CompanyId > 0);
            Assert.True(certificate.ProductId > 0);
            Assert.True(certificate.CertificationTypeId > 0);
        }

        [Fact]
        public void Company_ShouldInitializeCorrectly()
        {
            var company = new Company
            {
                Name = "Test Company",
                ContactInfo = "test@company.com",
                Location = "Test Location"
            };

            Assert.Equal("Test Company", company.Name);
            Assert.Equal("test@company.com", company.ContactInfo);
            Assert.Equal("Test Location", company.Location);
        }

        [Fact]
        public void Product_ShouldInitializeCorrectly()
        {
            var product = new Product
            {
                Name = "Test Product",
                Description = "Test Description"
            };

            Assert.Equal("Test Product", product.Name);
            Assert.Equal("Test Description", product.Description);
        }

        [Fact]
        public void CertificateEditViewModel_ShouldInitializeCorrectly()
        {
            var viewModel = new CertificateEditViewModel
            {
                Number = "GCC001",
                Status = "active"
            };

            Assert.Equal("GCC001", viewModel.Number);
            Assert.Equal("active", viewModel.Status);
        }
    }

    public class DatabaseContextTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public DatabaseContextTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
        }

        [Fact]
        public void DatabaseContext_ShouldInitialize()
        {
            Assert.NotNull(_context);
            Assert.NotNull(_context.Certificates);
            Assert.NotNull(_context.Companies);
            Assert.NotNull(_context.Products);
        }

        [Fact]
        public void DatabaseContext_ShouldAddAndRetrieveCertificate()
        {
            var company = new Company
            {
                Name = "Test Company",
                ContactInfo = "test@company.com",
                Location = "Test Location"
            };

            var product = new Product
            {
                Name = "Test Product",
                Description = "Test Description"
            };

            var certificationType = new CertificationType
            {
                Name = "Test Certificate Type",
                Description = "Test Description"
            };

            var certificate = new Certificate
            {
                Number = "GCC001",
                Status = "active",
                EffectiveDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddYears(1),
                CompanyId = 1,
                ProductId = 1,
                CertificationTypeId = 1
            };

            _context.Companies.Add(company);
            _context.Products.Add(product);
            _context.CertificationTypes.Add(certificationType);
            _context.Certificates.Add(certificate);
            _context.SaveChanges();

            var retrievedCertificate = _context.Certificates.First();

            Assert.NotNull(retrievedCertificate);
            Assert.Equal("GCC001", retrievedCertificate.Number);
            Assert.Equal("active", retrievedCertificate.Status);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }

    public class ViewModelTests
    {
        [Fact]
        public void CertificateListRequest_ShouldHaveDefaults()
        {
            var request = new CertificateListRequest();

            Assert.Equal(1, request.Page);
            Assert.Equal(20, request.PageSize);
            Assert.Equal("newest", request.Sort);
            Assert.Equal("Public", request.UserRole);
            Assert.NotNull(request.Filters);
        }

        [Fact]
        public void CertificateSearchFilters_ShouldInitializeEmpty()
        {
            var filters = new CertificateSearchFilters();

            Assert.Equal(string.Empty, filters.SearchTerm);
            Assert.NotNull(filters.CertificationTypes);
            Assert.NotNull(filters.Countries);
            Assert.NotNull(filters.Statuses);
            Assert.Empty(filters.CertificationTypes);
            Assert.Empty(filters.Countries);
            Assert.Empty(filters.Statuses);
        }

        [Fact]
        public void CertificateListResponse_ShouldCalculatePages()
        {
            var response = new CertificateListResponse
            {
                TotalCount = 25,
                PageSize = 10,
                CurrentPage = 2
            };

            Assert.Equal(3, response.TotalPages);
            Assert.True(response.HasPreviousPage);
            Assert.True(response.HasNextPage);
        }
    }
}
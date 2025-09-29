using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using GCCWebApp.Services;
using GCCWebApp.Models;
using GCCWebApp.Data;

namespace GCCWebApp.Tests
{
    public class FilterServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<FilterService>> _mockLogger;
        private readonly FilterService _service;

        public FilterServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mockLogger = new Mock<ILogger<FilterService>>();
            _service = new FilterService(_mockLogger.Object, _context);

            SeedTestData();
        }

        #region Available Filters Tests

        [Fact]
        public async Task GetAvailableFiltersAsync_ShouldReturnAllFilterTypes()
        {
            // Act
            var result = await _service.GetAvailableFiltersAsync("Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(5);
            result.Should().Contain(f => f.FilterType == "CertificationType");
            result.Should().Contain(f => f.FilterType == "ProductCategories");
            result.Should().Contain(f => f.FilterType == "Companies");
            result.Should().Contain(f => f.FilterType == "Countries");
            result.Should().Contain(f => f.FilterType == "Status");
        }

        [Fact]
        public async Task GetAvailableFiltersAsync_ShouldIncludeCounts()
        {
            // Act
            var result = await _service.GetAvailableFiltersAsync("Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(f => f.Count > 0);
        }

        #endregion

        #region Filter Options Tests

        [Fact]
        public async Task GetFilterOptionsAsync_CertificationType_ShouldReturnCertificationTypes()
        {
            // Act
            var result = await _service.GetFilterOptionsAsync("certificationtype", "", "Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThan(0);
            result.Should().OnlyContain(o => !string.IsNullOrEmpty(o.Text));
        }

        [Fact]
        public async Task GetFilterOptionsAsync_CertificationType_WithSearchTerm_ShouldFilterResults()
        {
            // Act
            var result = await _service.GetFilterOptionsAsync("certificationtype", "CSF", "Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(o => o.Text.Contains("CSF"));
        }

        [Fact]
        public async Task GetFilterOptionsAsync_Companies_ShouldReturnCompanies()
        {
            // Act
            var result = await _service.GetFilterOptionsAsync("companies", "", "Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThan(0);
            result.Should().OnlyContain(o => !string.IsNullOrEmpty(o.Text));
        }

        [Fact]
        public async Task GetFilterOptionsAsync_Companies_WithSearchTerm_ShouldFilterResults()
        {
            // Act
            var result = await _service.GetFilterOptionsAsync("companies", "Eco", "Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(o => o.Text.Contains("Eco"));
        }

        [Fact]
        public async Task GetFilterOptionsAsync_Countries_ShouldReturnUniqueCountries()
        {
            // Act
            var result = await _service.GetFilterOptionsAsync("countries", "", "Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThan(0);
            result.Should().OnlyContain(o => !string.IsNullOrEmpty(o.Text));
            result.Select(o => o.Text).Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task GetFilterOptionsAsync_Status_ShouldReturnAllStatuses()
        {
            // Act
            var result = await _service.GetFilterOptionsAsync("status", "", "Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThan(0);
            result.Should().Contain(o => o.Text == "active");
            result.Should().Contain(o => o.Text == "expired");
        }

        [Fact]
        public async Task GetFilterOptionsAsync_InvalidFilterType_ShouldReturnEmptyList()
        {
            // Act
            var result = await _service.GetFilterOptionsAsync("invalidtype", "", "Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region Product Categories Tests

        [Fact]
        public async Task GetProductCategoriesAsync_ShouldReturnHierarchicalStructure()
        {
            // Act
            var result = await _service.GetProductCategoriesAsync("Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThan(0);
            result.Should().OnlyContain(pc => !string.IsNullOrEmpty(pc.Text));
        }

        [Fact]
        public async Task GetProductCategoriesAsync_ShouldIncludeChildrenProductTypes()
        {
            // Act
            var result = await _service.GetProductCategoriesAsync("Public");

            // Assert
            result.Should().NotBeNull();
            var categoryWithChildren = result.FirstOrDefault(pc => pc.Children.Any());
            if (categoryWithChildren != null)
            {
                categoryWithChildren.Children.Should().OnlyContain(child => child.Level == 2);
            }
        }

        #endregion

        #region Companies Tests

        [Fact]
        public async Task GetCompaniesAsync_ShouldReturnOrderedCompanies()
        {
            // Act
            var result = await _service.GetCompaniesAsync("", "Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThan(0);
            
            var companyNames = result.Select(c => c.Text).ToList();
            companyNames.Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task GetCompaniesAsync_WithSearchTerm_ShouldFilterAndLimit()
        {
            // Act
            var result = await _service.GetCompaniesAsync("Eco", "Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(c => c.Text.Contains("Eco"));
            result.Should().HaveCountLessThanOrEqualTo(50); // Should respect the limit
        }

        [Fact]
        public async Task GetCompaniesAsync_ShouldIncludeCertificateCounts()
        {
            // Act
            var result = await _service.GetCompaniesAsync("", "Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(c => c.Count >= 0);
        }

        #endregion

        #region Countries Tests

        [Fact]
        public async Task GetCountriesAsync_ShouldReturnOrderedCountries()
        {
            // Act
            var result = await _service.GetCountriesAsync("Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThan(0);
            
            var countryNames = result.Select(c => c.Text).ToList();
            countryNames.Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task GetCountriesAsync_ShouldOnlyIncludeNonEmptyCountries()
        {
            // Act
            var result = await _service.GetCountriesAsync("Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(c => !string.IsNullOrEmpty(c.Text));
        }

        [Fact]
        public async Task GetCountriesAsync_ShouldIncludeCertificateCounts()
        {
            // Act
            var result = await _service.GetCountriesAsync("Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(c => c.Count > 0);
        }

        #endregion

        #region Certification Types Tests

        [Fact]
        public async Task GetCertificationTypesAsync_ShouldReturnOrderedTypes()
        {
            // Act
            var result = await _service.GetCertificationTypesAsync("Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThan(0);
            
            var typeNames = result.Select(t => t.Text).ToList();
            typeNames.Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task GetCertificationTypesAsync_ShouldIncludeCertificateCounts()
        {
            // Act
            var result = await _service.GetCertificationTypesAsync("Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(t => t.Count >= 0);
        }

        #endregion

        #region Filter Counts Tests

        [Fact]
        public async Task GetFilterCountsAsync_WithEmptyFilters_ShouldReturnAllCounts()
        {
            // Arrange
            var filters = new CertificateSearchFilters();

            // Act
            var result = await _service.GetFilterCountsAsync(filters, "Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().ContainKey("Total");
            result.Should().ContainKey("Active");
            result.Should().ContainKey("Expired");
            result.Should().ContainKey("Pending");
            result["Total"].Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetFilterCountsAsync_WithSearchTerm_ShouldReturnFilteredCounts()
        {
            // Arrange
            var filters = new CertificateSearchFilters
            {
                SearchTerm = "Eco"
            };

            // Act
            var result = await _service.GetFilterCountsAsync(filters, "Public");

            // Assert
            result.Should().NotBeNull();
            result.Should().ContainKey("Total");
            result["Total"].Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task GetFilterCountsAsync_ShouldReturnConsistentCounts()
        {
            // Arrange
            var filters = new CertificateSearchFilters();

            // Act
            var result = await _service.GetFilterCountsAsync(filters, "Public");

            // Assert
            result.Should().NotBeNull();
            var total = result["Total"];
            var active = result["Active"];
            var expired = result["Expired"];
            var pending = result["Pending"];

            // Total should be at least the sum of individual status counts
            // (there might be other statuses not counted separately)
            total.Should().BeGreaterThanOrEqualTo(active + expired + pending);
        }

        #endregion

        private void SeedTestData()
        {
            // Product Categories
            var categories = new[]
            {
                new ProductCategory { Name = "Electronics" },
                new ProductCategory { Name = "Home & Garden" },
                new ProductCategory { Name = "Food & Beverage" }
            };
            _context.ProductCategories.AddRange(categories);

            // Product Types
            var productTypes = new[]
            {
                new ProductType { Name = "Smartphones", CategoryId = 1 },
                new ProductType { Name = "Laptops", CategoryId = 1 },
                new ProductType { Name = "Furniture", CategoryId = 2 }
            };
            _context.ProductTypes.AddRange(productTypes);

            // Companies
            var companies = new[]
            {
                new Company { Name = "EcoTech Solutions", ContactInfo = "contact@ecotech.com", Location = "San Francisco" },
                new Company { Name = "Green Products Inc", ContactInfo = "info@greenproducts.com", Location = "Portland" },
                new Company { Name = "Sustainable Goods", ContactInfo = "hello@sustainable.com", Location = "Seattle" }
            };
            _context.Companies.AddRange(companies);

            // Products
            var products = new[]
            {
                new Product { Name = "Eco Phone", CategoryId = 1, ProductTypeId = 1 },
                new Product { Name = "Green Laptop", CategoryId = 1, ProductTypeId = 2 },
                new Product { Name = "Sustainable Chair", CategoryId = 2, ProductTypeId = 3 }
            };
            _context.Products.AddRange(products);

            // Certification Types
            var certificationTypes = new[]
            {
                new CertificationType { Name = "CSF Certified" },
                new CertificationType { Name = "Recycled Content" },
                new CertificationType { Name = "Carbon Neutral" }
            };
            _context.CertificationTypes.AddRange(certificationTypes);

            _context.SaveChanges();

            // Certificates
            var certificates = new[]
            {
                new Certificate
                {
                    Number = "ECO001",
                    Status = "active",
                    EffectiveDate = DateTime.Now.AddDays(-30),
                    ExpirationDate = DateTime.Now.AddDays(335),
                    Country = "USA",
                    CompanyId = companies[0].Id,
                    ProductId = products[0].Id,
                    CertificationTypeId = certificationTypes[0].Id
                },
                new Certificate
                {
                    Number = "ECO002",
                    Status = "active",
                    EffectiveDate = DateTime.Now.AddDays(-60),
                    ExpirationDate = DateTime.Now.AddDays(305),
                    Country = "Canada",
                    CompanyId = companies[1].Id,
                    ProductId = products[1].Id,
                    CertificationTypeId = certificationTypes[1].Id
                },
                new Certificate
                {
                    Number = "ECO003",
                    Status = "expired",
                    EffectiveDate = DateTime.Now.AddDays(-400),
                    ExpirationDate = DateTime.Now.AddDays(-30),
                    Country = "Mexico",
                    CompanyId = companies[2].Id,
                    ProductId = products[2].Id,
                    CertificationTypeId = certificationTypes[2].Id
                },
                new Certificate
                {
                    Number = "ECO004",
                    Status = "pending",
                    EffectiveDate = DateTime.Now.AddDays(10),
                    ExpirationDate = DateTime.Now.AddDays(375),
                    Country = "USA",
                    CompanyId = companies[0].Id,
                    ProductId = products[0].Id,
                    CertificationTypeId = certificationTypes[0].Id
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
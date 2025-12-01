using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;

namespace GCCWebApp.Api.Tests.TestUtilities
{
    public static class TestDbContextFactory
    {
        public static ApplicationDbContext CreateInMemoryContext(string databaseName = "")
        {
            if (string.IsNullOrEmpty(databaseName))
                databaseName = Guid.NewGuid().ToString();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            var context = new ApplicationDbContext(options);
            return context;
        }

        public static void SeedTestData(ApplicationDbContext context)
        {
            // Clear existing data
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // Seed test data
            var testCompany = new Company
            {
                Id = 1,
                Name = "Test Company",
                Email = "test@company.com"
            };

            var testProduct = new Product
            {
                Id = 1,
                Name = "Test Product",
                Model = "TEST-001",
                Sku = "TEST-SKU-001",
                CompanyId = 1,
                CreatedAt = DateTime.UtcNow
            };

            var testCertificationType = new CertificationType
            {
                Id = 1,
                Name = "Test Certification",
                Description = "Test certification type"
            };

            var testCertificate = new Certificate
            {
                Id = 1,
                Number = "CERT-TEST-001",
                ProductId = 1,
                CompanyId = 1,
                CertificationTypeId = 1,
                EffectiveDate = DateTime.UtcNow.AddDays(-30),
                ExpirationDate = DateTime.UtcNow.AddDays(30),
                Status = "Active",
                Partner = "Amazon",
                CreatedAt = DateTime.UtcNow
            };

            // Add Partners for Partner API tests
            var amazonPartner = new Partner
            {
                Id = 1,
                Name = "Amazon",
                Description = "Amazon test partner",
                Website = "https://www.amazon.com",
                Email = "partner-support@amazon.com",
                ContactInfo = "Amazon Partner Program",
                IsActive = true,
                IsDeleted = false,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var wayfairPartner = new Partner
            {
                Id = 2,
                Name = "Wayfair",
                Description = "Wayfair test partner",
                Website = "https://www.wayfair.com",
                Email = "sustainability@wayfair.com",
                ContactInfo = "Wayfair Sustainability Program",
                IsActive = true,
                IsDeleted = false,
                SortOrder = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add PartnerApiKeys with correct gcc_partner_* format
            var amazonKey = new PartnerApiKey
            {
                Id = 1,
                PartnerId = 1,
                ApiKey = "gcc_partner_amazon_test_key_123456",
                Description = "Amazon Test Key",
                IsActive = true,
                RateLimitPerHour = 2000,
                AllowedEndpoints = "[\"/api/*\"]",
                Permissions = "[\"read\", \"write\"]",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Test Seed",
                RequestCount = 0,
                ExpiresAt = DateTime.UtcNow.AddYears(1)
            };

            var wayfairKey = new PartnerApiKey
            {
                Id = 2,
                PartnerId = 2,
                ApiKey = "gcc_partner_wayfair_test_key_789012",
                Description = "Wayfair Test Key",
                IsActive = true,
                RateLimitPerHour = 5000,
                AllowedEndpoints = "[\"/api/*\"]",
                Permissions = "[\"read\", \"write\"]",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Test Seed",
                RequestCount = 0,
                ExpiresAt = DateTime.UtcNow.AddYears(1)
            };

            // Add ApiKeys for the ApiKey authentication scheme (used by /api/v1/* endpoints)
            var testApiKeys = new[]
            {
                new ApiKey
                {
                    Id = 1,
                    Name = "Amazon Test Key",
                    Key = "gcc_partner_amazon_test_key_123456",
                    Description = "Amazon partner test API key",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "Test Seed",
                    ExpiresAt = DateTime.UtcNow.AddYears(1),
                    RateLimitPerHour = 2000,
                    RequireTLS = false // Allow HTTP for testing
                },
                new ApiKey
                {
                    Id = 2,
                    Name = "Wayfair Test Key",
                    Key = "gcc_partner_wayfair_test_key_789012",
                    Description = "Wayfair partner test API key",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "Test Seed",
                    ExpiresAt = DateTime.UtcNow.AddYears(1),
                    RateLimitPerHour = 5000,
                    RequireTLS = false // Allow HTTP for testing
                },
                new ApiKey
                {
                    Id = 3,
                    Name = "General Test Key",
                    Key = "valid_test_key",
                    Description = "General purpose test API key",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "Test Seed",
                    ExpiresAt = DateTime.UtcNow.AddYears(1),
                    RateLimitPerHour = 1000,
                    RequireTLS = false // Allow HTTP for testing
                },
                new ApiKey
                {
                    Id = 4,
                    Name = "Rate Limit Test Key",
                    Key = "rate_limit_test_key",
                    Description = "Test API key for rate limiting tests",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "Test Seed",
                    ExpiresAt = DateTime.UtcNow.AddYears(1),
                    RateLimitPerHour = 5, // Low limit for rate limit testing
                    RequireTLS = false // Allow HTTP for testing
                },
                new ApiKey
                {
                    Id = 5,
                    Name = "Integration Test Key",
                    Key = "integration_test_key",
                    Description = "API key for integration tests",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "Test Seed",
                    ExpiresAt = DateTime.UtcNow.AddYears(1),
                    RateLimitPerHour = 2000,
                    RequireTLS = false // Allow HTTP for testing
                }
            };

            context.Companies.Add(testCompany);
            context.Products.Add(testProduct);
            context.CertificationTypes.Add(testCertificationType);
            context.Certificates.Add(testCertificate);
            context.Partners.AddRange(amazonPartner, wayfairPartner);
            context.PartnerApiKeys.AddRange(amazonKey, wayfairKey);
            context.ApiKeys.AddRange(testApiKeys);

            context.SaveChanges();
        }
    }
}
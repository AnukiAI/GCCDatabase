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
            // Check if data already exists to avoid duplicate seeding
            if (context.Roles.Any() || context.Users.Any() || context.Companies.Any())
            {
                return; // Data already seeded
            }

            // Ensure database is created
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
                CreatedAt = DateTime.UtcNow
            };

            // Add test roles
            var adminRole = new Role { Id = 1, Name = "Admin", Description = "Administrator role" };
            var analystRole = new Role { Id = 2, Name = "Analyst", Description = "Analyst role" };
            var externalRole = new Role { Id = 3, Name = "External", Description = "External partner role" };

            // Add test users
            var adminUser = new User
            {
                Id = 1,
                Username = "testadmin",
                Email = "admin@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
                Status = "active",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TwoFactorMethod = "Email",
                Roles = new List<Role> { adminRole }
            };

            var analystUser = new User
            {
                Id = 2,
                Username = "testanalyst",
                Email = "analyst@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
                Status = "active",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TwoFactorMethod = "Email",
                Roles = new List<Role> { analystRole }
            };

            // Add test API keys for partner authentication
            var amazonApiKey = new ApiKey
            {
                Id = 1,
                Name = "Amazon Test API Key",
                Key = "amazon_test_key", // Store plain text for testing
                KeyHash = BCrypt.Net.BCrypt.HashPassword("amazon_test_key"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Test System",
                Scopes = "read,write",
                AllowedIPs = null, // Allow all IPs for testing
                AllowedOrigins = "*"
            };

            var wayfairApiKey = new ApiKey
            {
                Id = 2,
                Name = "Wayfair Test API Key",
                Key = "wayfair_test_key", // Store plain text for testing
                KeyHash = BCrypt.Net.BCrypt.HashPassword("wayfair_test_key"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Test System",
                Scopes = "read,write",
                AllowedIPs = null, // Allow all IPs for testing
                AllowedOrigins = "*"
            };

            var generalApiKey = new ApiKey
            {
                Id = 3,
                Name = "General Test API Key",
                Key = "general_test_key",
                KeyHash = BCrypt.Net.BCrypt.HashPassword("general_test_key"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Test System",
                Scopes = "read,write",
                AllowedIPs = null, // Allow all IPs for testing
                AllowedOrigins = "*"
            };

            context.Roles.AddRange(adminRole, analystRole, externalRole);
            context.Users.AddRange(adminUser, analystUser);
            context.ApiKeys.AddRange(amazonApiKey, wayfairApiKey, generalApiKey);
            context.Companies.Add(testCompany);
            context.Products.Add(testProduct);
            context.CertificationTypes.Add(testCertificationType);
            context.Certificates.Add(testCertificate);

            context.SaveChanges();
        }
    }
}
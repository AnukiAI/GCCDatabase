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

            // Add valid_test_key for integration tests that use this key
            var validTestApiKey = new ApiKey
            {
                Id = 4,
                Name = "Valid Test API Key",
                Key = "valid_test_key",
                KeyHash = BCrypt.Net.BCrypt.HashPassword("valid_test_key"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Test System",
                Scopes = "read,write",
                AllowedIPs = null, // Allow all IPs for testing
                AllowedOrigins = "*"
            };

            // Add integration_test_key for workflow integration tests
            var integrationTestApiKey = new ApiKey
            {
                Id = 5,
                Name = "Integration Test API Key",
                Key = "integration_test_key",
                KeyHash = BCrypt.Net.BCrypt.HashPassword("integration_test_key"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Test System",
                Scopes = "read,write",
                AllowedIPs = null, // Allow all IPs for testing
                AllowedOrigins = "*"
            };

            // Add rate_limit_test_key for rate limiting tests
            var rateLimitTestApiKey = new ApiKey
            {
                Id = 6,
                Name = "Rate Limit Test API Key",
                Key = "rate_limit_test_key",
                KeyHash = BCrypt.Net.BCrypt.HashPassword("rate_limit_test_key"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Test System",
                Scopes = "read,write",
                AllowedIPs = null, // Allow all IPs for testing
                AllowedOrigins = "*"
            };

            context.Roles.AddRange(adminRole, analystRole, externalRole);
            context.Users.AddRange(adminUser, analystUser);
            context.ApiKeys.AddRange(amazonApiKey, wayfairApiKey, generalApiKey, validTestApiKey, integrationTestApiKey, rateLimitTestApiKey);
            context.Companies.Add(testCompany);
            context.Products.Add(testProduct);
            context.CertificationTypes.Add(testCertificationType);
            context.Certificates.Add(testCertificate);

            context.SaveChanges();

            // Seed partner-scoped test data for API scoping tests
            SeedPartnerScopingData(context);
        }

        /// <summary>
        /// Seeds partner entities, partner API keys, partner-scoped certificates, and product IDs
        /// for testing API data isolation per partner (Amazon, Novi, Other).
        /// Uses navigation properties (not just FK IDs) to ensure InMemory provider resolves correctly.
        /// </summary>
        public static void SeedPartnerScopingData(ApplicationDbContext context)
        {
            // Skip if partners already exist
            if (context.Partners.Any())
                return;

            // --- Partners ---
            var amazonPartner = new Partner { Name = "Amazon", Description = "Amazon Climate Pledge Friendly", IsActive = true };
            var noviPartner = new Partner { Name = "Novi", Description = "Novi Connect", IsActive = true };
            var otherPartner = new Partner { Name = "Other", Description = "Other partner", IsActive = true };

            context.Partners.AddRange(amazonPartner, noviPartner, otherPartner);
            context.SaveChanges();

            // --- Partner API Keys (gcc_partner_ prefix required by middleware) ---
            // Use navigation property references to ensure proper FK resolution in InMemory
            var amazonPartnerKey = new PartnerApiKey
            {
                PartnerId = amazonPartner.Id,
                ApiKey = "gcc_partner_amazon_test_key_0000000000000000000000000000",
                Description = "Amazon test key",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Test System"
            };
            var noviPartnerKey = new PartnerApiKey
            {
                PartnerId = noviPartner.Id,
                ApiKey = "gcc_partner_novi_test_key_00000000000000000000000000000000",
                Description = "Novi test key",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Test System"
            };
            var otherPartnerKey = new PartnerApiKey
            {
                PartnerId = otherPartner.Id,
                ApiKey = "gcc_partner_other_test_key_000000000000000000000000000000",
                Description = "Other partner test key",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Test System"
            };

            context.PartnerApiKeys.AddRange(amazonPartnerKey, noviPartnerKey, otherPartnerKey);
            context.SaveChanges();

            // --- Additional Companies ---
            var amazonCompany = new Company { Name = "EcoProduct Co", Email = "eco@ecoproduct.com" };
            var noviCompany = new Company { Name = "GreenHome Inc", Email = "info@greenhome.com" };
            var otherCompany = new Company { Name = "SustainGoods LLC", Email = "info@sustaingoods.com" };

            context.Companies.AddRange(amazonCompany, noviCompany, otherCompany);
            context.SaveChanges();

            // --- Additional Products ---
            var amazonProduct = new Product { Name = "Recycled Paper Towels", Model = "RPT-100", Sku = "RPT-SKU-001", CompanyId = amazonCompany.Id, CreatedAt = DateTime.UtcNow };
            var noviProduct = new Product { Name = "Eco Cleaning Spray", Model = "ECS-200", Sku = "ECS-SKU-001", CompanyId = noviCompany.Id, CreatedAt = DateTime.UtcNow };
            var otherProduct = new Product { Name = "Bamboo Utensils", Model = "BU-300", Sku = "BU-SKU-001", CompanyId = otherCompany.Id, CreatedAt = DateTime.UtcNow };

            context.Products.AddRange(amazonProduct, noviProduct, otherProduct);
            context.SaveChanges();

            // --- Certification Types with AmazonCode ---
            var certTypeWithAmazon = new CertificationType
            {
                Name = "UL 2809",
                Description = "Recycled Content Validation",
                AmazonCode = "greencircle_recycled_content",
                IsActive = true
            };
            var certTypeEnergy = new CertificationType
            {
                Name = "Certified Energy Savings",
                Description = "Energy usage reduction certification",
                AmazonCode = "greencircle_energy_savings",
                IsActive = true
            };

            context.CertificationTypes.AddRange(certTypeWithAmazon, certTypeEnergy);
            context.SaveChanges();

            // --- Pathways ---
            var pathway1 = new Pathway { Name = "Energy usage reduction", IsActive = true };
            var pathway2 = new Pathway { Name = "Recycled content", IsActive = true };

            context.Pathways.AddRange(pathway1, pathway2);
            context.SaveChanges();

            // --- Amazon Certificates ---
            var amazonCert1 = new Certificate
            {
                Number = "24-0100",
                ProductId = amazonProduct.Id,
                CompanyId = amazonCompany.Id,
                CertificationTypeId = certTypeWithAmazon.Id,
                PathwayId = pathway2.Id,
                Partner = "Amazon",
                CertificationResults = "95.5% recycled content verified",
                EffectiveDate = DateTime.UtcNow.AddDays(-60),
                ExpirationDate = DateTime.UtcNow.AddDays(305),
                Status = "Active",
                Location = "Philadelphia, PA",
                Country = "USA",
                CreatedAt = DateTime.UtcNow
            };
            var amazonCert2 = new Certificate
            {
                Number = "24-0101",
                ProductId = amazonProduct.Id,
                CompanyId = amazonCompany.Id,
                CertificationTypeId = certTypeEnergy.Id,
                PathwayId = pathway1.Id,
                Partner = "Amazon",
                CertificationResults = "22% energy reduction",
                EffectiveDate = DateTime.UtcNow.AddDays(-30),
                ExpirationDate = DateTime.UtcNow.AddDays(335),
                Status = "Active",
                Location = "Chicago, IL",
                Country = "USA",
                CreatedAt = DateTime.UtcNow
            };

            // --- Novi Certificates ---
            var noviCert1 = new Certificate
            {
                Number = "24-0200",
                ProductId = noviProduct.Id,
                CompanyId = noviCompany.Id,
                CertificationTypeId = certTypeWithAmazon.Id,
                PathwayId = pathway2.Id,
                Partner = "Novi",
                CertificationResults = "80% recycled content",
                EffectiveDate = DateTime.UtcNow.AddDays(-45),
                ExpirationDate = DateTime.UtcNow.AddDays(320),
                Status = "Active",
                Location = "Portland, OR",
                Country = "USA",
                CreatedAt = DateTime.UtcNow
            };

            // --- Other Partner Certificates ---
            var otherCert1 = new Certificate
            {
                Number = "24-0300",
                ProductId = otherProduct.Id,
                CompanyId = otherCompany.Id,
                CertificationTypeId = certTypeWithAmazon.Id,
                Partner = "Other",
                EffectiveDate = DateTime.UtcNow.AddDays(-20),
                ExpirationDate = DateTime.UtcNow.AddDays(345),
                Status = "Active",
                Location = "Austin, TX",
                Country = "USA",
                CreatedAt = DateTime.UtcNow
            };

            // --- No-Partner Certificate (should not appear for any partner scope) ---
            var noCert = new Certificate
            {
                Number = "24-0400",
                ProductId = amazonProduct.Id,
                CompanyId = amazonCompany.Id,
                CertificationTypeId = certTypeWithAmazon.Id,
                Partner = "None",
                EffectiveDate = DateTime.UtcNow.AddDays(-10),
                ExpirationDate = DateTime.UtcNow.AddDays(355),
                Status = "Active",
                Location = "Denver, CO",
                Country = "USA",
                CreatedAt = DateTime.UtcNow
            };

            context.Certificates.AddRange(amazonCert1, amazonCert2, noviCert1, otherCert1, noCert);
            context.SaveChanges();

            // --- Product IDs for Amazon certificate ---
            var amazonProductIds = new List<CertificateProductId>
            {
                new CertificateProductId { CertificateId = amazonCert1.Id, ProductIdType = "ASIN", ProductIdValue = "B08XQRT456", Status = "Active", Partner = "Amazon", ProductName = "Recycled Paper Towels - 12 Pack", ProductSku = "RPT-12PK-ECO" },
                new CertificateProductId { CertificateId = amazonCert1.Id, ProductIdType = "UPC", ProductIdValue = "012345678901", Status = "Active", Partner = "Amazon", ProductName = "Recycled Paper Towels - 12 Pack" },
                new CertificateProductId { CertificateId = amazonCert1.Id, ProductIdType = "GTIN", ProductIdValue = "00012345678901", Status = "Active", Partner = "Amazon" },
                new CertificateProductId { CertificateId = amazonCert2.Id, ProductIdType = "ASIN", ProductIdValue = "B07XYZ1234", Status = "Active", Partner = "Amazon", ProductName = "Recycled Paper Towels - 6 Pack" },
            };

            // --- Product IDs for Novi certificate ---
            var noviProductIds = new List<CertificateProductId>
            {
                new CertificateProductId { CertificateId = noviCert1.Id, ProductIdType = "SKU", ProductIdValue = "NOVI-ECS-001", Status = "Active", Partner = "Novi", ProductName = "Eco Cleaning Spray", ProductSku = "ECS-001" },
                new CertificateProductId { CertificateId = noviCert1.Id, ProductIdType = "MPN", ProductIdValue = "MPN-GH-2024", Status = "Active", Partner = "Novi" },
                new CertificateProductId { CertificateId = noviCert1.Id, ProductIdType = "UPC", ProductIdValue = "098765432109", Status = "Active", Partner = "Novi" },
            };

            // --- Product IDs for Other certificate ---
            var otherProductIds = new List<CertificateProductId>
            {
                new CertificateProductId { CertificateId = otherCert1.Id, ProductIdType = "UPC", ProductIdValue = "111222333444", Status = "Active", Partner = "Other", ProductName = "Bamboo Utensils Set" },
            };

            context.CertificateProductIds.AddRange(amazonProductIds);
            context.CertificateProductIds.AddRange(noviProductIds);
            context.CertificateProductIds.AddRange(otherProductIds);
            context.SaveChanges();
        }
    }
}
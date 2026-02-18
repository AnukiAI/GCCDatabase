using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using GCCWebApp.Data;
using GCCWebApp.Models;

namespace GCCWebApp.Api.Tests.Integration
{
    /// <summary>
    /// Custom WebApplicationFactory for partner API scoping tests.
    /// Seeds partner-specific test data (Partners, PartnerApiKeys, Certificates, ProductIds)
    /// using a shared InMemory database.
    /// </summary>
    public class PartnerScopingFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"PartnerScopingDb_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration(configBuilder =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Jwt:Key"] = "ThisIsASecretKeyForTestingPurposesOnlyAndShouldBeAtLeast256BitsLongForSecurityValidation",
                    ["Jwt:Issuer"] = "GCCWebApp.Api.Tests",
                    ["Jwt:Audience"] = "GCCWebApp.Api.Tests",
                    ["Jwt:ExpiryMinutes"] = "60",
                    ["GCC_JWT_SECRET"] = "ThisIsASecretKeyForTestingPurposesOnlyAndShouldBeAtLeast256BitsLongForSecurityValidation",
                    ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                    ["RateLimiting:Enabled"] = "false",
                    ["ASPNETCORE_ENVIRONMENT"] = "Testing"
                }!);
            });

            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Register InMemory database with stable name
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                    options.EnableSensitiveDataLogging();
                });

                // Build temporary provider for seeding
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                context.Database.EnsureCreated();
                SeedPartnerTestData(context);
            });
        }

        private static void SeedPartnerTestData(ApplicationDbContext context)
        {
            if (context.Partners.Any())
                return;

            // --- Roles (required by app) ---
            var adminRole = new Role { Name = "Admin", Description = "Administrator role" };
            var analystRole = new Role { Name = "Analyst", Description = "Analyst role" };
            var externalRole = new Role { Name = "External", Description = "External partner role" };
            context.Roles.AddRange(adminRole, analystRole, externalRole);
            context.SaveChanges();

            // --- Partners ---
            var amazonPartner = new Partner { Name = "Amazon", Description = "Amazon CPF", IsActive = true };
            var noviPartner = new Partner { Name = "Novi", Description = "Novi Connect", IsActive = true };
            var otherPartner = new Partner { Name = "Other", Description = "Other partner", IsActive = true };
            context.Partners.AddRange(amazonPartner, noviPartner, otherPartner);
            context.SaveChanges();

            // --- Partner API Keys ---
            context.PartnerApiKeys.AddRange(
                new PartnerApiKey { PartnerId = amazonPartner.Id, ApiKey = "gcc_partner_amazon_test_key_0000000000000000000000000000", Description = "Amazon test key", IsActive = true, CreatedBy = "Test" },
                new PartnerApiKey { PartnerId = noviPartner.Id, ApiKey = "gcc_partner_novi_test_key_00000000000000000000000000000000", Description = "Novi test key", IsActive = true, CreatedBy = "Test" },
                new PartnerApiKey { PartnerId = otherPartner.Id, ApiKey = "gcc_partner_other_test_key_000000000000000000000000000000", Description = "Other test key", IsActive = true, CreatedBy = "Test" }
            );
            context.SaveChanges();

            // --- Companies ---
            var co1 = new Company { Name = "EcoProduct Co", Email = "eco@test.com" };
            var co2 = new Company { Name = "GreenHome Inc", Email = "gh@test.com" };
            var co3 = new Company { Name = "SustainGoods LLC", Email = "sg@test.com" };
            context.Companies.AddRange(co1, co2, co3);
            context.SaveChanges();

            // --- Products ---
            var prod1 = new Product { Name = "Recycled Paper Towels", Model = "RPT-100", Sku = "RPT-001", CompanyId = co1.Id, CreatedAt = DateTime.UtcNow };
            var prod2 = new Product { Name = "Eco Cleaning Spray", Model = "ECS-200", Sku = "ECS-001", CompanyId = co2.Id, CreatedAt = DateTime.UtcNow };
            var prod3 = new Product { Name = "Bamboo Utensils", Model = "BU-300", Sku = "BU-001", CompanyId = co3.Id, CreatedAt = DateTime.UtcNow };
            context.Products.AddRange(prod1, prod2, prod3);
            context.SaveChanges();

            // --- Certification Types ---
            var ctRecycled = new CertificationType { Name = "UL 2809", Description = "Recycled Content", AmazonCode = "greencircle_recycled_content", IsActive = true };
            var ctEnergy = new CertificationType { Name = "Certified Energy Savings", Description = "Energy reduction", AmazonCode = "greencircle_energy_savings", IsActive = true };
            context.CertificationTypes.AddRange(ctRecycled, ctEnergy);
            context.SaveChanges();

            // --- Pathways ---
            var pw1 = new Pathway { Name = "Energy usage reduction", IsActive = true };
            var pw2 = new Pathway { Name = "Recycled content", IsActive = true };
            context.Pathways.AddRange(pw1, pw2);
            context.SaveChanges();

            // --- Certificates (each partner gets its own) ---
            var amazonCert1 = new Certificate
            {
                Number = "24-0100", ProductId = prod1.Id, CompanyId = co1.Id,
                CertificationTypeId = ctRecycled.Id, PathwayId = pw2.Id,
                Partner = "Amazon", CertificationResults = "95.5% recycled content verified",
                EffectiveDate = DateTime.UtcNow.AddDays(-60), ExpirationDate = DateTime.UtcNow.AddDays(305),
                Status = "Active", Location = "Philadelphia, PA", Country = "USA", CreatedAt = DateTime.UtcNow
            };
            var amazonCert2 = new Certificate
            {
                Number = "24-0101", ProductId = prod1.Id, CompanyId = co1.Id,
                CertificationTypeId = ctEnergy.Id, PathwayId = pw1.Id,
                Partner = "Amazon", CertificationResults = "22% energy reduction",
                EffectiveDate = DateTime.UtcNow.AddDays(-30), ExpirationDate = DateTime.UtcNow.AddDays(335),
                Status = "Active", Location = "Chicago, IL", Country = "USA", CreatedAt = DateTime.UtcNow
            };
            var noviCert = new Certificate
            {
                Number = "24-0200", ProductId = prod2.Id, CompanyId = co2.Id,
                CertificationTypeId = ctRecycled.Id, PathwayId = pw2.Id,
                Partner = "Novi", CertificationResults = "80% recycled content",
                EffectiveDate = DateTime.UtcNow.AddDays(-45), ExpirationDate = DateTime.UtcNow.AddDays(320),
                Status = "Active", Location = "Portland, OR", Country = "USA", CreatedAt = DateTime.UtcNow
            };
            var otherCert = new Certificate
            {
                Number = "24-0300", ProductId = prod3.Id, CompanyId = co3.Id,
                CertificationTypeId = ctRecycled.Id,
                Partner = "Other",
                EffectiveDate = DateTime.UtcNow.AddDays(-20), ExpirationDate = DateTime.UtcNow.AddDays(345),
                Status = "Active", Location = "Austin, TX", Country = "USA", CreatedAt = DateTime.UtcNow
            };
            var noneCert = new Certificate
            {
                Number = "24-0400", ProductId = prod1.Id, CompanyId = co1.Id,
                CertificationTypeId = ctRecycled.Id,
                Partner = "None",
                EffectiveDate = DateTime.UtcNow.AddDays(-10), ExpirationDate = DateTime.UtcNow.AddDays(355),
                Status = "Active", Location = "Denver, CO", Country = "USA", CreatedAt = DateTime.UtcNow
            };
            context.Certificates.AddRange(amazonCert1, amazonCert2, noviCert, otherCert, noneCert);
            context.SaveChanges();

            // --- Product IDs ---
            context.CertificateProductIds.AddRange(
                new CertificateProductId { CertificateId = amazonCert1.Id, ProductIdType = "ASIN", ProductIdValue = "B08XQRT456", Status = "Active", Partner = "Amazon", ProductName = "Recycled Paper Towels - 12 Pack", ProductSku = "RPT-12PK" },
                new CertificateProductId { CertificateId = amazonCert1.Id, ProductIdType = "UPC", ProductIdValue = "012345678901", Status = "Active", Partner = "Amazon" },
                new CertificateProductId { CertificateId = amazonCert1.Id, ProductIdType = "GTIN", ProductIdValue = "00012345678901", Status = "Active", Partner = "Amazon" },
                new CertificateProductId { CertificateId = amazonCert2.Id, ProductIdType = "ASIN", ProductIdValue = "B07XYZ1234", Status = "Active", Partner = "Amazon", ProductName = "Recycled Paper Towels - 6 Pack" },
                new CertificateProductId { CertificateId = noviCert.Id, ProductIdType = "SKU", ProductIdValue = "NOVI-ECS-001", Status = "Active", Partner = "Novi", ProductName = "Eco Cleaning Spray", ProductSku = "ECS-001" },
                new CertificateProductId { CertificateId = noviCert.Id, ProductIdType = "MPN", ProductIdValue = "MPN-GH-2024", Status = "Active", Partner = "Novi" },
                new CertificateProductId { CertificateId = noviCert.Id, ProductIdType = "UPC", ProductIdValue = "098765432109", Status = "Active", Partner = "Novi" },
                new CertificateProductId { CertificateId = otherCert.Id, ProductIdType = "UPC", ProductIdValue = "111222333444", Status = "Active", Partner = "Other", ProductName = "Bamboo Utensils Set" }
            );
            context.SaveChanges();

            // --- Users (minimum required) ---
            context.Users.Add(new User
            {
                Username = "testadmin", Email = "admin@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
                Status = "active", IsActive = true, CreatedAt = DateTime.UtcNow,
                TwoFactorMethod = "Email", Roles = new List<Role> { adminRole }
            });
            context.SaveChanges();
        }
    }
}

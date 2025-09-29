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
                CreatedAt = DateTime.UtcNow
            };

            context.Companies.Add(testCompany);
            context.Products.Add(testProduct);
            context.CertificationTypes.Add(testCertificationType);
            context.Certificates.Add(testCertificate);

            context.SaveChanges();
        }
    }
}
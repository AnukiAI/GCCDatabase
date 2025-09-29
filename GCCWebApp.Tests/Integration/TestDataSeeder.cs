using GCCWebApp.Data;
using GCCWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Tests.Integration
{
    public static class TestDataSeeder
    {
        public static async Task SeedBasicTestDataAsync(ApplicationDbContext context)
        {
            if (await context.Certificates.AnyAsync())
            {
                return; // Data already seeded
            }

            // Create test companies
            var companies = new List<Company>
            {
                new Company
                {
                    Id = 1,
                    Name = "Test Company A",
                    Email = "contact@testcompanya.com",
                    ContactInfo = "123 Test Street, Test City, TC 12345",
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new Company
                {
                    Id = 2,
                    Name = "Test Company B",
                    Email = "info@testcompanyb.com",
                    ContactInfo = "456 Demo Avenue, Demo City, DC 67890",
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                }
            };

            context.Companies.AddRange(companies);

            // Create test product categories first
            var categories = new List<ProductCategory>
            {
                new ProductCategory
                {
                    Id = 1,
                    Name = "Electronics",
                    Description = "Electronic products and components"
                },
                new ProductCategory
                {
                    Id = 2,
                    Name = "Textiles",
                    Description = "Textile and fabric products"
                }
            };

            context.ProductCategories.AddRange(categories);

            // Create test products
            var products = new List<Product>
            {
                new Product
                {
                    Id = 1,
                    Name = "Test Product Alpha",
                    Description = "Alpha test product for integration testing",
                    CategoryId = 1,
                    CompanyId = 1
                },
                new Product
                {
                    Id = 2,
                    Name = "Test Product Beta",
                    Description = "Beta test product for integration testing",
                    CategoryId = 2,
                    CompanyId = 2
                }
            };

            context.Products.AddRange(products);

            // Create test certification types first
            var certificationTypes = new List<CertificationType>
            {
                new CertificationType
                {
                    Id = 1,
                    Name = "Environmental Certification",
                    Description = "Environmental compliance certification"
                },
                new CertificationType
                {
                    Id = 2,
                    Name = "Safety Certification",
                    Description = "Product safety certification"
                }
            };

            context.CertificationTypes.AddRange(certificationTypes);

            // Create test certificates
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    Number = "TEST-001",
                    CompanyId = 1,
                    ProductId = 1,
                    CertificationTypeId = 1,
                    EffectiveDate = DateTime.UtcNow.AddDays(-15),
                    ExpirationDate = DateTime.UtcNow.AddDays(350),
                    Status = "active",
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new Certificate
                {
                    Id = 2,
                    Number = "TEST-002",
                    CompanyId = 2,
                    ProductId = 2,
                    CertificationTypeId = 2,
                    EffectiveDate = DateTime.UtcNow.AddDays(-10),
                    ExpirationDate = DateTime.UtcNow.AddDays(355),
                    Status = "active",
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Certificate
                {
                    Id = 3,
                    Number = "TEST-003",
                    CompanyId = 1,
                    ProductId = 1,
                    CertificationTypeId = 1,
                    EffectiveDate = DateTime.UtcNow.AddDays(-400),
                    ExpirationDate = DateTime.UtcNow.AddDays(-30),
                    Status = "expired",
                    CreatedAt = DateTime.UtcNow.AddDays(-400)
                }
            };

            context.Certificates.AddRange(certificates);

            // Create test certificate product IDs
            var certificateProductIds = new List<CertificateProductId>
            {
                new CertificateProductId
                {
                    CertificateId = 1,
                    ProductIdType = "ASIN",
                    ProductIdValue = "B000TEST01",
                    Description = "Alpha Product ASIN"
                },
                new CertificateProductId
                {
                    CertificateId = 1,
                    ProductIdType = "UPC",
                    ProductIdValue = "123456789012",
                    Description = "Alpha Product UPC"
                },
                new CertificateProductId
                {
                    CertificateId = 2,
                    ProductIdType = "ASIN",
                    ProductIdValue = "B000TEST02",
                    Description = "Beta Product ASIN"
                },
                new CertificateProductId
                {
                    CertificateId = 2,
                    ProductIdType = "GTIN",
                    ProductIdValue = "01234567890123",
                    Description = "Beta Product GTIN"
                }
            };

            context.CertificateProductIds.AddRange(certificateProductIds);

            // Create test users (if User model exists)
            try
            {
                var userType = context.Model.GetEntityTypes()
                    .FirstOrDefault(t => t.ClrType.Name == "User");

                if (userType != null)
                {
                    // Add test users if User entity exists
                    // This would need to be implemented based on the actual User model
                }
            }
            catch
            {
                // User model doesn't exist or can't be accessed, skip
            }

            await context.SaveChangesAsync();
        }

        public static async Task SeedPerformanceTestDataAsync(ApplicationDbContext context)
        {
            // Seed larger dataset for performance testing
            var companies = new List<Company>();
            var products = new List<Product>();
            var certificates = new List<Certificate>();
            var productIds = new List<CertificateProductId>();

            // Create 50 companies
            for (int i = 1; i <= 50; i++)
            {
                companies.Add(new Company
                {
                    Id = i + 100, // Offset to avoid conflicts
                    Name = $"Performance Test Company {i}",
                    Email = $"company{i}@perftest.com",
                    ContactInfo = $"{i} Performance Street, Perf City, PC {10000 + i:D5}",
                    CreatedAt = DateTime.UtcNow.AddDays(-i)
                });
            }

            context.Companies.AddRange(companies);

            // Create 100 products
            for (int i = 1; i <= 100; i++)
            {
                products.Add(new Product
                {
                    Id = i + 100, // Offset to avoid conflicts
                    Name = $"Performance Product {i}",
                    Description = $"Performance test product {i} for load testing",
                    CategoryId = i % 2 == 0 ? 1 : 2,
                    CompanyId = ((i - 1) % 50) + 101 // Distribute across companies
                });
            }

            context.Products.AddRange(products);

            // Create 200 certificates
            for (int i = 1; i <= 200; i++)
            {
                certificates.Add(new Certificate
                {
                    Id = i + 100, // Offset to avoid conflicts
                    Number = $"PERF-{i:D3}",
                    CompanyId = ((i - 1) % 50) + 101,
                    ProductId = ((i - 1) % 100) + 101,
                    CertificationTypeId = ((i - 1) % 2) + 1, // Alternate between type 1 and 2
                    EffectiveDate = DateTime.UtcNow.AddDays(-i),
                    ExpirationDate = DateTime.UtcNow.AddDays(365 - (i % 100)),
                    Status = i % 10 == 0 ? "expired" : "active",
                    CreatedAt = DateTime.UtcNow.AddDays(-i)
                });
            }

            context.Certificates.AddRange(certificates);

            // Create product IDs for certificates (multiple per certificate)
            for (int certId = 101; certId <= 300; certId++)
            {
                // Add 2-5 product IDs per certificate
                var productIdCount = (certId % 4) + 2; // 2-5 product IDs

                for (int pidx = 1; pidx <= productIdCount; pidx++)
                {
                    var productType = pidx switch
                    {
                        1 => "ASIN",
                        2 => "UPC",
                        3 => "GTIN",
                        4 => "EAN",
                        _ => "SKU"
                    };

                    var identifier = productType switch
                    {
                        "ASIN" => $"B00PERF{certId:D3}{pidx}",
                        "UPC" => $"{100000000000 + (certId * 10) + pidx}",
                        "GTIN" => $"{10000000000000 + (certId * 100) + pidx}",
                        "EAN" => $"{1000000000000 + (certId * 10) + pidx}",
                        _ => $"SKU-PERF-{certId}-{pidx}"
                    };

                    productIds.Add(new CertificateProductId
                    {
                        CertificateId = certId,
                        ProductIdType = productType,
                        ProductIdValue = identifier,
                        Description = $"Performance product ID {pidx} for certificate {certId}"
                    });
                }
            }

            context.CertificateProductIds.AddRange(productIds);
            await context.SaveChangesAsync();
        }

        public static async Task CleanupTestDataAsync(ApplicationDbContext context)
        {
            // Remove all test data
            context.CertificateProductIds.RemoveRange(context.CertificateProductIds);
            context.Certificates.RemoveRange(context.Certificates);
            context.Products.RemoveRange(context.Products);
            context.Companies.RemoveRange(context.Companies);

            await context.SaveChangesAsync();
        }
    }
}
using GCCWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            // Ensure database is created and migrations are applied
            await context.Database.EnsureCreatedAsync();

            // Check if data already exists
            if (await context.Companies.AnyAsync())
            {
                return; // Database has been seeded
            }

            // Seed ProductCategories
            var categories = new List<ProductCategory>
            {
                new ProductCategory { Name = "Construction / Building Products", Description = "Building materials and construction products", SortOrder = 1, IsActive = true },
                new ProductCategory { Name = "Electronics / Electrical", Description = "Electronic and electrical products", SortOrder = 2, IsActive = true },
                new ProductCategory { Name = "Materials / Plastics", Description = "Plastic materials and products", SortOrder = 3, IsActive = true },
                new ProductCategory { Name = "Paper Products", Description = "Paper and paper-based products", SortOrder = 4, IsActive = true },
                new ProductCategory { Name = "Textiles / Flooring", Description = "Textile and flooring materials", SortOrder = 5, IsActive = true }
            };

            await context.ProductCategories.AddRangeAsync(categories);
            await context.SaveChangesAsync();

            // Seed CertificationTypes
            var certTypes = new List<CertificationType>
            {
                new CertificationType { Name = "Certified Environmental Facts", AmazonCode = "CEF", Description = "Environmental impact certification", IsActive = true, SortOrder = 1 },
                new CertificationType { Name = "Recycled Content", AmazonCode = "RC", Description = "Recycled content certification", IsActive = true, SortOrder = 2 },
                new CertificationType { Name = "Certified Sustainability Facts", AmazonCode = "CSF", Description = "Comprehensive sustainability certification", IsActive = true, SortOrder = 3 },
                new CertificationType { Name = "Energy Savings", AmazonCode = "ES", Description = "Energy efficiency certification", IsActive = true, SortOrder = 4 },
                new CertificationType { Name = "Material Ingredient Reporting", AmazonCode = "MIR", Description = "Material composition reporting", IsActive = true, SortOrder = 5 }
            };

            await context.CertificationTypes.AddRangeAsync(certTypes);
            await context.SaveChangesAsync();

            // Seed Companies
            var companies = new List<Company>
            {
                new Company { Name = "ASSA ABLOY Access & Egress Hardware Group, Inc.", ContactInfo = "sustainability@assaabloy.com", Location = "Berlin, CT", Country = "United States", IsActive = true },
                new Company { Name = "GAF Materials Corporation", ContactInfo = "green@gaf.com", Location = "Cedar City, UT", Country = "United States", IsActive = true },
                new Company { Name = "Domtar Corporation", ContactInfo = "sustainability@domtar.com", Location = "Ashdown, AR", Country = "United States", IsActive = true },
                new Company { Name = "Superior Essex Inc.", ContactInfo = "sustainability@superioressex.com", Location = "Hoisington, KS", Country = "United States", IsActive = true },
                new Company { Name = "Interface Inc.", ContactInfo = "sustainability@interface.com", Location = "Dalton, GA", Country = "United States", IsActive = true }
            };

            await context.Companies.AddRangeAsync(companies);
            await context.SaveChangesAsync();

            // Seed ProductTypes
            var productTypes = new List<ProductType>
            {
                new ProductType { Name = "Door Hardware", CategoryId = categories[0].Id, Level = 0, SortOrder = 1, IsActive = true },
                new ProductType { Name = "Insulation", CategoryId = categories[0].Id, Level = 0, SortOrder = 2, IsActive = true },
                new ProductType { Name = "Network Cables", CategoryId = categories[1].Id, Level = 0, SortOrder = 1, IsActive = true },
                new ProductType { Name = "Lighting", CategoryId = categories[1].Id, Level = 0, SortOrder = 2, IsActive = true },
                new ProductType { Name = "Carpet & Flooring", CategoryId = categories[4].Id, Level = 0, SortOrder = 1, IsActive = true }
            };

            await context.ProductTypes.AddRangeAsync(productTypes);
            await context.SaveChangesAsync();

            // Seed Products
            var products = new List<Product>
            {
                new Product { Name = "900ET Exit Device Trim with EcoFlex", Description = "Eco-friendly exit device hardware", CategoryId = categories[0].Id, ProductTypeId = productTypes[0].Id, CompanyId = companies[0].Id, IsActive = true },
                new Product { Name = "EnergyGuard Polyiso Roof Insulation", Description = "High-performance roof insulation", CategoryId = categories[0].Id, ProductTypeId = productTypes[1].Id, CompanyId = companies[1].Id, IsActive = true },
                new Product { Name = "Amazon Basics Multipurpose Copy Paper", Description = "Eco-friendly office paper", CategoryId = categories[3].Id, CompanyId = companies[2].Id, IsActive = true },
                new Product { Name = "10Gain XP Category 6A Cable", Description = "High-performance network cable", CategoryId = categories[1].Id, ProductTypeId = productTypes[2].Id, CompanyId = companies[3].Id, IsActive = true },
                new Product { Name = "Carbon Negative Carpet Tile", Description = "Sustainable carpet tiles", CategoryId = categories[4].Id, ProductTypeId = productTypes[4].Id, CompanyId = companies[4].Id, IsActive = true }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            // Seed Certificates
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Number = "24-1605",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 9, 1),
                    ExpirationDate = new DateTime(2027, 8, 31),
                    Location = "Berlin, CT",
                    Country = "United States",
                    Asin = "B08XYZ1234",
                    Upc = "123456789012",
                    Gtin = "01234567890123",
                    Ean = "1234567890123",
                    CompanyId = companies[0].Id,
                    ProductId = products[0].Id,
                    CertificationTypeId = certTypes[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-5637",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 11, 1),
                    ExpirationDate = new DateTime(2025, 10, 31),
                    Location = "Cedar City, UT",
                    Country = "United States",
                    Asin = "B09ABC5678",
                    Upc = "987654321098",
                    CompanyId = companies[1].Id,
                    ProductId = products[1].Id,
                    CertificationTypeId = certTypes[1].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-5712",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 11, 25),
                    ExpirationDate = new DateTime(2027, 11, 24),
                    Location = "Ashdown, AR",
                    Country = "United States",
                    Upc = "456789012345",
                    CompanyId = companies[2].Id,
                    ProductId = products[2].Id,
                    CertificationTypeId = certTypes[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-5633",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 10, 1),
                    ExpirationDate = new DateTime(2027, 9, 30),
                    Location = "Hoisington, KS",
                    Country = "United States",
                    Asin = "B09XYZ7890",
                    Upc = "234567890123",
                    CompanyId = companies[3].Id,
                    ProductId = products[3].Id,
                    CertificationTypeId = certTypes[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-2108",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 10, 1),
                    ExpirationDate = new DateTime(2027, 9, 30),
                    Location = "Dalton, GA",
                    Country = "United States",
                    Asin = "B09DEF1234",
                    Upc = "345678901234",
                    CompanyId = companies[4].Id,
                    ProductId = products[4].Id,
                    CertificationTypeId = certTypes[2].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.Certificates.AddRangeAsync(certificates);
            await context.SaveChangesAsync();

            // Seed Tags
            var tags = new List<Tag>
            {
                new Tag { Name = "Environmental Facts", TagType = "external", Description = "Environmental impact data", IsActive = true },
                new Tag { Name = "Recycled Content", TagType = "external", Description = "Contains recycled materials", IsActive = true },
                new Tag { Name = "Carbon Neutral", TagType = "external", Description = "Carbon neutral product", IsActive = true },
                new Tag { Name = "High Priority Client", TagType = "internal", Description = "Important client certification", IsActive = true },
                new Tag { Name = "Q4 Review", TagType = "internal", Description = "Scheduled for quarterly review", IsActive = true }
            };

            await context.Tags.AddRangeAsync(tags);
            await context.SaveChangesAsync();

            // Seed Roles and Permissions
            var permissions = new List<Permission>
            {
                new Permission { Name = "View Certificates", Codename = "certificate_view", Category = "Certificate", Description = "Can view certificates" },
                new Permission { Name = "Create Certificates", Codename = "certificate_create", Category = "Certificate", Description = "Can create new certificates" },
                new Permission { Name = "Edit Certificates", Codename = "certificate_edit", Category = "Certificate", Description = "Can edit existing certificates" },
                new Permission { Name = "Delete Certificates", Codename = "certificate_delete", Category = "Certificate", Description = "Can delete certificates" },
                new Permission { Name = "Manage Users", Codename = "user_manage", Category = "User", Description = "Can manage user accounts" },
                new Permission { Name = "View Reports", Codename = "report_view", Category = "Report", Description = "Can view system reports" }
            };

            await context.Permissions.AddRangeAsync(permissions);
            await context.SaveChangesAsync();

            var roles = new List<Role>
            {
                new Role { Name = "Admin", Description = "Full system access", IsSystem = true },
                new Role { Name = "Analyst", Description = "Certificate management access", IsSystem = true },
                new Role { Name = "External", Description = "External user access", IsSystem = true },
                new Role { Name = "Public", Description = "Public read-only access", IsSystem = true }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();

            // Assign permissions to roles
            var adminRole = roles.First(r => r.Name == "Admin");
            var analystRole = roles.First(r => r.Name == "Analyst");
            var externalRole = roles.First(r => r.Name == "External");
            var publicRole = roles.First(r => r.Name == "Public");

            // Admin gets all permissions
            adminRole.Permissions = permissions.ToList();
            
            // Analyst gets certificate management permissions
            analystRole.Permissions = permissions.Where(p => p.Category == "Certificate" && p.Codename != "certificate_delete").ToList();
            
            // External gets view and reports
            externalRole.Permissions = permissions.Where(p => p.Codename == "certificate_view" || p.Codename == "report_view").ToList();
            
            // Public gets only view
            publicRole.Permissions = permissions.Where(p => p.Codename == "certificate_view").ToList();

            await context.SaveChangesAsync();
        }
    }
}
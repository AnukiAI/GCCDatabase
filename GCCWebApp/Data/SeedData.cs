using GCCWebApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GCCWebApp.Data
{
    public static class SeedData
    {
        private static string GenerateRandomAlphaNumeric(Random random, int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

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
                new ProductCategory { Name = "Building Materials", Description = "Building materials and construction products", SortOrder = 1, IsActive = true },
                new ProductCategory { Name = "Electronics / Electrical", Description = "Electronic and electrical products", SortOrder = 2, IsActive = true },
                new ProductCategory { Name = "Materials / Plastics", Description = "Plastic materials and products", SortOrder = 3, IsActive = true },
                new ProductCategory { Name = "Paper Products", Description = "Paper and paper-based products", SortOrder = 4, IsActive = true },
                new ProductCategory { Name = "Textiles", Description = "Textile materials", SortOrder = 5, IsActive = true },
                new ProductCategory { Name = "Consumer Products", Description = "Consumer goods and products", SortOrder = 6, IsActive = true }
            };

            await context.ProductCategories.AddRangeAsync(categories);
            await context.SaveChangesAsync();

            // Seed CertificationTypes (based on mockups)
            var certTypes = new List<CertificationType>
            {
                new CertificationType { Name = "Certified Environmental Facts", AmazonCode = "CEF", Description = "Environmental impact certification", IsActive = true, SortOrder = 1 },
                new CertificationType { Name = "Certified Sustainability Facts", AmazonCode = "CSF", Description = "Comprehensive sustainability certification", IsActive = true, SortOrder = 2 },
                new CertificationType { Name = "Recycled Content", AmazonCode = "RC", Description = "Recycled content certification", IsActive = true, SortOrder = 3 },
                new CertificationType { Name = "Energy Savings", AmazonCode = "ES", Description = "Energy efficiency certification", IsActive = true, SortOrder = 4 },
                new CertificationType { Name = "Material Ingredient Reporting", AmazonCode = "MIR", Description = "Material composition reporting", IsActive = true, SortOrder = 5 }
            };

            await context.CertificationTypes.AddRangeAsync(certTypes);
            await context.SaveChangesAsync();

            // Seed Partners (based on requirements - Amazon, Wayfair, Other)
            var partners = new List<Partner>
            {
                new Partner { Name = "Amazon", Description = "Global e-commerce marketplace specializing in certified sustainable products", Website = "https://www.amazon.com", SortOrder = 1, IsActive = true },
                new Partner { Name = "Wayfair", Description = "Online furniture and home goods retailer focused on sustainable product offerings", Website = "https://www.wayfair.com", SortOrder = 2, IsActive = true },
                new Partner { Name = "Other", Description = "Other retail partners and general partner category", SortOrder = 3, IsActive = true }
            };

            await context.Partners.AddRangeAsync(partners);
            await context.SaveChangesAsync();

            // Seed Companies (based on mockups and real data)
            var companies = new List<Company>
            {
                new Company { Name = "ASSA ABLOY Access & Egress Hardware Group, Inc. - Corbin Russwin Division", ContactInfo = "sustainability@assaabloy.com", Location = "Berlin, CT", Country = "United States", IsActive = true },
                new Company { Name = "Green Products Inc. - Environmental Division", ContactInfo = "contact@greenproducts.com", Location = "Portland, OR", Country = "United States", IsActive = true },
                new Company { Name = "Acme Corporation", ContactInfo = "info@acme.com", Location = "New York, NY", Country = "United States", IsActive = true, Website = "https://www.acme.com" },
                new Company { Name = "Globex Industries", ContactInfo = "sales@globex.com", Location = "Chicago, IL", Country = "United States", IsActive = true, Website = "https://www.globex.com" },
                new Company { Name = "Stark Enterprises", ContactInfo = "hello@stark.com", Location = "Malibu, CA", Country = "United States", IsActive = true, Website = "https://www.stark.com" },
                new Company { Name = "Wayne Technologies", ContactInfo = "contact@waynetech.com", Location = "Gotham, NY", Country = "United States", IsActive = true, Website = "https://www.waynetech.com" },
                new Company { Name = "EcoFlex Manufacturing", ContactInfo = "info@ecoflex.com", Location = "Austin, TX", Country = "United States", IsActive = true },
                new Company { Name = "GAF Materials Corporation", ContactInfo = "green@gaf.com", Location = "Cedar City, UT", Country = "United States", IsActive = true },
                new Company { Name = "3M Company", ContactInfo = "sustainability@3m.com", Location = "St. Paul, MN", Country = "United States", IsActive = true },
                new Company { Name = "Shaw Industries Group", ContactInfo = "green@shawinc.com", Location = "Dalton, GA", Country = "United States", IsActive = true },
                new Company { Name = "Mohawk Industries", ContactInfo = "sustainability@mohawkind.com", Location = "Calhoun, GA", Country = "United States", IsActive = true },
                new Company { Name = "Armstrong World Industries", ContactInfo = "green@armstrong.com", Location = "Lancaster, PA", Country = "United States", IsActive = true },
                new Company { Name = "Owens Corning", ContactInfo = "sustainability@owenscorning.com", Location = "Toledo, OH", Country = "United States", IsActive = true },
                
                // Additional realistic companies for more comprehensive testing
                new Company { Name = "CertainTeed Corporation", ContactInfo = "sustainability@certainteed.com", Location = "Malvern, PA", Country = "United States", IsActive = true },
                new Company { Name = "Johns Manville", ContactInfo = "environmental@jm.com", Location = "Denver, CO", Country = "United States", IsActive = true },
                new Company { Name = "Kingspan Insulation", ContactInfo = "green@kingspan.com", Location = "Atlanta, GA", Country = "United States", IsActive = true },
                new Company { Name = "Hunter Douglas", ContactInfo = "sustainability@hunterdouglas.com", Location = "Pearl River, NY", Country = "United States", IsActive = true },
                new Company { Name = "Steelcase Inc.", ContactInfo = "sustainability@steelcase.com", Location = "Grand Rapids, MI", Country = "United States", IsActive = true },
                new Company { Name = "Herman Miller Inc.", ContactInfo = "green@hermanmiller.com", Location = "Zeeland, MI", Country = "United States", IsActive = true },
                new Company { Name = "Knoll Inc.", ContactInfo = "sustainability@knoll.com", Location = "East Greenville, PA", Country = "United States", IsActive = true },
                new Company { Name = "Teknion Corporation", ContactInfo = "environmental@teknion.com", Location = "Toronto, ON", Country = "Canada", IsActive = true },
                new Company { Name = "Forbo Flooring Systems", ContactInfo = "sustainability@forbo.com", Location = "Hazleton, PA", Country = "United States", IsActive = true },
                new Company { Name = "Tarkett USA Inc.", ContactInfo = "green@tarkett.com", Location = "Solon, OH", Country = "United States", IsActive = true },
                new Company { Name = "Milliken & Company", ContactInfo = "sustainability@milliken.com", Location = "Spartanburg, SC", Country = "United States", IsActive = true },
                new Company { Name = "USG Corporation", ContactInfo = "environmental@usg.com", Location = "Chicago, IL", Country = "United States", IsActive = true },
                new Company { Name = "Lutron Electronics", ContactInfo = "sustainability@lutron.com", Location = "Coopersburg, PA", Country = "United States", IsActive = true },
                new Company { Name = "Philips Lighting", ContactInfo = "green@philips.com", Location = "Somerset, NJ", Country = "United States", IsActive = true },
                new Company { Name = "Cree Lighting", ContactInfo = "sustainability@cree.com", Location = "Durham, NC", Country = "United States", IsActive = true }
            };

            await context.Companies.AddRangeAsync(companies);
            await context.SaveChangesAsync();

            // Seed ProductTypes with hierarchical structure to match frontend
            var productTypes = new List<ProductType>
            {
                // Building Materials Category (categories[0])
                new ProductType { Name = "Insulation", CategoryId = categories[0].Id, Level = 1, SortOrder = 1, IsActive = true },
                new ProductType { Name = "Appliances", CategoryId = categories[0].Id, Level = 1, SortOrder = 2, IsActive = true },
                new ProductType { Name = "Flooring", CategoryId = categories[0].Id, Level = 1, SortOrder = 3, IsActive = true },
                
                // Textiles Category (categories[4])
                new ProductType { Name = "Carpeting", CategoryId = categories[4].Id, Level = 1, SortOrder = 1, IsActive = true },
                new ProductType { Name = "Fabric", CategoryId = categories[4].Id, Level = 1, SortOrder = 2, IsActive = true },
                
                // Electronics Category (categories[1])  
                new ProductType { Name = "Network Cables", CategoryId = categories[1].Id, Level = 1, SortOrder = 1, IsActive = true },
                new ProductType { Name = "Lighting", CategoryId = categories[1].Id, Level = 1, SortOrder = 2, IsActive = true },
                
                // Consumer Products Category (categories[5])
                new ProductType { Name = "Furniture", CategoryId = categories[5].Id, Level = 1, SortOrder = 1, IsActive = true }
            };

            await context.ProductTypes.AddRangeAsync(productTypes);
            await context.SaveChangesAsync();

            // Add subcategories (children of product types)
            var productSubTypes = new List<ProductType>
            {
                // Insulation subcategories
                new ProductType { Name = "Foam Board", CategoryId = categories[0].Id, ParentId = productTypes[0].Id, Level = 2, SortOrder = 1, IsActive = true },
                
                // Appliances subcategories  
                new ProductType { Name = "Kitchen Appliances", CategoryId = categories[0].Id, ParentId = productTypes[1].Id, Level = 2, SortOrder = 1, IsActive = true },
                new ProductType { Name = "Laundry Appliances", CategoryId = categories[0].Id, ParentId = productTypes[1].Id, Level = 2, SortOrder = 2, IsActive = true },
                
                // Flooring subcategories
                new ProductType { Name = "Hardwood", CategoryId = categories[0].Id, ParentId = productTypes[2].Id, Level = 2, SortOrder = 1, IsActive = true },
                new ProductType { Name = "Laminate", CategoryId = categories[0].Id, ParentId = productTypes[2].Id, Level = 2, SortOrder = 2, IsActive = true },
                
                // Carpeting subcategories
                new ProductType { Name = "Commercial Carpet", CategoryId = categories[4].Id, ParentId = productTypes[3].Id, Level = 2, SortOrder = 1, IsActive = true },
                new ProductType { Name = "Residential Carpet", CategoryId = categories[4].Id, ParentId = productTypes[3].Id, Level = 2, SortOrder = 2, IsActive = true },
                
                // Fabric subcategories
                new ProductType { Name = "Upholstery Fabric", CategoryId = categories[4].Id, ParentId = productTypes[4].Id, Level = 2, SortOrder = 1, IsActive = true },
                new ProductType { Name = "Apparel Fabric", CategoryId = categories[4].Id, ParentId = productTypes[4].Id, Level = 2, SortOrder = 2, IsActive = true },
                
                // Furniture subcategories (productTypes[7] is Furniture)
                new ProductType { Name = "Office Chairs", CategoryId = categories[5].Id, ParentId = productTypes[7].Id, Level = 2, SortOrder = 1, IsActive = true },
                new ProductType { Name = "Tables", CategoryId = categories[5].Id, ParentId = productTypes[7].Id, Level = 2, SortOrder = 2, IsActive = true }
            };

            await context.ProductTypes.AddRangeAsync(productSubTypes);
            await context.SaveChangesAsync();

            // Seed Products (based on mockups)
            var products = new List<Product>
            {
                new Product { Name = "900ET Exit Device Trim with EcoFlex®", Description = "Exit device trim with sustainable EcoFlex technology", CategoryId = categories[0].Id, ProductTypeId = productTypes[0].Id, CompanyId = companies[0].Id, IsActive = true },
                new Product { Name = "Standard Exit Device - Model 9800", Description = "Commercial grade exit device", CategoryId = categories[0].Id, ProductTypeId = productTypes[0].Id, CompanyId = companies[0].Id, IsActive = true },
                new Product { Name = "Heavy-Duty Exit Hardware - Series 2000", Description = "Heavy-duty commercial exit hardware", CategoryId = categories[0].Id, ProductTypeId = productTypes[0].Id, CompanyId = companies[0].Id, IsActive = true },
                new Product { Name = "Commercial Door Closer - DC300", Description = "Commercial grade door closing mechanism", CategoryId = categories[0].Id, ProductTypeId = productTypes[0].Id, CompanyId = companies[0].Id, IsActive = true },
                new Product { Name = "Security Exit Device - EX700", Description = "High-security exit device system", CategoryId = categories[0].Id, ProductTypeId = productTypes[0].Id, CompanyId = companies[0].Id, IsActive = true },
                new Product { Name = "Reusable Water Bottle - Stainless Steel", Description = "Eco-friendly stainless steel water bottle", CategoryId = categories[5].Id, CompanyId = companies[1].Id, IsActive = true },
                new Product { Name = "Bamboo Coffee Cup with Lid", Description = "Sustainable bamboo coffee cup", CategoryId = categories[5].Id, CompanyId = companies[1].Id, IsActive = true },
                new Product { Name = "Glass Storage Container Set", Description = "Reusable glass storage containers", CategoryId = categories[5].Id, CompanyId = companies[1].Id, IsActive = true },
                new Product { Name = "Organic Cotton Tote Bag", Description = "Sustainable organic cotton tote bag", CategoryId = categories[4].Id, CompanyId = companies[1].Id, IsActive = true },
                new Product { Name = "10Gain XP Category 6A Cable", Description = "High-performance network cable", CategoryId = categories[1].Id, ProductTypeId = productTypes[5].Id, CompanyId = companies[3].Id, IsActive = true },
                new Product { Name = "Carbon Negative Carpet Tile", Description = "Sustainable carpet tiles", CategoryId = categories[4].Id, ProductTypeId = productTypes[3].Id, CompanyId = companies[4].Id, IsActive = true },
                new Product { Name = "3M VHB Tape", Description = "Very High Bond structural glazing tape", CategoryId = categories[0].Id, CompanyId = companies[5].Id, IsActive = true },
                new Product { Name = "Shaw Contract Carpet Tile", Description = "Commercial carpet tile with recycled content", CategoryId = categories[4].Id, ProductTypeId = productTypes[3].Id, CompanyId = companies[6].Id, IsActive = true },
                new Product { Name = "Mohawk Hardwood Flooring", Description = "Sustainable hardwood flooring", CategoryId = categories[0].Id, ProductTypeId = productTypes[2].Id, CompanyId = companies[7].Id, IsActive = true },
                new Product { Name = "Armstrong Ceiling Tiles", Description = "Mineral fiber ceiling tiles", CategoryId = categories[0].Id, CompanyId = companies[8].Id, IsActive = true },
                new Product { Name = "Owens Corning Fiberglas Insulation", Description = "Pink fiberglass insulation batts", CategoryId = categories[0].Id, ProductTypeId = productTypes[0].Id, CompanyId = companies[9].Id, IsActive = true },
                new Product { Name = "LED Light Fixtures", Description = "Energy-efficient LED lighting", CategoryId = categories[1].Id, ProductTypeId = productTypes[6].Id, CompanyId = companies[5].Id, IsActive = true },
                new Product { Name = "Office Chair", Description = "Ergonomic office seating", CategoryId = categories[5].Id, ProductTypeId = productTypes[7].Id, CompanyId = companies[6].Id, IsActive = true },
                
                // Additional realistic products for comprehensive testing
                new Product { Name = "CertainTeed Fiber Glass Insulation", Description = "Sustainable fiberglass insulation with recycled content", CategoryId = categories[0].Id, ProductTypeId = productTypes[0].Id, CompanyId = companies[10].Id, IsActive = true },
                new Product { Name = "Johns Manville Mineral Wool Insulation", Description = "Fire-resistant mineral wool insulation", CategoryId = categories[0].Id, ProductTypeId = productTypes[0].Id, CompanyId = companies[11].Id, IsActive = true },
                new Product { Name = "Kingspan Kooltherm K5 External Wall Board", Description = "High-performance insulation board", CategoryId = categories[0].Id, ProductTypeId = productTypes[0].Id, CompanyId = companies[12].Id, IsActive = true },
                new Product { Name = "Hunter Douglas Architectural Window Treatments", Description = "Energy-efficient window coverings", CategoryId = categories[0].Id, CompanyId = companies[13].Id, IsActive = true },
                new Product { Name = "Steelcase Think Ergonomic Office Chair", Description = "Sustainable office seating with recycled materials", CategoryId = categories[5].Id, ProductTypeId = productTypes[7].Id, CompanyId = companies[14].Id, IsActive = true },
                new Product { Name = "Herman Miller Aeron Chair", Description = "Iconic ergonomic chair with sustainable design", CategoryId = categories[5].Id, ProductTypeId = productTypes[7].Id, CompanyId = companies[15].Id, IsActive = true },
                new Product { Name = "Knoll ReGeneration Chair", Description = "Made from recycled materials", CategoryId = categories[5].Id, ProductTypeId = productTypes[7].Id, CompanyId = companies[16].Id, IsActive = true },
                new Product { Name = "Teknion District Chair", Description = "Sustainable office seating solution", CategoryId = categories[5].Id, ProductTypeId = productTypes[7].Id, CompanyId = companies[17].Id, IsActive = true },
                new Product { Name = "Forbo Marmoleum Flooring", Description = "Natural linoleum flooring with low environmental impact", CategoryId = categories[0].Id, ProductTypeId = productTypes[2].Id, CompanyId = companies[18].Id, IsActive = true },
                new Product { Name = "Tarkett iQ Surface Flooring", Description = "Sustainable homogeneous vinyl flooring", CategoryId = categories[0].Id, ProductTypeId = productTypes[2].Id, CompanyId = companies[19].Id, IsActive = true },
                new Product { Name = "Milliken Modular Carpet", Description = "Carbon neutral carpet tiles", CategoryId = categories[4].Id, ProductTypeId = productTypes[3].Id, CompanyId = companies[20].Id, IsActive = true },
                new Product { Name = "USG Sheetrock Brand Gypsum Panels", Description = "Sustainable drywall with recycled content", CategoryId = categories[0].Id, CompanyId = companies[21].Id, IsActive = true },
                new Product { Name = "Lutron Caseta Smart Dimmer Switch", Description = "Energy-saving smart lighting control", CategoryId = categories[1].Id, ProductTypeId = productTypes[6].Id, CompanyId = companies[22].Id, IsActive = true },
                new Product { Name = "Philips LED Tube Light", Description = "Energy-efficient LED lighting solution", CategoryId = categories[1].Id, ProductTypeId = productTypes[6].Id, CompanyId = companies[23].Id, IsActive = true },
                new Product { Name = "Cree LED High Bay Fixture", Description = "Industrial LED lighting with smart controls", CategoryId = categories[1].Id, ProductTypeId = productTypes[6].Id, CompanyId = companies[24].Id, IsActive = true },
                new Product { Name = "Interface Net-Works Carpet Tiles", Description = "Made from recycled fishing nets", CategoryId = categories[4].Id, ProductTypeId = productTypes[3].Id, CompanyId = companies[4].Id, IsActive = true },
                new Product { Name = "3M Scotchshield Ultra Safety & Security Window Film", Description = "Window protection film", CategoryId = categories[0].Id, CompanyId = companies[5].Id, IsActive = true },
                new Product { Name = "Shaw EcoWorx Carpet Backing", Description = "Recyclable carpet backing system", CategoryId = categories[4].Id, ProductTypeId = productTypes[3].Id, CompanyId = companies[6].Id, IsActive = true },
                new Product { Name = "Armstrong Clean Room Ceiling Panels", Description = "Specialized ceiling tiles for clean environments", CategoryId = categories[0].Id, CompanyId = companies[8].Id, IsActive = true },
                new Product { Name = "GAF TPO Roofing Membrane", Description = "Single-ply roofing membrane", CategoryId = categories[0].Id, CompanyId = companies[1].Id, IsActive = true },
                new Product { Name = "Superior Essex PowerWise Cable", Description = "Energy-efficient data center cable", CategoryId = categories[1].Id, ProductTypeId = productTypes[5].Id, CompanyId = companies[3].Id, IsActive = true }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            // Seed Certificates with mockup data - Primary example from mockup
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Number = "24-1605",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 7, 1),
                    ExpirationDate = new DateTime(2027, 6, 30),
                    Location = "Berlin, CT",
                    Country = "United States",
                    Asin = "B08X7N9MRQ",
                    Upc = "846378985421",
                    Gtin = "00846378985421",
                    Ean = "0846378985421",
                    CompanyId = companies[0].Id,
                    ProductId = products[0].Id,
                    CertificationTypeId = certTypes[0].Id,
                    CreatedAt = new DateTime(2023, 1, 1),
                    UpdatedAt = new DateTime(2023, 4, 15)
                },
                new Certificate
                {
                    Number = "24-1606",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 9, 1),
                    ExpirationDate = new DateTime(2027, 8, 31),
                    Location = "Portland, OR",
                    Country = "United States",
                    Upc = "846378985422",
                    Gtin = "00846378985422",
                    Ean = "0846378985422",
                    CompanyId = companies[1].Id,
                    ProductId = products[5].Id,
                    CertificationTypeId = certTypes[1].Id,
                    CreatedAt = new DateTime(2023, 1, 1),
                    UpdatedAt = new DateTime(2023, 12, 31)
                },
                // Additional ASSA ABLOY products
                new Certificate
                {
                    Number = "24-1607",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 8, 1),
                    ExpirationDate = new DateTime(2027, 7, 31),
                    Location = "Berlin, CT",
                    Country = "United States",
                    CompanyId = companies[0].Id,
                    ProductId = products[1].Id,
                    CertificationTypeId = certTypes[1].Id,
                    CreatedAt = new DateTime(2023, 1, 1),
                    UpdatedAt = new DateTime(2023, 4, 15)
                },
                new Certificate
                {
                    Number = "24-1608",
                    Status = "draft",
                    EffectiveDate = new DateTime(2025, 1, 1),
                    ExpirationDate = new DateTime(2028, 12, 31),
                    Location = "Berlin, CT",
                    Country = "United States",
                    CompanyId = companies[0].Id,
                    ProductId = products[2].Id,
                    CertificationTypeId = certTypes[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-1609",
                    Status = "expired",
                    EffectiveDate = new DateTime(2021, 1, 1),
                    ExpirationDate = new DateTime(2024, 1, 1),
                    Location = "Berlin, CT",
                    Country = "United States",
                    CompanyId = companies[0].Id,
                    ProductId = products[3].Id,
                    CertificationTypeId = certTypes[2].Id,
                    CreatedAt = new DateTime(2021, 1, 1),
                    UpdatedAt = new DateTime(2024, 1, 1)
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
                },
                new Certificate
                {
                    Number = "24-6201",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 12, 1),
                    ExpirationDate = new DateTime(2027, 11, 30),
                    Location = "St. Paul, MN",
                    Country = "United States",
                    Asin = "B10ABC1234",
                    Upc = "456789012346",
                    CompanyId = companies[5].Id,
                    ProductId = products[5].Id,
                    CertificationTypeId = certTypes[1].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-6385",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 11, 15),
                    ExpirationDate = new DateTime(2025, 11, 14),
                    Location = "Dalton, GA",
                    Country = "United States",
                    Upc = "567890123457",
                    CompanyId = companies[6].Id,
                    ProductId = products[6].Id,
                    CertificationTypeId = certTypes[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-7892",
                    Status = "pending",
                    EffectiveDate = new DateTime(2025, 1, 1),
                    ExpirationDate = new DateTime(2028, 12, 31),
                    Location = "Calhoun, GA",
                    Country = "United States",
                    Asin = "B11XYZ5678",
                    Upc = "678901234568",
                    CompanyId = companies[7].Id,
                    ProductId = products[7].Id,
                    CertificationTypeId = certTypes[2].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-8134",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 9, 15),
                    ExpirationDate = new DateTime(2027, 9, 14),
                    Location = "Lancaster, PA",
                    Country = "United States",
                    Upc = "789012345679",
                    CompanyId = companies[8].Id,
                    ProductId = products[8].Id,
                    CertificationTypeId = certTypes[3].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-9567",
                    Status = "expired",
                    EffectiveDate = new DateTime(2021, 6, 1),
                    ExpirationDate = new DateTime(2024, 5, 31),
                    Location = "Toledo, OH",
                    Country = "United States",
                    Asin = "B12PQR9012",
                    Upc = "890123456780",
                    CompanyId = companies[9].Id,
                    ProductId = products[9].Id,
                    CertificationTypeId = certTypes[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                
                // Additional comprehensive certificates for testing
                new Certificate
                {
                    Number = "25-0001",
                    Status = "active",
                    EffectiveDate = new DateTime(2025, 1, 1),
                    ExpirationDate = new DateTime(2028, 12, 31),
                    Location = "Malvern, PA",
                    Country = "United States",
                    Asin = "B25CER0001",
                    Upc = "125478963014",
                    Gtin = "01254789630142",
                    Ean = "1254789630142",
                    CompanyId = companies[10].Id,
                    ProductId = products[12].Id,
                    CertificationTypeId = certTypes[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "25-0002",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 8, 15),
                    ExpirationDate = new DateTime(2027, 8, 14),
                    Location = "Denver, CO",
                    Country = "United States",
                    Asin = "B25JOH0002",
                    Upc = "147852369025",
                    CompanyId = companies[11].Id,
                    ProductId = products[13].Id,
                    CertificationTypeId = certTypes[3].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "25-0003",
                    Status = "pending",
                    EffectiveDate = new DateTime(2025, 3, 1),
                    ExpirationDate = new DateTime(2028, 2, 28),
                    Location = "Atlanta, GA",
                    Country = "United States",
                    Upc = "963741852036",
                    CompanyId = companies[12].Id,
                    ProductId = products[14].Id,
                    CertificationTypeId = certTypes[4].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-9876",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 7, 1),
                    ExpirationDate = new DateTime(2026, 6, 30),
                    Location = "Pearl River, NY",
                    Country = "United States",
                    Asin = "B24HUN9876",
                    Upc = "852741963047",
                    CompanyId = companies[13].Id,
                    ProductId = products[15].Id,
                    CertificationTypeId = certTypes[1].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-8765",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 6, 1),
                    ExpirationDate = new DateTime(2027, 5, 31),
                    Location = "Grand Rapids, MI",
                    Country = "United States",
                    Asin = "B24STE8765",
                    Upc = "741852963058",
                    Gtin = "07418529630589",
                    CompanyId = companies[14].Id,
                    ProductId = products[16].Id,
                    CertificationTypeId = certTypes[2].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-7654",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 5, 15),
                    ExpirationDate = new DateTime(2027, 5, 14),
                    Location = "Zeeland, MI",
                    Country = "United States",
                    Asin = "B24HER7654",
                    Upc = "630741852069",
                    CompanyId = companies[15].Id,
                    ProductId = products[17].Id,
                    CertificationTypeId = certTypes[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-6543",
                    Status = "suspended",
                    EffectiveDate = new DateTime(2024, 4, 1),
                    ExpirationDate = new DateTime(2027, 3, 31),
                    Location = "East Greenville, PA",
                    Country = "United States",
                    Upc = "529630741870",
                    CompanyId = companies[16].Id,
                    ProductId = products[18].Id,
                    CertificationTypeId = certTypes[1].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-5432",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 3, 1),
                    ExpirationDate = new DateTime(2026, 2, 28),
                    Location = "Toronto, ON",
                    Country = "Canada",
                    Asin = "B24TEK5432",
                    Upc = "418529630781",
                    CompanyId = companies[17].Id,
                    ProductId = products[19].Id,
                    CertificationTypeId = certTypes[3].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-4321",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 2, 1),
                    ExpirationDate = new DateTime(2027, 1, 31),
                    Location = "Hazleton, PA",
                    Country = "United States",
                    Upc = "307418529692",
                    CompanyId = companies[18].Id,
                    ProductId = products[20].Id,
                    CertificationTypeId = certTypes[4].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-3210",
                    Status = "expiring_soon",
                    EffectiveDate = new DateTime(2022, 1, 1),
                    ExpirationDate = new DateTime(2025, 3, 31),
                    Location = "Solon, OH",
                    Country = "United States",
                    Asin = "B24TAR3210",
                    Upc = "196307418503",
                    CompanyId = companies[19].Id,
                    ProductId = products[21].Id,
                    CertificationTypeId = certTypes[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-2109",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 1, 1),
                    ExpirationDate = new DateTime(2026, 12, 31),
                    Location = "Spartanburg, SC",
                    Country = "United States",
                    Upc = "085296307414",
                    CompanyId = companies[20].Id,
                    ProductId = products[22].Id,
                    CertificationTypeId = certTypes[2].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "23-9999",
                    Status = "expired",
                    EffectiveDate = new DateTime(2020, 12, 1),
                    ExpirationDate = new DateTime(2023, 11, 30),
                    Location = "Chicago, IL",
                    Country = "United States",
                    Asin = "B23USG9999",
                    Upc = "741963085225",
                    CompanyId = companies[21].Id,
                    ProductId = products[23].Id,
                    CertificationTypeId = certTypes[1].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-1098",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 12, 1),
                    ExpirationDate = new DateTime(2027, 11, 30),
                    Location = "Coopersburg, PA",
                    Country = "United States",
                    Upc = "630852741936",
                    CompanyId = companies[22].Id,
                    ProductId = products[24].Id,
                    CertificationTypeId = certTypes[3].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-0987",
                    Status = "active",
                    EffectiveDate = new DateTime(2024, 11, 1),
                    ExpirationDate = new DateTime(2027, 10, 31),
                    Location = "Somerset, NJ",
                    Country = "United States",
                    Asin = "B24PHI0987",
                    Upc = "519630852747",
                    Gtin = "05196308527478",
                    CompanyId = companies[23].Id,
                    ProductId = products[25].Id,
                    CertificationTypeId = certTypes[4].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Certificate
                {
                    Number = "24-0876",
                    Status = "draft",
                    EffectiveDate = new DateTime(2025, 2, 1),
                    ExpirationDate = new DateTime(2028, 1, 31),
                    Location = "Durham, NC",
                    Country = "United States",
                    Upc = "408529630758",
                    CompanyId = companies[24].Id,
                    ProductId = products[26].Id,
                    CertificationTypeId = certTypes[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            // Add many more certificates for lazy loading testing
            var additionalCertificates = new List<Certificate>();
            var random = new Random(42); // Fixed seed for consistent test data
            var statuses = new[] { "active", "draft", "expired", "pending", "suspended", "expiring_soon" };
            var locations = new[]
            {
                "New York, NY", "Los Angeles, CA", "Chicago, IL", "Houston, TX", "Phoenix, AZ",
                "Philadelphia, PA", "San Antonio, TX", "San Diego, CA", "Dallas, TX", "San Jose, CA",
                "Austin, TX", "Jacksonville, FL", "Fort Worth, TX", "Columbus, OH", "Charlotte, NC",
                "San Francisco, CA", "Indianapolis, IN", "Seattle, WA", "Denver, CO", "Washington, DC",
                "Boston, MA", "El Paso, TX", "Nashville, TN", "Detroit, MI", "Oklahoma City, OK",
                "Portland, OR", "Las Vegas, NV", "Memphis, TN", "Louisville, KY", "Baltimore, MD",
                "Milwaukee, WI", "Albuquerque, NM", "Tucson, AZ", "Fresno, CA", "Sacramento, CA",
                "Kansas City, MO", "Mesa, AZ", "Atlanta, GA", "Omaha, NE", "Colorado Springs, CO",
                "Raleigh, NC", "Miami, FL", "Oakland, CA", "Minneapolis, MN", "Tulsa, OK",
                "Cleveland, OH", "Wichita, KS", "Arlington, TX", "New Orleans, LA", "Bakersfield, CA"
            };

            // Generate 150 additional certificates for comprehensive lazy loading testing
            for (int i = 1; i <= 150; i++)
            {
                var certNumber = $"{(2024 + (i % 2)) % 100:D2}-{(5000 + i):D4}";
                var status = statuses[random.Next(statuses.Length)];
                var location = locations[random.Next(locations.Length)];
                var companyId = companies[random.Next(companies.Count)].Id;
                var productId = products[random.Next(products.Count)].Id;
                var certTypeId = certTypes[random.Next(certTypes.Count)].Id;

                var effectiveDate = DateTime.UtcNow.AddDays(random.Next(-1095, 365)); // Between 3 years ago and 1 year from now
                var expirationDate = effectiveDate.AddYears(random.Next(1, 4)); // 1-3 years after effective date

                // Adjust status based on dates for realism
                if (expirationDate < DateTime.UtcNow)
                    status = "expired";
                else if (expirationDate < DateTime.UtcNow.AddMonths(3))
                    status = "expiring_soon";
                else if (effectiveDate > DateTime.UtcNow)
                    status = random.Next(2) == 0 ? "draft" : "pending";
                else
                    status = "active";

                var certificate = new Certificate
                {
                    Number = certNumber,
                    Status = status,
                    EffectiveDate = effectiveDate,
                    ExpirationDate = expirationDate,
                    Location = location,
                    Country = "United States",
                    CompanyId = companyId,
                    ProductId = productId,
                    CertificationTypeId = certTypeId,
                    CreatedAt = effectiveDate.AddDays(-random.Next(1, 30)),
                    UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 7))
                };

                // Add product IDs randomly to some certificates
                if (random.Next(4) == 0) // 25% chance
                {
                    certificate.Upc = $"{random.Next(100000000, 999999999)}{random.Next(1000, 9999)}";
                }
                
                if (random.Next(5) == 0) // 20% chance
                {
                    certificate.Asin = $"B{random.Next(10, 99)}{GenerateRandomAlphaNumeric(random, 8)}";
                }
                
                if (random.Next(6) == 0) // ~17% chance
                {
                    certificate.Gtin = $"0{certificate.Upc ?? $"{random.Next(100000000, 999999999)}{random.Next(1000, 9999)}"}";
                }
                
                if (random.Next(7) == 0) // ~14% chance
                {
                    certificate.Ean = certificate.Gtin ?? certificate.Upc ?? $"{random.Next(100000000, 999999999)}{random.Next(1000, 9999)}";
                }

                additionalCertificates.Add(certificate);
            }

            certificates.AddRange(additionalCertificates);

            await context.Certificates.AddRangeAsync(certificates);
            await context.SaveChangesAsync();

            // Seed Tags with comprehensive examples
            var tags = new List<Tag>
            {
                new Tag { Name = "Environmental Facts", TagType = "external", Description = "Environmental impact data", IsActive = true },
                new Tag { Name = "Recycled Content", TagType = "external", Description = "Contains recycled materials", IsActive = true },
                new Tag { Name = "Carbon Neutral", TagType = "external", Description = "Carbon neutral product", IsActive = true },
                new Tag { Name = "High Priority Client", TagType = "internal", Description = "Important client certification", IsActive = true },
                new Tag { Name = "Q4 Review", TagType = "internal", Description = "Scheduled for quarterly review", IsActive = true },
                
                // Additional realistic tags for comprehensive testing
                new Tag { Name = "Energy Star", TagType = "external", Description = "Energy Star certified product", IsActive = true },
                new Tag { Name = "LEED Points", TagType = "external", Description = "Contributes to LEED certification", IsActive = true },
                new Tag { Name = "Cradle to Cradle", TagType = "external", Description = "Cradle to Cradle certified", IsActive = true },
                new Tag { Name = "Forest Stewardship Council", TagType = "external", Description = "FSC certified materials", IsActive = true },
                new Tag { Name = "GREENGUARD", TagType = "external", Description = "Low chemical emissions", IsActive = true },
                new Tag { Name = "Bio-Based", TagType = "external", Description = "Made from renewable biological resources", IsActive = true },
                new Tag { Name = "Renewable Energy", TagType = "external", Description = "Manufactured using renewable energy", IsActive = true },
                new Tag { Name = "Circular Economy", TagType = "external", Description = "Designed for circular economy principles", IsActive = true },
                new Tag { Name = "Water Stewardship", TagType = "external", Description = "Responsible water usage in manufacturing", IsActive = true },
                new Tag { Name = "Fair Trade", TagType = "external", Description = "Fair trade certified materials", IsActive = true },
                
                // Internal operation tags
                new Tag { Name = "Urgent Review", TagType = "internal", Description = "Requires immediate attention", IsActive = true },
                new Tag { Name = "Annual Audit", TagType = "internal", Description = "Scheduled for annual audit", IsActive = true },
                new Tag { Name = "Customer Request", TagType = "internal", Description = "Generated from customer inquiry", IsActive = true },
                new Tag { Name = "Regulatory Compliance", TagType = "internal", Description = "Required for regulatory compliance", IsActive = true },
                new Tag { Name = "Market Expansion", TagType = "internal", Description = "Supporting new market entry", IsActive = true },
                new Tag { Name = "Partnership Program", TagType = "internal", Description = "Part of strategic partnership", IsActive = true },
                new Tag { Name = "Innovation Project", TagType = "internal", Description = "Supporting innovation initiative", IsActive = true },
                new Tag { Name = "Cost Optimization", TagType = "internal", Description = "Cost reduction initiative", IsActive = true },
                new Tag { Name = "Quality Improvement", TagType = "internal", Description = "Quality enhancement program", IsActive = true },
                new Tag { Name = "Digital Transformation", TagType = "internal", Description = "Digital transformation project", IsActive = true }
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

            // Seed Sample Users (for testing)
            var users = new List<User>
            {
                new User 
                { 
                    Username = "admin",
                    Email = "admin@gcc.com", 
                    Name = "Admin", 
                    FullName = "Admin User", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true, 
                    IsStaff = true,
                    IsSuperuser = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User 
                { 
                    Username = "analyst",
                    Email = "analyst@gcc.com", 
                    Name = "Jane", 
                    FullName = "Jane Analyst", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true, 
                    IsStaff = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User 
                { 
                    Username = "external",
                    Email = "external@company.com", 
                    Name = "External", 
                    FullName = "External User", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User 
                { 
                    Username = "testuser",
                    Email = "test@gcc.com", 
                    Name = "Test", 
                    FullName = "Test User", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                
                // Additional realistic users for comprehensive testing
                new User 
                { 
                    Username = "sarah.johnson",
                    Email = "sarah.johnson@gcc.com", 
                    Name = "Sarah", 
                    FullName = "Sarah Johnson", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true, 
                    IsStaff = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User 
                { 
                    Username = "mike.chen",
                    Email = "mike.chen@gcc.com", 
                    Name = "Mike", 
                    FullName = "Mike Chen", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true, 
                    IsStaff = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User 
                { 
                    Username = "lisa.rodriguez",
                    Email = "lisa.rodriguez@assaabloy.com", 
                    Name = "Lisa", 
                    FullName = "Lisa Rodriguez", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User 
                { 
                    Username = "david.kim",
                    Email = "david.kim@3m.com", 
                    Name = "David", 
                    FullName = "David Kim", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User 
                { 
                    Username = "emma.wilson",
                    Email = "emma.wilson@interface.com", 
                    Name = "Emma", 
                    FullName = "Emma Wilson", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User 
                { 
                    Username = "alex.martinez",
                    Email = "alex.martinez@steelcase.com", 
                    Name = "Alex", 
                    FullName = "Alex Martinez", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User 
                { 
                    Username = "jennifer.lee",
                    Email = "jennifer.lee@gcc.com", 
                    Name = "Jennifer", 
                    FullName = "Jennifer Lee", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true, 
                    IsStaff = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User 
                { 
                    Username = "robert.taylor",
                    Email = "robert.taylor@gcc.com", 
                    Name = "Robert", 
                    FullName = "Robert Taylor", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true, 
                    IsStaff = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User 
                { 
                    Username = "maria.garcia",
                    Email = "maria.garcia@mohawkind.com", 
                    Name = "Maria", 
                    FullName = "Maria Garcia", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User 
                { 
                    Username = "public.user",
                    Email = "public@example.com", 
                    Name = "Public", 
                    FullName = "Public User", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            // Assign roles to users
            var adminUser = users.First(u => u.Email == "admin@gcc.com");
            var analystUser = users.First(u => u.Email == "analyst@gcc.com");
            var externalUser = users.First(u => u.Email == "external@company.com");
            var testUser = users.First(u => u.Email == "test@gcc.com");
            var sarahUser = users.First(u => u.Email == "sarah.johnson@gcc.com");
            var mikeUser = users.First(u => u.Email == "mike.chen@gcc.com");
            var lisaUser = users.First(u => u.Email == "lisa.rodriguez@assaabloy.com");
            var davidUser = users.First(u => u.Email == "david.kim@3m.com");
            var emmaUser = users.First(u => u.Email == "emma.wilson@interface.com");
            var alexUser = users.First(u => u.Email == "alex.martinez@steelcase.com");
            var jenniferUser = users.First(u => u.Email == "jennifer.lee@gcc.com");
            var robertUser = users.First(u => u.Email == "robert.taylor@gcc.com");
            var mariaUser = users.First(u => u.Email == "maria.garcia@mohawkind.com");
            var publicUser = users.First(u => u.Email == "public@example.com");

            // Assign appropriate roles
            adminUser.Roles = new List<Role> { adminRole };
            analystUser.Roles = new List<Role> { analystRole };
            sarahUser.Roles = new List<Role> { analystRole };
            mikeUser.Roles = new List<Role> { analystRole };
            jenniferUser.Roles = new List<Role> { analystRole };
            robertUser.Roles = new List<Role> { adminRole };
            
            // External company users
            externalUser.Roles = new List<Role> { externalRole };
            lisaUser.Roles = new List<Role> { externalRole };
            davidUser.Roles = new List<Role> { externalRole };
            emmaUser.Roles = new List<Role> { externalRole };
            alexUser.Roles = new List<Role> { externalRole };
            mariaUser.Roles = new List<Role> { externalRole };
            
            // Public users
            testUser.Roles = new List<Role> { publicRole };
            publicUser.Roles = new List<Role> { publicRole };

            await context.SaveChangesAsync();
            
            // Seed API Keys for testing
            var apiKeys = new List<ApiKey>
            {
                new ApiKey
                {
                    Name = "Integration Test Key",
                    Key = "gcc_test_key_1234567890abcdef",
                    KeyHash = "test_key_hash_1234567890abcdef",
                    Description = "API key for integration testing",
                    CreatedBy = adminUser.Username,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddYears(1),
                    Environment = "Development",
                    Scopes = "read,write,admin"
                },
                new ApiKey
                {
                    Name = "Partner API Access",
                    Key = "gcc_partner_key_abcdef1234567890",
                    KeyHash = "partner_key_hash_abcdef1234567890",
                    Description = "API access for external partners",
                    CreatedBy = externalUser.Username,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow.AddDays(-5),
                    ExpiresAt = DateTime.UtcNow.AddMonths(6),
                    Environment = "Production",
                    Scopes = "read"
                },
                new ApiKey
                {
                    Name = "Mobile App Key",
                    Key = "gcc_mobile_key_fedcba0987654321",
                    KeyHash = "mobile_key_hash_fedcba0987654321",
                    Description = "API key for mobile application",
                    CreatedBy = analystUser.Username,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    LastUsedAt = DateTime.UtcNow.AddHours(-2),
                    ExpiresAt = DateTime.UtcNow.AddMonths(3),
                    Environment = "Production",
                    Scopes = "read,write"
                },
                new ApiKey
                {
                    Name = "Reporting Service",
                    Key = "gcc_report_key_1a2b3c4d5e6f7890",
                    KeyHash = "report_key_hash_1a2b3c4d5e6f7890",
                    Description = "API key for automated reporting service",
                    CreatedBy = sarahUser.Username,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    LastUsedAt = DateTime.UtcNow.AddMinutes(-30),
                    ExpiresAt = DateTime.UtcNow.AddYears(2),
                    Environment = "Production",
                    Scopes = "read,report"
                },
                new ApiKey
                {
                    Name = "Legacy Integration",
                    Key = "gcc_legacy_key_0987654321abcdef",
                    KeyHash = "legacy_key_hash_0987654321abcdef",
                    Description = "API key for legacy system integration",
                    CreatedBy = mikeUser.Username,
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6),
                    LastUsedAt = DateTime.UtcNow.AddDays(-90),
                    ExpiresAt = DateTime.UtcNow.AddDays(-30),
                    Environment = "Staging",
                    Scopes = "read"
                }
            };

            await context.ApiKeys.AddRangeAsync(apiKeys);
            await context.SaveChangesAsync();
        }
    }
}
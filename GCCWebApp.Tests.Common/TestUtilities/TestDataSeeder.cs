using Bogus;
using GCCWebApp.Models;
using GCCWebApp.Data;

namespace GCCWebApp.Tests.Common.TestUtilities;

/// <summary>
/// Provides standardized test data generation using Bogus
/// </summary>
public static class TestDataSeeder
{
    private static readonly Faker _faker = new();

    /// <summary>
    /// Creates a list of test certificates
    /// </summary>
    public static List<Certificate> CreateTestCertificates(int count = 10)
    {
        var certificateFaker = new Faker<Certificate>()
            .RuleFor(c => c.Id, f => f.Random.Int(1, 10000))
            .RuleFor(c => c.Number, f => f.Random.AlphaNumeric(7))
            .RuleFor(c => c.Status, f => f.PickRandom("active", "expired", "pending"))
            .RuleFor(c => c.EffectiveDate, f => f.Date.Past())
            .RuleFor(c => c.ExpirationDate, f => f.Date.Future())
            .RuleFor(c => c.Location, f => f.Address.City())
            .RuleFor(c => c.Country, f => f.Address.Country())
            .RuleFor(c => c.CompanyId, f => f.Random.Int(1, 100))
            .RuleFor(c => c.ProductId, f => f.Random.Int(1, 1000))
            .RuleFor(c => c.CertificationTypeId, f => f.Random.Int(1, 10))
            .RuleFor(c => c.CreatedAt, f => f.Date.Past())
            .RuleFor(c => c.UpdatedAt, f => f.Date.Recent());

        return certificateFaker.Generate(count);
    }

    /// <summary>
    /// Creates a list of test users
    /// </summary>
    public static List<User> CreateTestUsers(int count = 5)
    {
        var userFaker = new Faker<User>()
            .RuleFor(u => u.Id, f => f.Random.Int(1, 1000))
            .RuleFor(u => u.Username, f => f.Internet.UserName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Name, f => f.Name.FullName())
            .RuleFor(u => u.FullName, f => f.Name.FullName())
            .RuleFor(u => u.PasswordHash, f => f.Internet.Password())
            .RuleFor(u => u.Status, f => f.PickRandom("active", "inactive", "locked"))
            .RuleFor(u => u.IsActive, f => f.Random.Bool(0.9f))
            .RuleFor(u => u.CreatedAt, f => f.Date.Past())
            .RuleFor(u => u.UpdatedAt, f => f.Date.Recent())
            .RuleFor(u => u.LastLogin, f => f.Date.Recent());

        return userFaker.Generate(count);
    }

    /// <summary>
    /// Creates a list of test companies
    /// </summary>
    public static List<Company> CreateTestCompanies(int count = 5)
    {
        var companyFaker = new Faker<Company>()
            .RuleFor(c => c.Id, f => f.Random.Int(1, 1000))
            .RuleFor(c => c.Name, f => f.Company.CompanyName())
            .RuleFor(c => c.ContactInfo, f => f.Internet.Email())
            .RuleFor(c => c.Location, f => f.Address.City())
            .RuleFor(c => c.Country, f => f.Address.Country())
            .RuleFor(c => c.IsActive, f => f.Random.Bool(0.85f))
            .RuleFor(c => c.CreatedAt, f => f.Date.Past())
            .RuleFor(c => c.UpdatedAt, f => f.Date.Recent());

        return companyFaker.Generate(count);
    }

    /// <summary>
    /// Creates a list of test products
    /// </summary>
    public static List<Product> CreateTestProducts(int count = 10)
    {
        var productFaker = new Faker<Product>()
            .RuleFor(p => p.Id, f => f.Random.Int(1, 10000))
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
            .RuleFor(p => p.Model, f => f.Commerce.ProductMaterial())
            .RuleFor(p => p.Sku, f => f.Random.AlphaNumeric(10))
            .RuleFor(p => p.CategoryId, f => f.Random.Int(1, 10))
            .RuleFor(p => p.CompanyId, f => f.Random.Int(1, 100))
            .RuleFor(p => p.IsActive, f => f.Random.Bool(0.8f))
            .RuleFor(p => p.CreatedAt, f => f.Date.Past())
            .RuleFor(p => p.UpdatedAt, f => f.Date.Recent());

        return productFaker.Generate(count);
    }

    /// <summary>
    /// Seeds a database context with comprehensive test data
    /// </summary>
    public static async Task SeedDatabaseAsync(ApplicationDbContext context)
    {
        // Clear existing data
        context.Certificates.RemoveRange(context.Certificates);
        context.Users.RemoveRange(context.Users);
        context.Companies.RemoveRange(context.Companies);
        context.Products.RemoveRange(context.Products);

        // Add test data
        context.Companies.AddRange(CreateTestCompanies(5));
        context.Users.AddRange(CreateTestUsers(10));
        context.Products.AddRange(CreateTestProducts(20));
        context.Certificates.AddRange(CreateTestCertificates(15));

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a single test certificate with specific properties
    /// </summary>
    public static Certificate CreateTestCertificate(
        string? certificateNumber = null,
        string? status = null,
        bool isActive = true)
    {
        return new Certificate
        {
            Id = _faker.Random.Int(1, 10000),
            Number = certificateNumber ?? _faker.Random.AlphaNumeric(7),
            Status = status ?? "active",
            EffectiveDate = _faker.Date.Past(),
            ExpirationDate = _faker.Date.Future(),
            Location = _faker.Address.City(),
            Country = _faker.Address.Country(),
            CompanyId = _faker.Random.Int(1, 100),
            ProductId = _faker.Random.Int(1, 1000),
            CertificationTypeId = _faker.Random.Int(1, 10),
            CreatedAt = _faker.Date.Past(),
            UpdatedAt = _faker.Date.Recent()
        };
    }

    /// <summary>
    /// Creates a test user with specific properties
    /// </summary>
    public static User CreateTestUser(
        string? username = null,
        string? email = null,
        bool isActive = true)
    {
        return new User
        {
            Id = _faker.Random.Int(1, 1000),
            Username = username ?? _faker.Internet.UserName(),
            Email = email ?? _faker.Internet.Email(),
            Name = _faker.Name.FullName(),
            FullName = _faker.Name.FullName(),
            PasswordHash = _faker.Internet.Password(),
            Status = "active",
            IsActive = isActive,
            CreatedAt = _faker.Date.Past(),
            UpdatedAt = _faker.Date.Recent(),
            LastLogin = _faker.Date.Recent(),
            TwoFactorMethod = "Email"
        };
    }

    #region Product ID Test Data Generation

    private static readonly string[] ProductIdTypes = { "ASIN", "UPC", "GTIN", "EAN", "ISBN", "SKU", "MPN" };
    private static readonly string[] Partners = { "Amazon", "Novi", "None", "Other" };
    private static readonly string[] Statuses = { "Active", "Inactive", "Pending", "Expired" };

    /// <summary>
    /// Creates a list of test product IDs (ASINs, UPCs, etc.) for a certificate
    /// </summary>
    /// <param name="count">Number of product IDs to generate</param>
    /// <param name="certificateId">Certificate to associate with</param>
    /// <returns>List of CertificateProductId</returns>
    public static List<CertificateProductId> CreateTestProductIds(int count = 10, int certificateId = 1)
    {
        var productIds = new List<CertificateProductId>();

        for (int i = 0; i < count; i++)
        {
            var type = ProductIdTypes[_faker.Random.Int(0, ProductIdTypes.Length - 1)];
            productIds.Add(new CertificateProductId
            {
                CertificateId = certificateId,
                ProductIdType = type,
                ProductIdValue = GenerateProductIdValue(type, i),
                Status = Statuses[_faker.Random.Int(0, 2)], // Mostly Active/Inactive/Pending
                Partner = Partners[_faker.Random.Int(0, Partners.Length - 1)],
                ProductName = _faker.Commerce.ProductName(),
                ProductSku = $"SKU-{_faker.Random.AlphaNumeric(8).ToUpper()}",
                Description = _faker.Random.Bool(0.7f) ? _faker.Lorem.Sentence() : null,
                IsActive = _faker.Random.Bool(0.95f),
                IsPublic = _faker.Random.Bool(0.9f),
                DisplayOrder = i + 1,
                CreatedAt = _faker.Date.Past(),
                UpdatedAt = _faker.Date.Recent()
            });
        }

        return productIds;
    }

    /// <summary>
    /// Creates a single test product ID with specific properties
    /// </summary>
    public static CertificateProductId CreateTestProductId(
        int certificateId = 1,
        string? type = null,
        string? value = null,
        string? partner = null)
    {
        var productIdType = type ?? "ASIN";
        return new CertificateProductId
        {
            CertificateId = certificateId,
            ProductIdType = productIdType,
            ProductIdValue = value ?? GenerateProductIdValue(productIdType, _faker.Random.Int(1, 100000)),
            Status = "Active",
            Partner = partner ?? "Amazon",
            ProductName = _faker.Commerce.ProductName(),
            ProductSku = $"SKU-{_faker.Random.AlphaNumeric(8).ToUpper()}",
            IsActive = true,
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates bulk test product IDs for stress testing
    /// Distribution: Mixed types with majority being ASINs
    /// </summary>
    /// <param name="count">Total count (can be thousands)</param>
    /// <param name="certificateId">Certificate to associate with</param>
    /// <returns>List of CertificateProductId</returns>
    public static List<CertificateProductId> CreateBulkTestProductIds(int count, int certificateId = 1)
    {
        var productIds = new List<CertificateProductId>(count);
        var random = new Random(42); // Fixed seed for reproducibility

        for (int i = 0; i < count; i++)
        {
            // Weight towards ASINs (60% ASIN, 20% UPC, 20% other)
            var typeIndex = random.NextDouble() switch
            {
                < 0.6 => 0,  // ASIN
                < 0.8 => 1,  // UPC
                _ => random.Next(2, ProductIdTypes.Length)
            };
            var type = ProductIdTypes[typeIndex];

            productIds.Add(new CertificateProductId
            {
                CertificateId = certificateId,
                ProductIdType = type,
                ProductIdValue = GenerateProductIdValue(type, i),
                Status = "Active",
                Partner = Partners[random.Next(Partners.Length)],
                ProductName = $"Bulk Test Product {i + 1}",
                ProductSku = $"BULK-{i + 1:D8}",
                IsActive = true,
                IsPublic = true,
                DisplayOrder = i + 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return productIds;
    }

    /// <summary>
    /// Generates a valid product ID value based on type
    /// </summary>
    private static string GenerateProductIdValue(string type, int index)
    {
        return type switch
        {
            "ASIN" => GenerateAsin(index),
            "UPC" => GenerateUpc(index),
            "GTIN" => $"{10000000000000 + index:D14}",
            "EAN" => GenerateEan(index),
            "ISBN" => $"978{1000000000 + index:D10}",
            "SKU" => $"SKU-{index:D8}",
            "MPN" => $"MPN-{_faker.Random.Int(100, 999)}-{index:D6}",
            _ => $"ID{index:D10}"
        };
    }

    /// <summary>
    /// Generates a valid ASIN (10 alphanumeric characters starting with B0)
    /// </summary>
    private static string GenerateAsin(int index)
    {
        var chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var seed = index + 1000000;
        var suffix = new char[8];
        for (int i = 0; i < 8; i++)
        {
            suffix[i] = chars[(seed + i * 7) % chars.Length];
        }
        return $"B0{new string(suffix)}";
    }

    /// <summary>
    /// Generates a valid UPC-A (12 digits with check digit)
    /// </summary>
    private static string GenerateUpc(int index)
    {
        var baseNumber = (100000000000 + index) % 100000000000;
        var upc = baseNumber.ToString("D11");
        var sum = 0;
        for (int i = 0; i < 11; i++)
        {
            var digit = int.Parse(upc[i].ToString());
            sum += (i % 2 == 0) ? digit * 3 : digit;
        }
        var checkDigit = (10 - (sum % 10)) % 10;
        return upc + checkDigit;
    }

    /// <summary>
    /// Generates a valid EAN-13 (13 digits with check digit)
    /// </summary>
    private static string GenerateEan(int index)
    {
        var baseNumber = (100000000000 + index) % 1000000000000;
        var ean = baseNumber.ToString("D12");
        var sum = 0;
        for (int i = 0; i < 12; i++)
        {
            var digit = int.Parse(ean[i].ToString());
            sum += (i % 2 == 0) ? digit : digit * 3;
        }
        var checkDigit = (10 - (sum % 10)) % 10;
        return ean + checkDigit;
    }

    #endregion
}
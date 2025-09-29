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
}
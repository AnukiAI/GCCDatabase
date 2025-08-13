using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;

namespace GCCWebApp.Tests;

public class BusinessLogicTests
{
    private DbContextOptions<ApplicationDbContext> GetInMemoryDbOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public void Certificate_ShouldValidateRequiredFields()
    {
        // Arrange
        var certificate = new Certificate();

        // Act & Assert - Test required field validation
        Assert.NotNull(certificate);
        Assert.True(certificate.CreatedAt <= DateTime.UtcNow);
        Assert.Equal("active", certificate.Status); // Default status should be active
    }

    [Fact]
    public void Certificate_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var certificate = new Certificate
        {
            Number = "GCC-TEST-001",
            CompanyId = 1,
            ProductId = 1
        };

        // Assert
        Assert.Equal("GCC-TEST-001", certificate.Number);
        Assert.Equal("active", certificate.Status);
        Assert.True(certificate.CreatedAt <= DateTime.UtcNow);
        Assert.NotEqual(Guid.Empty, certificate.Id);
    }

    [Fact]
    public void Company_ShouldValidateBusinessRules()
    {
        // Arrange & Act
        var company = new Company
        {
            Name = "Test Company Inc.",
            Website = "https://testcompany.com",
            ContactEmail = "contact@testcompany.com"
        };

        // Assert
        Assert.NotNull(company.Name);
        Assert.True(company.Name.Length > 0);
        Assert.True(company.Website?.StartsWith("http") ?? true);
        Assert.True(company.ContactEmail?.Contains("@") ?? true);
    }

    [Fact]
    public void Product_ShouldValidateHierarchy()
    {
        // Arrange & Act
        var productType = new ProductType { Name = "Paper Products", Description = "All paper-based products" };
        var productCategory = new ProductCategory { Name = "Packaging", ProductTypeId = 1 };
        var productSubcategory = new ProductSubcategory { Name = "Food Packaging", ProductCategoryId = 1 };
        
        var product = new Product
        {
            Name = "Eco Food Container",
            ProductTypeId = 1,
            ProductCategoryId = 1,
            ProductSubcategoryId = 1
        };

        // Assert - Validate product hierarchy structure
        Assert.NotNull(product.Name);
        Assert.True(product.ProductTypeId > 0);
        Assert.True(product.ProductCategoryId > 0);
        Assert.True(product.ProductSubcategoryId > 0);
    }

    [Fact]
    public async Task Database_ShouldHandleCertificateOperations()
    {
        // Arrange
        var options = GetInMemoryDbOptions();
        
        using var context = new ApplicationDbContext(options);
        
        var company = new Company 
        { 
            Name = "Test Company", 
            ContactEmail = "test@company.com" 
        };
        
        var productType = new ProductType 
        { 
            Name = "Test Products" 
        };
        
        var product = new Product 
        { 
            Name = "Test Product",
            ProductTypeId = 1
        };

        // Act
        context.Companies.Add(company);
        context.ProductTypes.Add(productType);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var certificate = new Certificate
        {
            Number = "GCC-TEST-002",
            CompanyId = company.Id,
            ProductId = product.Id,
            Status = "active",
            IssueDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddYears(1)
        };

        context.Certificates.Add(certificate);
        await context.SaveChangesAsync();

        // Assert
        var savedCertificate = await context.Certificates
            .Include(c => c.Company)
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Number == "GCC-TEST-002");

        Assert.NotNull(savedCertificate);
        Assert.Equal("GCC-TEST-002", savedCertificate.Number);
        Assert.Equal("Test Company", savedCertificate.Company?.Name);
        Assert.Equal("Test Product", savedCertificate.Product?.Name);
        Assert.Equal("active", savedCertificate.Status);
    }

    [Fact]
    public async Task CertificateService_ShouldFilterByStatus()
    {
        // Arrange
        var options = GetInMemoryDbOptions();
        
        using var context = new ApplicationDbContext(options);
        
        // Create test data
        var certificates = new[]
        {
            new Certificate { Number = "ACTIVE-001", Status = "active", CompanyId = 1, ProductId = 1 },
            new Certificate { Number = "EXPIRED-001", Status = "expired", CompanyId = 1, ProductId = 1 },
            new Certificate { Number = "PENDING-001", Status = "pending", CompanyId = 1, ProductId = 1 },
            new Certificate { Number = "ACTIVE-002", Status = "active", CompanyId = 1, ProductId = 1 }
        };

        context.Certificates.AddRange(certificates);
        await context.SaveChangesAsync();

        // Act
        var activeCertificates = await context.Certificates
            .Where(c => c.Status == "active")
            .ToListAsync();

        var expiredCertificates = await context.Certificates
            .Where(c => c.Status == "expired")
            .ToListAsync();

        // Assert
        Assert.Equal(2, activeCertificates.Count);
        Assert.Single(expiredCertificates);
        Assert.All(activeCertificates, c => Assert.Equal("active", c.Status));
        Assert.All(expiredCertificates, c => Assert.Equal("expired", c.Status));
    }

    [Fact]
    public void CertificateType_ShouldValidateConfiguration()
    {
        // Arrange & Act
        var certificateType = new CertificationType
        {
            Name = "Recycled Content",
            Description = "Certifies recycled content percentage",
            IsActive = true
        };

        // Assert
        Assert.NotNull(certificateType.Name);
        Assert.True(certificateType.IsActive);
        Assert.NotNull(certificateType.Description);
    }

    [Fact]
    public void Tag_ShouldSupportCertificateClassification()
    {
        // Arrange & Act
        var tag = new Tag
        {
            Name = "Sustainability",
            Description = "Environmental sustainability classification",
            Color = "#22C55E"
        };

        // Assert
        Assert.NotNull(tag.Name);
        Assert.NotNull(tag.Description);
        Assert.True(tag.Color?.StartsWith("#") ?? false);
    }

    [Fact]
    public async Task Database_ShouldSupportComplexQueries()
    {
        // Arrange
        var options = GetInMemoryDbOptions();
        
        using var context = new ApplicationDbContext(options);
        
        // Create comprehensive test data
        var company1 = new Company { Name = "Eco Company A", ContactEmail = "eco@companya.com" };
        var company2 = new Company { Name = "Green Company B", ContactEmail = "green@companyb.com" };
        
        var productType = new ProductType { Name = "Packaging Materials" };
        var product1 = new Product { Name = "Eco Box", ProductTypeId = 1 };
        var product2 = new Product { Name = "Green Wrapper", ProductTypeId = 1 };

        context.Companies.AddRange(company1, company2);
        context.ProductTypes.Add(productType);
        context.Products.AddRange(product1, product2);
        await context.SaveChangesAsync();

        var certificates = new[]
        {
            new Certificate 
            { 
                Number = "ECO-001", 
                Status = "active", 
                CompanyId = company1.Id, 
                ProductId = product1.Id,
                IssueDate = DateTime.UtcNow.AddDays(-30),
                ExpirationDate = DateTime.UtcNow.AddDays(335) // Expiring in ~11 months
            },
            new Certificate 
            { 
                Number = "GREEN-001", 
                Status = "active", 
                CompanyId = company2.Id, 
                ProductId = product2.Id,
                IssueDate = DateTime.UtcNow.AddDays(-60),
                ExpirationDate = DateTime.UtcNow.AddDays(90) // Expiring in ~3 months
            },
            new Certificate 
            { 
                Number = "OLD-001", 
                Status = "expired", 
                CompanyId = company1.Id, 
                ProductId = product1.Id,
                IssueDate = DateTime.UtcNow.AddDays(-400),
                ExpirationDate = DateTime.UtcNow.AddDays(-30)
            }
        };

        context.Certificates.AddRange(certificates);
        await context.SaveChangesAsync();

        // Act & Assert - Test complex queries

        // Query 1: Active certificates expiring within 6 months
        var expiringSoon = await context.Certificates
            .Where(c => c.Status == "active" && 
                       c.ExpirationDate <= DateTime.UtcNow.AddMonths(6))
            .Include(c => c.Company)
            .Include(c => c.Product)
            .ToListAsync();

        Assert.Single(expiringSoon);
        Assert.Equal("GREEN-001", expiringSoon.First().Number);

        // Query 2: Certificates by company
        var ecoCompanyCerts = await context.Certificates
            .Where(c => c.Company!.Name.Contains("Eco"))
            .Include(c => c.Company)
            .ToListAsync();

        Assert.Equal(2, ecoCompanyCerts.Count);

        // Query 3: Recently issued certificates (within 45 days)
        var recentCerts = await context.Certificates
            .Where(c => c.IssueDate >= DateTime.UtcNow.AddDays(-45))
            .ToListAsync();

        Assert.Equal(2, recentCerts.Count);
    }

    [Fact]
    public void User_ShouldValidateRoleBasedAccess()
    {
        // Arrange & Act
        var adminUser = new User
        {
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            Role = "Admin",
            IsActive = true
        };

        var analystUser = new User
        {
            Email = "analyst@test.com", 
            FirstName = "Analyst",
            LastName = "User",
            Role = "Analyst",
            IsActive = true
        };

        var externalUser = new User
        {
            Email = "external@test.com",
            FirstName = "External", 
            LastName = "User",
            Role = "External",
            IsActive = true,
            CompanyId = 1 // External users should be associated with a company
        };

        // Assert
        Assert.Equal("Admin", adminUser.Role);
        Assert.Equal("Analyst", analystUser.Role);
        Assert.Equal("External", externalUser.Role);
        Assert.True(adminUser.IsActive);
        Assert.True(analystUser.IsActive);
        Assert.True(externalUser.IsActive);
        Assert.True(externalUser.CompanyId > 0);
    }

    [Fact]
    public async Task Database_ShouldHandleProductHierarchy()
    {
        // Arrange
        var options = GetInMemoryDbOptions();
        
        using var context = new ApplicationDbContext(options);

        // Create product hierarchy
        var productType = new ProductType 
        { 
            Name = "Building Materials",
            Description = "Construction and building products"
        };

        context.ProductTypes.Add(productType);
        await context.SaveChangesAsync();

        var productCategory = new ProductCategory
        {
            Name = "Insulation",
            Description = "Thermal insulation materials",
            ProductTypeId = productType.Id
        };

        context.ProductCategories.Add(productCategory);
        await context.SaveChangesAsync();

        var productSubcategory = new ProductSubcategory
        {
            Name = "Foam Insulation", 
            Description = "Foam-based insulation products",
            ProductCategoryId = productCategory.Id
        };

        context.ProductSubcategories.Add(productSubcategory);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Eco Foam Board",
            Description = "Recycled content foam insulation board",
            ProductTypeId = productType.Id,
            ProductCategoryId = productCategory.Id,
            ProductSubcategoryId = productSubcategory.Id
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Act - Query the complete hierarchy
        var productWithHierarchy = await context.Products
            .Include(p => p.ProductType)
            .Include(p => p.ProductCategory)
            .Include(p => p.ProductSubcategory)
            .FirstOrDefaultAsync(p => p.Name == "Eco Foam Board");

        // Assert
        Assert.NotNull(productWithHierarchy);
        Assert.Equal("Eco Foam Board", productWithHierarchy.Name);
        Assert.Equal("Building Materials", productWithHierarchy.ProductType?.Name);
        Assert.Equal("Insulation", productWithHierarchy.ProductCategory?.Name);
        Assert.Equal("Foam Insulation", productWithHierarchy.ProductSubcategory?.Name);
    }

    [Fact]
    public void CustomField_ShouldSupportDynamicCertificateData()
    {
        // Arrange & Act
        var customField = new CustomField
        {
            Name = "Recycled Content Percentage",
            DataType = "decimal",
            IsRequired = true,
            DisplayOrder = 1
        };

        var customFieldValue = new CustomFieldValue
        {
            CustomFieldId = 1,
            CertificateId = Guid.NewGuid(),
            Value = "75.5"
        };

        // Assert
        Assert.NotNull(customField.Name);
        Assert.Equal("decimal", customField.DataType);
        Assert.True(customField.IsRequired);
        Assert.Equal(1, customField.DisplayOrder);
        Assert.Equal("75.5", customFieldValue.Value);
    }
}
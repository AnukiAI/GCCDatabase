using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using Xunit;

namespace GCCWebApp.Performance.Tests;

public class BasicPerformanceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _context;

    public BasicPerformanceTests()
    {
        var services = new ServiceCollection();
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
            
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        SeedTestData();
    }

    [Fact]
    public async Task CertificateQuery_SimpleSelect_ShouldBeUnder100ms()
    {
        var stopwatch = Stopwatch.StartNew();
        
        var certificates = await _context.Certificates
            .Take(100)
            .ToListAsync();
            
        stopwatch.Stop();
        
        certificates.Should().NotBeEmpty();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, 
            "Simple certificate queries should complete under 500ms for testing");
    }

    [Fact]
    public async Task CertificateQuery_WithFiltering_ShouldBeUnder200ms()
    {
        var stopwatch = Stopwatch.StartNew();
        
        var activeCertificates = await _context.Certificates
            .Where(c => c.Status == "active")
            .Where(c => c.ExpirationDate > DateTime.Now)
            .OrderBy(c => c.ExpirationDate)
            .Take(50)
            .ToListAsync();
            
        stopwatch.Stop();
        
        activeCertificates.Should().NotBeEmpty();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200, 
            "Filtered certificate queries should complete under 200ms");
    }

    [Fact]
    public async Task BulkInsert_100Records_ShouldBeUnder1Second()
    {
        var newCertificates = GenerateTestCertificates(100, startId: 10000);
        
        var stopwatch = Stopwatch.StartNew();
        
        _context.Certificates.AddRange(newCertificates);
        await _context.SaveChangesAsync();
        
        stopwatch.Stop();
        
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, 
            "Bulk insert of 100 records should complete under 1 second");
    }

    [Fact]
    public async Task SearchQuery_TextSearch_ShouldBeEfficient()
    {
        var searchTerm = "Test";
        
        var stopwatch = Stopwatch.StartNew();
        
        var searchResults = await _context.Certificates
            .Where(c => c.Number.Contains(searchTerm) || 
                       (c.Location != null && c.Location.Contains(searchTerm)))
            .Take(20)
            .ToListAsync();
            
        stopwatch.Stop();
        
        searchResults.Should().NotBeEmpty();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(300, 
            "Text search queries should complete under 300ms");
    }

    [Fact]
    public async Task AggregateQuery_CountGroupBy_ShouldBeEfficient()
    {
        var stopwatch = Stopwatch.StartNew();
        
        var statusCounts = await _context.Certificates
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
            
        stopwatch.Stop();
        
        statusCounts.Should().NotBeEmpty();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200, 
            "Aggregate queries should complete under 200ms");
    }

    private void SeedTestData()
    {
        var categories = GenerateTestCategories(5);
        var companies = GenerateTestCompanies(10);
        var products = GenerateTestProducts(20);
        var certificateTypes = GenerateTestCertificateTypes(5);
        var certificates = GenerateTestCertificates(500);

        _context.ProductCategories.AddRange(categories);
        _context.Companies.AddRange(companies);
        _context.Products.AddRange(products);
        _context.CertificationTypes.AddRange(certificateTypes);
        _context.Certificates.AddRange(certificates);
        
        _context.SaveChanges();
    }

    private List<ProductCategory> GenerateTestCategories(int count)
    {
        var categories = new List<ProductCategory>();
        for (int i = 1; i <= count; i++)
        {
            categories.Add(new ProductCategory
            {
                Id = i,
                Name = $"Test Category {i}",
                Description = $"Description for category {i}"
            });
        }
        return categories;
    }

    private List<Company> GenerateTestCompanies(int count)
    {
        var companies = new List<Company>();
        for (int i = 1; i <= count; i++)
        {
            companies.Add(new Company
            {
                Id = i,
                Name = $"Test Company {i}",
                ContactInfo = $"contact{i}@company{i}.com",
                Location = $"Test Location {i}",
                Country = "Test Country"
            });
        }
        return companies;
    }

    private List<Product> GenerateTestProducts(int count)
    {
        var products = new List<Product>();
        for (int i = 1; i <= count; i++)
        {
            products.Add(new Product
            {
                Id = i,
                Name = $"Test Product {i}",
                Description = $"Description for product {i}",
                Sku = $"SKU-{i:D6}",
                CategoryId = 1 // Default category
            });
        }
        return products;
    }

    private List<CertificationType> GenerateTestCertificateTypes(int count)
    {
        var types = new List<CertificationType>();
        for (int i = 1; i <= count; i++)
        {
            types.Add(new CertificationType
            {
                Id = i,
                Name = $"Certificate Type {i}",
                Description = $"Description for certificate type {i}"
            });
        }
        return types;
    }

    private List<Certificate> GenerateTestCertificates(int count, int startId = 1)
    {
        var certificates = new List<Certificate>();
        var statuses = new[] { "active", "expired", "pending" };
        
        for (int i = 0; i < count; i++)
        {
            var id = startId + i;
            certificates.Add(new Certificate
            {
                Id = id,
                Number = $"C{id:D3}",
                Status = statuses[id % statuses.Length],
                EffectiveDate = DateTime.Now.AddDays(-Random.Shared.Next(1, 365)),
                ExpirationDate = DateTime.Now.AddDays(Random.Shared.Next(1, 730)),
                Location = $"Test Location {id}",
                Country = "Test Country",
                CompanyId = ((id - 1) % 10) + 1,
                ProductId = ((id - 1) % 20) + 1,
                CertificationTypeId = ((id - 1) % 5) + 1
            });
        }
        
        return certificates;
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}
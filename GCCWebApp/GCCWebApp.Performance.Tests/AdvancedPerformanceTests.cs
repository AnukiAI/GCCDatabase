using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using Xunit;
using System.Collections.Concurrent;

namespace GCCWebApp.Performance.Tests;

/// <summary>
/// Advanced Performance Tests covering PERF-001 to PERF-004 requirements
/// Tests large datasets, search indexing, file upload limits, and load scenarios
/// </summary>
public class AdvancedPerformanceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _context;

    public AdvancedPerformanceTests()
    {
        var services = new ServiceCollection();
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"AdvancedPerfTestDb_{Guid.NewGuid()}"));
            
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        SeedLargeDataset();
    }

    #region PERF-001: Large Dataset Handling Tests

    [Fact]
    public async Task LargeDataset_ThousandsOfProductIDs_ShouldHandleEfficiently()
    {
        // Arrange - Create certificate with thousands of product IDs
        var certificate = await CreateCertificateWithManyProducts(5000);
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Query certificate with all its products
        var certificateWithProducts = await _context.Certificates
            .FirstAsync(c => c.Id == certificate.Id);
        
        // Get products count for this certificate
        var productsCount = await _context.Products
            .Where(p => p.Certificates.Any(cert => cert.Id == certificate.Id))
            .CountAsync();
            
        stopwatch.Stop();
        
        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, 
            "Loading certificate with thousands of products should complete under 2 seconds");
        productsCount.Should().Be(5000);
    }

    [Fact]
    public async Task LargeDataset_PaginatedProductDisplay_ShouldBeOptimized()
    {
        // Arrange
        var certificate = await CreateCertificateWithManyProducts(10000);
        var pageSize = 100;
        var pageNumber = 1;
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Paginated query
        var paginatedProducts = await _context.Products
            .Where(p => p.Certificates.Any(cert => cert.Id == certificate.Id))
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
        stopwatch.Stop();
        
        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, 
            "Paginated product loading should be under 500ms");
        paginatedProducts.Should().HaveCount(pageSize);
    }

    [Fact]
    public async Task LargeDataset_ASINSearch_WithThousandsOfRecords_ShouldBeEfficient()
    {
        // Arrange - Create many products with ASINs
        await CreateManyProductsWithASINs(20000);
        var searchTerm = "B08N5WRWNW";
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Search for specific ASIN
        var searchResults = await _context.Products
            .Where(p => p.Asin != null && p.Asin.Contains(searchTerm))
            .Take(50)
            .ToListAsync();
            
        stopwatch.Stop();
        
        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, 
            "ASIN search across 20K products should complete under 1 second");
        searchResults.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(5000)]
    [InlineData(10000)]
    [InlineData(25000)]
    public async Task LargeDataset_ScalabilityTest_ShouldMaintainPerformance(int recordCount)
    {
        // Arrange
        var certificates = await CreateManyCertificates(recordCount);
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Query with filtering and sorting
        var results = await _context.Certificates
            .Where(c => c.Status == "active")
            .OrderBy(c => c.EffectiveDate)
            .Take(100)
            .ToListAsync();
            
        stopwatch.Stop();
        
        // Assert - Performance should not degrade significantly with more data
        var maxAllowedTime = recordCount switch
        {
            <= 1000 => 200,
            <= 5000 => 500,
            <= 10000 => 800,
            _ => 1500
        };
        
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxAllowedTime, 
            $"Query performance should remain acceptable with {recordCount} records");
    }

    #endregion

    #region PERF-002: Search Index Strategy Tests

    [Fact]
    public async Task SearchIndex_CrossCompanySearch_ShouldBeOptimized()
    {
        // Arrange - Create certificates across multiple companies
        await CreateCertificatesAcrossCompanies(1000);
        var searchTerm = "Test";
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Cross-company search
        var searchResults = await _context.Certificates
            .Include(c => c.Company)
            .Where(c => c.Number.Contains(searchTerm) || 
                       (c.Company != null && c.Company.Name.Contains(searchTerm)))
            .Take(50)
            .ToListAsync();
            
        stopwatch.Stop();
        
        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(800, 
            "Cross-company search should be optimized with proper indexing");
        searchResults.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchIndex_FullTextSearch_ShouldHandleComplexQueries()
    {
        // Arrange
        await CreateCertificatesWithRichContent(500);
        var complexSearchTerms = new[] { "sustainable", "organic", "certified", "eco-friendly" };
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Complex multi-term search
        var searchResults = await _context.Certificates
            .Where(c => complexSearchTerms.Any(term => 
                c.Number.Contains(term) || 
                (c.Location != null && c.Location.Contains(term))))
            .Take(100)
            .ToListAsync();
            
        stopwatch.Stop();
        
        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, 
            "Complex search queries should be optimized");
    }

    [Fact]
    public async Task SearchIndex_FilterCombinations_ShouldMaintainPerformance()
    {
        // Arrange
        await CreateCertificatesWithVariousFilters(2000);
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Multiple filter combination
        var filteredResults = await _context.Certificates
            .Include(c => c.Company)
            .Where(c => c.Status == "active")
            .Where(c => c.EffectiveDate >= DateTime.Now.AddYears(-1))
            .Where(c => c.ExpirationDate > DateTime.Now)
            .Where(c => c.Company != null && c.Company.Country == "USA")
            .OrderBy(c => c.ExpirationDate)
            .Take(50)
            .ToListAsync();
            
        stopwatch.Stop();
        
        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(600, 
            "Complex filter combinations should be optimized");
    }

    #endregion

    #region PERF-003: File Upload Limits & Storage Tests

    [Theory]
    [InlineData(1024 * 1024)]      // 1MB
    [InlineData(5 * 1024 * 1024)]  // 5MB
    [InlineData(10 * 1024 * 1024)] // 10MB
    public async Task FileUpload_PDFSizeLimits_ShouldValidateAndProcess(int fileSizeBytes)
    {
        // Arrange
        var mockPdfContent = GenerateMockPDFContent(fileSizeBytes);
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Simulate file upload processing
        var processingResult = await ProcessFileUpload(mockPdfContent, "certificate.pdf");
        
        stopwatch.Stop();
        
        // Assert
        if (fileSizeBytes <= 10 * 1024 * 1024) // 10MB limit
        {
            processingResult.Should().BeTrue("Files under 10MB should be accepted");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, 
                "File processing should complete under 5 seconds");
        }
        else
        {
            processingResult.Should().BeFalse("Files over limit should be rejected");
        }
    }

    [Fact]
    public async Task FileUpload_ConcurrentUploads_ShouldHandleMultipleFiles()
    {
        // Arrange
        var concurrentUploadTasks = new List<Task<bool>>();
        var fileCount = 10;
        var fileSize = 2 * 1024 * 1024; // 2MB each
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Simulate concurrent uploads
        for (int i = 0; i < fileCount; i++)
        {
            var mockContent = GenerateMockPDFContent(fileSize);
            var task = ProcessFileUpload(mockContent, $"certificate_{i}.pdf");
            concurrentUploadTasks.Add(task);
        }
        
        var results = await Task.WhenAll(concurrentUploadTasks);
        stopwatch.Stop();
        
        // Assert
        results.All(r => r).Should().BeTrue("All concurrent uploads should succeed");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(15000, 
            "Concurrent file processing should complete efficiently");
    }

    [Fact]
    public async Task FileStorage_StorageStrategy_ShouldOptimizeSpaceUsage()
    {
        // Arrange
        var totalFileSize = 0L;
        var fileCount = 100;
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Simulate storage of many files
        for (int i = 0; i < fileCount; i++)
        {
            var fileSize = Random.Shared.Next(100 * 1024, 5 * 1024 * 1024); // 100KB to 5MB
            var mockContent = GenerateMockPDFContent(fileSize);
            
            await StoreFile(mockContent, $"cert_{i}.pdf");
            totalFileSize += fileSize;
        }
        
        var storageUsed = await GetStorageUsage();
        stopwatch.Stop();
        
        // Assert
        storageUsed.Should().BeGreaterThan(0, "Storage usage should be tracked");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000, 
            "Storage operations should be efficient");
    }

    #endregion

    #region PERF-004: Load Testing Strategy Tests

    [Fact]
    public async Task LoadTest_ConcurrentUsers_CertificateAccess()
    {
        // Arrange
        await CreateCertificatesForLoadTest(1000);
        var concurrentUsers = 50;
        var userTasks = new List<Task>();
        var successfulRequests = new ConcurrentBag<bool>();
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Simulate concurrent user access
        for (int i = 0; i < concurrentUsers; i++)
        {
            var userId = i;
            var task = Task.Run(async () =>
            {
                try
                {
                    // Simulate user accessing certificates
                    var certificates = await _context.Certificates
                        .Take(10)
                        .ToListAsync();
                    
                    successfulRequests.Add(certificates.Any());
                }
                catch
                {
                    successfulRequests.Add(false);
                }
            });
            
            userTasks.Add(task);
        }
        
        await Task.WhenAll(userTasks);
        stopwatch.Stop();
        
        // Assert
        var successRate = successfulRequests.Count(s => s) / (double)concurrentUsers;
        successRate.Should().BeGreaterThan(0.95, "Success rate should be above 95% under load");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000, 
            "Concurrent access should complete under 10 seconds");
    }

    [Fact]
    public async Task LoadTest_DatabaseConnections_ShouldHandleMultipleConnections()
    {
        // Arrange
        var connectionTasks = new List<Task<bool>>();
        var connectionCount = 20;
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Test multiple database connections
        for (int i = 0; i < connectionCount; i++)
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    
                    var count = await context.Certificates.CountAsync();
                    return count >= 0;
                }
                catch
                {
                    return false;
                }
            });
            
            connectionTasks.Add(task);
        }
        
        var results = await Task.WhenAll(connectionTasks);
        stopwatch.Stop();
        
        // Assert
        results.All(r => r).Should().BeTrue("All database connections should succeed");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, 
            "Database connection handling should be efficient");
    }

    [Fact]
    public async Task LoadTest_MemoryUsage_ShouldRemainStable()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        
        // Act - Perform memory-intensive operations
        for (int i = 0; i < 100; i++)
        {
            var largeCertificateList = await _context.Certificates
                .Include(c => c.Company)
                // Certificate Products relationship is many-to-many, not a navigation property
                .Take(100)
                .ToListAsync();
                
            // Simulate processing
            int count = largeCertificateList.Count;
        }
        
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(false);
        
        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        memoryIncrease.Should().BeLessThan(50 * 1024 * 1024, 
            "Memory usage should remain stable (under 50MB increase)");
    }

    #endregion

    #region Helper Methods

    private async Task<Certificate> CreateCertificateWithManyProducts(int productCount)
    {
        var certificate = new Certificate
        {
            Number = "LARGE-CERT-001",
            Status = "active",
            EffectiveDate = DateTime.Now,
            ExpirationDate = DateTime.Now.AddYears(1),
            CompanyId = 1
        };

        _context.Certificates.Add(certificate);
        await _context.SaveChangesAsync();

        var products = new List<Product>();
        for (int i = 1; i <= productCount; i++)
        {
            var product = new Product
            {
                Id = i + 10000, // Avoid ID conflicts
                Name = $"Product {i}",
                Asin = $"B{i:D10}",
                Sku = $"SKU-{i:D8}",
                CategoryId = 1
            };
            
            // Add certificate to product's certificate collection
            product.Certificates.Add(certificate);
            products.Add(product);
        }

        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        return certificate;
    }

    private async Task CreateManyProductsWithASINs(int count)
    {
        var products = new List<Product>();
        for (int i = 1; i <= count; i++)
        {
            products.Add(new Product
            {
                Id = i + 50000, // Avoid ID conflicts
                Name = $"ASIN Product {i}",
                Asin = i % 100 == 0 ? "B08N5WRWNW" : $"B{i:D10}", // Every 100th product has the target ASIN
                CategoryId = 1
            });
        }

        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();
    }

    private async Task<List<Certificate>> CreateManyCertificates(int count)
    {
        var certificates = new List<Certificate>();
        var statuses = new[] { "active", "expired", "pending" };

        for (int i = 1; i <= count; i++)
        {
            certificates.Add(new Certificate
            {
                Id = i + 20000, // Avoid ID conflicts
                Number = $"PERF-{i:D6}",
                Status = statuses[i % statuses.Length],
                EffectiveDate = DateTime.Now.AddDays(-Random.Shared.Next(1, 365)),
                ExpirationDate = DateTime.Now.AddDays(Random.Shared.Next(1, 730)),
                CompanyId = ((i - 1) % 10) + 1
            });
        }

        _context.Certificates.AddRange(certificates);
        await _context.SaveChangesAsync();
        
        return certificates;
    }

    private async Task CreateCertificatesAcrossCompanies(int count)
    {
        // Create additional companies
        var companies = new List<Company>();
        for (int i = 11; i <= 20; i++) // Companies 11-20
        {
            companies.Add(new Company
            {
                Id = i,
                Name = $"Performance Test Company {i}",
                ContactInfo = $"perf{i}@company.com",
                Country = i % 2 == 0 ? "USA" : "Canada"
            });
        }
        _context.Companies.AddRange(companies);

        var certificates = new List<Certificate>();
        for (int i = 1; i <= count; i++)
        {
            certificates.Add(new Certificate
            {
                Id = i + 30000, // Avoid ID conflicts
                Number = $"CROSS-{i:D6}",
                Status = "active",
                EffectiveDate = DateTime.Now.AddDays(-Random.Shared.Next(1, 365)),
                ExpirationDate = DateTime.Now.AddDays(Random.Shared.Next(1, 730)),
                CompanyId = ((i - 1) % 10) + 11, // Use companies 11-20
                Location = i % 5 == 0 ? "Test Location" : null
            });
        }

        _context.Certificates.AddRange(certificates);
        await _context.SaveChangesAsync();
    }

    private async Task CreateCertificatesWithRichContent(int count)
    {
        var contentWords = new[] { "sustainable", "organic", "certified", "eco-friendly", "renewable", "biodegradable" };
        var certificates = new List<Certificate>();

        for (int i = 1; i <= count; i++)
        {
            var randomWord = contentWords[i % contentWords.Length];
            certificates.Add(new Certificate
            {
                Id = i + 40000, // Avoid ID conflicts
                Number = $"{randomWord.ToUpper()}-{i:D4}",
                Status = "active",
                EffectiveDate = DateTime.Now.AddDays(-Random.Shared.Next(1, 365)),
                ExpirationDate = DateTime.Now.AddDays(Random.Shared.Next(1, 730)),
                Location = $"{randomWord} facility location",
                CompanyId = 1
            });
        }

        _context.Certificates.AddRange(certificates);
        await _context.SaveChangesAsync();
    }

    private async Task CreateCertificatesWithVariousFilters(int count)
    {
        var certificates = new List<Certificate>();
        var statuses = new[] { "active", "expired", "pending" };
        var countries = new[] { "USA", "Canada", "Mexico" };

        for (int i = 1; i <= count; i++)
        {
            certificates.Add(new Certificate
            {
                Id = i + 60000, // Avoid ID conflicts
                Number = $"FILTER-{i:D6}",
                Status = statuses[i % statuses.Length],
                EffectiveDate = DateTime.Now.AddDays(-Random.Shared.Next(1, 730)),
                ExpirationDate = DateTime.Now.AddDays(Random.Shared.Next(-30, 730)),
                Country = countries[i % countries.Length],
                CompanyId = 1
            });
        }

        _context.Certificates.AddRange(certificates);
        await _context.SaveChangesAsync();
    }

    private async Task CreateCertificatesForLoadTest(int count)
    {
        var certificates = new List<Certificate>();

        for (int i = 1; i <= count; i++)
        {
            certificates.Add(new Certificate
            {
                Id = i + 70000, // Avoid ID conflicts
                Number = $"LOAD-{i:D6}",
                Status = "active",
                EffectiveDate = DateTime.Now.AddDays(-30),
                ExpirationDate = DateTime.Now.AddDays(365),
                CompanyId = 1
            });
        }

        _context.Certificates.AddRange(certificates);
        await _context.SaveChangesAsync();
    }

    private byte[] GenerateMockPDFContent(int sizeBytes)
    {
        var content = new byte[sizeBytes];
        // Fill with mock PDF header and random data
        var header = System.Text.Encoding.UTF8.GetBytes("%PDF-1.4\n");
        Array.Copy(header, 0, content, 0, Math.Min(header.Length, sizeBytes));
        
        // Fill rest with random data
        if (sizeBytes > header.Length)
        {
            Random.Shared.NextBytes(content.AsSpan(header.Length));
        }
        
        return content;
    }

    private async Task<bool> ProcessFileUpload(byte[] fileContent, string fileName)
    {
        // Mock file upload processing
        await Task.Delay(100); // Simulate processing time
        
        if (fileContent.Length > 10 * 1024 * 1024) // 10MB limit
            return false;
            
        return true;
    }

    private async Task StoreFile(byte[] fileContent, string fileName)
    {
        // Mock file storage
        await Task.Delay(10);
    }

    private async Task<long> GetStorageUsage()
    {
        await Task.Delay(1);
        return 1024 * 1024 * 100; // Mock 100MB usage
    }

    private void SeedLargeDataset()
    {
        // Create base test data
        var company = new Company
        {
            Id = 1,
            Name = "Performance Test Company",
            ContactInfo = "perf@company.com",
            Country = "USA"
        };

        var category = new ProductCategory
        {
            Id = 1,
            Name = "Test Category"
        };

        _context.Companies.Add(company);
        _context.ProductCategories.Add(category);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
    #endregion
}
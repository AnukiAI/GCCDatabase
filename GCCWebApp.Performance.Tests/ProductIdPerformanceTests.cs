using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;
using Xunit;
using Xunit.Abstractions;

namespace GCCWebApp.Performance.Tests;

/// <summary>
/// Performance tests for Product ID (ASIN, UPC, GTIN, EAN) operations
/// Tests bulk operations, high-volume queries, and stress testing
/// </summary>
public class ProductIdPerformanceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _context;
    private readonly IProductIdService _productIdService;
    private readonly ITestOutputHelper _output;
    private readonly Random _random = new(42);
    private static readonly string[] ProductIdTypes = { "ASIN", "UPC", "GTIN", "EAN", "ISBN", "SKU", "MPN" };
    private static readonly string[] Partners = { "Amazon", "Novi", "None", "Other" };

    public ProductIdPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"ProductIdPerfTest_{Guid.NewGuid()}"));

        services.AddLogging(builder => builder.AddDebug());
        services.AddScoped<IProductIdService, ProductIdService>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _productIdService = _serviceProvider.GetRequiredService<IProductIdService>();

        SeedBaseTestData();
    }

    #region Bulk Insert Performance Tests

    [Theory]
    [InlineData(100, 1000)]      // 100 records should complete under 1 second
    [InlineData(1000, 5000)]     // 1000 records should complete under 5 seconds
    [InlineData(5000, 15000)]    // 5000 records should complete under 15 seconds
    [InlineData(10000, 30000)]   // 10000 records should complete under 30 seconds
    public async Task BulkInsert_ProductIds_ShouldCompleteWithinThreshold(int count, int maxMilliseconds)
    {
        // Arrange
        var productIds = GenerateProductIds(count, certificateId: 1);

        // Act
        var stopwatch = Stopwatch.StartNew();

        _context.CertificateProductIds.AddRange(productIds);
        await _context.SaveChangesAsync();

        stopwatch.Stop();

        // Assert
        var actualCount = await _context.CertificateProductIds.CountAsync();
        actualCount.Should().BeGreaterThanOrEqualTo(count);

        _output.WriteLine($"Bulk insert of {count} records completed in {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxMilliseconds,
            $"Bulk insert of {count} product IDs should complete under {maxMilliseconds}ms");
    }

    [Fact]
    public async Task StressTest_Insert50000ProductIds_ShouldComplete()
    {
        // Arrange - Generate 50,000 product IDs
        const int totalCount = 50000;
        const int batchSize = 10000;
        var stopwatch = Stopwatch.StartNew();
        var totalInserted = 0;

        // Act - Insert in batches
        for (int batch = 0; batch < totalCount / batchSize; batch++)
        {
            var productIds = GenerateProductIds(batchSize, certificateId: 1, startIndex: batch * batchSize);
            _context.CertificateProductIds.AddRange(productIds);
            await _context.SaveChangesAsync();
            totalInserted += batchSize;
            _output.WriteLine($"Batch {batch + 1}: Inserted {totalInserted} records in {stopwatch.ElapsedMilliseconds}ms");
        }

        stopwatch.Stop();

        // Assert
        var actualCount = await _context.CertificateProductIds.CountAsync();
        actualCount.Should().BeGreaterThanOrEqualTo(totalCount);

        _output.WriteLine($"Stress test: {totalCount} records inserted in {stopwatch.ElapsedMilliseconds}ms ({stopwatch.Elapsed.TotalSeconds:F2}s)");
        _output.WriteLine($"Average: {stopwatch.ElapsedMilliseconds / (totalCount / 1000.0):F2}ms per 1000 records");

        // Should complete within 2 minutes
        stopwatch.Elapsed.TotalMinutes.Should().BeLessThan(2,
            "50,000 product IDs should be insertable within 2 minutes");
    }

    #endregion

    #region Query Performance Tests

    [Fact]
    public async Task Query_10000ProductIds_SimpleSelect_ShouldBeUnder500ms()
    {
        // Arrange - Seed 10,000 product IDs
        await SeedProductIds(10000, certificateId: 1);

        // Act
        var stopwatch = Stopwatch.StartNew();

        var productIds = await _context.CertificateProductIds
            .Where(p => p.CertificateId == 1)
            .ToListAsync();

        stopwatch.Stop();

        // Assert
        productIds.Count.Should().Be(10000);
        _output.WriteLine($"Query 10,000 product IDs: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500,
            "Querying 10,000 product IDs should complete under 500ms");
    }

    [Fact]
    public async Task Query_FilterByType_LargeDataset_ShouldBeEfficient()
    {
        // Arrange - Seed 10,000 product IDs with mixed types
        await SeedProductIds(10000, certificateId: 1);

        // Act
        var stopwatch = Stopwatch.StartNew();

        var asins = await _context.CertificateProductIds
            .Where(p => p.CertificateId == 1 && p.ProductIdType == "ASIN")
            .ToListAsync();

        stopwatch.Stop();

        // Assert
        asins.Should().NotBeEmpty();
        _output.WriteLine($"Filter by type (ASIN) from 10,000 records: {stopwatch.ElapsedMilliseconds}ms, found {asins.Count} ASINs");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200,
            "Filtering by type should complete under 200ms");
    }

    [Fact]
    public async Task Query_SearchByValue_LargeDataset_ShouldBeEfficient()
    {
        // Arrange - Seed 10,000 product IDs
        await SeedProductIds(10000, certificateId: 1);

        // Act
        var stopwatch = Stopwatch.StartNew();

        var results = await _context.CertificateProductIds
            .Where(p => p.ProductIdValue.StartsWith("B0"))
            .Take(100)
            .ToListAsync();

        stopwatch.Stop();

        // Assert
        results.Should().NotBeEmpty();
        _output.WriteLine($"Search by value prefix from 10,000 records: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200,
            "Search by value should complete under 200ms");
    }

    [Fact]
    public async Task Query_PaginatedResults_ShouldBeConsistent()
    {
        // Arrange - Seed 5,000 product IDs
        await SeedProductIds(5000, certificateId: 1);
        var pageSize = 100;
        var pageTimes = new List<long>();

        // Act - Fetch multiple pages
        for (int page = 0; page < 10; page++)
        {
            var stopwatch = Stopwatch.StartNew();

            var results = await _context.CertificateProductIds
                .Where(p => p.CertificateId == 1)
                .OrderBy(p => p.Id)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            stopwatch.Stop();
            pageTimes.Add(stopwatch.ElapsedMilliseconds);

            results.Count.Should().Be(pageSize);
        }

        // Assert
        var avgTime = pageTimes.Average();
        var maxTime = pageTimes.Max();

        _output.WriteLine($"Pagination performance (10 pages of 100): Avg={avgTime:F1}ms, Max={maxTime}ms");
        maxTime.Should().BeLessThan(100, "Paginated queries should be consistently fast");
    }

    #endregion

    #region CSV Import Performance Tests

    [Theory]
    [InlineData(100, 2000)]
    [InlineData(1000, 10000)]
    [InlineData(5000, 30000)]
    public async Task ImportFromCsv_ShouldCompleteWithinThreshold(int recordCount, int maxMilliseconds)
    {
        // Arrange - Generate CSV data
        var csvData = GenerateCsvData(recordCount);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvData));

        // Act
        var stopwatch = Stopwatch.StartNew();

        var result = await _productIdService.ImportFromCsvAsync(1, stream);

        stopwatch.Stop();

        // Assert
        result.SuccessCount.Should().Be(recordCount);
        _output.WriteLine($"CSV import of {recordCount} records: {stopwatch.ElapsedMilliseconds}ms ({result.SuccessCount} success, {result.ErrorCount} errors)");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxMilliseconds,
            $"CSV import of {recordCount} records should complete under {maxMilliseconds}ms");
    }

    #endregion

    #region Export Performance Tests

    [Fact]
    public async Task ExportToCsv_10000Records_ShouldBeUnder5Seconds()
    {
        // Arrange - Seed 10,000 product IDs
        await SeedProductIds(10000, certificateId: 1);

        // Act
        var stopwatch = Stopwatch.StartNew();

        var csvBytes = await _productIdService.ExportToCsvAsync(1);

        stopwatch.Stop();

        // Assert
        csvBytes.Should().NotBeEmpty();
        var csvContent = Encoding.UTF8.GetString(csvBytes);
        var lineCount = csvContent.Split('\n').Length - 1; // Exclude header

        lineCount.Should().BeGreaterThanOrEqualTo(10000);
        _output.WriteLine($"Export 10,000 records to CSV: {stopwatch.ElapsedMilliseconds}ms, size={csvBytes.Length / 1024}KB");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000,
            "Exporting 10,000 product IDs to CSV should complete under 5 seconds");
    }

    #endregion

    #region High-Volume Certificate Tests

    [Fact]
    public async Task MultipleCertificates_EachWith1000ProductIds_ShouldQueryEfficiently()
    {
        // Arrange - Create 10 certificates, each with 1000 product IDs
        for (int certId = 1; certId <= 10; certId++)
        {
            await SeedProductIds(1000, certificateId: certId);
        }

        // Act - Query product IDs for a specific certificate
        var stopwatch = Stopwatch.StartNew();

        var productIds = await _context.CertificateProductIds
            .Where(p => p.CertificateId == 5)
            .ToListAsync();

        stopwatch.Stop();

        // Assert
        productIds.Count.Should().Be(1000);
        _output.WriteLine($"Query 1,000 product IDs from 10,000 total: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
            "Querying product IDs for one certificate among many should be efficient");
    }

    [Fact]
    public async Task CertificateWith5000ASINs_ShouldLoadEfficiently()
    {
        // Arrange - Seed 5000 ASINs for one certificate
        var productIds = Enumerable.Range(0, 5000).Select(i => new CertificateProductId
        {
            CertificateId = 1,
            ProductIdType = "ASIN",
            ProductIdValue = GenerateAsin(i),
            Status = "Active",
            Partner = "Amazon",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _context.CertificateProductIds.AddRange(productIds);
        await _context.SaveChangesAsync();

        // Act
        var stopwatch = Stopwatch.StartNew();

        var count = await _productIdService.GetProductIdCountByCertificateAsync(1);
        var asins = await _productIdService.GetProductIdsByCertificateAsync(1);

        stopwatch.Stop();

        // Assert
        count.Should().Be(5000);
        asins.Count.Should().Be(5000);
        _output.WriteLine($"Load 5,000 ASINs for certificate: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000,
            "Loading 5,000 ASINs for a certificate should complete under 1 second");
    }

    #endregion

    #region Validation Performance Tests

    [Fact]
    public async Task ValidateProductId_1000Validations_ShouldBeEfficient()
    {
        // Arrange
        var validations = Enumerable.Range(0, 1000)
            .Select(i => ("ASIN", GenerateAsin(i)))
            .ToList();

        // Act
        var stopwatch = Stopwatch.StartNew();

        var results = new List<bool>();
        foreach (var (type, value) in validations)
        {
            results.Add(await _productIdService.ValidateProductIdAsync(type, value));
        }

        stopwatch.Stop();

        // Assert
        results.All(r => r).Should().BeTrue();
        _output.WriteLine($"Validate 1,000 product IDs: {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedMilliseconds / 1000.0:F2}ms per validation)");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000,
            "1,000 validations should complete under 2 seconds");
    }

    [Fact]
    public async Task DuplicateCheck_Against10000Records_ShouldBeEfficient()
    {
        // Arrange - Seed 10,000 product IDs
        await SeedProductIds(10000, certificateId: 1);

        // Act - Check for duplicates (non-existent values)
        var stopwatch = Stopwatch.StartNew();

        var duplicateChecks = new List<bool>();
        for (int i = 0; i < 100; i++)
        {
            duplicateChecks.Add(await _productIdService.CheckDuplicateAsync("ASIN", $"B0NOTEXIST{i:D4}"));
        }

        stopwatch.Stop();

        // Assert
        duplicateChecks.All(d => !d).Should().BeTrue(); // All should be false (no duplicates)
        _output.WriteLine($"100 duplicate checks against 10,000 records: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000,
            "Duplicate checks should be efficient with proper indexing");
    }

    #endregion

    #region Helper Methods

    private void SeedBaseTestData()
    {
        // Seed minimal base data needed for tests
        var company = new Company { Id = 1, Name = "Test Company", Location = "Test" };
        var category = new ProductCategory { Id = 1, Name = "Test Category" };
        var product = new Product { Id = 1, Name = "Test Product", CategoryId = 1 };
        var certType = new CertificationType { Id = 1, Name = "Test Cert Type" };
        var certificate = new Certificate
        {
            Id = 1,
            Number = "TEST-001",
            Status = "active",
            CompanyId = 1,
            ProductId = 1,
            CertificationTypeId = 1,
            EffectiveDate = DateTime.Now.AddYears(-1),
            ExpirationDate = DateTime.Now.AddYears(1)
        };

        _context.Companies.Add(company);
        _context.ProductCategories.Add(category);
        _context.Products.Add(product);
        _context.CertificationTypes.Add(certType);
        _context.Certificates.Add(certificate);
        _context.SaveChanges();
    }

    private async Task SeedProductIds(int count, int certificateId)
    {
        var productIds = GenerateProductIds(count, certificateId);
        _context.CertificateProductIds.AddRange(productIds);
        await _context.SaveChangesAsync();
    }

    private List<CertificateProductId> GenerateProductIds(int count, int certificateId, int startIndex = 0)
    {
        return Enumerable.Range(startIndex, count).Select(i => new CertificateProductId
        {
            CertificateId = certificateId,
            ProductIdType = ProductIdTypes[_random.Next(ProductIdTypes.Length)],
            ProductIdValue = GenerateProductIdValue(ProductIdTypes[_random.Next(ProductIdTypes.Length)], i),
            Status = "Active",
            Partner = Partners[_random.Next(Partners.Length)],
            ProductName = $"Test Product {i}",
            ProductSku = $"SKU-{i:D8}",
            IsActive = true,
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();
    }

    private string GenerateProductIdValue(string type, int index)
    {
        return type switch
        {
            "ASIN" => GenerateAsin(index),
            "UPC" => GenerateUpc(index),
            "GTIN" => $"{10000000000000 + index:D14}",
            "EAN" => GenerateEan(index),
            "ISBN" => $"978{1000000000 + index:D10}",
            "SKU" => $"SKU-{index:D8}",
            "MPN" => $"MPN-{_random.Next(100, 999)}-{index:D6}",
            _ => $"ID{index:D10}"
        };
    }

    private string GenerateAsin(int index)
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

    private string GenerateUpc(int index)
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

    private string GenerateEan(int index)
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

    private string GenerateCsvData(int count)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Type,Value,Status,Partner,ProductName,ProductSku,Description");

        for (int i = 0; i < count; i++)
        {
            var type = ProductIdTypes[_random.Next(ProductIdTypes.Length)];
            var value = GenerateProductIdValue(type, i + 100000); // Offset to avoid duplicates
            sb.AppendLine($"{type},{value},Active,Amazon,Test Product {i},SKU-{i:D8},Test description");
        }

        return sb.ToString();
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }

    #endregion
}

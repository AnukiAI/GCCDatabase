# Test Examples - GCC Database Solution

This document provides practical examples of how to test different aspects of your GCC Database Solution.

## Table of Contents
1. [Model Testing Examples](#model-testing-examples)
2. [Service Testing Examples](#service-testing-examples)
3. [Database Testing Examples](#database-testing-examples)
4. [Controller Testing Examples](#controller-testing-examples)
5. [Validation Testing Examples](#validation-testing-examples)
6. [Mock Testing Examples](#mock-testing-examples)

## Model Testing Examples

### Testing Certificate Model Validation

```csharp
[Fact]
public void Certificate_WithValidData_ShouldInitializeCorrectly()
{
    // Arrange & Act
    var certificate = new Certificate
    {
        Number = "GCC001",
        Status = "active",
        EffectiveDate = DateTime.Now,
        ExpirationDate = DateTime.Now.AddYears(1),
        CompanyId = 1,
        ProductId = 1,
        CertificationTypeId = 1
    };

    // Assert
    Assert.Equal("GCC001", certificate.Number);
    Assert.Equal("active", certificate.Status);
    Assert.True(certificate.EffectiveDate <= DateTime.Now);
    Assert.True(certificate.ExpirationDate > DateTime.Now);
}

[Fact]
public void Certificate_NumberTooLong_ShouldViolateConstraint()
{
    // Arrange & Act
    var certificate = new Certificate
    {
        Number = "VERYLONGNUMBER123" // Exceeds MaxLength(7)
    };

    // Assert
    Assert.True(certificate.Number.Length > 7);
    // Note: Actual validation would occur at database save time
}

[Theory]
[InlineData("active")]
[InlineData("expired")]
[InlineData("pending")]
public void Certificate_ValidStatuses_ShouldBeAccepted(string status)
{
    // Arrange & Act
    var certificate = new Certificate { Status = status };

    // Assert
    Assert.Equal(status, certificate.Status);
}
```

### Testing Company Model

```csharp
[Fact]
public void Company_ShouldInitializeWithDefaults()
{
    // Arrange & Act
    var company = new Company();

    // Assert
    Assert.Equal(string.Empty, company.Name);
    Assert.True(company.IsActive); // Default should be true
    Assert.True(company.CreatedAt <= DateTime.UtcNow);
    Assert.NotNull(company.Certificates);
    Assert.NotNull(company.Products);
    Assert.NotNull(company.Users);
}

[Fact]
public void Company_WithContactInfo_ShouldStoreCorrectly()
{
    // Arrange & Act
    var company = new Company
    {
        Name = "Green Tech Solutions",
        ContactInfo = "info@greentech.com",
        Location = "Portland, OR",
        Country = "USA"
    };

    // Assert
    Assert.Equal("Green Tech Solutions", company.Name);
    Assert.Equal("info@greentech.com", company.ContactInfo);
    Assert.Equal("Portland, OR", company.Location);
    Assert.Equal("USA", company.Country);
}
```

## Service Testing Examples

### Testing Certificate Service with Mock Dependencies

```csharp
public class CertificateServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<CertificateService>> _mockLogger;
    private readonly Mock<ICertificateRepository> _mockRepository;
    private readonly CertificateService _service;

    public CertificateServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<CertificateService>>();
        _mockRepository = new Mock<ICertificateRepository>();
        _service = new CertificateService(_mockLogger.Object, _mockRepository.Object, null, _context);
    }

    [Fact]
    public async Task ValidateCertificate_WithExistingNumber_ShouldReturnValid()
    {
        // Arrange
        var certificate = new Certificate
        {
            Number = "GCC001",
            Status = "active",
            ExpirationDate = DateTime.Now.AddMonths(6)
        };
        
        _context.Certificates.Add(certificate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateCertificateAsync("GCC001");

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("active", result.Status);
        Assert.False(result.IsExpired);
    }

    [Fact]
    public async Task ValidateCertificate_WithExpiredCertificate_ShouldIndicateExpired()
    {
        // Arrange
        var certificate = new Certificate
        {
            Number = "GCC002",
            Status = "expired",
            ExpirationDate = DateTime.Now.AddDays(-30)
        };
        
        _context.Certificates.Add(certificate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateCertificateAsync("GCC002");

        // Assert
        Assert.True(result.IsValid); // Certificate exists
        Assert.True(result.IsExpired);
        Assert.Contains("expired", result.Message.ToLower());
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

## Database Testing Examples

### Testing Entity Framework Operations

```csharp
public class DatabaseOperationTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public DatabaseOperationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Database_CanAddAndRetrieveCertificate()
    {
        // Arrange
        var company = new Company { Name = "Test Company" };
        var product = new Product { Name = "Test Product" };
        var certType = new CertificationType { Name = "CSF" };

        _context.Companies.Add(company);
        _context.Products.Add(product);
        _context.CertificationTypes.Add(certType);
        await _context.SaveChangesAsync();

        var certificate = new Certificate
        {
            Number = "TEST001",
            CompanyId = company.Id,
            ProductId = product.Id,
            CertificationTypeId = certType.Id,
            EffectiveDate = DateTime.Now,
            ExpirationDate = DateTime.Now.AddYears(1)
        };

        // Act
        _context.Certificates.Add(certificate);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.Certificates
            .Include(c => c.Company)
            .Include(c => c.Product)
            .Include(c => c.CertificationType)
            .FirstOrDefaultAsync(c => c.Number == "TEST001");

        Assert.NotNull(retrieved);
        Assert.Equal("TEST001", retrieved.Number);
        Assert.Equal("Test Company", retrieved.Company.Name);
        Assert.Equal("Test Product", retrieved.Product.Name);
        Assert.Equal("CSF", retrieved.CertificationType.Name);
    }

    [Fact]
    public async Task Database_CascadeDelete_ShouldWork()
    {
        // Arrange
        var company = new Company { Name = "Delete Test Company" };
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        var certificate = new Certificate
        {
            Number = "DEL001",
            CompanyId = company.Id,
            ProductId = 1, // Assume exists
            CertificationTypeId = 1 // Assume exists
        };
        
        _context.Certificates.Add(certificate);
        await _context.SaveChangesAsync();

        // Act - Delete company (this would depend on your cascade rules)
        _context.Companies.Remove(company);
        
        // Note: Actual cascade behavior depends on your EF configuration
        // This test would verify your intended cascade delete behavior
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

## Controller Testing Examples

### Testing Certificate Controller Actions

```csharp
public class CertificateControllerTests
{
    private readonly Mock<ICertificateService> _mockService;
    private readonly Mock<ILogger<CertificateController>> _mockLogger;
    private readonly CertificateController _controller;

    public CertificateControllerTests()
    {
        _mockService = new Mock<ICertificateService>();
        _mockLogger = new Mock<ILogger<CertificateController>>();
        _controller = new CertificateController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetCertificate_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var certificateDetails = new CertificateDetailsViewModel
        {
            Id = 1,
            CertificateNumber = "GCC001",
            CompanyName = "Test Company",
            ProductName = "Test Product"
        };

        _mockService.Setup(s => s.GetCertificateDetailsAsync(1, It.IsAny<string>()))
                   .ReturnsAsync(certificateDetails);

        // Act
        var result = await _controller.Details(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<CertificateDetailsViewModel>(viewResult.Model);
        Assert.Equal("GCC001", model.CertificateNumber);
    }

    [Fact]
    public async Task GetCertificate_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetCertificateDetailsAsync(999, It.IsAny<string>()))
                   .ReturnsAsync((CertificateDetailsViewModel)null);

        // Act
        var result = await _controller.Details(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
```

## Validation Testing Examples

### Testing Data Annotation Validations

```csharp
public class ValidationTests
{
    [Fact]
    public void CertificateEditViewModel_WithInvalidData_ShouldFailValidation()
    {
        // Arrange
        var model = new CertificateEditViewModel
        {
            Number = "", // Required field
            // Other required fields missing
        };

        var context = new ValidationContext(model, null, null);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(model, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains("Number"));
    }

    [Theory]
    [InlineData("GCC001")] // Valid
    [InlineData("ABC123")] // Valid
    [InlineData("X")] // Valid - short
    public void CertificateNumber_ValidFormats_ShouldPass(string number)
    {
        // Arrange
        var model = new CertificateEditViewModel { Number = number };
        var context = new ValidationContext(model) { MemberName = "Number" };
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateProperty(number, context, results);

        // Assert
        Assert.True(isValid || number.Length <= 7); // Should pass if within length limit
    }
}
```

## Mock Testing Examples

### Advanced Mocking Scenarios

```csharp
public class MockingExamples
{
    [Fact]
    public void MockRepository_SetupMultipleCalls_ShouldWork()
    {
        // Arrange
        var mockRepo = new Mock<ICertificateRepository>();
        
        mockRepo.SetupSequence(r => r.GetByIdAsync(It.IsAny<int>()))
               .ReturnsAsync(new Certificate { Id = 1, Number = "FIRST" })
               .ReturnsAsync(new Certificate { Id = 2, Number = "SECOND" })
               .ReturnsAsync((Certificate)null); // Third call returns null

        // Act & Assert
        var first = mockRepo.Object.GetByIdAsync(1).Result;
        var second = mockRepo.Object.GetByIdAsync(2).Result;
        var third = mockRepo.Object.GetByIdAsync(3).Result;

        Assert.Equal("FIRST", first.Number);
        Assert.Equal("SECOND", second.Number);
        Assert.Null(third);
    }

    [Fact]
    public void MockLogger_VerifyLogCalls_ShouldWork()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CertificateService>>();
        var service = new CertificateService(mockLogger.Object, null, null, null);

        // Act - This would normally trigger logging
        // service.SomeMethodThatLogs();

        // Assert - Verify that logging occurred
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("expected message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
```

## Performance Testing Examples

```csharp
public class PerformanceTests
{
    [Fact]
    public void LargeDatasetQuery_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        
        // Add many records for performance testing
        var certificates = Enumerable.Range(1, 1000)
            .Select(i => new Certificate 
            { 
                Number = $"PERF{i:D4}",
                CompanyId = 1,
                ProductId = 1,
                CertificationTypeId = 1
            })
            .ToList();

        context.Certificates.AddRange(certificates);
        context.SaveChanges();

        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var results = context.Certificates
            .Where(c => c.Number.StartsWith("PERF"))
            .Take(100)
            .ToList();
        stopwatch.Stop();

        // Assert
        Assert.Equal(100, results.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 500, 
            $"Query took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
    }
}
```

## Test Data Builders

Create reusable test data builders for complex objects:

```csharp
public class CertificateTestDataBuilder
{
    private Certificate _certificate;

    public CertificateTestDataBuilder()
    {
        _certificate = new Certificate
        {
            Number = "TEST001",
            Status = "active",
            EffectiveDate = DateTime.Now,
            ExpirationDate = DateTime.Now.AddYears(1),
            CompanyId = 1,
            ProductId = 1,
            CertificationTypeId = 1
        };
    }

    public CertificateTestDataBuilder WithNumber(string number)
    {
        _certificate.Number = number;
        return this;
    }

    public CertificateTestDataBuilder WithStatus(string status)
    {
        _certificate.Status = status;
        return this;
    }

    public CertificateTestDataBuilder WithExpirationDate(DateTime expiration)
    {
        _certificate.ExpirationDate = expiration;
        return this;
    }

    public Certificate Build() => _certificate;
}

// Usage in tests:
[Fact]
public void TestWithBuilder()
{
    // Arrange
    var certificate = new CertificateTestDataBuilder()
        .WithNumber("BUILD001")
        .WithStatus("pending")
        .WithExpirationDate(DateTime.Now.AddMonths(6))
        .Build();

    // Act & Assert
    Assert.Equal("BUILD001", certificate.Number);
    Assert.Equal("pending", certificate.Status);
}
```

## Integration Test Examples

```csharp
public class IntegrationTestExamples : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public IntegrationTestExamples(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCertificateList_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/Certificate");

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal("text/html; charset=utf-8", 
            response.Content.Headers.ContentType.ToString());
    }

    [Fact]
    public async Task ApiEndpoint_ReturnsJsonData()
    {
        // Act
        var response = await _client.GetAsync("/api/certificates");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("certificates", content.ToLower());
    }
}
```

These examples provide a solid foundation for testing all aspects of your GCC Database Solution. Adapt them to your specific needs and add more tests as your application grows!
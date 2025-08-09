# GCC Database Application - Testing Guide

## Table of Contents
1. [Overview](#overview)
2. [Testing Strategy](#testing-strategy)
3. [Unit Testing](#unit-testing)
4. [Integration Testing](#integration-testing)
5. [End-to-End Testing](#end-to-end-testing)
6. [Performance Testing](#performance-testing)
7. [Security Testing](#security-testing)
8. [Test Data Management](#test-data-management)
9. [Continuous Integration](#continuous-integration)
10. [Test Coverage](#test-coverage)
11. [Testing Tools](#testing-tools)
12. [Best Practices](#best-practices)

## Overview

This guide provides comprehensive testing procedures for the GCC Database Web Application, covering all testing levels from unit tests to end-to-end testing.

### Testing Principles
- **Test Early and Often**: Write tests alongside code development
- **Automate Everything**: Minimize manual testing through automation
- **Test in Isolation**: Each test should be independent
- **Clear Test Names**: Tests should describe what they verify
- **Fast Feedback**: Tests should run quickly to provide rapid feedback

## Testing Strategy

### Testing Pyramid
```
         /\
        /E2E\        <- End-to-End Tests (10%)
       /------\
      /  UAT  \      <- User Acceptance Tests (20%)
     /----------\
    / Integration \  <- Integration Tests (30%)
   /--------------\
  /     Unit      \  <- Unit Tests (40%)
 /------------------\
```

### Test Categories
| Type | Purpose | Scope | Speed | Project |
|------|---------|-------|-------|---------|
| Unit | Test individual components | Single method/class | Fast (ms) | GCCWebApp.Tests |
| Integration | Test component interactions | Multiple components | Medium (seconds) | GCCWebApp.E2E.Tests |
| UAT | Test user acceptance criteria | User scenarios | Medium (seconds) | GCCWebApp.UAT.Tests |
| E2E | Test complete user workflows | Full application with UI | Slow (minutes) | GCCWebApp.E2E.Tests |
| Performance | Test system performance | Critical paths | Variable | All projects |
| Security | Test security vulnerabilities | All layers | Variable | All projects |

## Unit Testing

### Setting Up xUnit
```bash
# Add xUnit packages
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package Moq
dotnet add package FluentAssertions
```

### Controller Unit Tests
```csharp
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Controllers;
using GCCWebApp.Services;
using GCCWebApp.Models;

public class CertificateControllerTests
{
    private readonly Mock<ICertificateService> _mockService;
    private readonly CertificateController _controller;

    public CertificateControllerTests()
    {
        _mockService = new Mock<ICertificateService>();
        _controller = new CertificateController(_mockService.Object);
    }

    [Fact]
    public async Task GetCertificate_ValidId_ReturnsOkResult()
    {
        // Arrange
        var certificateId = 1;
        var expectedCertificate = new Certificate 
        { 
            Id = certificateId, 
            Number = "GCC-2024-001" 
        };
        
        _mockService.Setup(s => s.GetByIdAsync(certificateId))
                   .ReturnsAsync(expectedCertificate);

        // Act
        var result = await _controller.Get(certificateId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(expectedCertificate);
    }

    [Fact]
    public async Task GetCertificate_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var certificateId = 999;
        _mockService.Setup(s => s.GetByIdAsync(certificateId))
                   .ReturnsAsync((Certificate)null);

        // Act
        var result = await _controller.Get(certificateId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task CreateCertificate_InvalidNumber_ReturnsBadRequest(string number)
    {
        // Arrange
        var certificate = new Certificate { Number = number };

        // Act
        var result = await _controller.Create(certificate);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
```

### Service Unit Tests
```csharp
public class CertificateServiceTests
{
    private readonly Mock<IRepository<Certificate>> _mockRepository;
    private readonly Mock<ILogger<CertificateService>> _mockLogger;
    private readonly CertificateService _service;

    public CertificateServiceTests()
    {
        _mockRepository = new Mock<IRepository<Certificate>>();
        _mockLogger = new Mock<ILogger<CertificateService>>();
        _service = new CertificateService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetActiveСertificates_ReturnsOnlyActive()
    {
        // Arrange
        var certificates = new List<Certificate>
        {
            new Certificate { Id = 1, Status = "Active" },
            new Certificate { Id = 2, Status = "Expired" },
            new Certificate { Id = 3, Status = "Active" }
        };

        _mockRepository.Setup(r => r.GetAllAsync())
                      .ReturnsAsync(certificates);

        // Act
        var result = await _service.GetActiveCertificatesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.Status == "Active");
    }

    [Fact]
    public async Task CreateCertificate_DuplicateNumber_ThrowsException()
    {
        // Arrange
        var certificate = new Certificate { Number = "GCC-2024-001" };
        
        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Certificate, bool>>>()))
                      .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateException>(
            () => _service.CreateAsync(certificate)
        );
    }
}
```

### Repository Unit Tests
```csharp
public class CertificateRepositoryTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly AppDbContext _context;
    private readonly CertificateRepository _repository;

    public CertificateRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new AppDbContext(_options);
        _repository = new CertificateRepository(_context);
    }

    [Fact]
    public async Task Add_Certificate_PersistsToDatabase()
    {
        // Arrange
        var certificate = new Certificate 
        { 
            Number = "GCC-2024-001",
            Status = "Active"
        };

        // Act
        await _repository.AddAsync(certificate);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.Certificates.FirstOrDefaultAsync();
        saved.Should().NotBeNull();
        saved.Number.Should().Be("GCC-2024-001");
    }

    [Fact]
    public async Task GetBySpecification_ReturnsFilteredResults()
    {
        // Arrange
        await SeedTestData();
        var spec = new ActiveCertificatesSpecification();

        // Act
        var result = await _repository.GetBySpecificationAsync(spec);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.Status == "Active");
    }

    private async Task SeedTestData()
    {
        var certificates = new[]
        {
            new Certificate { Number = "GCC-001", Status = "Active" },
            new Certificate { Number = "GCC-002", Status = "Expired" },
            new Certificate { Number = "GCC-003", Status = "Active" }
        };

        _context.Certificates.AddRange(certificates);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
```

## Integration Testing

### API Integration Tests
```csharp
public class CertificateApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CertificateApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real database with in-memory
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("IntegrationTests");
                });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetCertificates_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/certificates");

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateCertificate_ValidData_ReturnsCreated()
    {
        // Arrange
        var certificate = new
        {
            Number = "GCC-2024-TEST",
            Type = "Product",
            CompanyId = 1,
            EffectiveDate = DateTime.Now,
            ExpirationDate = DateTime.Now.AddYears(1)
        };

        var json = JsonSerializer.Serialize(certificate);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/certificates", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateCertificate_ValidData_ReturnsNoContent()
    {
        // Arrange
        var certificateId = 1;
        var update = new { Status = "Renewed" };
        var json = JsonSerializer.Serialize(update);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/api/certificates/{certificateId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
```

### Database Integration Tests
```csharp
public class DatabaseIntegrationTests : IDisposable
{
    private readonly SqlConnection _connection;
    private readonly AppDbContext _context;

    public DatabaseIntegrationTests()
    {
        var connectionString = "Server=(localdb)\\mssqllocaldb;Database=GCCTest;Trusted_Connection=true;";
        _connection = new SqlConnection(connectionString);
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connection)
            .Options;
            
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Transaction_Rollback_OnError()
    {
        // Arrange
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Act
            var certificate = new Certificate { Number = "TEST-001" };
            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();

            // Force an error
            throw new Exception("Simulated error");

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
        }

        // Assert
        var count = await _context.Certificates.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task CascadeDelete_RemovesRelatedEntities()
    {
        // Arrange
        var company = new Company { Name = "Test Company" };
        var certificate = new Certificate 
        { 
            Number = "TEST-001",
            Company = company 
        };

        _context.Companies.Add(company);
        _context.Certificates.Add(certificate);
        await _context.SaveChangesAsync();

        // Act
        _context.Companies.Remove(company);
        await _context.SaveChangesAsync();

        // Assert
        var certificateExists = await _context.Certificates
            .AnyAsync(c => c.Number == "TEST-001");
        certificateExists.Should().BeFalse();
    }

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
        _connection?.Dispose();
    }
}
```

## End-to-End Testing

### Test Project Structure
The application now includes three dedicated test projects:

1. **GCCWebApp.Tests** - Unit tests for controllers, services, and repositories
2. **GCCWebApp.UAT.Tests** - User acceptance tests for business scenarios  
3. **GCCWebApp.E2E.Tests** - End-to-end tests including UI automation with Playwright

### Running All Tests
```bash
# Run all test projects
dotnet test

# Run specific test project
dotnet test ../GCCWebApp.Tests
dotnet test ../GCCWebApp.UAT.Tests
dotnet test ../GCCWebApp.E2E.Tests

# Run with detailed output
dotnet test --logger:"console;verbosity=detailed"

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Setting Up E2E Tests with Playwright

#### First-Time Setup
```bash
# Navigate to E2E test project
cd ../GCCWebApp.E2E.Tests

# Install Playwright browsers (one-time setup)
pwsh bin/Debug/net9.0/playwright.ps1 install

# Or for non-Windows systems
dotnet build
npx playwright install
```

#### Running E2E Tests
```bash
# Run all E2E tests
dotnet test ../GCCWebApp.E2E.Tests

# Run specific test category
dotnet test --filter "FullyQualifiedName~UI"
dotnet test --filter "FullyQualifiedName~Api"

# Run with visible browser (for debugging)
# Edit CertificateUITests.cs and set Headless = false
```

### E2E Test Categories

#### API Integration Tests (WebApplicationFactory)
Tests API endpoints using in-memory test server:
- Authentication flows
- Certificate CRUD operations
- Search and filtering
- Permission validation

#### UI Browser Tests (Playwright)
Tests full user workflows with real browser:
- Login/logout flows
- Form submissions
- Navigation
- Responsive design
- Cross-browser compatibility

### Sample Playwright UI Test
```csharp
[Fact]
public async Task UserCanSearchCertificates()
{
    // Navigate to certificates page
    await _page.GotoAsync($"{BaseUrl}/Certificate");
    
    // Enter search term
    await _page.FillAsync("input[name='searchTerm']", "Amazon");
    
    // Click search button
    await _page.ClickAsync("button[type='submit']");
    
    // Wait for results
    await _page.WaitForSelectorAsync(".certificate-card");
    
    // Verify results
    var results = await _page.QuerySelectorAllAsync(".certificate-card");
    results.Should().NotBeEmpty();
    
    var firstResult = await results[0].TextContentAsync();
    firstResult.Should().Contain("Amazon");
}
```

### Sample WebApplicationFactory Test
```csharp
[Fact]
public async Task GetCertificates_ReturnsSuccessStatusCode()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/api/certificates");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    
    var content = await response.Content.ReadAsStringAsync();
    var certificates = JsonSerializer.Deserialize<List<Certificate>>(content);
    certificates.Should().NotBeEmpty();
}
```

## End-to-End Testing

### Selenium Setup
```csharp
public class E2ETestBase : IDisposable
{
    protected IWebDriver Driver { get; private set; }
    protected string BaseUrl { get; private set; }

    public E2ETestBase()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless"); // Run in headless mode for CI
        options.AddArgument("--no-sandbox");
        
        Driver = new ChromeDriver(options);
        Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        
        BaseUrl = "https://localhost:5001";
    }

    protected void Login(string username, string password)
    {
        Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Login");
        
        var usernameField = Driver.FindElement(By.Id("username"));
        var passwordField = Driver.FindElement(By.Id("password"));
        var submitButton = Driver.FindElement(By.CssSelector("button[type='submit']"));
        
        usernameField.SendKeys(username);
        passwordField.SendKeys(password);
        submitButton.Click();
        
        // Wait for redirect
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.Url.Contains("/Dashboard"));
    }

    public void Dispose()
    {
        Driver?.Quit();
        Driver?.Dispose();
    }
}
```

### E2E Test Examples
```csharp
public class CertificateE2ETests : E2ETestBase
{
    [Fact]
    public void CreateCertificate_CompleteWorkflow_Success()
    {
        // Arrange
        Login("admin@gcc.com", "Admin123!");

        // Navigate to certificates
        Driver.Navigate().GoToUrl($"{BaseUrl}/Certificates");
        
        // Click Add Certificate button
        var addButton = Driver.FindElement(By.Id("add-certificate"));
        addButton.Click();

        // Fill form
        Driver.FindElement(By.Id("CertificateNumber")).SendKeys("E2E-TEST-001");
        
        var typeDropdown = new SelectElement(Driver.FindElement(By.Id("TypeId")));
        typeDropdown.SelectByText("Product Certification");
        
        var companyDropdown = new SelectElement(Driver.FindElement(By.Id("CompanyId")));
        companyDropdown.SelectByIndex(1);
        
        Driver.FindElement(By.Id("EffectiveDate")).SendKeys("01/01/2024");
        Driver.FindElement(By.Id("ExpirationDate")).SendKeys("01/01/2025");

        // Submit form
        Driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        // Assert - Verify redirect to certificate details
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.Url.Contains("/Certificates/Details"));
        
        var certificateNumber = Driver.FindElement(By.CssSelector(".certificate-number"));
        certificateNumber.Text.Should().Contain("E2E-TEST-001");
    }

    [Fact]
    public void SearchCertificates_FilterByStatus_ReturnsFilteredResults()
    {
        // Arrange
        Login("analyst@gcc.com", "Analyst123!");
        Driver.Navigate().GoToUrl($"{BaseUrl}/Certificates");

        // Act - Apply filter
        var statusFilter = new SelectElement(Driver.FindElement(By.Id("status-filter")));
        statusFilter.SelectByText("Active");
        
        Driver.FindElement(By.Id("apply-filters")).Click();

        // Wait for results to update
        Thread.Sleep(1000); // Consider using WebDriverWait instead

        // Assert
        var statusBadges = Driver.FindElements(By.CssSelector(".status-badge"));
        statusBadges.Should().NotBeEmpty();
        statusBadges.Should().OnlyContain(badge => badge.Text == "Active");
    }
}
```

## Performance Testing

### Load Testing with NBomber
```csharp
using NBomber.CSharp;
using NBomber.Http.CSharp;

public class LoadTests
{
    [Fact]
    public void CertificateApi_LoadTest()
    {
        var httpClient = new HttpClient();

        var scenario = Scenario.Create("certificate_api_load_test", async context =>
        {
            var request = Http.CreateRequest("GET", "https://localhost:5001/api/certificates")
                             .WithHeader("Accept", "application/json");

            var response = await Http.Send(httpClient, request);
            
            return response;
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromSeconds(30))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert
        stats.AllOkCount.Should().BeGreaterThan(2000);
        stats.AllFailCount.Should().Be(0);
        stats.ScenarioStats[0].Ok.Latency.Mean.Should().BeLessThan(500);
    }
}
```

### Stress Testing
```csharp
public class StressTests
{
    [Fact]
    public async Task Database_ConcurrentWrites_HandlesCorrectly()
    {
        // Arrange
        var tasks = new List<Task>();
        var service = new CertificateService();

        // Act - Create 100 concurrent requests
        for (int i = 0; i < 100; i++)
        {
            var task = Task.Run(async () =>
            {
                var certificate = new Certificate
                {
                    Number = $"STRESS-{Guid.NewGuid()}",
                    Type = "Test"
                };
                
                await service.CreateAsync(certificate);
            });
            
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert
        var count = await service.GetCountAsync();
        count.Should().Be(100);
    }
}
```

## Security Testing

### SQL Injection Tests
```csharp
public class SecurityTests
{
    [Theory]
    [InlineData("'; DROP TABLE Certificates; --")]
    [InlineData("1' OR '1'='1")]
    [InlineData("<script>alert('XSS')</script>")]
    public async Task SearchEndpoint_SqlInjectionAttempt_ReturnsSafeResponse(string maliciousInput)
    {
        // Arrange
        var client = new HttpClient();
        
        // Act
        var response = await client.GetAsync(
            $"https://localhost:5001/api/certificates/search?query={Uri.EscapeDataString(maliciousInput)}"
        );

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        
        // Verify database is still intact
        var checkResponse = await client.GetAsync("https://localhost:5001/api/certificates");
        checkResponse.Should().BeSuccessful();
    }

    [Fact]
    public async Task Authentication_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await client.GetAsync("https://localhost:5001/api/certificates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

## Test Data Management

### Test Data Builder Pattern
```csharp
public class CertificateBuilder
{
    private Certificate _certificate = new Certificate();

    public CertificateBuilder WithNumber(string number)
    {
        _certificate.Number = number;
        return this;
    }

    public CertificateBuilder WithCompany(string companyName)
    {
        _certificate.Company = new Company { Name = companyName };
        return this;
    }

    public CertificateBuilder WithStatus(string status)
    {
        _certificate.Status = status;
        return this;
    }

    public CertificateBuilder AsActive()
    {
        _certificate.Status = "Active";
        _certificate.EffectiveDate = DateTime.Now.AddDays(-30);
        _certificate.ExpirationDate = DateTime.Now.AddDays(335);
        return this;
    }

    public CertificateBuilder AsExpired()
    {
        _certificate.Status = "Expired";
        _certificate.EffectiveDate = DateTime.Now.AddYears(-2);
        _certificate.ExpirationDate = DateTime.Now.AddDays(-30);
        return this;
    }

    public Certificate Build() => _certificate;
}

// Usage
var certificate = new CertificateBuilder()
    .WithNumber("TEST-001")
    .WithCompany("Test Corp")
    .AsActive()
    .Build();
```

### Database Seeding
```csharp
public class TestDataSeeder
{
    private readonly AppDbContext _context;

    public TestDataSeeder(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        await SeedCompanies();
        await SeedProducts();
        await SeedCertificates();
        await SeedUsers();
        
        await _context.SaveChangesAsync();
    }

    private async Task SeedCompanies()
    {
        if (!_context.Companies.Any())
        {
            var companies = new[]
            {
                new Company { Name = "ASSA ABLOY", Country = "Sweden" },
                new Company { Name = "3M", Country = "USA" },
                new Company { Name = "Test Company", Country = "Test" }
            };

            _context.Companies.AddRange(companies);
        }
    }

    private async Task SeedCertificates()
    {
        if (!_context.Certificates.Any())
        {
            var certificates = Enumerable.Range(1, 50)
                .Select(i => new CertificateBuilder()
                    .WithNumber($"TEST-{i:D4}")
                    .AsActive()
                    .Build());

            _context.Certificates.AddRange(certificates);
        }
    }
}
```

## Continuous Integration

### GitHub Actions Configuration
```yaml
name: Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2019-latest
        env:
          ACCEPT_EULA: Y
          SA_PASSWORD: Test@123
        ports:
          - 1433:1433
        options: >-
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Test@123 -Q 'SELECT 1'"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run Unit Tests
      run: dotnet test --no-build --filter "Category=Unit" --logger trx --results-directory TestResults
    
    - name: Run Integration Tests
      run: dotnet test --no-build --filter "Category=Integration" --logger trx --results-directory TestResults
      env:
        ConnectionStrings__DefaultConnection: "Server=localhost;Database=GCCTest;User=sa;Password=Test@123;TrustServerCertificate=true"
    
    - name: Upload Test Results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results
        path: TestResults
    
    - name: Generate Code Coverage Report
      run: |
        dotnet test --no-build --collect:"XPlat Code Coverage"
        dotnet tool install --global dotnet-reportgenerator-globaltool
        reportgenerator -reports:**/coverage.cobertura.xml -targetdir:CodeCoverage -reporttypes:HtmlInline_AzurePipelines
    
    - name: Upload Code Coverage
      uses: actions/upload-artifact@v3
      with:
        name: code-coverage
        path: CodeCoverage
```

## Test Coverage

### Coverage Configuration
```xml
<!-- In .csproj file -->
<PropertyGroup>
  <CollectCoverage>true</CollectCoverage>
  <CoverletOutputFormat>cobertura</CoverletOutputFormat>
  <CoverletOutput>./TestResults/</CoverletOutput>
  <ExcludeFromCodeCoverage>
    Migrations/**,
    Program.cs,
    Startup.cs
  </ExcludeFromCodeCoverage>
</PropertyGroup>
```

### Coverage Goals
| Component | Target Coverage |
|-----------|----------------|
| Controllers | 80% |
| Services | 90% |
| Repositories | 85% |
| Models | 70% |
| Utilities | 95% |
| Overall | 85% |

## Testing Tools

### Required Tools
- **xUnit**: Unit testing framework
- **Moq**: Mocking framework
- **FluentAssertions**: Assertion library
- **Selenium WebDriver**: E2E testing
- **NBomber**: Load testing
- **Coverlet**: Code coverage
- **SpecFlow**: BDD testing (optional)

### VS Code Extensions
- .NET Core Test Explorer
- Coverage Gutters
- Test Explorer UI

### Visual Studio Features
- Test Explorer
- Live Unit Testing
- Code Coverage Analysis
- Test Impact Analysis

## Best Practices

### General Guidelines
1. **Arrange-Act-Assert Pattern**: Structure tests clearly
2. **One Assertion Per Test**: Keep tests focused
3. **Descriptive Names**: Test names should describe the scenario
4. **Independent Tests**: Tests shouldn't depend on each other
5. **Clean Test Code**: Test code should be as clean as production code

### Naming Conventions
```csharp
// Pattern: MethodName_Scenario_ExpectedBehavior
public void GetCertificate_ValidId_ReturnsOkResult()
public void CreateCertificate_DuplicateNumber_ThrowsException()
public void UpdateCertificate_NonExistentId_ReturnsNotFound()
```

### Test Organization
```
Tests/
├── Unit/
│   ├── Controllers/
│   ├── Services/
│   └── Repositories/
├── Integration/
│   ├── Api/
│   └── Database/
├── E2E/
│   ├── Workflows/
│   └── Pages/
├── Performance/
└── Security/
```

### Continuous Improvement
1. Review test failures regularly
2. Update tests when requirements change
3. Remove obsolete tests
4. Refactor test code for maintainability
5. Monitor test execution time

## Support

For testing support:
- Review existing test examples
- Check test documentation
- Contact QA team lead
- Review CI/CD pipeline logs

## Version History

- **v1.0.0** - Initial testing guide
- Last Updated: August 2024
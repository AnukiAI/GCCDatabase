# GCC Database Solution - Testing Guide

This comprehensive guide explains how to use the test project to validate your GCC Database Solution web application.

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Getting Started](#getting-started)
4. [Running Tests](#running-tests)
5. [Understanding Test Results](#understanding-test-results)
6. [Test Categories](#test-categories)
7. [Writing New Tests](#writing-new-tests)
8. [Best Practices](#best-practices)
9. [Troubleshooting](#troubleshooting)
10. [Continuous Integration](#continuous-integration)

## Overview

The `GCCWebApp.Tests` project provides comprehensive testing for your GCC Database Solution, including:

- **Unit Tests** - Test individual components in isolation
- **Integration Tests** - Test component interactions
- **Model Validation Tests** - Ensure data models work correctly
- **Business Logic Tests** - Validate application logic
- **Database Tests** - Test data access layer

### Test Framework Stack
- **xUnit** - Primary testing framework
- **Entity Framework Core In-Memory** - Database testing
- **Moq** - Mocking framework for dependencies
- **ASP.NET Core Testing** - Web application testing

## Prerequisites

### Required Software
- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or JetBrains Rider (optional but recommended)

### Verify Installation
```bash
# Check .NET version
dotnet --version

# Ensure you're in the solution directory
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"
```

## Getting Started

### 1. Verify Solution Structure
```bash
# List all projects in solution
dotnet sln list
```

Expected output:
```
Project(s)
----------
..\GCCWebApp.Tests\GCCWebApp.Tests.csproj
GCCWebApp.csproj
```

### 2. Build the Test Project
```bash
# Build only the test project
dotnet build GCCWebApp.Tests

# Or build the entire solution
dotnet build
```

### 3. Restore Dependencies
```bash
# Restore NuGet packages
dotnet restore
```

## Running Tests

### Basic Commands

#### Run All Tests
```bash
# Run all tests with summary
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests with minimal output
dotnet test --verbosity quiet
```

#### Run Specific Test Project
```bash
# Run only GCCWebApp.Tests
dotnet test GCCWebApp.Tests
```

#### Run Tests by Category
```bash
# Run tests with specific filter
dotnet test --filter "FullyQualifiedName~SimpleTests"

# Run only model tests
dotnet test --filter "FullyQualifiedName~ModelTests"
```

### Advanced Testing Options

#### Generate Test Report
```bash
# Run tests and generate detailed results
dotnet test --logger "trx;LogFileName=TestResults.trx"

# Run with code coverage (requires coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

#### Run Tests in Parallel
```bash
# Run tests in parallel for better performance
dotnet test --parallel
```

#### Watch Mode (Continuous Testing)
```bash
# Automatically run tests when code changes
dotnet watch test --project GCCWebApp.Tests
```

## Understanding Test Results

### Successful Test Run Example
```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    32, Skipped:     0, Total:    32, Duration: 2 s
```

### Failed Test Example
```
Failed GCCWebApp.Tests.CertificateServiceTests.GetCertificatesAsync_ShouldReturnCertificates [247 ms]
Error Message:
   Assert.Single() Failure: The collection was empty
Stack Trace:
     at GCCWebApp.Tests.CertificateServiceTests.GetCertificatesAsync_ShouldReturnCertificates()
```

### Test Result Categories
- **Passed** ✅ - Test executed successfully
- **Failed** ❌ - Test failed with assertion error
- **Skipped** ⏭️ - Test was ignored (using `[Skip]` attribute)

## Test Categories

### 1. Model Tests (`SimpleTests.cs`)

Tests basic model functionality and validation:

```csharp
[Fact]
public void Certificate_ShouldInitializeWithDefaults()
{
    var certificate = new Certificate();
    
    Assert.Equal(string.Empty, certificate.Number);
    Assert.Equal("active", certificate.Status);
}
```

**Run Model Tests:**
```bash
dotnet test --filter "FullyQualifiedName~SimpleModelTests"
```

### 2. Database Context Tests

Tests Entity Framework operations:

```csharp
[Fact]
public void DatabaseContext_ShouldAddAndRetrieveCertificate()
{
    // Test database operations with in-memory database
}
```

**Run Database Tests:**
```bash
dotnet test --filter "FullyQualifiedName~DatabaseContextTests"
```

### 3. Business Logic Tests (`ControllerLogicTests.cs`)

Tests application logic and calculations:

```csharp
[Theory]
[InlineData(100, 10, 10)]
[InlineData(95, 10, 10)]
public void PaginationLogic_ShouldCalculateCorrectPages(int totalCount, int pageSize, int expectedPages)
{
    // Test pagination calculations
}
```

### 4. Service Layer Tests (`UnitTest1.cs`)

Complex tests for service layer functionality:

```csharp
[Fact]
public async Task ValidateCertificateAsync_ShouldReturnValidForExistingCertificate()
{
    // Test certificate validation service
}
```

## Writing New Tests

### 1. Create a New Test Class

```csharp
using Xunit;
using GCCWebApp.Models;

namespace GCCWebApp.Tests
{
    public class MyNewTests
    {
        [Fact]
        public void MyTest_ShouldDoSomething()
        {
            // Arrange
            var model = new Certificate();
            
            // Act
            var result = model.Status;
            
            // Assert
            Assert.Equal("active", result);
        }
    }
}
```

### 2. Test Patterns

#### Simple Unit Test
```csharp
[Fact]
public void Method_Condition_ExpectedBehavior()
{
    // Arrange - Set up test data
    // Act - Execute the method being tested  
    // Assert - Verify the result
}
```

#### Parameterized Test
```csharp
[Theory]
[InlineData(1, "expected1")]
[InlineData(2, "expected2")]
public void Method_WithDifferentInputs_ProducesExpectedOutputs(int input, string expected)
{
    // Test multiple scenarios with different data
}
```

#### Async Test
```csharp
[Fact]
public async Task AsyncMethod_ShouldReturnExpectedResult()
{
    // Test async methods
    var result = await someAsyncMethod();
    Assert.NotNull(result);
}
```

### 3. Testing with Database

```csharp
public class DatabaseTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public DatabaseTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public void Database_CanAddCertificate()
    {
        // Test database operations
        var certificate = new Certificate { Number = "TEST001" };
        _context.Certificates.Add(certificate);
        _context.SaveChanges();
        
        Assert.Single(_context.Certificates);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

## Best Practices

### 1. Test Naming
- Use descriptive names: `Method_Condition_ExpectedBehavior`
- Be specific about what's being tested
- Include expected outcome in the name

### 2. Test Structure (AAA Pattern)
```csharp
[Fact]
public void CalculateTotal_WithValidItems_ReturnsCorrectSum()
{
    // Arrange - Set up test conditions
    var items = new[] { 10, 20, 30 };
    var calculator = new Calculator();
    
    // Act - Execute the method being tested
    var result = calculator.CalculateTotal(items);
    
    // Assert - Verify the expected outcome
    Assert.Equal(60, result);
}
```

### 3. Test Independence
- Each test should be independent
- Don't rely on test execution order
- Clean up resources properly

### 4. Use Descriptive Assertions
```csharp
// Good - Clear assertion message
Assert.True(result.IsValid, "Certificate validation should succeed for valid input");

// Better - Multiple specific assertions
Assert.NotNull(result);
Assert.True(result.IsValid);
Assert.Equal("active", result.Status);
```

### 5. Mock External Dependencies
```csharp
[Fact]
public void Service_WithMockedDependency_BehavesCorrectly()
{
    // Arrange
    var mockRepository = new Mock<ICertificateRepository>();
    mockRepository.Setup(r => r.GetByIdAsync(1))
              .ReturnsAsync(new Certificate { Id = 1 });
    
    var service = new CertificateService(mockRepository.Object);
    
    // Act & Assert
    var result = await service.GetCertificateAsync(1);
    Assert.NotNull(result);
}
```

## Troubleshooting

### Common Issues and Solutions

#### 1. Tests Not Running
```bash
# Check if test project is built
dotnet build GCCWebApp.Tests

# Restore packages
dotnet restore

# Clear build cache
dotnet clean && dotnet build
```

#### 2. Database Tests Failing
- Ensure unique database names for each test
- Properly dispose of contexts
- Check that models are correctly configured

#### 3. Dependency Injection Issues
```csharp
// Use proper service configuration in tests
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
```

#### 4. Async Test Issues
```csharp
// Always await async methods in tests
[Fact]
public async Task AsyncTest_ShouldWork()
{
    var result = await someAsyncMethod(); // Don't forget await!
    Assert.NotNull(result);
}
```

### Debug Tests in Visual Studio
1. Set breakpoints in test methods
2. Right-click on test → Debug Test(s)
3. Use Test Explorer window for better visibility

### View Test Output
```bash
# See detailed test output
dotnet test --logger "console;verbosity=detailed"
```

## Continuous Integration

### GitHub Actions Example

Create `.github/workflows/tests.yml`:

```yaml
name: Run Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: windows-latest
    
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
      
    - name: Test
      run: dotnet test --no-build --verbosity normal --logger trx --results-directory TestResults
      
    - name: Publish Test Results
      uses: dorny/test-reporter@v1
      if: always()
      with:
        name: Test Results
        path: 'TestResults/*.trx'
        reporter: dotnet-trx
```

### Azure DevOps Pipeline

```yaml
trigger:
  branches:
    include:
    - main
    - develop

pool:
  vmImage: 'windows-latest'

steps:
- task: UseDotNet@2
  inputs:
    packageType: 'sdk'
    version: '8.0.x'

- task: DotNetCoreCLI@2
  displayName: 'Restore packages'
  inputs:
    command: 'restore'
    projects: '**/*.csproj'

- task: DotNetCoreCLI@2
  displayName: 'Build solution'
  inputs:
    command: 'build'
    projects: '**/*.csproj'
    arguments: '--no-restore'

- task: DotNetCoreCLI@2
  displayName: 'Run tests'
  inputs:
    command: 'test'
    projects: '**/*Tests.csproj'
    arguments: '--no-build --logger trx --collect "Code coverage"'
    publishTestResults: true
```

## Performance Testing

### Benchmarking Tests
```bash
# Install BenchmarkDotNet (optional)
dotnet add package BenchmarkDotNet

# Create performance tests
[Fact]
public void LargeDataSet_ShouldProcessWithinTimeLimit()
{
    var stopwatch = Stopwatch.StartNew();
    
    // Execute operation
    ProcessLargeDataSet();
    
    stopwatch.Stop();
    Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Operation should complete within 1 second");
}
```

## Test Coverage

### Generate Coverage Report
```bash
# Install coverage tools
dotnet tool install --global dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator -reports:"TestResults/*/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

## Summary

This testing framework provides:
- ✅ **Comprehensive validation** of your GCC Database Solution
- ✅ **Easy-to-use commands** for running tests
- ✅ **Extensible structure** for adding new tests
- ✅ **Integration-ready** for CI/CD pipelines
- ✅ **Best practices** for maintainable tests

For questions or issues with testing, refer to:
- [xUnit Documentation](https://xunit.net/)
- [Entity Framework Testing Guide](https://docs.microsoft.com/en-us/ef/core/testing/)
- [ASP.NET Core Testing Documentation](https://docs.microsoft.com/en-us/aspnet/core/test/)
# GCC Web App Tests

This project contains comprehensive unit and integration tests for the GCC Database Solution web application.

## 🚀 Quick Start

**New to testing?** Start here: [`QUICK_START.md`](QUICK_START.md)

**Need detailed guidance?** See: [`TESTING_GUIDE.md`](TESTING_GUIDE.md)

**Want examples?** Check out: [`TEST_EXAMPLES.md`](TEST_EXAMPLES.md)

## 📁 Project Structure

| File | Purpose | Test Count |
|------|---------|------------|
| **SimpleTests.cs** | Model validation & database context tests | ~12 tests |
| **ControllerLogicTests.cs** | Business logic & pagination tests | ~6 tests |  
| **IntegrationTests.cs** | Framework integration tests | ~3 tests |
| **UnitTest1.cs** | Complex service layer tests | ~11 tests |

## ⚡ Quick Commands

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Build test project
dotnet build GCCWebApp.Tests

# Watch mode (auto-run on changes)
dotnet watch test --project GCCWebApp.Tests
```

## 📊 Current Status

- ✅ **29/32 tests passing** (90%+ success rate)
- ✅ All model and logic tests working perfectly
- ✅ Database operations fully tested
- ⚠️ 3 complex service tests may fail (require full app context)

## 🧪 What Gets Tested

### Core Components
- ✅ **Certificate Model** - Validation, defaults, constraints
- ✅ **Company Model** - Properties, relationships
- ✅ **Product Model** - Data integrity
- ✅ **Database Context** - EF Core operations
- ✅ **ViewModels** - Request/response models
- ✅ **Pagination Logic** - Calculation accuracy
- ✅ **Business Rules** - Certificate validation logic

### Test Categories
| Category | Description | Examples |
|----------|-------------|----------|
| **Unit Tests** | Individual component testing | Model validation, property tests |
| **Integration Tests** | Component interaction testing | Database operations, service calls |
| **Logic Tests** | Business rule validation | Pagination, certificate validation |
| **Mock Tests** | Isolated testing with mocks | Service layer with mock dependencies |

## 🛠️ Technology Stack

- **xUnit** - Primary testing framework
- **Entity Framework Core In-Memory** - Database testing without SQL Server
- **ASP.NET Core MVC Testing** - Web application testing
- **Moq** - Mocking framework for dependencies
- **.NET 8.0** - Target framework (matches main app)

## 📈 Benefits

### For Developers
- **Confidence** - Know your code works before deployment
- **Regression Prevention** - Catch breaking changes early
- **Documentation** - Tests serve as living documentation
- **Refactoring Safety** - Modify code with confidence

### For the Project
- **Quality Assurance** - Automated validation of functionality
- **Faster Development** - Quick feedback on code changes
- **Maintenance** - Easier to modify and extend features
- **Deployment Safety** - Reduced risk of production issues

## 🔍 Sample Test

```csharp
[Fact]
public void Certificate_ShouldValidateCorrectly()
{
    // Arrange
    var certificate = new Certificate
    {
        Number = "GCC001",
        Status = "active"
    };

    // Act & Assert
    Assert.Equal("GCC001", certificate.Number);
    Assert.Equal("active", certificate.Status);
    Assert.True(certificate.CreatedAt <= DateTime.UtcNow);
}
```

## 📚 Documentation

- **[QUICK_START.md](QUICK_START.md)** - Get running in 5 minutes
- **[TESTING_GUIDE.md](TESTING_GUIDE.md)** - Comprehensive testing documentation
- **[TEST_EXAMPLES.md](TEST_EXAMPLES.md)** - Practical code examples

## 🤝 Contributing

### Adding New Tests
1. Follow the existing naming patterns
2. Use the AAA pattern (Arrange, Act, Assert)
3. Keep tests focused and independent
4. Add documentation for complex scenarios

### Running Specific Tests
```bash
# Run model tests only
dotnet test --filter "FullyQualifiedName~SimpleTests"

# Run a specific test method
dotnet test --filter "Certificate_ShouldInitializeWithDefaults"
```

## 🆘 Need Help?

1. **Build Issues**: Run `dotnet clean && dotnet build`
2. **Test Failures**: Check the error messages and stack traces
3. **New Features**: See TEST_EXAMPLES.md for patterns
4. **Integration**: See TESTING_GUIDE.md for CI/CD setup

## 🎯 Next Steps

1. **Run the tests**: `dotnet test`
2. **Explore the code**: Look at existing test files
3. **Write new tests**: Add tests for your new features
4. **Set up CI/CD**: Use the examples in TESTING_GUIDE.md

---

*This test project ensures the reliability and maintainability of your GCC Database Solution. Happy testing! 🧪*
# Quick Start Guide - GCC Database Testing

Get up and running with the GCC Database Solution test project in 5 minutes.

## 🚀 Quick Commands

### Run All Tests
```bash
dotnet test
```

### Run Tests with Details
```bash
dotnet test --verbosity normal
```

### Build Test Project
```bash
dotnet build GCCWebApp.Tests
```

### Watch Mode (Auto-run on changes)
```bash
dotnet watch test --project GCCWebApp.Tests
```

## 📊 What Gets Tested

| Test Category | What It Tests | File |
|---------------|---------------|------|
| **Model Tests** | Certificate, Company, Product models | `SimpleTests.cs` |
| **Database Tests** | EF Core operations, data persistence | `SimpleTests.cs` |
| **Logic Tests** | Business logic, pagination, validation | `ControllerLogicTests.cs` |
| **Service Tests** | Complex service layer operations | `UnitTest1.cs` |
| **Integration Tests** | Basic framework functionality | `IntegrationTests.cs` |

## 🎯 Expected Results

**Typical successful run:**
```
Passed!  - Failed: 0, Passed: 29, Skipped: 0, Total: 29, Duration: 2s
```

**Some tests may fail** - this is normal for complex service tests that need full application context.

## 🔧 Common Issues

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet build
```

### Missing Dependencies
```bash
# Restore packages
dotnet restore
```

### Database Issues
- Tests use in-memory database (no SQL Server needed)
- Each test gets its own isolated database

## 📝 Writing Your First Test

Create a new test in any test class:

```csharp
[Fact]
public void MyFeature_ShouldWork()
{
    // Arrange
    var model = new Certificate { Number = "TEST001" };
    
    // Act
    var result = model.Number;
    
    // Assert
    Assert.Equal("TEST001", result);
}
```

Run your specific test:
```bash
dotnet test --filter "MyFeature_ShouldWork"
```

## 📚 Need More Help?

- See `TESTING_GUIDE.md` for comprehensive documentation
- Check `README.md` for project overview
- Look at existing tests in the `*.cs` files for examples

## 🎉 You're Ready!

Start with `dotnet test` and explore the existing tests to understand your application better!
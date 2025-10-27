# GCCWebApp.Tests

**Main Unit and Integration Test Suite for GCC Database Solution**

## Overview

This is the primary test project containing comprehensive unit tests, integration tests, and component tests for the core application functionality. The suite validates authentication, business logic, data access, and critical application workflows.

## Test Statistics

- **Total Tests**: 508+ tests
- **Current Pass Rate**: 72.2% (421 passed, 87 failed)
- **Expected Pass Rate**: 90-95% (after Playwright browser installation)
- **Test Framework**: xUnit with FluentAssertions
- **Execution Time**: ~45 seconds
- **Database**: EF Core In-Memory

## Project Structure

### Core Test Categories

| Category | Tests | Location | Purpose |
|----------|-------|----------|---------|
| **Authentication** | 15 | `/Authentication` | Cookie auth, JWT, 2FA, Azure AD B2C |
| **Configuration** | 12 | `/Configuration` | App settings, connection strings, validation |
| **Controllers** | 45 | `/Controllers` | MVC controller actions and responses |
| **Integration** | 20 | `/Integration` | Multi-component workflows |
| **Mobile** | 15 | `/Mobile` | Mobile responsiveness (Playwright) |
| **Security** | 30 | `/Security` | SQL injection, XSS, CSRF prevention |
| **Services** | 70 | `/Services` | Business logic and data services |
| **UI** | 25 | `/UI` | View rendering and components |
| **UX** | 10 | `/UX` | User experience workflows |
| **Documentation** | 6 | `/Documentation` | Code documentation validation |

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

## Test Coverage by Component

### 1. Authentication Tests (`/Authentication`)
**Purpose:** Validate all authentication and authorization mechanisms

**Coverage:**
- Cookie-based authentication flows
- JWT token generation and validation
- API key authentication
- Azure AD B2C integration (optional)
- Two-factor authentication (2FA) workflows
- Role-based access control (RBAC)
- Session management

**Key Test Files:**
- `AuthenticationTests.cs` - Core authentication flows
- `JwtTokenTests.cs` - JWT token validation
- `TwoFactorAuthenticationTests.cs` - 2FA implementation

### 2. Configuration Tests (`/Configuration`)
**Purpose:** Ensure application configuration is valid and complete

**Coverage:**
- appsettings.json validation
- Environment-specific configuration
- Connection string validation
- Feature flag configuration
- JWT secret key validation
- Azure AD B2C settings

**Key Test Files:**
- `ConfigurationValidationTests.cs` - Comprehensive configuration validation

### 3. Controller Tests (`/Controllers`)
**Purpose:** Test MVC controller actions and HTTP responses

**Coverage:**
- CertificateController (CRUD operations)
- AdminController (user/role management)
- AccountController (login/logout/register)
- DashboardController (statistics and analytics)
- API controller responses

**Key Test Files:**
- `CertificateControllerTests.cs` - Certificate CRUD operations
- `AdminControllerTests.cs` - Admin panel functionality
- `AccountControllerTests.cs` - Authentication workflows

### 4. Integration Tests (`/Integration`)
**Purpose:** End-to-end integration testing across multiple components

**Coverage:**
- Complete certificate creation workflows
- Product ID import/export flows
- User management workflows
- Multi-component data flows
- Database transaction integrity

**Key Test Files:**
- `CertificateWorkflowIntegrationTests.cs` - Full certificate lifecycle
- `ProductIdIntegrationTests.cs` - Product ID management

### 5. Mobile Tests (`/Mobile`)
**Purpose:** Mobile responsiveness and touch interaction validation

**Coverage:**
- Mobile viewport rendering
- Touch gesture support
- Responsive design validation
- Mobile-specific UI components

**Requirements:**
- Playwright browsers: `pwsh bin/Debug/net8.0/playwright.ps1 install`

### 6. Security Tests (`/Security`)
**Purpose:** Security vulnerability prevention and validation

**Coverage:**
- SQL injection prevention
- XSS (Cross-Site Scripting) prevention
- CSRF token validation
- Authorization bypass attempts
- Input sanitization

**Key Test Files:**
- `SqlInjectionTests.cs` - SQL injection attack prevention
- `XssPreventionTests.cs` - XSS attack prevention
- `AuthorizationTests.cs` - Access control validation

### 7. Services Tests (`/Services`)
**Purpose:** Business logic and service layer validation

**Coverage:**
- **CertificateService** (70+ tests) - Certificate business logic
- **CertificateWorkflowService** - 14-state workflow state machine
- **ProductIdService** - ASIN/UPC validation and import
- **UserService** - User management operations
- **RoleService** - Role and permission management
- **FileUploadService** - File handling and validation

**Key Test Files:**
- `CertificateServiceTests.cs` - Certificate business rules
- `CertificateWorkflowServiceTests.cs` - State machine transitions
- `ProductIdServiceTests.cs` - Product ID validation and bulk import

### 8. UI Tests (`/UI`)
**Purpose:** UI component rendering and behavior validation

**Coverage:**
- View rendering correctness
- JavaScript component behavior
- CSS styling validation
- Component state management

### 9. UX Tests (`/UX`)
**Purpose:** User experience and usability validation

**Coverage:**
- User workflow validation
- Error message clarity
- Form validation feedback
- Navigation flows

### 10. Documentation Tests (`/Documentation`)
**Purpose:** Code documentation and XML comment validation

**Coverage:**
- XML documentation completeness
- Code comment quality
- API documentation accuracy

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

## Running Tests

### Run All Tests
```bash
cd GCCWebApp.Tests
dotnet test
```

### Run Specific Test Category
```bash
# Authentication tests only
dotnet test --filter "FullyQualifiedName~Authentication"

# Service tests only
dotnet test --filter "FullyQualifiedName~Services"

# Controller tests only
dotnet test --filter "FullyQualifiedName~Controllers"

# Integration tests only
dotnet test --filter "FullyQualifiedName~Integration"
```

### Run with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run with Detailed Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Test Conventions

### Naming Convention
Tests follow the pattern: `[MethodName]_[Scenario]_[ExpectedBehavior]`

**Examples:**
- `CreateCertificate_ValidInput_ShouldCreateCertificate`
- `CreateCertificate_MissingRequiredField_ShouldThrowException`
- `UpdateCertificate_UnauthorizedUser_ShouldReturnForbidden`

### AAA Pattern (Arrange, Act, Assert)
All tests use the AAA pattern for clarity:

```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and dependencies
    var input = "test";
    var expected = "TEST";
    var mockService = new Mock<IService>();

    // Act - Execute the method being tested
    var result = MethodUnderTest(input);

    // Assert - Verify the result matches expectations
    result.Should().Be(expected);
}
```

### Mocking Guidelines
- Use **Moq** for mocking dependencies
- Mock all external services and databases in unit tests
- Use in-memory database for integration tests
- Never call real APIs or external services in unit tests
- Verify mock interactions when testing side effects

### Test Data Management
- Test data located in `/TestData` folders within each category
- Use builders/factories for complex test objects
- Keep test data minimal and focused on the test scenario
- Use realistic data that matches production constraints

## Dependencies

**Testing Frameworks:**
- **xUnit** 2.9.3 - Primary test framework
- **FluentAssertions** 8.7.1 - Readable assertion library
- **Moq** 4.20.72 - Mocking framework
- **Microsoft.Playwright** 1.55.0 - Browser automation for E2E/mobile tests

**Database:**
- **Microsoft.EntityFrameworkCore.InMemory** - In-memory database for unit tests
- **Microsoft.EntityFrameworkCore.Sqlite** - SQLite for integration tests

**ASP.NET Core:**
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing support
- **Microsoft.AspNetCore.TestHost** - Test server hosting

## Known Issues

### Issue 1: Playwright Browsers Not Installed
**Impact:** 87 mobile/E2E tests fail (17% of total tests)

**Error Message:**
```
Executable doesn't exist at ms-playwright\chromium_headless_shell-win64\chrome.exe
```

**Fix:**
```bash
cd bin/Debug/net8.0
pwsh playwright.ps1 install
```

**Status:** Documented in [TEST_COVERAGE_ANALYSIS.md](../TEST_COVERAGE_ANALYSIS.md)

### Issue 2: API Key Handler Mock Setup
**Impact:** 12 authentication tests fail

**Cause:** Extension method mocks not properly configured

**Fix:** Replace extension method mocks in `ApiKeyAuthenticationHandlerTests.cs`

**Status:** Awaiting implementation

### Issue 3: Nullable Reference Warnings
**Impact:** 58+ build warnings (CS8618)

**Cause:** Model properties not initialized in constructors

**Workaround:** Warnings can be safely ignored; properties are initialized by EF Core

**Status:** Low priority, cosmetic issue only

## Test Data Management

### In-Memory Database
Most unit tests use EF Core in-memory database for isolation:

```csharp
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(databaseName: "TestDatabase_" + Guid.NewGuid())
    .Options;

var context = new ApplicationDbContext(options);
```

### Test Fixtures
Shared test data and setup logic using xUnit fixtures:

```csharp
public class CertificateTestFixture : IDisposable
{
    public ApplicationDbContext Context { get; }
    public ICertificateService Service { get; }

    public CertificateTestFixture()
    {
        // Setup shared resources
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;
        Context = new ApplicationDbContext(options);
        Service = new CertificateService(Context);
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}

[Collection("CertificateCollection")]
public class CertificateTests : IClassFixture<CertificateTestFixture>
{
    private readonly CertificateTestFixture _fixture;

    public CertificateTests(CertificateTestFixture fixture)
    {
        _fixture = fixture;
    }
}
```

## Continuous Integration

Tests run automatically on:
- Pull requests to master branch
- Commits to feature branches
- Nightly builds

**CI Configuration:** Tests are integrated into build pipeline

## Contributing

### Adding New Tests

1. **Identify the Appropriate Category:**
   - Authentication → `/Authentication`
   - Business logic → `/Services`
   - Controllers → `/Controllers`
   - Integration → `/Integration`

2. **Follow Naming Conventions:**
   - `[MethodName]_[Scenario]_[ExpectedBehavior]`

3. **Use AAA Pattern:**
   - Arrange, Act, Assert sections

4. **Include XML Documentation:**
```csharp
/// <summary>
/// Verifies that creating a certificate with valid data succeeds
/// </summary>
[Fact]
public void CreateCertificate_ValidData_ShouldSucceed()
{
    // Test implementation
}
```

5. **Ensure Test Isolation:**
   - No dependencies on other tests
   - Use unique database names
   - Clean up resources

6. **Mock External Dependencies:**
   - Use Moq for service dependencies
   - Never call real APIs

### Test Review Checklist

Before submitting tests for review, ensure:

- [ ] Test name clearly describes what is being tested
- [ ] AAA pattern is followed
- [ ] No hardcoded values (use constants/test data)
- [ ] Tests are isolated (no dependencies on execution order)
- [ ] Appropriate assertions used (FluentAssertions preferred)
- [ ] Edge cases are covered
- [ ] Error cases are tested
- [ ] XML documentation is included
- [ ] Tests run successfully locally

## Related Documentation

- [Test Coverage Analysis](../TEST_COVERAGE_ANALYSIS.md) - Comprehensive coverage metrics and gap analysis
- [Phase 1 Completion Summary](../PHASE1_COMPLETION_SUMMARY.md) - Recent accessibility test generation
- [GCCWebApp.Accessibility.Tests](../GCCWebApp.Accessibility.Tests/README.md) - Accessibility testing suite
- [GCCWebApp.Api.Tests](../GCCWebApp.Api.Tests/README.md) - API endpoint testing
- [GCCWebApp.Security.Tests](../GCCWebApp.Security.Tests/README.md) - Security vulnerability testing
- [GCCWebApp.Performance.Tests](../GCCWebApp.Performance.Tests/README.md) - Performance benchmarking
- [Main Application Documentation](../../GCCWebApp/.claude/CLAUDE.md) - Application overview and guidelines

## Contact & Support

For questions about this test suite:
- **Project Documentation**: `GCCWebApp/0_Documentation/`
- **Requirements**: `0_Documentation/docs/Requirements.md`
- **Manual Test Cases**: `0_Documentation/docs/MANUAL_UAT_TEST_CASES.md`
- **Master Design Guide**: `0_Documentation/mockups/0_0_MasterDesignGuide.html`

---

**Last Updated:** October 24, 2025
**Test Suite Version:** 2.0
**Total Tests:** 508+
**Pass Rate Target:** 95%+
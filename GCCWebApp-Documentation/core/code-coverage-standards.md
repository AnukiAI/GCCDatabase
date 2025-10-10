# Code Coverage Standards - GCC Database Solution

## Overview
This document establishes code coverage standards and practices for the GCC Database Solution project. Code coverage is a quality metric that helps ensure our test suite adequately exercises the codebase, providing confidence in our software quality and reducing the risk of undetected bugs.

## Coverage Targets and Standards

### Overall Coverage Targets

#### Minimum Coverage Requirements
- **Overall Project Coverage**: 80% minimum
- **New Code Coverage**: 90% minimum
- **Critical Path Coverage**: 95% minimum
- **Public API Coverage**: 100% required

#### Component-Specific Targets
```
Component                    | Target | Minimum | Notes
----------------------------|--------|---------|-------------------
Controllers                 | 90%    | 85%     | Focus on action methods
Services                    | 95%    | 90%     | Core business logic
Repositories               | 85%    | 80%     | Data access layer
Models/DTOs                | 70%    | 60%     | Properties and validation
Utilities/Helpers          | 90%    | 85%     | Reusable components
Middleware                 | 85%    | 80%     | Request pipeline
Authentication/Security    | 100%   | 95%     | Critical security code
API Controllers            | 95%    | 90%     | External interfaces
```

### Quality Gates

#### Pull Request Requirements
- **Branch Coverage**: New code must have ≥90% coverage
- **Line Coverage**: New code must have ≥85% coverage
- **No Coverage Regression**: Overall coverage cannot decrease
- **Critical Path Coverage**: 100% for authentication and authorization code

#### Release Requirements
- **Overall Coverage**: ≥80% for release approval
- **Security Code Coverage**: 100% required
- **Integration Test Coverage**: ≥70% of critical user journeys
- **API Coverage**: 100% of public endpoints

## Coverage Measurement and Reporting

### Tools and Configuration

#### Primary Coverage Tool: Coverlet
```xml
<!-- In test projects -->
<PackageReference Include="coverlet.collector" Version="6.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>

<PackageReference Include="coverlet.msbuild" Version="6.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
```

#### Coverage Collection Configuration
```xml
<!-- In Directory.Build.props -->
<PropertyGroup>
  <CollectCoverage>true</CollectCoverage>
  <CoverletOutputFormat>opencover,cobertura,json</CoverletOutputFormat>
  <CoverletOutput>./coverage/</CoverletOutput>
  <ExcludeByFile>**/Migrations/**/*.cs</ExcludeByFile>
  <ExcludeByAttribute>Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute</ExcludeByAttribute>
</PropertyGroup>
```

### Running Coverage Locally

#### Command Line Coverage Collection
```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults \
  --logger trx

# Run specific test project with coverage
dotnet test GCCWebApp.Tests \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults/Unit

# Generate detailed coverage report
dotnet test /p:CollectCoverage=true \
  /p:CoverletOutputFormat=opencover \
  /p:CoverletOutput=./coverage/coverage.opencover.xml
```

#### Coverage Report Generation
```bash
# Install ReportGenerator tool
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML coverage report
reportgenerator \
  -reports:"TestResults/**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:"Html;Badges;TextSummary"

# Generate multiple format reports
reportgenerator \
  -reports:"coverage/coverage.opencover.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:"Html;HtmlSummary;JsonSummary;Badges;Cobertura"
```

### Coverage in CI/CD Pipeline

#### GitHub Actions Coverage Configuration
```yaml
# In .github/workflows/ci-cd.yml
- name: Run tests with coverage
  run: |
    dotnet test --configuration Release \
      --collect:"XPlat Code Coverage" \
      --results-directory ./coverage \
      --logger trx

- name: Generate coverage report
  uses: danielpalme/ReportGenerator-GitHub-Action@5.1.20
  with:
    reports: 'coverage/**/coverage.cobertura.xml'
    targetdir: 'coverage-report'
    reporttypes: 'Html;JsonSummary;Badges'

- name: Upload coverage to Codecov
  uses: codecov/codecov-action@v3
  with:
    file: ./coverage/coverage.cobertura.xml
    flags: unittests
    name: codecov-umbrella

- name: Comment PR with coverage
  uses: marocchino/sticky-pull-request-comment@v2
  with:
    recreate: true
    path: coverage-report/index.html
```

#### SonarCloud Integration
```yaml
- name: SonarCloud analysis with coverage
  env:
    GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
    SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
  run: |
    dotnet sonarscanner begin \
      /k:"gccwebapp" \
      /o:"pointsolve" \
      /d:sonar.login="${{ secrets.SONAR_TOKEN }}" \
      /d:sonar.host.url="https://sonarcloud.io" \
      /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"

    dotnet build --configuration Release

    dotnet sonarscanner end /d:sonar.login="${{ secrets.SONAR_TOKEN }}"
```

## Coverage Analysis Guidelines

### What to Measure

#### Include in Coverage
- **Business Logic**: Core application functionality
- **Controllers**: Action methods and their logic
- **Services**: Business rule implementations
- **Repositories**: Data access patterns
- **Validators**: Input validation logic
- **Security Components**: Authentication and authorization
- **API Endpoints**: All public interfaces
- **Error Handling**: Exception scenarios

#### Exclude from Coverage
```xml
<!-- Coverage exclusions -->
<PropertyGroup>
  <ExcludeByFile>
    **/Migrations/**/*.cs;
    **/Program.cs;
    **/Startup.cs;
    **/GlobalUsings.cs;
    **/*Designer.cs;
    **/AssemblyInfo.cs
  </ExcludeByFile>

  <ExcludeByAttribute>
    Obsolete;
    GeneratedCodeAttribute;
    CompilerGeneratedAttribute;
    ExcludeFromCodeCoverageAttribute;
    DebuggerNonUserCodeAttribute
  </ExcludeByAttribute>
</PropertyGroup>
```

### Coverage Quality Assessment

#### High-Quality Coverage Indicators
1. **Branch Coverage**: Tests exercise all decision points
2. **Path Coverage**: Critical user journeys are tested
3. **Edge Case Coverage**: Boundary conditions and error scenarios
4. **Integration Coverage**: End-to-end workflows tested

#### Coverage Quality Metrics
```bash
# Quality assessment criteria
Line Coverage >= 85%           # Code execution
Branch Coverage >= 80%         # Decision points
Method Coverage >= 90%         # Function coverage
Class Coverage >= 85%          # Type coverage
```

### Coverage Analysis by Component

#### Controller Coverage Standards
```csharp
// Example: High-quality controller test
[Fact]
public async Task CreateCertificate_ValidInput_ReturnsSuccessResult()
{
    // Arrange
    var request = new CreateCertificateRequest
    {
        Name = "Test Certificate",
        CompanyId = 1
    };

    _mockService.Setup(s => s.CreateCertificateAsync(It.IsAny<CreateCertificateRequest>()))
               .ReturnsAsync(ServiceResult<Certificate>.Success(new Certificate()));

    // Act
    var result = await _controller.Create(request);

    // Assert
    var redirectResult = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal(nameof(CertificateController.Details), redirectResult.ActionName);

    _mockService.Verify(s => s.CreateCertificateAsync(request), Times.Once);
}

// Cover all action methods, HTTP verbs, and authorization scenarios
[Fact] public async Task CreateCertificate_InvalidModel_ReturnsViewWithErrors() { }
[Fact] public async Task CreateCertificate_ServiceFailure_ReturnsViewWithError() { }
[Fact] public async Task CreateCertificate_UnauthorizedUser_ReturnsForbidden() { }
```

#### Service Layer Coverage Standards
```csharp
// Example: Comprehensive service testing
public class CertificateServiceTests
{
    // Happy path scenarios
    [Fact] public async Task CreateCertificateAsync_ValidInput_CreatesSuccessfully() { }

    // Edge cases
    [Fact] public async Task CreateCertificateAsync_DuplicateName_ReturnsFailure() { }
    [Fact] public async Task CreateCertificateAsync_InvalidCompany_ReturnsFailure() { }

    // Error scenarios
    [Fact] public async Task CreateCertificateAsync_DatabaseError_ThrowsException() { }
    [Fact] public async Task CreateCertificateAsync_NullInput_ThrowsArgumentNullException() { }

    // Performance scenarios
    [Fact] public async Task GetCertificatesAsync_LargeDataset_PerformsWithinTimeout() { }
}
```

#### Repository Coverage Standards
```csharp
// Focus on data access patterns and LINQ queries
public class CertificateRepositoryTests
{
    [Fact] public async Task GetByIdAsync_ExistingId_ReturnsCertificate() { }
    [Fact] public async Task GetByIdAsync_NonExistingId_ReturnsNull() { }
    [Fact] public async Task SearchAsync_WithFilters_ReturnsFilteredResults() { }
    [Fact] public async Task SearchAsync_EmptyDatabase_ReturnsEmptyList() { }
    [Fact] public async Task AddAsync_ValidEntity_SavesSuccessfully() { }
    [Fact] public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully() { }
    [Fact] public async Task DeleteAsync_ExistingEntity_RemovesSuccessfully() { }
}
```

## Coverage Monitoring and Reporting

### Automated Coverage Reporting

#### Daily Coverage Reports
```yaml
# Scheduled workflow for coverage monitoring
name: Daily Coverage Report
on:
  schedule:
    - cron: '0 6 * * *'  # 6 AM UTC daily

jobs:
  coverage-report:
    runs-on: ubuntu-latest
    steps:
      - name: Generate coverage report
        run: |
          dotnet test --collect:"XPlat Code Coverage"
          reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"daily-coverage"

      - name: Upload to storage
        uses: azure/CLI@v1
        with:
          azcliversion: 2.30.0
          inlineScript: |
            az storage blob upload-batch \
              --destination coverage-reports \
              --source daily-coverage \
              --account-name gccstorageaccount
```

#### Coverage Trend Analysis
```bash
# Track coverage trends over time
# Store coverage metrics in database/monitoring system
# Generate trend reports and alerts

# Example metrics collection
curl -X POST "https://monitoring.gcc.com/api/metrics" \
  -H "Content-Type: application/json" \
  -d '{
    "timestamp": "2025-09-17T10:00:00Z",
    "branch": "develop",
    "commit": "abc123",
    "coverage": {
      "line": 84.5,
      "branch": 78.2,
      "method": 89.1,
      "class": 82.7
    }
  }'
```

### Coverage Dashboards

#### Key Metrics Dashboard
```markdown
## GCC Coverage Dashboard

### Current Status (September 17, 2025)
- **Overall Coverage**: 84.2% ✅ (Target: 80%)
- **New Code Coverage**: 91.5% ✅ (Target: 90%)
- **Critical Path Coverage**: 96.8% ✅ (Target: 95%)
- **API Coverage**: 100% ✅ (Target: 100%)

### Component Breakdown
| Component           | Coverage | Status | Trend |
|--------------------|----------|--------|-------|
| Controllers        | 88.3%    | ✅     | ↗️    |
| Services           | 92.1%    | ✅     | ↗️    |
| Repositories       | 83.7%    | ✅     | →     |
| Security           | 100%     | ✅     | →     |
| API Controllers    | 94.2%    | ✅     | ↗️    |

### Areas Needing Attention
- Model validation tests (67.3% - below 70% target)
- Legacy utility classes (72.1% - below 85% target)
```

## Coverage Improvement Strategies

### Systematic Coverage Improvement

#### Identify Coverage Gaps
```bash
# Generate detailed coverage report
reportgenerator \
  -reports:"coverage.xml" \
  -targetdir:"detailed-coverage" \
  -reporttypes:"Html;JsonSummary"

# Analyze uncovered lines
grep -n "not covered" detailed-coverage/index.html

# Focus on high-impact, low-coverage areas
# Priority: Critical business logic with <80% coverage
```

#### Targeted Test Development
```csharp
// Strategy: Start with critical paths
[Theory]
[InlineData("admin@gcc.com", "Admin123!", true)]
[InlineData("analyst@gcc.com", "Analyst123!", true)]
[InlineData("invalid@gcc.com", "wrong", false)]
public async Task Login_VariousCredentials_ReturnsExpectedResult(
    string email, string password, bool expectedSuccess)
{
    // Cover authentication scenarios systematically
}

// Strategy: Edge cases and error paths
[Fact]
public async Task CreateCertificate_DatabaseConnectionFailure_HandlesGracefully()
{
    // Simulate infrastructure failures
    _mockRepository.Setup(r => r.AddAsync(It.IsAny<Certificate>()))
                   .ThrowsAsync(new SqlException("Connection timeout"));

    var result = await _service.CreateCertificateAsync(request);

    Assert.False(result.IsSuccess);
    Assert.Contains("database", result.ErrorMessage.ToLower());
}
```

### Test Types for Coverage

#### Unit Tests (Primary Coverage Driver)
```csharp
// Focus: Isolated component testing
// Coverage target: 90%+ for business logic
[Fact]
public async Task CalculateCertificateScore_ValidInputs_ReturnsCorrectScore()
{
    // Test business rules in isolation
}
```

#### Integration Tests (End-to-End Coverage)
```csharp
// Focus: Component interaction testing
// Coverage target: 70%+ for critical paths
[Fact]
public async Task CreateCertificate_FullWorkflow_CompletesSuccessfully()
{
    // Test complete user journeys
}
```

#### API Tests (Interface Coverage)
```csharp
// Focus: Public API contract testing
// Coverage target: 100% for all endpoints
[Fact]
public async Task GET_Certificates_ReturnsValidResponse()
{
    // Test all API endpoints and status codes
}
```

## Coverage Anti-Patterns and Best Practices

### Anti-Patterns to Avoid

#### Coverage Theater
```csharp
// BAD: Tests that don't verify behavior
[Fact]
public void SomeMethod_DoesNothing()
{
    var service = new MyService();
    service.SomeMethod(); // No assertions!
    // This increases coverage but provides no value
}

// GOOD: Tests that verify expected behavior
[Fact]
public void CalculateTotal_WithValidItems_ReturnsCorrectSum()
{
    var calculator = new PriceCalculator();
    var items = new[] { 10.50m, 25.00m, 5.25m };

    var result = calculator.CalculateTotal(items);

    Assert.Equal(40.75m, result);
}
```

#### Flaky Coverage Tests
```csharp
// BAD: Tests that depend on external factors
[Fact]
public async Task GetCurrentTime_ReturnsCurrentTime()
{
    var result = await _service.GetCurrentTimeAsync();
    Assert.Equal(DateTime.Now, result); // Flaky!
}

// GOOD: Tests with controlled dependencies
[Fact]
public async Task GetCurrentTime_ReturnsTimeFromProvider()
{
    var expectedTime = new DateTime(2025, 9, 17, 10, 0, 0);
    _mockTimeProvider.Setup(p => p.Now).Returns(expectedTime);

    var result = await _service.GetCurrentTimeAsync();

    Assert.Equal(expectedTime, result);
}
```

### Best Practices

#### Meaningful Test Scenarios
```csharp
// Test business scenarios, not just code paths
[Theory]
[InlineData(CertificateStatus.Draft, CertificateStatus.UnderReview, true)]
[InlineData(CertificateStatus.UnderReview, CertificateStatus.Approved, true)]
[InlineData(CertificateStatus.Approved, CertificateStatus.Draft, false)]
public void CanTransitionStatus_VariousTransitions_ReturnsExpectedResult(
    CertificateStatus from, CertificateStatus to, bool expectedCanTransition)
{
    var result = CertificateWorkflowService.CanTransition(from, to);
    Assert.Equal(expectedCanTransition, result);
}
```

#### Test Data Builders
```csharp
// Use builders for maintainable test data
public class CertificateBuilder
{
    private Certificate _certificate = new Certificate();

    public CertificateBuilder WithName(string name)
    {
        _certificate.Name = name;
        return this;
    }

    public CertificateBuilder WithStatus(CertificateStatus status)
    {
        _certificate.Status = status;
        return this;
    }

    public Certificate Build() => _certificate;
}

// Usage in tests
[Fact]
public async Task DeleteCertificate_DraftStatus_DeletesSuccessfully()
{
    var certificate = new CertificateBuilder()
        .WithName("Test Certificate")
        .WithStatus(CertificateStatus.Draft)
        .Build();

    var result = await _service.DeleteCertificateAsync(certificate.Id);

    Assert.True(result.IsSuccess);
}
```

## Coverage Exceptions and Waivers

### Justified Coverage Exceptions

#### Framework Integration Code
```csharp
// Generated code and framework integration
[ExcludeFromCodeCoverage]
public class Startup
{
    public void ConfigureServices(IServiceCollection services) { }
    public void Configure(IApplicationBuilder app) { }
}
```

#### Error Handling Edge Cases
```csharp
// Extremely rare error scenarios
[ExcludeFromCodeCoverage]
private void HandleOutOfMemoryException()
{
    // This scenario is difficult to test reliably
    // and represents a catastrophic system failure
}
```

### Coverage Waiver Process

#### Waiver Request Template
```markdown
## Coverage Waiver Request

**Component**: Services/LegacyIntegrationService.cs
**Lines**: 145-167
**Current Coverage**: 23%
**Requested Waiver**: Lines 145-167 (legacy COM interop)

**Justification**:
- Legacy COM component requires specific system configuration
- Testing requires expensive third-party license
- Component scheduled for replacement in Q2 2026

**Risk Assessment**:
- Low: Component has been stable for 3+ years
- Covered by integration tests in production environment

**Approval**: [PM Signature] [Tech Lead Signature]
```

## Coverage Metrics Integration

### IDE Integration

#### Visual Studio Coverage
```bash
# Enable coverage highlighting in Visual Studio
dotnet test --collect:"Code Coverage"

# View coverage in Test Explorer
# Red/green highlighting in editor
# Coverage summary in output window
```

#### VS Code Coverage
```json
// settings.json
{
  "coverage-gutters.coverageFileNames": [
    "lcov.info",
    "cov.xml",
    "coverage.xml",
    "jacoco.xml",
    "coverage.cobertura.xml"
  ],
  "coverage-gutters.showLineCoverage": true,
  "coverage-gutters.showRulerCoverage": true
}
```

### Team Metrics

#### Weekly Coverage Review
```markdown
## Week of September 11-17, 2025

**Team Coverage Summary**
- Overall: 84.2% (+1.3% from last week)
- New Code: 91.5% (target met)
- Critical Paths: 96.8% (target exceeded)

**Top Contributors**
- John D.: +5.2% coverage improvement in Services
- Sarah M.: 100% coverage on new Authentication features
- Mike K.: Added integration tests for API endpoints

**Focus Areas for Next Week**
- Model validation testing (currently 67.3%)
- Legacy utility refactoring
- Performance test coverage improvement
```

## Tools and Automation

### Recommended Tools Stack

#### Coverage Collection
- **Coverlet**: Primary coverage engine for .NET
- **dotnet-coverage**: Microsoft's coverage tool
- **OpenCover**: Alternative coverage engine

#### Report Generation
- **ReportGenerator**: HTML/JSON/XML report generation
- **Codecov**: Cloud-based coverage tracking
- **SonarCloud**: Code quality with coverage integration

#### IDE Integration
- **Fine Code Coverage**: Visual Studio extension
- **Coverage Gutters**: VS Code extension
- **JetBrains dotCover**: ReSharper/Rider integration

### Custom Coverage Scripts

#### Coverage Collection Script
```bash
#!/bin/bash
# scripts/collect-coverage.sh

set -e

echo "🧪 Running tests with coverage collection..."

# Clean previous results
rm -rf TestResults/
rm -rf coverage-report/

# Run tests with coverage
dotnet test \
  --configuration Release \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults \
  --verbosity normal

# Generate HTML report
reportgenerator \
  -reports:"TestResults/**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:"Html;JsonSummary;Badges"

# Display summary
echo "📊 Coverage Summary:"
cat coverage-report/Summary.txt

# Open report in browser (optional)
if command -v xdg-open > /dev/null; then
    xdg-open coverage-report/index.html
elif command -v open > /dev/null; then
    open coverage-report/index.html
fi

echo "✅ Coverage report generated at: coverage-report/index.html"
```

#### Coverage Validation Script
```powershell
# scripts/validate-coverage.ps1
param(
    [Parameter(Mandatory=$true)]
    [double]$MinimumCoverage = 80.0
)

$coverageFile = "coverage-report/Summary.json"

if (-not (Test-Path $coverageFile)) {
    Write-Error "Coverage file not found: $coverageFile"
    exit 1
}

$coverage = Get-Content $coverageFile | ConvertFrom-Json
$lineCoverage = $coverage.summary.linecoverage

Write-Host "📊 Current line coverage: $lineCoverage%"
Write-Host "🎯 Minimum required: $MinimumCoverage%"

if ($lineCoverage -lt $MinimumCoverage) {
    Write-Error "❌ Coverage below minimum threshold!"
    Write-Host "Required: $MinimumCoverage%, Actual: $lineCoverage%"
    exit 1
} else {
    Write-Host "✅ Coverage meets requirements!"
    exit 0
}
```

---

## Continuous Improvement

### Coverage Maturity Model

#### Level 1: Basic (Current)
- Coverage collection in CI/CD
- Minimum 80% overall target
- Basic reporting

#### Level 2: Intermediate (Target Q4 2025)
- Component-specific targets
- Coverage trend analysis
- Quality gate enforcement

#### Level 3: Advanced (Target Q2 2026)
- Mutation testing
- Performance test coverage
- AI-powered coverage suggestions

### Regular Review Process

#### Monthly Coverage Review
1. **Metrics Analysis**: Review trends and patterns
2. **Gap Identification**: Find undertested areas
3. **Process Improvement**: Refine standards and practices
4. **Tool Evaluation**: Assess new coverage tools
5. **Team Training**: Share best practices and techniques

This comprehensive coverage standards document ensures consistent, high-quality test coverage across the GCC Database Solution project.

---
*Last Updated: September 17, 2025*
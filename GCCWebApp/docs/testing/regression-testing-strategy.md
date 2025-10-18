# Regression Testing Strategy
## GCC Database Solution Project

### Document Information
- **Version:** 1.0
- **Created Date:** September 17, 2025
- **Document Type:** Regression Testing Strategy and Framework
- **Classification:** INTERNAL

---

## 1. Executive Summary

### 1.1 Purpose
This Regression Testing Strategy defines the comprehensive approach for ensuring that new changes to the GCC Database Solution do not introduce defects into existing functionality. It establishes frameworks for automated and manual regression testing, risk-based testing strategies, and continuous integration practices to maintain software quality throughout the development lifecycle.

### 1.2 Scope
**In Scope:**
- Automated regression test suites (Unit, Integration, UI)
- Manual regression testing procedures
- Risk-based regression testing approach
- Continuous integration and deployment testing
- Performance regression testing
- Security regression testing
- Cross-browser and cross-platform testing

**Out of Scope:**
- New feature development testing (covered in feature test plans)
- Third-party system regression testing
- Infrastructure regression testing
- User training on regression testing procedures

### 1.3 Objectives
- **Prevent Regressions:** Ensure existing functionality remains intact after changes
- **Early Detection:** Identify regression issues as early as possible in the development cycle
- **Risk Mitigation:** Focus testing efforts on high-risk areas based on change impact
- **Automation Efficiency:** Maximize automated test coverage to reduce manual effort
- **Continuous Quality:** Maintain consistent quality standards throughout development

---

## 2. Regression Testing Framework

### 2.1 Regression Testing Types

#### 2.1.1 Complete Regression Testing
**Definition:** Re-execution of the entire test suite
**When to Use:**
- Major architectural changes
- Core framework upgrades
- Security patches affecting multiple components
- Pre-production releases

**Coverage:** 100% of automated test suite
**Duration:** 4-8 hours
**Frequency:** Pre-release, major milestones

#### 2.1.2 Selective Regression Testing
**Definition:** Re-execution of selected test cases based on change impact
**When to Use:**
- Feature updates and enhancements
- Bug fixes in specific components
- Configuration changes
- Regular sprint releases

**Coverage:** 30-60% of test suite based on impact analysis
**Duration:** 1-3 hours
**Frequency:** Every sprint, code changes

#### 2.1.3 Progressive Regression Testing
**Definition:** New test cases added for new functionality + existing regression suite
**When to Use:**
- New feature additions
- API endpoint additions
- UI component additions
- Integration enhancements

**Coverage:** Existing regression suite + new test cases
**Duration:** 2-4 hours
**Frequency:** Feature releases

#### 2.1.4 Corrective Regression Testing
**Definition:** Testing specific to verify bug fixes don't break existing functionality
**When to Use:**
- Critical bug fixes
- Security vulnerability fixes
- Production hotfixes
- Emergency patches

**Coverage:** Bug-specific tests + related functionality
**Duration:** 30 minutes - 2 hours
**Frequency:** After each critical fix

### 2.2 Risk-Based Regression Testing

#### 2.2.1 Risk Assessment Matrix
| Risk Factor | Weight | High Risk (3) | Medium Risk (2) | Low Risk (1) |
|-------------|--------|---------------|-----------------|--------------|
| **Change Frequency** | 25% | >5 changes/week | 2-5 changes/week | <2 changes/week |
| **Business Criticality** | 30% | Core business functions | Important features | Nice-to-have features |
| **User Impact** | 20% | >80% users affected | 20-80% users affected | <20% users affected |
| **Technical Complexity** | 15% | High complexity | Medium complexity | Low complexity |
| **Historical Defects** | 10% | >10 defects/release | 3-10 defects/release | <3 defects/release |

#### 2.2.2 Component Risk Classification
| Component | Risk Level | Justification | Testing Priority |
|-----------|------------|---------------|------------------|
| **Certificate Management** | High | Core business function, frequent changes | P1 - Every build |
| **Authentication/Authorization** | High | Security critical, affects all users | P1 - Every build |
| **API Endpoints** | High | External partner dependencies | P1 - Every build |
| **Dashboard Analytics** | Medium | Important but not critical | P2 - Daily builds |
| **Search Functionality** | Medium | High usage, moderate complexity | P2 - Daily builds |
| **Export Features** | Medium | Important for users, moderate risk | P2 - Daily builds |
| **Admin Functions** | Low | Limited users, low frequency | P3 - Weekly builds |
| **UI Styling** | Low | Cosmetic changes, minimal impact | P3 - Release builds |

### 2.3 Test Selection Criteria

#### 2.3.1 Automated Test Selection Algorithm
```csharp
// Automated test selection service
public class RegressionTestSelectionService
{
    public async Task<TestSuite> SelectRegressionTestsAsync(ChangeSet changeSet)
    {
        var selectedTests = new TestSuite();

        // 1. Direct impact analysis
        var directlyImpactedTests = await GetDirectlyImpactedTestsAsync(changeSet);
        selectedTests.AddRange(directlyImpactedTests);

        // 2. Dependency impact analysis
        var dependencyImpactedTests = await GetDependencyImpactedTestsAsync(changeSet);
        selectedTests.AddRange(dependencyImpactedTests);

        // 3. Risk-based selection
        var riskBasedTests = await GetRiskBasedTestsAsync(changeSet);
        selectedTests.AddRange(riskBasedTests);

        // 4. Historical failure analysis
        var historicallyFailingTests = await GetHistoricallyFailingTestsAsync(changeSet);
        selectedTests.AddRange(historicallyFailingTests);

        // 5. Critical path testing
        var criticalPathTests = await GetCriticalPathTestsAsync();
        selectedTests.AddRange(criticalPathTests);

        // Remove duplicates and optimize execution order
        selectedTests = OptimizeTestSuite(selectedTests);

        return selectedTests;
    }

    private async Task<List<TestCase>> GetDirectlyImpactedTestsAsync(ChangeSet changeSet)
    {
        var impactedTests = new List<TestCase>();

        foreach (var changedFile in changeSet.ModifiedFiles)
        {
            // Find tests that directly test the changed code
            var relatedTests = await _testMappingService.GetTestsForFileAsync(changedFile);
            impactedTests.AddRange(relatedTests);

            // Find tests that import or depend on changed modules
            var dependentTests = await _dependencyAnalyzer.GetDependentTestsAsync(changedFile);
            impactedTests.AddRange(dependentTests);
        }

        return impactedTests.Distinct().ToList();
    }

    private async Task<List<TestCase>> GetRiskBasedTestsAsync(ChangeSet changeSet)
    {
        var riskTests = new List<TestCase>();

        // Get components affected by changes
        var affectedComponents = await _componentAnalyzer.GetAffectedComponentsAsync(changeSet);

        foreach (var component in affectedComponents)
        {
            var componentRisk = await _riskAssessmentService.GetComponentRiskAsync(component);

            if (componentRisk >= RiskLevel.High)
            {
                // Include all tests for high-risk components
                var componentTests = await _testRepository.GetTestsByComponentAsync(component);
                riskTests.AddRange(componentTests);
            }
            else if (componentRisk == RiskLevel.Medium)
            {
                // Include smoke tests for medium-risk components
                var smokeTests = await _testRepository.GetSmokeTestsByComponentAsync(component);
                riskTests.AddRange(smokeTests);
            }
        }

        return riskTests;
    }
}
```

---

## 3. Automated Regression Testing

### 3.1 Test Automation Pyramid

#### 3.1.1 Automation Strategy Overview
```
                    UI Tests (E2E)
                   ┌─────────────────┐
                   │  5% - 10%       │  <- Slow, expensive, high value
                   │  Critical flows │
                   └─────────────────┘

              Integration Tests
             ┌─────────────────────┐
             │     20% - 30%       │  <- Medium speed, medium cost
             │  API & Service      │
             │  Integration        │
             └─────────────────────┘

          Unit Tests
    ┌─────────────────────────┐
    │       60% - 75%         │  <- Fast, cheap, focused
    │   Component Logic       │
    │   Business Rules        │
    └─────────────────────────┘
```

#### 3.1.2 Unit Test Regression Strategy
```csharp
// Unit test regression framework
[TestClass]
[TestCategory("Regression")]
[TestCategory("Unit")]
public class CertificateServiceRegressionTests
{
    private CertificateService _certificateService;
    private Mock<ICertificateRepository> _mockRepository;
    private Mock<IWorkflowService> _mockWorkflowService;

    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<ICertificateRepository>();
        _mockWorkflowService = new Mock<IWorkflowService>();
        _certificateService = new CertificateService(_mockRepository.Object, _mockWorkflowService.Object);
    }

    [TestMethod]
    [TestCategory("P1")]
    public async Task CreateCertificate_ValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var certificate = CreateValidCertificate();
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Certificate>()))
                      .ReturnsAsync(certificate);

        // Act
        var result = await _certificateService.CreateCertificateAsync(certificate);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(certificate.Name, result.Name);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Certificate>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("P1")]
    [DataRow(null, "CompanyId")]
    [DataRow("", "Name")]
    [DataRow("   ", "Name")]
    public async Task CreateCertificate_InvalidData_ShouldThrowValidationException(
        string invalidValue, string propertyName)
    {
        // Arrange
        var certificate = CreateValidCertificate();
        SetPropertyValue(certificate, propertyName, invalidValue);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ValidationException>(
            () => _certificateService.CreateCertificateAsync(certificate));
    }

    [TestMethod]
    [TestCategory("P2")]
    public async Task GetCertificateById_ExistingId_ShouldReturnCertificate()
    {
        // Arrange
        var certificateId = 1;
        var expectedCertificate = CreateValidCertificate();
        _mockRepository.Setup(r => r.GetByIdAsync(certificateId))
                      .ReturnsAsync(expectedCertificate);

        // Act
        var result = await _certificateService.GetCertificateByIdAsync(certificateId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedCertificate.Id, result.Id);
    }

    // Performance regression test
    [TestMethod]
    [TestCategory("Performance")]
    [Timeout(1000)] // Should complete within 1 second
    public async Task SearchCertificates_LargeDataset_ShouldCompleteWithinTimeout()
    {
        // Arrange
        var certificates = GenerateLargeCertificateDataset(10000);
        _mockRepository.Setup(r => r.SearchAsync(It.IsAny<string>()))
                      .ReturnsAsync(certificates.Take(100).ToList());

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _certificateService.SearchCertificatesAsync("test");
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500,
            $"Search took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
        Assert.IsNotNull(result);
    }
}
```

#### 3.1.3 Integration Test Regression Strategy
```csharp
// Integration test regression framework
[TestClass]
[TestCategory("Regression")]
[TestCategory("Integration")]
public class CertificateApiRegressionTests : IntegrationTestBase
{
    [TestMethod]
    [TestCategory("P1")]
    public async Task GetCertificates_ValidRequest_ShouldReturnExpectedFormat()
    {
        // Arrange
        await SeedTestDataAsync();
        var client = CreateAuthenticatedClient("Admin");

        // Act
        var response = await client.GetAsync("/api/v1/certificates");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<List<CertificateDto>>>(content);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Data);
        Assert.IsTrue(result.Data.Count > 0);

        // Verify response structure hasn't changed
        var firstCertificate = result.Data.First();
        Assert.IsNotNull(firstCertificate.Id);
        Assert.IsNotNull(firstCertificate.Name);
        Assert.IsNotNull(firstCertificate.Status);
    }

    [TestMethod]
    [TestCategory("P1")]
    public async Task CreateCertificate_ValidData_ShouldMaintainWorkflow()
    {
        // Arrange
        var client = CreateAuthenticatedClient("Admin");
        var certificateData = CreateValidCertificateRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/certificates", certificateData);

        // Assert
        response.EnsureSuccessStatusCode();
        var createdCertificate = await DeserializeResponseAsync<CertificateDto>(response);

        // Verify workflow is properly initialized
        Assert.AreEqual(CertificateStatus.Draft, createdCertificate.Status);

        // Verify database state
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GCCDbContext>();
        var dbCertificate = await context.Certificates.FindAsync(createdCertificate.Id);

        Assert.IsNotNull(dbCertificate);
        Assert.AreEqual(certificateData.Name, dbCertificate.Name);
    }

    [TestMethod]
    [TestCategory("P2")]
    public async Task CertificateWorkflow_StateTransitions_ShouldFollowBusinessRules()
    {
        // Arrange
        var client = CreateAuthenticatedClient("Admin");
        var certificate = await CreateTestCertificateAsync();

        // Act & Assert - Test each valid transition
        // Draft -> Under Review
        await TransitionCertificateStatusAsync(client, certificate.Id, "SubmitForReview");
        await VerifyCertificateStatusAsync(certificate.Id, CertificateStatus.UnderReview);

        // Under Review -> Approved
        await TransitionCertificateStatusAsync(client, certificate.Id, "Approve");
        await VerifyCertificateStatusAsync(certificate.Id, CertificateStatus.Approved);

        // Approved -> Active
        await TransitionCertificateStatusAsync(client, certificate.Id, "Activate");
        await VerifyCertificateStatusAsync(certificate.Id, CertificateStatus.Active);
    }

    [TestMethod]
    [TestCategory("Security")]
    public async Task ApiEndpoints_AuthorizationRegressionTest_ShouldEnforceRoles()
    {
        // Test role-based access hasn't regressed
        var testCases = new[]
        {
            new { Endpoint = "/api/v1/certificates", Method = "GET", Role = "External", ShouldSucceed = true },
            new { Endpoint = "/api/v1/certificates", Method = "POST", Role = "External", ShouldSucceed = false },
            new { Endpoint = "/api/v1/admin/users", Method = "GET", Role = "Analyst", ShouldSucceed = false },
            new { Endpoint = "/api/v1/admin/users", Method = "GET", Role = "Admin", ShouldSucceed = true }
        };

        foreach (var testCase in testCases)
        {
            var client = CreateAuthenticatedClient(testCase.Role);
            HttpResponseMessage response;

            if (testCase.Method == "GET")
                response = await client.GetAsync(testCase.Endpoint);
            else
                response = await client.PostAsJsonAsync(testCase.Endpoint, new { });

            if (testCase.ShouldSucceed)
                Assert.AreNotEqual(HttpStatusCode.Forbidden, response.StatusCode,
                    $"{testCase.Role} should have access to {testCase.Endpoint}");
            else
                Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode,
                    $"{testCase.Role} should NOT have access to {testCase.Endpoint}");
        }
    }
}
```

#### 3.1.4 UI Regression Testing with Playwright
```csharp
// UI regression test framework
[TestClass]
[TestCategory("Regression")]
[TestCategory("UI")]
public class CertificateUIRegressionTests : PlaywrightTestBase
{
    [TestMethod]
    [TestCategory("P1")]
    public async Task CertificateTileView_LoadAndDisplay_ShouldMaintainLayout()
    {
        // Arrange
        await SeedTestDataAsync();
        var page = await Browser.NewPageAsync();

        // Act
        await page.GotoAsync("/Certificate/CertificateTileView");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        // Verify page structure hasn't changed
        await Expect(page.Locator(".certificate-tile-container")).ToBeVisibleAsync();
        await Expect(page.Locator(".search-container")).ToBeVisibleAsync();
        await Expect(page.Locator(".filter-container")).ToBeVisibleAsync();

        // Verify minimum number of tiles are displayed
        var tiles = page.Locator(".certificate-tile");
        await Expect(tiles).ToHaveCountAsync(x => x >= 1);

        // Verify tile structure
        var firstTile = tiles.First();
        await Expect(firstTile.Locator(".certificate-name")).ToBeVisibleAsync();
        await Expect(firstTile.Locator(".certificate-status")).ToBeVisibleAsync();
        await Expect(firstTile.Locator(".certificate-company")).ToBeVisibleAsync();
    }

    [TestMethod]
    [TestCategory("P1")]
    public async Task CertificateSearch_Functionality_ShouldWorkAsExpected()
    {
        // Arrange
        await SeedTestDataAsync();
        var page = await Browser.NewPageAsync();
        await page.GotoAsync("/Certificate/CertificateTileView");

        // Act
        var searchInput = page.Locator("#search-input");
        await searchInput.FillAsync("Environmental");
        await searchInput.PressAsync("Enter");
        await page.WaitForTimeoutAsync(1000); // Wait for search results

        // Assert
        var tiles = page.Locator(".certificate-tile");
        var tileCount = await tiles.CountAsync();
        Assert.IsTrue(tileCount > 0, "Search should return results");

        // Verify search results contain the search term
        for (int i = 0; i < tileCount; i++)
        {
            var tileText = await tiles.Nth(i).TextContentAsync();
            Assert.IsTrue(tileText.Contains("Environmental", StringComparison.OrdinalIgnoreCase),
                "Search results should contain the search term");
        }
    }

    [TestMethod]
    [TestCategory("P2")]
    public async Task CertificateCreation_Workflow_ShouldCompleteSuccessfully()
    {
        // Arrange
        var page = await Browser.NewPageAsync();
        await LoginAsync(page, "admin@greencirclecertified.com", "Admin123!");

        // Act
        await page.GotoAsync("/Certificate/Create");

        // Fill form
        await page.Locator("#Name").FillAsync("Regression Test Certificate");
        await page.Locator("#CompanyId").SelectOptionAsync("1");
        await page.Locator("#ProductId").SelectOptionAsync("1");

        // Submit form
        await page.Locator("button[type='submit']").ClickAsync();

        // Assert
        // Should redirect to certificate details
        await page.WaitForURLAsync(url => url.Contains("/Certificate/Details/"));

        // Verify certificate was created
        await Expect(page.Locator(".certificate-name")).ToContainTextAsync("Regression Test Certificate");
        await Expect(page.Locator(".certificate-status")).ToContainTextAsync("Draft");
    }

    [TestMethod]
    [TestCategory("Accessibility")]
    public async Task CertificateTileView_AccessibilityRegression_ShouldMaintainStandards()
    {
        // Arrange
        var page = await Browser.NewPageAsync();
        await page.GotoAsync("/Certificate/CertificateTileView");

        // Assert accessibility standards
        // Check for minimum font size (16px requirement)
        var fontSize = await page.EvaluateAsync<string>(
            "getComputedStyle(document.querySelector('.certificate-title')).fontSize"
        );
        var fontSizeValue = int.Parse(fontSize.Replace("px", ""));
        Assert.IsTrue(fontSizeValue >= 16, $"Font size {fontSizeValue}px is below minimum 16px requirement");

        // Check for proper heading structure
        var headings = await page.Locator("h1, h2, h3, h4, h5, h6").AllAsync();
        Assert.IsTrue(headings.Count > 0, "Page should have proper heading structure");

        // Check for alt text on images
        var images = await page.Locator("img").AllAsync();
        foreach (var image in images)
        {
            var altText = await image.GetAttributeAsync("alt");
            Assert.IsFalse(string.IsNullOrEmpty(altText), "All images should have alt text");
        }

        // Check for proper form labels
        var inputs = await page.Locator("input[type='text'], input[type='email']").AllAsync();
        foreach (var input in inputs)
        {
            var id = await input.GetAttributeAsync("id");
            if (!string.IsNullOrEmpty(id))
            {
                var label = page.Locator($"label[for='{id}']");
                await Expect(label).ToBeVisibleAsync();
            }
        }
    }

    [TestMethod]
    [TestCategory("Performance")]
    [Timeout(10000)] // 10 second timeout
    public async Task CertificateTileView_LoadPerformance_ShouldMeetTargets()
    {
        // Arrange
        var page = await Browser.NewPageAsync();

        // Act
        var stopwatch = Stopwatch.StartNew();
        await page.GotoAsync("/Certificate/CertificateTileView");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 3000,
            $"Page load took {stopwatch.ElapsedMilliseconds}ms, expected < 3000ms");

        // Verify performance metrics using browser API
        var performanceMetrics = await page.EvaluateAsync<Dictionary<string, double>>(@"
            () => {
                const navigation = performance.getEntriesByType('navigation')[0];
                return {
                    domContentLoaded: navigation.domContentLoadedEventEnd - navigation.domContentLoadedEventStart,
                    loadComplete: navigation.loadEventEnd - navigation.loadEventStart,
                    firstContentfulPaint: performance.getEntriesByName('first-contentful-paint')[0]?.startTime || 0
                };
            }
        ");

        Assert.IsTrue(performanceMetrics["domContentLoaded"] < 2000,
            "DOM content loaded should be < 2 seconds");
        Assert.IsTrue(performanceMetrics["firstContentfulPaint"] < 1500,
            "First contentful paint should be < 1.5 seconds");
    }
}
```

### 3.2 Continuous Integration Regression Testing

#### 3.2.1 CI/CD Pipeline Integration
```yaml
# Azure DevOps regression testing pipeline
trigger:
- main
- develop
- release/*

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

stages:
- stage: Build
  jobs:
  - job: BuildApplication
    steps:
    - task: DotNetCoreCLI@2
      displayName: 'Restore packages'
      inputs:
        command: 'restore'

    - task: DotNetCoreCLI@2
      displayName: 'Build application'
      inputs:
        command: 'build'
        configuration: $(buildConfiguration)

- stage: RegressionTesting
  dependsOn: Build
  jobs:
  - job: UnitRegressionTests
    displayName: 'Unit Regression Tests'
    steps:
    - task: DotNetCoreCLI@2
      displayName: 'Run Unit Regression Tests'
      inputs:
        command: 'test'
        projects: '**/*Tests.csproj'
        arguments: '--configuration $(buildConfiguration) --filter "TestCategory=Regression&TestCategory=Unit" --logger trx --collect:"XPlat Code Coverage"'

    - task: PublishTestResults@2
      inputs:
        testResultsFormat: 'VSTest'
        testResultsFiles: '**/*.trx'
        testRunTitle: 'Unit Regression Tests'

  - job: IntegrationRegressionTests
    displayName: 'Integration Regression Tests'
    dependsOn: UnitRegressionTests
    condition: succeeded()
    steps:
    - task: DockerCompose@0
      displayName: 'Start Test Environment'
      inputs:
        action: 'Run services'
        dockerComposeFile: 'docker-compose.test.yml'

    - task: DotNetCoreCLI@2
      displayName: 'Run Integration Regression Tests'
      inputs:
        command: 'test'
        projects: '**/*Integration.Tests.csproj'
        arguments: '--configuration $(buildConfiguration) --filter "TestCategory=Regression" --logger trx'

    - task: DockerCompose@0
      displayName: 'Stop Test Environment'
      inputs:
        action: 'Down'
        dockerComposeFile: 'docker-compose.test.yml'

  - job: UIRegressionTests
    displayName: 'UI Regression Tests'
    dependsOn: IntegrationRegressionTests
    condition: succeeded()
    steps:
    - task: DockerCompose@0
      displayName: 'Start Application'
      inputs:
        action: 'Run services'
        dockerComposeFile: 'docker-compose.test.yml'

    - task: PlaywrightCLI@1
      displayName: 'Install Playwright'
      inputs:
        workingDirectory: 'UITests'

    - task: DotNetCoreCLI@2
      displayName: 'Run UI Regression Tests'
      inputs:
        command: 'test'
        projects: '**/*UI.Tests.csproj'
        arguments: '--configuration $(buildConfiguration) --filter "TestCategory=Regression&TestCategory=P1" --logger trx'

  - job: PerformanceRegressionTests
    displayName: 'Performance Regression Tests'
    dependsOn: IntegrationRegressionTests
    condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
    steps:
    - script: |
        dotnet run --project PerformanceTests -- --regression --baseline
      displayName: 'Run Performance Regression Tests'

    - task: PublishTestResults@2
      inputs:
        testResultsFormat: 'JUnit'
        testResultsFiles: 'performance-results.xml'
        testRunTitle: 'Performance Regression Tests'

- stage: RegressionReport
  dependsOn: RegressionTesting
  condition: always()
  jobs:
  - job: GenerateReport
    steps:
    - task: PowerShell@2
      displayName: 'Generate Regression Test Report'
      inputs:
        targetType: 'inline'
        script: |
          # Collect test results and generate comprehensive report
          $testResults = Get-ChildItem -Path "$(Agent.TempDirectory)" -Filter "*.trx" -Recurse

          # Generate HTML report
          dotnet tool install --global dotnet-reportgenerator-globaltool
          reportgenerator -reports:"**/*.trx" -targetdir:"regression-report" -reporttypes:Html

    - task: PublishHtmlReport@1
      inputs:
        reportDir: 'regression-report'
        tabName: 'Regression Test Report'
```

#### 3.2.2 Automated Test Selection in CI
```csharp
// CI regression test selection service
public class CIRegressionTestSelector
{
    public async Task<TestExecutionPlan> SelectTestsForCIAsync(BuildContext buildContext)
    {
        var plan = new TestExecutionPlan();

        // Always run P1 tests
        plan.UnitTests.AddRange(await GetTestsByCategoryAsync("P1", "Unit"));
        plan.IntegrationTests.AddRange(await GetTestsByCategoryAsync("P1", "Integration"));

        // Determine additional tests based on change scope
        var changeScope = await AnalyzeChangeScopeAsync(buildContext.ChangedFiles);

        switch (changeScope)
        {
            case ChangeScope.Minor:
                // Run focused regression tests
                plan.UITests.AddRange(await GetSmokeTestsAsync());
                break;

            case ChangeScope.Major:
                // Run comprehensive regression suite
                plan.UnitTests.AddRange(await GetTestsByCategoryAsync("P2", "Unit"));
                plan.IntegrationTests.AddRange(await GetTestsByCategoryAsync("P2", "Integration"));
                plan.UITests.AddRange(await GetTestsByCategoryAsync("P1", "UI"));
                break;

            case ChangeScope.Critical:
                // Run full regression suite
                plan = await GetFullRegressionSuiteAsync();
                break;
        }

        // Add component-specific tests based on changed files
        var componentTests = await GetComponentSpecificTestsAsync(buildContext.ChangedFiles);
        plan.AddRange(componentTests);

        // Optimize execution order
        plan = OptimizeExecutionOrder(plan);

        return plan;
    }

    private async Task<ChangeScope> AnalyzeChangeScopeAsync(List<string> changedFiles)
    {
        // Analyze changed files to determine impact scope
        var criticalFiles = new[] { "Program.cs", "Startup.cs", "appsettings.json" };
        var coreComponents = new[] { "Certificate", "Authentication", "Authorization" };

        if (changedFiles.Any(f => criticalFiles.Any(cf => f.Contains(cf))))
            return ChangeScope.Critical;

        if (changedFiles.Any(f => coreComponents.Any(cc => f.Contains(cc))))
            return ChangeScope.Major;

        return ChangeScope.Minor;
    }
}
```

---

## 4. Manual Regression Testing

### 4.1 Manual Test Case Selection

#### 4.1.1 Critical User Journey Tests
```markdown
## Critical User Journey Regression Tests

### CUJ-001: Public Certificate Discovery
**Priority:** P1
**Frequency:** Every release
**Estimated Time:** 15 minutes

#### Test Steps:
1. **Navigate to Certificate Tile View**
   - Open browser and go to main application URL
   - Verify page loads without errors
   - Confirm certificate tiles are displayed

2. **Search for Certificates**
   - Enter search term "Environmental"
   - Verify search results are relevant
   - Clear search and verify all certificates return

3. **Filter Certificates**
   - Apply partner filter (Amazon)
   - Verify only Amazon certificates shown
   - Apply status filter (Active)
   - Verify filter combinations work

4. **View Certificate Details**
   - Click on a certificate tile
   - Verify details page loads correctly
   - Verify all certificate information is displayed

5. **Download Certificate**
   - Click download button
   - Verify PDF is generated and downloaded
   - Verify PDF content is correct

#### Acceptance Criteria:
- [ ] All pages load within 3 seconds
- [ ] Search returns relevant results
- [ ] Filters work independently and in combination
- [ ] Certificate details are complete and accurate
- [ ] PDF download works and content is correct

### CUJ-002: Certificate Management Workflow
**Priority:** P1
**Frequency:** Every release
**Estimated Time:** 20 minutes

#### Test Steps:
1. **Login as Admin**
   - Navigate to login page
   - Enter valid admin credentials
   - Verify successful login and dashboard access

2. **Create New Certificate**
   - Navigate to certificate creation page
   - Fill all required fields with valid data
   - Submit form
   - Verify certificate is created in Draft status

3. **Certificate Workflow Progression**
   - Submit certificate for review
   - Verify status changes to "Under Review"
   - Approve certificate
   - Verify status changes to "Approved"
   - Activate certificate
   - Verify status changes to "Active"

4. **Certificate Editing**
   - Edit certificate details
   - Save changes
   - Verify changes are saved correctly
   - Verify audit trail is updated

5. **Certificate Management Operations**
   - Search for created certificate
   - Export certificate data
   - Verify export contains correct data

#### Acceptance Criteria:
- [ ] Login process works correctly
- [ ] Certificate creation saves all data
- [ ] Workflow transitions follow business rules
- [ ] Edit operations preserve data integrity
- [ ] Export functionality works correctly
```

#### 4.1.2 Browser Compatibility Testing
```markdown
## Browser Compatibility Regression Matrix

### Desktop Browsers
| Browser | Version | Test Frequency | Critical Features |
|---------|---------|----------------|-------------------|
| **Chrome** | Latest | Every build | Full functionality |
| **Firefox** | Latest | Every build | Full functionality |
| **Safari** | Latest | Weekly | Core functionality |
| **Edge** | Latest | Weekly | Core functionality |

### Mobile Browsers
| Browser | Version | Test Frequency | Critical Features |
|---------|---------|----------------|-------------------|
| **Chrome Mobile** | Latest | Sprint releases | Core functionality |
| **Safari Mobile** | Latest | Sprint releases | Core functionality |
| **Samsung Internet** | Latest | Release cycles | Basic functionality |

### Test Scenarios by Browser
#### Chrome/Firefox (Full Testing)
- [ ] Complete user journeys
- [ ] Form interactions and validation
- [ ] File upload and download
- [ ] Real-time features
- [ ] Performance benchmarks

#### Safari/Edge (Core Testing)
- [ ] Login and authentication
- [ ] Certificate viewing and search
- [ ] Basic navigation
- [ ] PDF download
- [ ] Mobile responsiveness

#### Mobile Browsers (Basic Testing)
- [ ] Login functionality
- [ ] Certificate tile view
- [ ] Basic search
- [ ] Touch interactions
- [ ] Responsive layout
```

### 4.2 Exploratory Regression Testing

#### 4.2.1 Exploratory Testing Charter Template
```markdown
# Exploratory Testing Charter: Certificate Search Functionality

## Mission
Test the certificate search functionality to discover any regressions in search behavior, performance, or user experience following recent changes to the search algorithm.

## Time Box
60 minutes

## Test Areas
- Search input validation
- Search result accuracy
- Search performance
- Filter interactions
- Edge cases and error handling

## Test Data
- Use production-like dataset (1000+ certificates)
- Mix of active, inactive, and archived certificates
- Various certificate types and partners

## Testing Approach
### Session 1: Basic Search Functionality (20 minutes)
- Test various search terms (short, long, special characters)
- Verify search suggestions appear
- Test search with empty input
- Test search with very long strings
- Verify case-insensitive search

### Session 2: Advanced Search Scenarios (20 minutes)
- Combine search with filters
- Test search across different certificate fields
- Test search with partial matches
- Verify boolean search operators if supported
- Test search result sorting

### Session 3: Performance and Edge Cases (20 minutes)
- Test search with large result sets
- Test rapid consecutive searches
- Test search during high system load
- Test search with network interruptions
- Test search result pagination

## Notes and Findings
[Document any issues, unusual behaviors, or improvements discovered]

## Follow-up Actions
[List any bugs to be reported or further investigation needed]
```

#### 4.2.2 Session-Based Test Management
```csharp
// Exploratory testing session tracker
public class ExploratoryTestSession
{
    public string SessionId { get; set; }
    public string Tester { get; set; }
    public string Charter { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan TimeBox { get; set; }
    public List<TestNote> Notes { get; set; } = new();
    public List<Bug> BugsFound { get; set; } = new();
    public List<ImprovementSuggestion> Improvements { get; set; } = new();
    public SessionOutcome Outcome { get; set; }

    public void AddNote(string note, TestNoteType type = TestNoteType.Observation)
    {
        Notes.Add(new TestNote
        {
            Timestamp = DateTime.UtcNow,
            Content = note,
            Type = type
        });
    }

    public void ReportBug(string title, string description, Severity severity)
    {
        BugsFound.Add(new Bug
        {
            Title = title,
            Description = description,
            Severity = severity,
            FoundInSession = SessionId,
            ReportedBy = Tester,
            ReportedAt = DateTime.UtcNow
        });
    }

    public SessionSummary GenerateSummary()
    {
        return new SessionSummary
        {
            SessionId = SessionId,
            Duration = EndTime - StartTime,
            Charter = Charter,
            TotalNotes = Notes.Count,
            BugsFound = BugsFound.Count,
            CriticalIssues = BugsFound.Count(b => b.Severity == Severity.Critical),
            Improvements = Improvements.Count,
            CoverageAchieved = CalculateCoverageAchieved(),
            Effectiveness = CalculateSessionEffectiveness()
        };
    }
}

public class ExploratoryTestingCoordinator
{
    public async Task<ExploratoryTestPlan> CreateRegressionExploratoryPlanAsync(Release release)
    {
        var plan = new ExploratoryTestPlan
        {
            ReleaseId = release.Id,
            PlannedSessions = new List<ExploratoryTestSession>()
        };

        // Create sessions based on changed components
        var changedComponents = await GetChangedComponentsAsync(release);

        foreach (var component in changedComponents)
        {
            var charter = await GenerateCharterForComponentAsync(component, release);
            var session = new ExploratoryTestSession
            {
                SessionId = Guid.NewGuid().ToString(),
                Charter = charter,
                TimeBox = TimeSpan.FromMinutes(60),
                Component = component.Name
            };

            plan.PlannedSessions.Add(session);
        }

        // Add general regression exploration sessions
        plan.PlannedSessions.AddRange(await GenerateGeneralRegressionSessionsAsync(release));

        return plan;
    }
}
```

---

## 5. Performance Regression Testing

### 5.1 Performance Baseline Management

#### 5.1.1 Performance Baseline Framework
```csharp
// Performance baseline management service
public class PerformanceBaselineService
{
    public async Task<PerformanceBaseline> EstablishBaselineAsync(string version)
    {
        var baseline = new PerformanceBaseline
        {
            Version = version,
            EstablishedDate = DateTime.UtcNow,
            Metrics = new Dictionary<string, PerformanceMetric>()
        };

        // Collect baseline metrics for key scenarios
        baseline.Metrics["page_load_certificate_tile_view"] = await MeasurePageLoadAsync("/Certificate/CertificateTileView");
        baseline.Metrics["api_get_certificates"] = await MeasureApiResponseAsync("GET", "/api/v1/certificates");
        baseline.Metrics["certificate_search"] = await MeasureCertificateSearchAsync("environmental");
        baseline.Metrics["certificate_creation"] = await MeasureCertificateCreationAsync();
        baseline.Metrics["pdf_generation"] = await MeasurePdfGenerationAsync();
        baseline.Metrics["dashboard_load"] = await MeasurePageLoadAsync("/Dashboard");

        await _baselineRepository.SaveBaselineAsync(baseline);
        return baseline;
    }

    public async Task<PerformanceRegressionReport> CheckForRegressionsAsync(string currentVersion, string baselineVersion)
    {
        var currentBaseline = await EstablishBaselineAsync(currentVersion);
        var previousBaseline = await _baselineRepository.GetBaselineAsync(baselineVersion);

        var report = new PerformanceRegressionReport
        {
            CurrentVersion = currentVersion,
            BaselineVersion = baselineVersion,
            ComparisonDate = DateTime.UtcNow,
            Regressions = new List<PerformanceRegression>()
        };

        foreach (var metric in currentBaseline.Metrics)
        {
            if (previousBaseline.Metrics.TryGetValue(metric.Key, out var baselineMetric))
            {
                var regression = AnalyzeMetricRegression(metric.Key, metric.Value, baselineMetric);
                if (regression != null)
                {
                    report.Regressions.Add(regression);
                }
            }
        }

        await _reportRepository.SaveRegressionReportAsync(report);
        return report;
    }

    private PerformanceRegression AnalyzeMetricRegression(string metricName, PerformanceMetric current, PerformanceMetric baseline)
    {
        var percentageChange = ((current.Value - baseline.Value) / baseline.Value) * 100;

        // Define regression thresholds
        var regressionThreshold = GetRegressionThreshold(metricName);

        if (percentageChange > regressionThreshold)
        {
            return new PerformanceRegression
            {
                MetricName = metricName,
                BaselineValue = baseline.Value,
                CurrentValue = current.Value,
                PercentageChange = percentageChange,
                Severity = DetermineRegressionSeverity(percentageChange),
                RegressionType = DetermineRegressionType(metricName)
            };
        }

        return null;
    }

    private double GetRegressionThreshold(string metricName)
    {
        return metricName switch
        {
            "page_load_certificate_tile_view" => 20.0, // 20% slower is a regression
            "api_get_certificates" => 15.0, // 15% slower for API calls
            "certificate_search" => 25.0, // 25% slower for search
            "pdf_generation" => 30.0, // 30% slower for PDF generation
            _ => 20.0 // Default 20% threshold
        };
    }
}
```

#### 5.1.2 Automated Performance Tests
```csharp
// Automated performance regression tests
[TestClass]
[TestCategory("Performance")]
[TestCategory("Regression")]
public class PerformanceRegressionTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private PerformanceBaselineService _baselineService;

    [TestInitialize]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        _baselineService = new PerformanceBaselineService();
    }

    [TestMethod]
    [TestCategory("P1")]
    public async Task CertificateTileView_LoadTime_ShouldNotRegress()
    {
        // Arrange
        var baseline = await _baselineService.GetLatestBaselineAsync();
        var targetLoadTime = baseline.Metrics["page_load_certificate_tile_view"].Value;
        var regressionThreshold = targetLoadTime * 1.2; // 20% regression threshold

        // Act
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.GetAsync("/Certificate/CertificateTileView");
        stopwatch.Stop();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < regressionThreshold,
            $"Page load time {stopwatch.ElapsedMilliseconds}ms exceeds regression threshold {regressionThreshold}ms");

        // Log performance metric
        await _baselineService.LogPerformanceMetricAsync("page_load_certificate_tile_view",
            stopwatch.ElapsedMilliseconds, DateTime.UtcNow);
    }

    [TestMethod]
    [TestCategory("P1")]
    public async Task CertificateApi_ResponseTime_ShouldNotRegress()
    {
        // Arrange
        var baseline = await _baselineService.GetLatestBaselineAsync();
        var targetResponseTime = baseline.Metrics["api_get_certificates"].Value;
        var regressionThreshold = targetResponseTime * 1.15; // 15% regression threshold

        // Act
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.GetAsync("/api/v1/certificates?pageSize=20");
        stopwatch.Stop();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < regressionThreshold,
            $"API response time {stopwatch.ElapsedMilliseconds}ms exceeds regression threshold {regressionThreshold}ms");

        // Verify response quality hasn't degraded
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<List<CertificateDto>>>(content);
        Assert.IsTrue(result.Data.Count > 0, "API should return data");
    }

    [TestMethod]
    [TestCategory("P2")]
    public async Task CertificateSearch_Performance_ShouldMaintainSpeed()
    {
        // Arrange
        var searchTerms = new[] { "environmental", "organic", "sustainable", "manufacturing" };
        var baseline = await _baselineService.GetLatestBaselineAsync();
        var targetTime = baseline.Metrics["certificate_search"].Value;

        foreach (var searchTerm in searchTerms)
        {
            // Act
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.GetAsync($"/api/v1/certificates?search={searchTerm}");
            stopwatch.Stop();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < targetTime * 1.25,
                $"Search for '{searchTerm}' took {stopwatch.ElapsedMilliseconds}ms, expected < {targetTime * 1.25}ms");
        }
    }

    [TestMethod]
    [TestCategory("P2")]
    public async Task DatabaseQueries_Performance_ShouldNotDegrade()
    {
        // Test database query performance hasn't regressed
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GCCDbContext>();

        // Test complex query performance
        var stopwatch = Stopwatch.StartNew();
        var results = await context.Certificates
            .Include(c => c.Company)
            .Include(c => c.Product)
            .Where(c => c.Status == CertificateStatus.Active)
            .OrderByDescending(c => c.CreatedDate)
            .Take(100)
            .ToListAsync();
        stopwatch.Stop();

        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500,
            $"Complex query took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
        Assert.IsTrue(results.Count > 0, "Query should return results");
    }
}
```

### 5.2 Memory and Resource Regression Testing

#### 5.2.1 Memory Leak Detection
```csharp
// Memory regression testing
[TestClass]
[TestCategory("Memory")]
[TestCategory("Regression")]
public class MemoryRegressionTests
{
    [TestMethod]
    [TestCategory("Endurance")]
    public async Task LongRunningOperations_MemoryUsage_ShouldNotLeak()
    {
        // Arrange
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var initialMemory = GC.GetTotalMemory(true);
        var memoryMeasurements = new List<long>();

        // Act - Simulate extended usage
        for (int i = 0; i < 1000; i++)
        {
            // Perform various operations
            await client.GetAsync("/Certificate/CertificateTileView");
            await client.GetAsync("/api/v1/certificates");
            await client.GetAsync("/Dashboard");

            // Measure memory every 100 iterations
            if (i % 100 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var currentMemory = GC.GetTotalMemory(false);
                memoryMeasurements.Add(currentMemory);
            }
        }

        // Assert - Check for memory leaks
        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreasePercentage = (double)memoryIncrease / initialMemory * 100;

        // Allow up to 50% memory increase for caching and normal operations
        Assert.IsTrue(memoryIncreasePercentage < 50,
            $"Memory increased by {memoryIncreasePercentage:F2}%, expected < 50%");

        // Verify memory trend is not consistently increasing
        var trendSlope = CalculateMemoryTrend(memoryMeasurements);
        Assert.IsTrue(trendSlope < 1000, // Allow some growth but not exponential
            $"Memory trend slope {trendSlope} indicates potential memory leak");
    }

    private double CalculateMemoryTrend(List<long> measurements)
    {
        if (measurements.Count < 2) return 0;

        var n = measurements.Count;
        var sumX = n * (n - 1) / 2; // 0 + 1 + 2 + ... + (n-1)
        var sumY = measurements.Sum();
        var sumXY = measurements.Select((y, x) => x * y).Sum();
        var sumX2 = measurements.Select((_, x) => x * x).Sum();

        return (double)(n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
    }
}
```

---

## 6. Security Regression Testing

### 6.1 Security Regression Framework

#### 6.1.1 Security Test Automation
```csharp
// Security regression testing framework
[TestClass]
[TestCategory("Security")]
[TestCategory("Regression")]
public class SecurityRegressionTests
{
    [TestMethod]
    [TestCategory("P1")]
    public async Task Authentication_SecurityControls_ShouldNotRegress()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        // Test 1: SQL Injection in login
        var sqlInjectionPayloads = new[]
        {
            "admin'; DROP TABLE Users; --",
            "' OR '1'='1' --",
            "'; EXEC xp_cmdshell('dir'); --"
        };

        foreach (var payload in sqlInjectionPayloads)
        {
            var loginData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Email", payload),
                new KeyValuePair<string, string>("Password", "password")
            });

            var response = await client.PostAsync("/Account/Login", loginData);

            // Should not succeed with SQL injection
            Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode);

            // Verify application is still responsive (not crashed by injection)
            var healthCheck = await client.GetAsync("/health");
            Assert.AreEqual(HttpStatusCode.OK, healthCheck.StatusCode);
        }
    }

    [TestMethod]
    [TestCategory("P1")]
    public async Task Authorization_RoleBasedAccess_ShouldMaintainRestrictions()
    {
        var factory = new WebApplicationFactory<Program>();

        var authorizationTests = new[]
        {
            new { Role = "External", Endpoint = "/Admin/UserManagement", ShouldAllow = false },
            new { Role = "Internal", Endpoint = "/Certificate/Create", ShouldAllow = false },
            new { Role = "Analyst", Endpoint = "/Admin/UserManagement", ShouldAllow = false },
            new { Role = "Admin", Endpoint = "/Admin/UserManagement", ShouldAllow = true }
        };

        foreach (var test in authorizationTests)
        {
            var client = await CreateAuthenticatedClientAsync(factory, test.Role);
            var response = await client.GetAsync(test.Endpoint);

            if (test.ShouldAllow)
            {
                Assert.AreNotEqual(HttpStatusCode.Forbidden, response.StatusCode,
                    $"{test.Role} should have access to {test.Endpoint}");
            }
            else
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode,
                    $"{test.Role} should NOT have access to {test.Endpoint}");
            }
        }
    }

    [TestMethod]
    [TestCategory("P2")]
    public async Task ApiSecurity_RateLimiting_ShouldPreventAbuse()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        // Add API key for testing
        client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var successCount = 0;
        var rateLimitedCount = 0;

        // Make requests up to the rate limit
        for (int i = 0; i < 150; i++) // Rate limit is typically 100/minute
        {
            var response = await client.GetAsync("/api/v1/certificates");

            if (response.StatusCode == HttpStatusCode.OK)
                successCount++;
            else if (response.StatusCode == HttpStatusCode.TooManyRequests)
                rateLimitedCount++;
        }

        // Verify rate limiting is working
        Assert.IsTrue(rateLimitedCount > 0, "Rate limiting should be enforced");
        Assert.IsTrue(successCount <= 100, "Should not exceed rate limit");
    }

    [TestMethod]
    [TestCategory("P2")]
    public async Task InputValidation_XSSPrevention_ShouldSanitizeInputs()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = await CreateAuthenticatedClientAsync(factory, "Admin");

        var xssPayloads = new[]
        {
            "<script>alert('XSS')</script>",
            "<img src='x' onerror='alert(1)'>",
            "javascript:alert('XSS')",
            "<svg onload='alert(1)'>"
        };

        foreach (var payload in xssPayloads)
        {
            var certificateData = new
            {
                Name = payload,
                CompanyId = 1,
                ProductId = 1
            };

            var response = await client.PostAsJsonAsync("/api/v1/certificates", certificateData);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                // Verify XSS payload was sanitized
                Assert.IsFalse(content.Contains("<script>"), "Script tags should be sanitized");
                Assert.IsFalse(content.Contains("javascript:"), "JavaScript protocols should be sanitized");
                Assert.IsFalse(content.Contains("onerror="), "Event handlers should be sanitized");
            }
        }
    }

    [TestMethod]
    [TestCategory("P2")]
    public async Task SessionSecurity_TokenManagement_ShouldBeSecure()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        // Login and capture session
        await LoginAsync(client, "admin@greencirclecertified.com", "Admin123!");

        // Verify secure cookie attributes
        var cookies = GetCookiesFromClient(client);
        var sessionCookie = cookies.FirstOrDefault(c => c.Name.Contains("session") || c.Name.Contains("auth"));

        Assert.IsNotNull(sessionCookie, "Session cookie should be present");
        Assert.IsTrue(sessionCookie.HttpOnly, "Session cookie should be HttpOnly");
        Assert.IsTrue(sessionCookie.Secure, "Session cookie should be Secure");
        Assert.AreEqual(SameSiteMode.Strict, sessionCookie.SameSite, "Session cookie should be SameSite=Strict");

        // Test session timeout
        await Task.Delay(TimeSpan.FromMinutes(16)); // Session timeout is 15 minutes
        var protectedResponse = await client.GetAsync("/Dashboard");
        Assert.AreEqual(HttpStatusCode.Redirect, protectedResponse.StatusCode);
    }
}
```

### 6.2 Automated Security Scanning Integration

#### 6.2.1 OWASP ZAP Integration
```yaml
# ZAP security regression testing
automation:
  name: "Security Regression Test"
  contexts:
  - name: "GCC Application"
    url: "https://localhost:7130"

  jobs:
  - type: "spider"
    parameters:
      context: "GCC Application"
      maxDepth: 10

  - type: "passiveScan"
    parameters:
      maxAlertsPerRule: 10

  - type: "activeScan"
    parameters:
      context: "GCC Application"
      strength: "High"
      threshold: "Medium"

  - type: "report"
    parameters:
      template: "traditional-json"
      reportDir: "/reports/"
      reportFile: "security-regression-report"
```

---

## 7. Test Data Management for Regression Testing

### 7.1 Test Data Strategy

#### 7.1.1 Test Data Categories
```markdown
## Regression Test Data Requirements

### Static Reference Data
- **User Accounts:** Predefined users for each role (Admin, Analyst, Internal, External)
- **Companies:** Standard set of test companies across different industries
- **Products:** Representative product catalog for testing
- **Partners:** Amazon, Wayfair, and None partner configurations

### Dynamic Test Data
- **Certificates:** Generated certificates in various states and configurations
- **Audit Logs:** Historical activity data for testing workflows
- **Performance Data:** Large datasets for performance regression testing

### Edge Case Data
- **Boundary Values:** Maximum/minimum values for all fields
- **Special Characters:** Unicode, SQL injection patterns, XSS payloads
- **Invalid Data:** Malformed data for negative testing
```

#### 7.1.2 Test Data Generation
```csharp
// Regression test data generator
public class RegressionTestDataGenerator
{
    public async Task GenerateRegressionTestDataAsync(GCCDbContext context)
    {
        // Clear existing test data
        await CleanupTestDataAsync(context);

        // Generate baseline test data
        await GenerateBaselineDataAsync(context);

        // Generate edge case data
        await GenerateEdgeCaseDataAsync(context);

        // Generate performance test data
        await GeneratePerformanceTestDataAsync(context);

        await context.SaveChangesAsync();
    }

    private async Task GenerateBaselineDataAsync(GCCDbContext context)
    {
        var faker = new Faker();

        // Generate test companies
        var companies = new List<Company>();
        for (int i = 1; i <= 50; i++)
        {
            companies.Add(new Company
            {
                Name = $"Test Company {i}",
                Industry = faker.Commerce.Categories(1).First(),
                Website = faker.Internet.Url(),
                CreatedDate = DateTime.UtcNow.AddDays(-faker.Random.Int(1, 365))
            });
        }
        context.Companies.AddRange(companies);
        await context.SaveChangesAsync();

        // Generate test certificates with predictable patterns
        var certificates = new List<Certificate>();
        var partners = new[] { Partner.None, Partner.Amazon, Partner.Wayfair };
        var statuses = new[] { CertificateStatus.Active, CertificateStatus.Draft, CertificateStatus.Expired };

        for (int i = 1; i <= 200; i++)
        {
            certificates.Add(new Certificate
            {
                Name = $"Regression Test Certificate {i}",
                CertificateNumber = $"REG-{i:D6}",
                CompanyId = faker.Random.Int(1, companies.Count),
                ProductId = faker.Random.Int(1, 20),
                Partner = partners[i % partners.Length],
                Status = statuses[i % statuses.Length],
                EffectiveDate = DateTime.UtcNow.AddDays(-faker.Random.Int(0, 730)),
                ExpirationDate = DateTime.UtcNow.AddDays(faker.Random.Int(30, 730)),
                CreatedDate = DateTime.UtcNow.AddDays(-faker.Random.Int(1, 365))
            });
        }
        context.Certificates.AddRange(certificates);
    }

    private async Task GenerateEdgeCaseDataAsync(GCCDbContext context)
    {
        // Generate certificates with edge case data
        var edgeCaseCertificates = new[]
        {
            new Certificate
            {
                Name = new string('A', 255), // Maximum length name
                CertificateNumber = "EDGE-001",
                CompanyId = 1,
                ProductId = 1,
                Status = CertificateStatus.Active,
                CreatedDate = DateTime.UtcNow
            },
            new Certificate
            {
                Name = "Certificate with Special Characters: <>\"'&",
                CertificateNumber = "EDGE-002",
                CompanyId = 1,
                ProductId = 1,
                Status = CertificateStatus.Active,
                CreatedDate = DateTime.UtcNow
            },
            new Certificate
            {
                Name = "Unicode Test Certificate: 测试证书 🌟",
                CertificateNumber = "EDGE-003",
                CompanyId = 1,
                ProductId = 1,
                Status = CertificateStatus.Active,
                CreatedDate = DateTime.UtcNow
            }
        };

        context.Certificates.AddRange(edgeCaseCertificates);
    }

    private async Task GeneratePerformanceTestDataAsync(GCCDbContext context)
    {
        // Generate large dataset for performance testing
        var performanceCertificates = new List<Certificate>();

        for (int i = 1; i <= 10000; i++)
        {
            performanceCertificates.Add(new Certificate
            {
                Name = $"Performance Test Certificate {i}",
                CertificateNumber = $"PERF-{i:D6}",
                CompanyId = (i % 50) + 1,
                ProductId = (i % 20) + 1,
                Partner = (Partner)(i % 3),
                Status = (CertificateStatus)(i % 4),
                EffectiveDate = DateTime.UtcNow.AddDays(-i),
                ExpirationDate = DateTime.UtcNow.AddDays(i),
                CreatedDate = DateTime.UtcNow.AddDays(-i)
            });

            // Add in batches to avoid memory issues
            if (i % 1000 == 0)
            {
                context.Certificates.AddRange(performanceCertificates);
                await context.SaveChangesAsync();
                performanceCertificates.Clear();
            }
        }

        if (performanceCertificates.Any())
        {
            context.Certificates.AddRange(performanceCertificates);
        }
    }
}
```

### 7.2 Test Data Isolation and Cleanup

#### 7.2.1 Test Data Isolation Strategy
```csharp
// Test data isolation service
public class TestDataIsolationService
{
    public async Task<TestDataContext> CreateIsolatedTestContextAsync(string testSuiteName)
    {
        var isolatedContext = new TestDataContext
        {
            TestSuiteId = Guid.NewGuid().ToString(),
            TestSuiteName = testSuiteName,
            CreatedAt = DateTime.UtcNow,
            DatabaseSchema = $"test_{testSuiteName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}"
        };

        // Create isolated database schema for test
        await CreateIsolatedSchemaAsync(isolatedContext.DatabaseSchema);

        // Populate with regression test data
        await PopulateTestDataAsync(isolatedContext);

        return isolatedContext;
    }

    public async Task CleanupTestDataAsync(TestDataContext testContext)
    {
        try
        {
            // Clean up test-specific data
            await RemoveTestDataAsync(testContext);

            // Drop isolated schema if created
            if (!string.IsNullOrEmpty(testContext.DatabaseSchema))
            {
                await DropIsolatedSchemaAsync(testContext.DatabaseSchema);
            }

            // Log cleanup completion
            _logger.LogInformation("Test data cleanup completed for {TestSuite}",
                testContext.TestSuiteName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup test data for {TestSuite}",
                testContext.TestSuiteName);
            throw;
        }
    }

    private async Task CreateIsolatedSchemaAsync(string schemaName)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var createSchemaCommand = $"CREATE SCHEMA [{schemaName}]";
        using var command = new SqlCommand(createSchemaCommand, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task PopulateTestDataAsync(TestDataContext testContext)
    {
        // Create test-specific data that won't interfere with other tests
        var testDataGenerator = new RegressionTestDataGenerator();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GCCDbContext>();

        // Override connection to use isolated schema
        if (!string.IsNullOrEmpty(testContext.DatabaseSchema))
        {
            context.Database.GetDbConnection().ConnectionString =
                GetIsolatedConnectionString(testContext.DatabaseSchema);
        }

        await testDataGenerator.GenerateRegressionTestDataAsync(context);
    }
}
```

---

## 8. Regression Test Reporting and Analysis

### 8.1 Regression Test Reporting Framework

#### 8.1.1 Comprehensive Test Report Generation
```csharp
// Regression test report generator
public class RegressionTestReportGenerator
{
    public async Task<RegressionTestReport> GenerateComprehensiveReportAsync(TestExecutionResults results)
    {
        var report = new RegressionTestReport
        {
            GeneratedAt = DateTime.UtcNow,
            TestExecutionId = results.ExecutionId,
            OverallSummary = GenerateOverallSummary(results),
            ComponentSummaries = await GenerateComponentSummariesAsync(results),
            PerformanceAnalysis = await GeneratePerformanceAnalysisAsync(results),
            SecurityAnalysis = await GenerateSecurityAnalysisAsync(results),
            TrendAnalysis = await GenerateTrendAnalysisAsync(results),
            RecommendedActions = GenerateRecommendedActions(results)
        };

        return report;
    }

    private TestSummary GenerateOverallSummary(TestExecutionResults results)
    {
        return new TestSummary
        {
            TotalTests = results.AllTests.Count,
            PassedTests = results.PassedTests.Count,
            FailedTests = results.FailedTests.Count,
            SkippedTests = results.SkippedTests.Count,
            PassRate = (double)results.PassedTests.Count / results.AllTests.Count * 100,
            ExecutionTime = results.TotalExecutionTime,
            NewFailures = results.NewFailures.Count,
            FixedIssues = results.FixedIssues.Count,
            RegressionDetected = results.HasRegressions
        };
    }

    private async Task<List<ComponentSummary>> GenerateComponentSummariesAsync(TestExecutionResults results)
    {
        var componentSummaries = new List<ComponentSummary>();
        var componentsAffected = results.AllTests.Select(t => t.Component).Distinct();

        foreach (var component in componentsAffected)
        {
            var componentTests = results.AllTests.Where(t => t.Component == component).ToList();
            var componentFailures = results.FailedTests.Where(t => t.Component == component).ToList();

            var summary = new ComponentSummary
            {
                ComponentName = component,
                TotalTests = componentTests.Count,
                PassedTests = componentTests.Count - componentFailures.Count,
                FailedTests = componentFailures.Count,
                PassRate = (double)(componentTests.Count - componentFailures.Count) / componentTests.Count * 100,
                CriticalFailures = componentFailures.Count(f => f.Severity == TestFailureSeverity.Critical),
                RiskLevel = CalculateComponentRiskLevel(component, componentFailures)
            };

            componentSummaries.Add(summary);
        }

        return componentSummaries.OrderByDescending(c => c.RiskLevel).ToList();
    }

    private async Task<PerformanceAnalysis> GeneratePerformanceAnalysisAsync(TestExecutionResults results)
    {
        var performanceTests = results.AllTests.Where(t => t.Category == "Performance").ToList();

        return new PerformanceAnalysis
        {
            PerformanceTestsExecuted = performanceTests.Count,
            PerformanceRegressions = await IdentifyPerformanceRegressionsAsync(performanceTests),
            PerformanceImprovements = await IdentifyPerformanceImprovementsAsync(performanceTests),
            BaselineComparison = await CompareWithBaselineAsync(performanceTests),
            TrendData = await GetPerformanceTrendDataAsync(performanceTests)
        };
    }

    private List<RecommendedAction> GenerateRecommendedActions(TestExecutionResults results)
    {
        var actions = new List<RecommendedAction>();

        // High priority actions for critical failures
        var criticalFailures = results.FailedTests.Where(f => f.Severity == TestFailureSeverity.Critical);
        if (criticalFailures.Any())
        {
            actions.Add(new RecommendedAction
            {
                Priority = ActionPriority.Critical,
                Description = $"Address {criticalFailures.Count()} critical test failures immediately",
                EstimatedEffort = "1-2 days",
                Impact = "High - blocks release"
            });
        }

        // Performance regression actions
        if (results.HasPerformanceRegressions)
        {
            actions.Add(new RecommendedAction
            {
                Priority = ActionPriority.High,
                Description = "Investigate and fix performance regressions",
                EstimatedEffort = "2-3 days",
                Impact = "Medium - affects user experience"
            });
        }

        // Test coverage improvements
        var coverageGaps = IdentifyCoverageGaps(results);
        if (coverageGaps.Any())
        {
            actions.Add(new RecommendedAction
            {
                Priority = ActionPriority.Medium,
                Description = $"Improve test coverage in {string.Join(", ", coverageGaps)}",
                EstimatedEffort = "1 week",
                Impact = "Medium - prevents future regressions"
            });
        }

        return actions.OrderBy(a => a.Priority).ToList();
    }
}
```

#### 8.1.2 Automated Report Distribution
```csharp
// Automated report distribution service
public class RegressionReportDistributionService
{
    public async Task DistributeReportAsync(RegressionTestReport report)
    {
        // Generate different report formats for different audiences
        var executiveSummary = await GenerateExecutiveSummaryAsync(report);
        var technicalReport = await GenerateTechnicalReportAsync(report);
        var dashboardUpdate = await GenerateDashboardUpdateAsync(report);

        // Distribute to appropriate stakeholders
        await DistributeExecutiveSummaryAsync(executiveSummary);
        await DistributeTechnicalReportAsync(technicalReport);
        await UpdateDashboardAsync(dashboardUpdate);

        // Send alerts for critical issues
        if (report.OverallSummary.RegressionDetected)
        {
            await SendRegressionAlertAsync(report);
        }
    }

    private async Task<ExecutiveSummary> GenerateExecutiveSummaryAsync(RegressionTestReport report)
    {
        return new ExecutiveSummary
        {
            Title = $"Regression Test Results - {report.GeneratedAt:yyyy-MM-dd}",
            OverallStatus = DetermineOverallStatus(report),
            KeyMetrics = new Dictionary<string, string>
            {
                ["Pass Rate"] = $"{report.OverallSummary.PassRate:F1}%",
                ["Execution Time"] = $"{report.OverallSummary.ExecutionTime.TotalMinutes:F0} minutes",
                ["New Failures"] = report.OverallSummary.NewFailures.ToString(),
                ["Regressions"] = report.OverallSummary.RegressionDetected ? "Yes" : "No"
            },
            CriticalIssues = report.RecommendedActions.Where(a => a.Priority == ActionPriority.Critical).ToList(),
            NextSteps = GenerateExecutiveNextSteps(report)
        };
    }

    private async Task SendRegressionAlertAsync(RegressionTestReport report)
    {
        var alertMessage = new RegressionAlert
        {
            Severity = AlertSeverity.High,
            Subject = "Regression Detected in Automated Testing",
            Message = $"Regression testing has detected {report.OverallSummary.NewFailures} new failures. " +
                     $"Overall pass rate: {report.OverallSummary.PassRate:F1}%",
            ActionRequired = "Immediate investigation and remediation required",
            ReportUrl = await _reportStorage.GetReportUrlAsync(report.TestExecutionId)
        };

        // Send to appropriate channels
        await _slackService.SendAlertAsync(alertMessage);
        await _emailService.SendAlertAsync(alertMessage, GetAlertRecipients());
        await _teamsService.SendAlertAsync(alertMessage);
    }
}
```

### 8.2 Trend Analysis and Predictive Analytics

#### 8.2.1 Regression Trend Analysis
```csharp
// Regression trend analysis service
public class RegressionTrendAnalysisService
{
    public async Task<TrendAnalysisReport> AnalyzeRegressionTrendsAsync(TimeSpan analysisWindow)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.Subtract(analysisWindow);

        var historicalResults = await _testResultRepository.GetResultsInDateRangeAsync(startDate, endDate);

        var trendReport = new TrendAnalysisReport
        {
            AnalysisPeriod = new DateRange(startDate, endDate),
            QualityTrends = AnalyzeQualityTrends(historicalResults),
            ComponentStability = AnalyzeComponentStability(historicalResults),
            PerformanceTrends = AnalyzePerformanceTrends(historicalResults),
            PredictiveInsights = GeneratePredictiveInsights(historicalResults),
            RecommendedImprovements = GenerateImprovementRecommendations(historicalResults)
        };

        return trendReport;
    }

    private QualityTrends AnalyzeQualityTrends(List<TestExecutionResults> results)
    {
        var trends = new QualityTrends();

        // Calculate rolling averages and trends
        var passRates = results.Select(r => (double)r.PassedTests.Count / r.AllTests.Count * 100).ToList();
        trends.PassRateTrend = CalculateTrendDirection(passRates);
        trends.AveragePassRate = passRates.Average();

        // Analyze failure patterns
        var failureCounts = results.Select(r => r.FailedTests.Count).ToList();
        trends.FailureCountTrend = CalculateTrendDirection(failureCounts);
        trends.AverageFailures = failureCounts.Average();

        // Identify most problematic components
        var componentFailures = results.SelectMany(r => r.FailedTests)
                                      .GroupBy(f => f.Component)
                                      .OrderByDescending(g => g.Count())
                                      .Take(5)
                                      .ToDictionary(g => g.Key, g => g.Count());

        trends.ProblematicComponents = componentFailures;

        return trends;
    }

    private List<PredictiveInsight> GeneratePredictiveInsights(List<TestExecutionResults> results)
    {
        var insights = new List<PredictiveInsight>();

        // Predict potential stability issues
        var componentStabilityScores = CalculateComponentStabilityScores(results);
        var unstableComponents = componentStabilityScores.Where(kvp => kvp.Value < 0.8).ToList();

        if (unstableComponents.Any())
        {
            insights.Add(new PredictiveInsight
            {
                Type = InsightType.StabilityRisk,
                Confidence = 0.85,
                Description = $"Components {string.Join(", ", unstableComponents.Select(c => c.Key))} " +
                             "show declining stability patterns and may require attention",
                RecommendedAction = "Increase test coverage and code review focus for these components",
                PredictedImpact = ImpactLevel.Medium
            });
        }

        // Predict performance degradation
        var performanceMetrics = ExtractPerformanceMetrics(results);
        if (HasPerformanceDegradationTrend(performanceMetrics))
        {
            insights.Add(new PredictiveInsight
            {
                Type = InsightType.PerformanceRisk,
                Confidence = 0.75,
                Description = "Performance metrics show gradual degradation trend",
                RecommendedAction = "Conduct performance profiling and optimization review",
                PredictedImpact = ImpactLevel.High
            });
        }

        return insights;
    }

    private TrendDirection CalculateTrendDirection(List<double> values)
    {
        if (values.Count < 2) return TrendDirection.Stable;

        var n = values.Count;
        var slope = CalculateLinearRegressionSlope(values);

        if (slope > 0.1) return TrendDirection.Improving;
        if (slope < -0.1) return TrendDirection.Declining;
        return TrendDirection.Stable;
    }
}
```

---

## 9. Appendices

### Appendix A: Regression Test Checklists

#### A.1 Pre-Regression Testing Checklist
```markdown
## Pre-Regression Testing Checklist

### Environment Preparation
- [ ] Test environment deployed with correct application version
- [ ] Database schema updated to latest version
- [ ] Test data refreshed and validated
- [ ] External dependencies (Azure AD B2C) accessible
- [ ] Monitoring and logging enabled
- [ ] Performance baseline established

### Test Suite Validation
- [ ] Automated test suite compilation successful
- [ ] Test selection algorithm validated
- [ ] Risk-based test prioritization reviewed
- [ ] Test data dependencies verified
- [ ] Test execution timeboxes confirmed

### Tool Configuration
- [ ] Test automation tools updated to latest versions
- [ ] CI/CD pipeline configuration validated
- [ ] Report generation templates updated
- [ ] Notification channels configured
- [ ] Dashboard integration tested

### Stakeholder Communication
- [ ] Regression testing schedule communicated
- [ ] Expected duration and scope shared
- [ ] Escalation procedures reviewed
- [ ] Report distribution list updated
```

#### A.2 Post-Regression Testing Checklist
```markdown
## Post-Regression Testing Checklist

### Results Analysis
- [ ] Test execution results reviewed
- [ ] Failure analysis completed
- [ ] Regression detection validated
- [ ] Performance comparison analyzed
- [ ] Security regression check completed

### Reporting and Communication
- [ ] Comprehensive test report generated
- [ ] Executive summary prepared
- [ ] Technical findings documented
- [ ] Stakeholder notifications sent
- [ ] Dashboard metrics updated

### Follow-up Actions
- [ ] Critical issues escalated
- [ ] Bug reports created for failures
- [ ] Performance baselines updated
- [ ] Test suite improvements identified
- [ ] Lessons learned documented

### Environment Cleanup
- [ ] Test data cleaned up
- [ ] Temporary resources released
- [ ] Log files archived
- [ ] Environment status updated
```

### Appendix B: Risk Assessment Templates

#### B.1 Component Risk Assessment Matrix
```markdown
## Component Risk Assessment Matrix

### Certificate Management Module
| Risk Factor | Score | Weight | Weighted Score |
|-------------|-------|--------|----------------|
| Business Criticality | 5 | 30% | 1.5 |
| Change Frequency | 4 | 25% | 1.0 |
| User Impact | 5 | 20% | 1.0 |
| Technical Complexity | 3 | 15% | 0.45 |
| Historical Defects | 3 | 10% | 0.3 |
| **Total Risk Score** | | | **4.25** |

**Risk Level:** HIGH
**Recommended Testing:** P1 - Every build, comprehensive coverage

### Authentication Module
| Risk Factor | Score | Weight | Weighted Score |
|-------------|-------|--------|----------------|
| Business Criticality | 5 | 30% | 1.5 |
| Change Frequency | 2 | 25% | 0.5 |
| User Impact | 5 | 20% | 1.0 |
| Technical Complexity | 4 | 15% | 0.6 |
| Historical Defects | 2 | 10% | 0.2 |
| **Total Risk Score** | | | **3.8** |

**Risk Level:** HIGH
**Recommended Testing:** P1 - Every build, security focus
```

### Appendix C: Performance Baseline Templates

#### C.1 Performance Baseline Template
```markdown
## Performance Baseline - Version {Version}

### Baseline Metrics
| Metric | Target | Baseline | Regression Threshold |
|--------|--------|----------|---------------------|
| **Page Load - Certificate Tile View** | <3s | 2.1s | 2.5s |
| **API Response - Get Certificates** | <500ms | 280ms | 322ms |
| **Certificate Search** | <500ms | 340ms | 408ms |
| **Certificate Creation** | <2s | 1.4s | 1.68s |
| **PDF Generation** | <5s | 3.2s | 3.84s |
| **Dashboard Load** | <3s | 2.8s | 3.36s |

### Test Environment Configuration
- **Application Server:** 4 CPU, 8GB RAM
- **Database Server:** 8 CPU, 16GB RAM
- **Test Data Volume:** 10,000 certificates
- **Concurrent Users:** 50 virtual users
- **Test Duration:** 30 minutes

### Notes
- Baseline established with clean environment
- No other applications running during test
- Database recently optimized with updated statistics
- Network latency: <10ms
```

---

**Document Control:**
- **Version:** 1.0
- **Created Date:** September 17, 2025
- **Last Updated:** September 17, 2025
- **Next Review Date:** December 17, 2025
- **Document Owner:** QA Lead
- **Approved By:** QA Manager
- **Classification:** INTERNAL USE ONLY

**Change History:**
| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-09-17 | QA Team | Initial creation of comprehensive regression testing strategy |
# GCC Web App - Comprehensive Test Suite

## Overview
This test suite provides comprehensive coverage for the GCC Web Application, including role-based access control, security testing, performance validation, and UI automation.

## Test Categories

### 🔐 Role-Based Tests
- **Admin Role Tests**: Full system access validation
- **Analyst Role Tests**: Limited admin access verification  
- **External Role Tests**: External user experience testing
- **Public Role Tests**: Unauthenticated user validation

### 🛡️ Security Tests
- SQL injection prevention
- XSS attack prevention
- CSRF protection validation
- Session security verification
- Password security checks
- Account lockout testing
- Error message security

### ⚡ Performance Tests
- Page load time validation
- Asset loading optimization
- Concurrent user simulation
- Navigation responsiveness
- Search functionality speed

### 🔒 Cross-Role Security Tests
- Privilege escalation prevention
- Direct URL access blocking
- Role boundary enforcement
- API endpoint authorization

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- PowerShell (for browser installation)
- GCC Web App running locally

### Running Tests

#### Quick Test (Development)
```bash
.\run-quick-tests.cmd
```

#### Comprehensive Test Suite
```bash
.\run-all-tests.cmd
```

#### Individual Test Categories
```bash
# Admin role tests only
dotnet test --filter "FullyQualifiedName~AdminRole"

# Security tests only
dotnet test --filter "FullyQualifiedName~Security"

# Performance tests only
dotnet test --filter "FullyQualifiedName~Performance"
```

#### Single Test
```bash
dotnet test --filter "TestName~AdminLogin_ShouldAccessDashboard"
```

## Test Configuration

### Test Users
The test suite uses these predefined users:
- **Admin**: admin@test.com / TestAdmin123!
- **Analyst**: analyst@test.com / TestAnalyst123!
- **External**: external@test.com / TestExternal123!
- **Public**: Unauthenticated access

### Configuration Files
- `appsettings.Test.json` - Test environment configuration
- `GCCWebApp.RoleBasedUI.Tests.csproj` - Project dependencies

## Test Results

### Generated Artifacts
- **Test Reports**: `test-results/*.trx` (MSTest format)
- **Screenshots**: `Screenshots/[Role]/[TestName]_[Timestamp].png`
- **HTML Summary**: `test-results/summary.html`
- **Console Output**: Real-time test execution feedback

### Screenshot Organization
```
Screenshots/
├── Admin/          # Admin role test screenshots
├── Analyst/        # Analyst role test screenshots  
├── External/       # External role test screenshots
├── Public/         # Public role test screenshots
├── Security/       # Security test screenshots
└── Performance/    # Performance test screenshots
```

## Test Infrastructure

### Key Components
- **TestWebApplicationFactory**: In-memory test environment setup
- **RoleTestHelper**: Common role-based testing utilities
- **Playwright**: Browser automation framework
- **NUnit**: Test execution framework

### Test Data
- In-memory SQLite database
- Seeded test users and companies
- Isolated test environment per test run

## CI/CD Integration

### GitHub Actions Example
```yaml
- name: Run Comprehensive Tests
  run: |
    cd GCCWebApp.RoleBasedUI.Tests
    .\run-all-tests.cmd
    
- name: Upload Test Results
  uses: actions/upload-artifact@v3
  if: always()
  with:
    name: test-results
    path: GCCWebApp.RoleBasedUI.Tests/test-results/
    
- name: Upload Screenshots
  uses: actions/upload-artifact@v3
  if: always()
  with:
    name: screenshots
    path: GCCWebApp.RoleBasedUI.Tests/Screenshots/
```

### Azure DevOps Example
```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Role-Based UI Tests'
  inputs:
    command: 'test'
    projects: 'GCCWebApp.RoleBasedUI.Tests/*.csproj'
    arguments: '--logger trx --results-directory $(Agent.TempDirectory)'
```

## Test Coverage

### Functional Coverage
- ✅ User authentication and authorization
- ✅ Role-based page access control  
- ✅ Navigation menu customization
- ✅ Form submission authorization
- ✅ API endpoint security
- ✅ Session management
- ✅ Error handling

### Security Coverage
- ✅ SQL injection prevention
- ✅ XSS attack prevention
- ✅ CSRF protection
- ✅ Session security
- ✅ Password policies
- ✅ Account lockout mechanisms
- ✅ Information disclosure prevention

### Performance Coverage
- ✅ Page load times
- ✅ Asset optimization
- ✅ Concurrent user handling
- ✅ Search performance
- ✅ Navigation responsiveness

## Troubleshooting

### Common Issues

#### Playwright Browser Not Found
```bash
pwsh -Command "& {npx playwright install chromium}"
```

#### Test Database Issues
Tests use in-memory database that's recreated for each test run. No cleanup required.

#### Port Conflicts
Update `appsettings.Test.json` if the default port conflicts with your setup.

#### Screenshot Permissions
Ensure the test runner has write permissions to the Screenshots directory.

### Debug Mode
For detailed test output:
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Test Timeout Issues
Increase timeouts in test configuration:
```json
{
  "TestSettings": {
    "DefaultTimeout": 60000
  }
}
```

## Extending the Test Suite

### Adding New Role Tests
1. Create new test file in appropriate role directory
2. Inherit from `PageTest`
3. Use `RoleTestHelper` for common operations
4. Add screenshot capture for visual validation

### Adding New Security Tests
1. Add test method to `SecurityTests.cs`
2. Focus on specific attack vectors
3. Validate prevention mechanisms
4. Document expected behavior

### Adding Performance Benchmarks
1. Add test method to `PerformanceTests.cs`
2. Set realistic performance thresholds
3. Use consistent measurement approaches
4. Consider concurrent user scenarios

## Best Practices

### Test Organization
- Group related tests in logical test classes
- Use descriptive test method names
- Include setup and teardown appropriately
- Maintain test isolation

### Screenshot Strategy
- Capture screenshots for key user journeys
- Include error states and access violations
- Organize by test category and role
- Use consistent naming conventions

### Test Maintenance
- Regularly update test user credentials
- Review performance thresholds quarterly
- Update security tests for new attack vectors
- Maintain test environment consistency

## Support

### Documentation
- Comprehensive inline code comments
- Test method documentation
- Configuration file examples
- Troubleshooting guides

### Monitoring
- Regular test execution in CI/CD
- Performance threshold monitoring
- Security vulnerability scanning
- Test result trend analysis

## Compliance & Auditing

### Audit Trail
- Complete test execution logs
- Screenshot evidence of functionality
- Security validation documentation
- Performance benchmark records

### Compliance Reporting
- Role-based access validation
- Security control verification
- Performance standard compliance
- Quality assurance documentation

---

**Created**: August 22, 2025  
**Version**: 1.0  
**Framework**: .NET 8.0 + Playwright + NUnit  
**Coverage**: Role-Based Access, Security, Performance, UI Automation
# GCC Web App Accessibility Tests

This project contains comprehensive accessibility tests for the GreenCircle Certified Web Application, ensuring compliance with ADA, WCAG 2.1 Level AA, and Section 508 standards.

## Overview

The accessibility test suite uses:
- **Playwright** for browser automation
- **axe-core** for accessibility rule engine
- **WCAG 2.1 Level AA** standards
- **Section 508** compliance testing

## Test Structure

### Core Components
- `BaseAccessibilityTest.cs` - Base test class with common functionality
- `AccessibilityTestWebApplicationFactory.cs` - Test application factory

### Test Categories

#### Pages Tests (`Tests/Pages/`)
- **HomePageAccessibilityTests** - Home page accessibility
- **CertificatePageAccessibilityTests** - Certificate-related pages
- **AdminPageAccessibilityTests** - Admin panel accessibility

#### Component Tests (`Tests/Components/`)
- **NavigationAccessibilityTests** - Navigation components
- **FormAccessibilityTests** - Form controls and validation

## Running Tests

### Quick Start
```bash
# Run all accessibility tests
dotnet test

# Or use the batch file
./run-accessibility-tests.cmd
```

### Advanced Usage
```bash
# Run specific test class
dotnet test --filter "ClassName=HomePageAccessibilityTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Test Features

### Automated Checks
- **WCAG 2.1 Level AA** compliance
- **Section 508** compliance  
- **Color contrast** validation
- **Keyboard navigation** testing
- **Screen reader** compatibility
- **Form accessibility** validation
- **Image alt text** verification
- **Heading structure** validation

### Accessibility Rules Tested
- `color-contrast` - Text color contrast ratios
- `image-alt` - Images have alt text
- `label` - Form controls have labels
- `heading-order` - Proper heading hierarchy
- `button-name` - Buttons have accessible names
- `link-name` - Links have accessible names
- `aria-*` - ARIA attributes are valid
- `table-header` - Data tables have headers
- `focus-order-semantics` - Focus order is logical

### Role-Based Testing
Tests accommodate the application's role-based UI:
- **Public** users (limited access)
- **External** users (certificate viewing)
- **Analyst** users (data management)
- **Admin** users (full system access)

## Test Configuration

### Browser Configuration
Tests run in headless Chromium by default. Configure in `BaseAccessibilityTest.cs`:
```csharp
_browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = true, // Set to false for debugging
    Args = new[] { "--disable-web-security", "--allow-running-insecure-content" }
});
```

### Accessibility Standards
```csharp
// WCAG 2.1 Level AA
var results = await RunWCAG21AACheck(url);

// Section 508
var results = await RunSection508Check(url);

// Custom rule sets
var results = await RunAccessibilityCheck(url, new AxeOptions
{
    Rules = new[] { "color-contrast", "image-alt" }
});
```

## Reports and Results

### Violation Reports
When violations are found, detailed JSON reports are saved to the `Reports/` folder:
- `TestName_violations.json` - Detailed violation information
- Includes specific HTML elements, failure descriptions, and remediation guidance

### Report Structure
```json
{
  "TestName": "HomePage_WCAG21AA",
  "Timestamp": "2025-01-15T10:30:00Z",
  "Url": "https://localhost:7265/",
  "ViolationCount": 2,
  "Violations": [
    {
      "Id": "color-contrast",
      "Description": "Ensure text has sufficient color contrast",
      "Impact": "serious",
      "Help": "Color contrast must meet WCAG AA standards",
      "HelpUrl": "https://dequeuniversity.com/rules/axe/4.4/color-contrast",
      "Nodes": [...]
    }
  ]
}
```

## Integration with CI/CD

### GitHub Actions Example
```yaml
- name: Run Accessibility Tests
  run: |
    cd GCCWebApp.Accessibility.Tests
    dotnet test --logger "trx" --results-directory TestResults
    
- name: Upload Test Results
  uses: actions/upload-artifact@v3
  with:
    name: accessibility-test-results
    path: GCCWebApp.Accessibility.Tests/TestResults/
```

### Azure DevOps Example
```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Accessibility Tests'
  inputs:
    command: 'test'
    projects: 'GCCWebApp.Accessibility.Tests/*.csproj'
    arguments: '--logger trx --results-directory $(Agent.TempDirectory)'
```

## Best Practices

### Test Organization
1. **Page-level tests** for full page accessibility
2. **Component-level tests** for reusable UI components
3. **Role-based tests** for different user contexts
4. **Responsive tests** for mobile accessibility

### Assertion Strategies
```csharp
// Strict compliance - fail on any violation
AssertNoViolations(results, "TestName");

// Tolerance - allow limited violations during development
AssertMaxViolations(results, "TestName", maxViolations: 3);
```

### Custom Test Scenarios
```csharp
// Test specific user flows
[Fact]
public async Task CertificateSearch_ShouldBeAccessible()
{
    await _page.GotoAsync("/Certificate");
    await _page.FillAsync("[data-testid='search']", "organic");
    await _page.ClickAsync("[data-testid='search-button']");
    
    var results = await RunWCAG21AACheck(_page.Url);
    AssertNoViolations(results, "CertificateSearch");
}
```

## Troubleshooting

### Common Issues

1. **Browser not found**
   ```bash
   # Install Playwright browsers
   pwsh bin/Debug/net9.0/playwright.ps1 install
   ```

2. **Application not starting**
   - Ensure the main application builds successfully
   - Check database connections in test configuration

3. **False positives**
   - Use `Include`/`Exclude` options to focus tests
   - Customize rules for specific scenarios

### Debug Mode
Set `Headless = false` in browser configuration to watch tests run:
```csharp
_browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false,
    SlowMo = 1000 // Slow down for visibility
});
```

## Manual Testing Complement

These automated tests should be complemented with:
- **Screen reader testing** (NVDA, JAWS, VoiceOver)
- **Keyboard-only navigation** testing
- **High contrast mode** testing
- **Zoom level** testing (up to 200%)
- **Real user testing** with disabilities

## Resources

- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [axe-core Rules](https://github.com/dequelabs/axe-core/blob/develop/doc/rule-descriptions.md)
- [Section 508 Standards](https://www.section508.gov/)
- [Playwright Documentation](https://playwright.dev/dotnet/)

## Support

For questions about accessibility testing or to report issues:
1. Review existing test reports in the `Reports/` folder
2. Check the application's main accessibility documentation
3. Consult WCAG 2.1 guidelines for specific requirements
# Getting Started with GCC Accessibility Tests

## Quick Start

The accessibility test project has been successfully set up but requires some final configuration due to package compatibility issues discovered during setup.

## Current Status

✅ **Project Structure Created**
- GCCWebApp.Accessibility.Tests project added to solution
- Test directories organized (Tests/Pages, Tests/Components, Tests/Core)
- Documentation and batch files created

✅ **Dependencies Added**
- Microsoft.Playwright (browser automation)
- Selenium.Axe (accessibility testing - alternative to Playwright.Axe)
- Microsoft.AspNetCore.Mvc.Testing (integration testing)

✅ **Documentation Complete**
- Comprehensive README with testing procedures
- CI/CD integration guide
- Manual testing procedures guide

## Next Steps Required

### 1. Fix Package Compatibility
The Playwright.Axe package had compatibility issues. Two options:

**Option A: Use Selenium with axe-core**
```bash
# Already installed
dotnet add package Selenium.Axe
dotnet add package Selenium.WebDriver.ChromeDriver
```

**Option B: Use Playwright with manual axe injection**
```bash
# Keep Playwright, inject axe-core JavaScript manually
# More complex but keeps Playwright benefits
```

### 2. Update Test Implementation
Based on chosen approach, update the base test class in:
- `Tests/Core/BaseAccessibilityTest.cs`

### 3. Run Initial Tests
```bash
# Build and test
dotnet build
dotnet test
```

## Recommended Implementation Path

### Option 1: Selenium + Axe (Recommended for Quick Start)

Update `BaseAccessibilityTest.cs` to use:
```csharp
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Selenium.Axe;

// Use ChromeDriver with Selenium.Axe for accessibility testing
var driver = new ChromeDriver();
var axeResult = driver.Analyze();
```

### Option 2: Playwright + Manual Axe Integration

Keep Playwright and inject axe-core JavaScript:
```csharp
// Inject axe-core into page
await page.AddScriptTagAsync(new PageAddScriptTagOptions 
{ 
    Url = "https://unpkg.com/axe-core@latest/axe.min.js" 
});

// Run axe analysis
var results = await page.EvaluateAsync("axe.run()");
```

## Project Structure

```
GCCWebApp.Accessibility.Tests/
├── Fixtures/                     # Test setup and factories
├── Tests/
│   ├── Core/                    # Base test classes
│   ├── Pages/                   # Page-specific tests
│   └── Components/              # Component tests
├── Reports/                     # Test violation reports
├── README.md                    # Full documentation
├── CICD_INTEGRATION.md         # CI/CD setup guide
├── MANUAL_TESTING_GUIDE.md     # Manual testing procedures
└── run-accessibility-tests.cmd # Batch runner
```

## Test Coverage Planned

### Automated Tests
- **WCAG 2.1 Level AA** compliance
- **Section 508** compliance
- **Color contrast** validation
- **Keyboard navigation** testing
- **Form accessibility** validation
- **Image alt text** verification
- **Heading structure** validation

### Pages Covered
- Home page
- Certificate pages (list, details, search)
- Admin interface (user management, product management)
- Authentication pages

### Components Covered
- Navigation (main menu, breadcrumbs, dropdowns)
- Forms (inputs, validation, labels)
- Data tables
- Modal dialogs
- Search functionality

## Configuration

### Test Configuration
- Tests configured to run against local development server
- Headless browser mode for CI/CD
- Detailed JSON reporting for violations
- Integration with existing test infrastructure

### Browser Support
- Chromium (primary)
- Firefox (optional)
- WebKit/Safari (optional)

## Integration Points

### Existing Test Projects
- Extends current test structure alongside:
  - GCCWebApp.Tests (unit tests)
  - GCCWebApp.UAT.Tests (user acceptance)
  - GCCWebApp.Security.Tests (security tests)
  - GCCWebApp.Performance.Tests (performance tests)

### CI/CD Ready
- GitHub Actions workflow examples
- Azure DevOps pipeline templates
- Jenkins integration guide
- Automated reporting and notifications

## Compliance Standards

### Primary Standards
- **ADA** (Americans with Disabilities Act)
- **WCAG 2.1 Level AA** (Web Content Accessibility Guidelines)
- **Section 508** (US Federal accessibility requirements)

### Key Test Areas
- **Perceivable**: Text alternatives, captions, adaptable content
- **Operable**: Keyboard accessible, no seizures, navigable
- **Understandable**: Readable, predictable, input assistance
- **Robust**: Compatible with assistive technologies

## Next Actions

1. **Choose implementation approach** (Selenium vs Playwright)
2. **Update base test class** with chosen approach
3. **Run sample tests** to verify setup
4. **Customize tests** for your specific pages and components
5. **Integrate with CI/CD** using provided templates
6. **Train team** on accessibility testing procedures

## Support Resources

- `/README.md` - Complete testing documentation
- `/MANUAL_TESTING_GUIDE.md` - Manual accessibility testing procedures
- `/CICD_INTEGRATION.md` - CI/CD integration instructions
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [axe-core Rules](https://github.com/dequelabs/axe-core/blob/develop/doc/rule-descriptions.md)

The foundation is solid and ready for accessibility testing implementation!
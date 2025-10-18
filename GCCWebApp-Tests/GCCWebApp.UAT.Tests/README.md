# GCC Web App UAT Tests

This project contains User Acceptance Tests (UAT) with comprehensive screenshot capabilities for the GCC Certificate Database Web Application using Playwright and NUnit.

## 🚀 Quick Start

### Step 1: Start the GCC Web Application
```cmd
# Navigate to the main web app folder
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"

# Start the application
dotnet run
```
**Wait for the message:** `Now listening on: https://localhost:7248`

### Step 2: Run Your First Test
```cmd
# Navigate to the UAT test folder
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp.UAT.Tests"

# Run a quick sample test
test-sample.cmd
```

### Step 3: View Results
- **Screenshots**: Check the `Screenshots/` folder for captured images
- **Console Output**: Review test results in the command window
- **Test Reports**: Find detailed reports in `test-results/` folder

## 📋 Prerequisites

1. ✅ **.NET 8.0 SDK** (already installed - your project uses .NET 8)
2. ✅ **Playwright browsers** (automatically installed - already done)
3. ✅ **GCC Web App** running at `https://localhost:7248`

## 📁 Project Structure

```
GCCWebApp.UAT.Tests/
├── 📄 run-tests.cmd         # Run all tests
├── 📄 test-sample.cmd       # Run single test for verification
├── 📄 appsettings.Test.json # Test configuration
├── 📄 playwright.config.json # Browser settings
├── 📂 PageObjects/          # Page interaction classes
│   ├── HomePage.cs          # Home page interactions
│   ├── LoginPage.cs         # Login/authentication
│   └── CertificatePage.cs   # Certificate views
├── 📂 Helpers/              # Test utilities
│   └── TestDataHelper.cs    # Test data constants
├── 📂 Tests/                # Test suites
│   ├── Authentication/      # Login/logout tests
│   ├── Certificate/         # Certificate functionality  
│   └── UserJourneys/        # End-to-end scenarios
├── 📂 Screenshots/          # 📸 Generated screenshots
└── 📂 test-results/         # Test reports and videos
```

## 🎯 Available Test Scenarios

### 1. Public User Journeys (`PublicUserJourneyTests.cs`)
- ✅ Browse and search certificates without login
- ✅ Switch between tile and list views
- ✅ Apply filters and sorting (if available)
- 📸 **Screenshots**: Homepage, search results, certificate details

### 2. Authentication Tests (`LoginTests.cs`)  
- ✅ Valid credential login
- ✅ Invalid credential handling
- ✅ Forgot password workflow
- ✅ Role-based access control
- 📸 **Screenshots**: Login forms, success/error states

### 3. Certificate View Tests (`CertificateViewTests.cs`)
- ✅ Certificate details display
- ✅ Tile view functionality
- ✅ External vs Internal view access
- 📸 **Screenshots**: All certificate view modes

## 🏃‍♂️ Running Tests

### Easy Method - Use Scripts
```cmd
# Run single test to verify setup
test-sample.cmd

# Run all UAT tests
run-tests.cmd
```

### Command Line Options
```cmd
# Run all tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "ClassName=PublicUserJourneyTests"

# Run specific test method
dotnet test --filter "TestName~CanBrowseAndSearchCertificates"

# Run tests for specific feature
dotnet test --filter "FullyQualifiedName~Authentication"
```

### Visual Studio Method
1. Open `GCCWebApp.sln` in Visual Studio
2. Build the solution (`Ctrl+Shift+B`)
3. Open **Test Explorer** (`Test → Test Explorer`)
4. Right-click any test and select **Run**

## ⚙️ Configuration

### Application URL Configuration
Edit `appsettings.Test.json`:
```json
{
  "BaseUrl": "https://localhost:7248"  // Change if your app runs on different port
}
```

### Test User Credentials
The tests use these default credentials (update as needed):
```json
"TestUsers": {
  "Admin": { "Email": "admin@test.com", "Password": "TestPass123!" },
  "Analyst": { "Email": "analyst@test.com", "Password": "TestPass123!" },
  "External": { "Email": "external@test.com", "Password": "TestPass123!" }
}
```

### Browser Settings
```json
"TestSettings": {
  "BrowserHeadless": false,    // Set true to hide browser during tests
  "ScreenshotOnFailure": true, // Always capture failure screenshots
  "DefaultTimeout": 30000      // 30 second timeout for actions
}
```

## 📸 Screenshot Strategy

### Automatic Captures
- 🟢 **Test Start**: Initial page state
- 🔄 **Major Actions**: Navigation, form submission, clicks
- 🔍 **Verification Points**: Before/after assertions
- ❌ **Test Failures**: Full page + error details
- ✅ **Test Completion**: Final state

### Screenshot Naming Convention
```
Screenshots/
├── TestName_description_20240108_143052.png
├── PublicUser_CanBrowseAndSearch_homepage_loaded_20240108_143053.png
├── PublicUser_CanBrowseAndSearch_search_completed_20240108_143055.png
└── PublicUser_CanBrowseAndSearch_test_completed_20240108_143058.png
```

## 🔧 Specific GCC Web App Testing

### Certificate Management Features Tested
- **Home Page**: Certificate search and display
- **Certificate Views**: 
  - `/Certificate/CertificateTileView` - Grid layout
  - `/Certificate/CertificateExternalView` - Public access
  - `/Certificate/CertificateInternalView` - Authenticated access
- **Authentication**: Cookie-based and Azure AD B2C
- **Role-Based Access**: Admin, Analyst, External, Public

### Database Considerations
- Tests work with your SQLite database (`gcc_database.db`)
- Existing seed data will be used for testing
- No test data modification - read-only operations

## 🛠️ Troubleshooting

### ❌ "Application not running" Error
```cmd
# Check if app is running
curl -k https://localhost:7248

# If not running, start it:
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"
dotnet run
```

### ❌ "Playwright browsers not found"
```cmd
# Reinstall browsers
playwright install
```

### ❌ Tests failing due to missing elements
- Your app might use different CSS selectors
- Screenshots will show what the test is looking for
- Update `PageObjects/*.cs` files with correct selectors

### ❌ Authentication issues
- Update test user credentials in `appsettings.Test.json`
- Ensure test users exist in your database
- Check if Azure AD B2C is configured (tests fall back to cookies)

### 🐛 Debug Mode
Set `"BrowserHeadless": false` in `appsettings.Test.json` to watch tests run in real browser.

## 📊 Test Reports

After running tests, check these locations:

### Screenshots Folder
```
Screenshots/
├── [TestName]_test_start_[timestamp].png
├── [TestName]_action_description_[timestamp].png
└── [TestName]_test_completed_[timestamp].png
```

### Test Results Folder
```
test-results/
├── TestResults.xml          # JUnit format for CI/CD
├── coverage.json           # Code coverage data
└── playwright-report/      # Detailed HTML report
```

## 🚀 Adding New Tests

### Create a New Test Class
```csharp
[TestFixture]
public class AdminFunctionalityTests : BaseTest
{
    [Test]
    public async Task Admin_CanManageUsers()
    {
        // Login as admin
        await LoginAsRole("Admin");
        await TakeScreenshot("admin_logged_in");
        
        // Navigate to admin area
        await NavigateToPage("/Admin/UserManagement");
        await TakeScreenshot("user_management_loaded");
        
        // Your test logic here...
    }
}
```

### Test Your GCC-Specific Features
- Certificate import functionality
- Company/Product management
- Role-based dashboard access
- Search and filtering capabilities

## 🔄 CI/CD Integration

### Azure DevOps Pipeline
```yaml
- task: DotNetCoreCLI@2
  displayName: 'Start GCC Web App'
  inputs:
    command: 'run'
    projects: '**/GCCWebApp.csproj'
  condition: always()
  
- task: DotNetCoreCLI@2  
  displayName: 'Run UAT Tests'
  inputs:
    command: 'test'
    projects: '**/GCCWebApp.UAT.Tests.csproj'
    arguments: '--logger trx --collect:"XPlat Code Coverage"'
  condition: always()
  
- task: PublishTestResults@2
  displayName: 'Publish Test Results'
  inputs:
    testResultsFormat: 'VSTest'
    testResultsFiles: '**/*.trx'
  condition: always()
```

### GitHub Actions
```yaml
- name: Start GCC Web App
  run: dotnet run --project GCCWebApp/GCCWebApp.csproj &
  
- name: Wait for app to start
  run: sleep 10
  
- name: Run UAT Tests
  run: dotnet test GCCWebApp.UAT.Tests/GCCWebApp.UAT.Tests.csproj --logger trx
  
- name: Upload Screenshots
  uses: actions/upload-artifact@v3
  if: always()
  with:
    name: test-screenshots
    path: GCCWebApp.UAT.Tests/Screenshots/
```

## 📞 Support & Maintenance

### Regular Maintenance Tasks
1. **Update selectors** if UI changes
2. **Add new test scenarios** for new features  
3. **Review screenshots** for visual regressions
4. **Update test data** as business rules change

### Getting Help
- Review screenshot evidence when tests fail
- Check console output for detailed error messages
- Use debug mode to watch tests run in browser
- Update page object locators if HTML structure changes

---

**🎉 You're all set!** Start with `test-sample.cmd` to verify everything works, then use `run-tests.cmd` for comprehensive testing with full screenshot documentation.
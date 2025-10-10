# GCCWebApp End-to-End Tests

This project contains end-to-end tests for the GCC Web Application using:
- **xUnit** - Test framework
- **WebApplicationFactory** - For API/Integration testing
- **Playwright** - For browser-based UI testing
- **FluentAssertions** - For readable test assertions
- **Entity Framework InMemory** - For test database

## Project Structure

```
GCCWebApp.E2E.Tests/
├── Fixtures/
│   └── TestWebApplicationFactory.cs    # Test server configuration
├── Tests/
│   ├── CertificateApiTests.cs         # API endpoint tests
│   ├── AuthenticationE2ETests.cs      # Authentication flow tests
│   └── UI/
│       └── CertificateUITests.cs      # Browser-based UI tests
└── README.md
```

## Running Tests

### Prerequisites

1. Install Playwright browsers (first time only):
```bash
pwsh bin/Debug/net9.0/playwright.ps1 install
```

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Categories

#### API Tests Only
```bash
dotnet test --filter "FullyQualifiedName~GCCWebApp.E2E.Tests.Tests.Certificate"
```

#### UI Tests Only
```bash
dotnet test --filter "FullyQualifiedName~GCCWebApp.E2E.Tests.Tests.UI"
```

#### Authentication Tests
```bash
dotnet test --filter "FullyQualifiedName~GCCWebApp.E2E.Tests.Tests.Authentication"
```

### Run with Detailed Output
```bash
dotnet test --logger:"console;verbosity=detailed"
```

## Configuration

### Test Server URL
Update the `BaseUrl` in UI tests to match your development server:
```csharp
private const string BaseUrl = "https://localhost:7001";
```

### Browser Settings
In `CertificateUITests.cs`, you can configure:
- `Headless: false` - Show browser during tests
- `SlowMo: 1000` - Slow down operations by 1 second
- Browser type: Chromium, Firefox, or WebKit

### Test Data
Add test data in `TestWebApplicationFactory.InitializeDbForTests()`:
```csharp
private void InitializeDbForTests(ApplicationDbContext db)
{
    db.Users.Add(new User { 
        Email = "test@example.com",
        // ... other properties
    });
    db.SaveChanges();
}
```

## Writing New Tests

### API Test Example
```csharp
[Fact]
public async Task YourEndpoint_ReturnsExpectedResult()
{
    // Arrange
    var request = "/api/your-endpoint";
    
    // Act
    var response = await _client.GetAsync(request);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

### UI Test Example
```csharp
[Fact]
public async Task YourPage_PerformsAction()
{
    // Navigate
    await _page.GotoAsync($"{BaseUrl}/your-page");
    
    // Interact
    await _page.ClickAsync("button#submit");
    
    // Assert
    var result = await _page.TextContentAsync(".result");
    result.Should().Contain("Expected Text");
}
```

## Debugging Tests

### Visual Studio
1. Set breakpoints in test methods
2. Right-click test in Test Explorer → Debug

### VS Code
1. Install C# extension
2. Click "Debug Test" above test methods

### Show Browser During UI Tests
```csharp
_browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false,
    SlowMo = 500 // Slow down by 500ms
});
```

## CI/CD Integration

### GitHub Actions Example
```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '9.0.x'
    
- name: Install Playwright
  run: pwsh GCCWebApp.E2E.Tests/bin/Debug/net9.0/playwright.ps1 install
  
- name: Run E2E Tests
  run: dotnet test GCCWebApp.E2E.Tests/GCCWebApp.E2E.Tests.csproj
```

## Troubleshooting

### Tests Can't Connect to Server
- Ensure your application is running on the expected port
- Check firewall settings
- Verify SSL certificates for HTTPS

### Playwright Browser Not Found
Run: `pwsh bin/Debug/net9.0/playwright.ps1 install`

### InMemory Database Issues
- Clear database between tests
- Use unique database names for parallel tests

### Authentication Issues
- Mock authentication in `TestWebApplicationFactory`
- Or use test credentials with proper setup
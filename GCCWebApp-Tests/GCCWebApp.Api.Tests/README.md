# GCCWebApp.Api.Tests

**API Endpoint and Integration Test Suite**

## Overview

This project contains comprehensive tests for the GCC Database Solution REST API endpoints, ensuring proper functionality, security, and data validation for all API operations.

## Test Statistics

- **Test Framework**: xUnit with FluentAssertions
- **Authentication Testing**: API Key and JWT Bearer token validation
- **API Versioning**: v1 endpoint coverage
- **Response Validation**: JSON schema and data integrity

## Test Organization

### API Controller Tests (`/Unit/Controllers`)
Tests for individual API controller endpoints and responses.

**Coverage:**
- Certificate API endpoints (GET, POST, PUT, DELETE)
- Authentication API (login, token generation)
- User management API
- Partner API (Amazon/Wayfair integration)
- Certificate Tile External API (partner access)

**Key Test Files:**
- `CertificateApiTests.cs` - Certificate CRUD API operations
- `AuthenticationApiTests.cs` - Login and token endpoints
- `CertificateTileExternalControllerTests.cs` - Partner API access

### API Integration Tests (`/Integration`)
End-to-end API workflow testing.

**Coverage:**
- Complete API workflows (create → read → update → delete)
- Multi-endpoint data flows
- Transaction integrity across API calls
- Partner API integration scenarios

### API Security Tests (`/Security`)
API-specific security validation.

**Coverage:**
- API key authentication
- JWT token validation
- Rate limiting enforcement
- Authorization bypass prevention
- API input sanitization

### API Performance Tests (`/Performance`)
API endpoint performance validation.

**Coverage:**
- Response time benchmarks
- Concurrent request handling
- Throughput measurements
- Rate limit verification

## Running Tests

### Run All API Tests
```bash
cd GCCWebApp.Api.Tests
dotnet test
```

### Run Specific Test Categories
```bash
# Controller tests only
dotnet test --filter "FullyQualifiedName~Controllers"

# Security tests only
dotnet test --filter "FullyQualifiedName~Security"

# Integration tests only
dotnet test --filter "FullyQualifiedName~Integration"
```

### Run with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Conventions

### API Test Naming
Tests follow the pattern: `[Endpoint]_[HttpMethod]_[Scenario]_[ExpectedResult]`

**Examples:**
- `Certificates_GET_ValidId_ReturnsOk`
- `Certificates_POST_InvalidData_ReturnsBadRequest`
- `Certificates_DELETE_Unauthorized_ReturnsForbidden`

### AAA Pattern for API Tests
```csharp
[Fact]
public async Task Endpoint_HttpMethod_Scenario_ExpectedResult()
{
    // Arrange - Set up request data and expectations
    var request = new CreateCertificateRequest { /* data */ };
    var expectedStatus = HttpStatusCode.Created;

    // Act - Execute the API call
    var response = await _client.PostAsJsonAsync("/api/v1/certificates", request);

    // Assert - Verify the response
    response.StatusCode.Should().Be(expectedStatus);
    var result = await response.Content.ReadFromJsonAsync<CertificateResponse>();
    result.Should().NotBeNull();
}
```

### API Authentication Testing
```csharp
// API Key authentication
_client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

// JWT Bearer token
_client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", jwtToken);
```

## API Endpoints Tested

### Certificate API (`/api/v1/certificates`)

| Method | Endpoint | Auth | Tests |
|--------|----------|------|-------|
| GET | `/api/v1/certificates` | None | List certificates, pagination, filtering |
| GET | `/api/v1/certificates/{id}` | None | Get single certificate, not found handling |
| POST | `/api/v1/certificates` | Admin/Analyst | Create certificate, validation |
| PUT | `/api/v1/certificates/{id}` | Admin/Analyst | Update certificate, concurrency |
| DELETE | `/api/v1/certificates/{id}` | Admin | Delete certificate, authorization |

### Partner API (`/api/CertificateTileExternal`)

| Method | Endpoint | Auth | Tests |
|--------|----------|------|-------|
| GET | `/api/CertificateTileExternal/tiles` | API Key | Partner-filtered certificates |
| GET | `/api/CertificateTileExternal/search` | API Key | Search with partner context |
| GET | `/api/CertificateTileExternal/autocomplete` | API Key | Autocomplete suggestions |

### Authentication API (`/api/auth`)

| Method | Endpoint | Auth | Tests |
|--------|----------|------|-------|
| POST | `/api/auth/login` | None | Credential validation, token generation |
| POST | `/api/auth/refresh` | Valid Token | Token refresh, expiration handling |
| POST | `/api/auth/logout` | Valid Token | Session termination |

## Dependencies

**Testing Frameworks:**
- **xUnit** 2.9.3 - Test framework
- **FluentAssertions** 8.7.1 - Assertion library
- **Microsoft.AspNetCore.Mvc.Testing** - API testing support

**HTTP Testing:**
- **HttpClient** - API request execution
- **System.Net.Http.Json** - JSON serialization/deserialization

**Authentication:**
- **JWT Bearer Token** - Token-based authentication testing
- **API Key** - Partner API authentication

## Test Data Management

### API Request Builders
```csharp
public class CertificateRequestBuilder
{
    private CreateCertificateRequest _request = new();

    public CertificateRequestBuilder WithNumber(string number)
    {
        _request.Number = number;
        return this;
    }

    public CertificateRequestBuilder WithValidData()
    {
        _request.Number = "GCC001";
        _request.Status = "Active";
        _request.CompanyId = 1;
        return this;
    }

    public CreateCertificateRequest Build() => _request;
}
```

### API Response Validators
```csharp
public static class CertificateResponseValidator
{
    public static void ShouldBeValid(this CertificateResponse response)
    {
        response.Should().NotBeNull();
        response.Id.Should().BeGreaterThan(0);
        response.Number.Should().NotBeNullOrWhiteSpace();
        response.Status.Should().NotBeNullOrWhiteSpace();
    }
}
```

## API Test Scenarios

### Happy Path Tests
- Valid requests return expected responses
- Data is correctly created/updated/deleted
- Pagination works correctly
- Filtering and sorting function properly

### Error Handling Tests
- Invalid data returns 400 Bad Request
- Missing resources return 404 Not Found
- Unauthorized access returns 401 Unauthorized
- Forbidden actions return 403 Forbidden

### Security Tests
- API key validation
- JWT token expiration
- Rate limiting enforcement
- Authorization checks

### Edge Case Tests
- Empty lists handled correctly
- Large datasets paginated properly
- Concurrent requests handled safely
- Invalid JSON rejected appropriately

## Known Issues

### Issue 1: Swagger Documentation
**Status:** API documentation available at `/swagger` in development mode

**Access:**
```bash
# Start application
dotnet run --project GCCWebApp

# Navigate to Swagger UI
https://localhost:5101/swagger
```

## API Testing Best Practices

### 1. Use TestServer for Integration Tests
```csharp
public class ApiTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly HttpClient _client;
    protected readonly WebApplicationFactory<Program> _factory;

    public ApiTestBase(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
}
```

### 2. Validate Response Schema
```csharp
var response = await _client.GetAsync("/api/v1/certificates");
var content = await response.Content.ReadAsStringAsync();

// Validate JSON structure
var json = JsonDocument.Parse(content);
json.RootElement.TryGetProperty("data", out _).Should().BeTrue();
json.RootElement.TryGetProperty("totalCount", out _).Should().BeTrue();
```

### 3. Test Rate Limiting
```csharp
[Fact]
public async Task API_ExceedsRateLimit_ReturnsTooManyRequests()
{
    // Arrange - Configure client with API key
    _client.DefaultRequestHeaders.Add("X-API-Key", "test-key");

    // Act - Make requests exceeding rate limit
    var tasks = Enumerable.Range(0, 1001)
        .Select(_ => _client.GetAsync("/api/v1/certificates"));
    var responses = await Task.WhenAll(tasks);

    // Assert - At least one request should be rate limited
    responses.Should().Contain(r => r.StatusCode == HttpStatusCode.TooManyRequests);
}
```

### 4. Test API Versioning
```csharp
[Theory]
[InlineData("/api/v1/certificates")]
[InlineData("/api/v2/certificates")] // Future version
public async Task API_Versioning_ReturnsCorrectVersion(string endpoint)
{
    var response = await _client.GetAsync(endpoint);

    if (endpoint.Contains("v2"))
    {
        // v2 not implemented yet
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    else
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## Continuous Integration

API tests run as part of the CI pipeline:

```yaml
- name: Run API Tests
  run: dotnet test GCCWebApp.Api.Tests --logger "console;verbosity=detailed"

- name: Generate API Test Report
  run: dotnet test GCCWebApp.Api.Tests --logger "trx;LogFileName=api-test-results.trx"
```

## Contributing

### Adding New API Tests

1. **Identify API Endpoint:**
   - Determine controller and action
   - Document expected request/response

2. **Create Test Class:**
   - Name: `{Controller}ApiTests.cs`
   - Inherit from `ApiTestBase`

3. **Write Test Methods:**
   - Follow naming convention
   - Test happy path + error cases
   - Validate response schema

4. **Add Authentication:**
   - Configure API key or JWT token
   - Test authorization scenarios

### API Test Review Checklist

- [ ] All HTTP methods tested (GET, POST, PUT, DELETE)
- [ ] Success responses validated (200, 201, 204)
- [ ] Error responses tested (400, 401, 403, 404)
- [ ] Request validation tested
- [ ] Response schema validated
- [ ] Authentication tested
- [ ] Authorization tested
- [ ] Rate limiting tested (if applicable)

## Related Documentation

- [Swagger API Documentation](http://localhost:5101/swagger) - Interactive API explorer
- [Main Test Suite](../GCCWebApp.Tests/README.md) - Core unit and integration tests
- [Security Tests](../GCCWebApp.Security.Tests/README.md) - Security-specific testing
- [Performance Tests](../GCCWebApp.Performance.Tests/README.md) - API performance benchmarks
- [Test Coverage Analysis](../TEST_COVERAGE_ANALYSIS.md) - Coverage metrics and gaps

## Contact & Support

For API testing questions:
- **API Documentation**: `/swagger` endpoint (development mode)
- **Requirements**: `0_Documentation/docs/Requirements.md`
- **Partner API Setup**: See main application CLAUDE.md for API key configuration

---

**Last Updated:** October 24, 2025
**API Version:** v1
**Test Framework:** xUnit
**Authentication:** API Key + JWT Bearer

# Test Issues Found - 2026-01-18

## Executive Summary

### After Fixes Applied (2026-01-18)

| Test Project | Total | Passed | Failed | Skipped | Pass Rate | Change |
|--------------|-------|--------|--------|---------|-----------|--------|
| GCCWebApp.Api.Tests | 81 | 51 | 30 | 0 | 63.0% | +7.4% |
| GCCWebApp.Tests | 784 | 626 | 106 | 52 | 79.8% | - |
| GCCWebApp.Security.Tests | 29 | 21 | 8 | 0 | 72.4% | - |
| GCCWebApp.E2E.Tests | 14 | 6 | 8 | 0 | 42.9% | - |
| GCCWebApp.Accessibility.Tests | 54 | 17 | 37 | 0 | 31.5% | - |
| GCCWebApp.UAT.Tests | - | - | - | - | Build Error | - |
| **TOTAL** | **962** | **721** | **189** | **52** | **74.9%** | +0.6% |

### Before Fixes (Baseline)

| Test Project | Total | Passed | Failed | Skipped | Pass Rate |
|--------------|-------|--------|--------|---------|-----------|
| GCCWebApp.Tests | 784 | 626 | 106 | 52 | 79.8% |
| GCCWebApp.Api.Tests | 81 | 45 | 36 | 0 | 55.6% |
| GCCWebApp.Security.Tests | 29 | 21 | 8 | 0 | 72.4% |
| GCCWebApp.E2E.Tests | 14 | 6 | 8 | 0 | 42.9% |
| GCCWebApp.Accessibility.Tests | 54 | 17 | 37 | 0 | 31.5% |
| GCCWebApp.UAT.Tests | - | - | - | - | Build Error |
| **TOTAL** | **962** | **715** | **195** | **52** | **74.3%** |

---

## Critical Issues (Priority 1 - Must Fix)

### ISSUE-001: API Authentication Test Failures
**Status:** PARTIALLY FIXED
**Severity:** High
**Test Project:** GCCWebApp.Api.Tests
**Tests Affected:** 12+ tests

**Description:**
Multiple API integration tests fail with 401 Unauthorized errors. The API key authentication handler is not properly recognizing test API keys in the integration test environment.

**Failing Tests:**
- `CreateProduct_WithUPCAndGTIN_ShouldAcceptBothFormats` - Returns 401 instead of 201/200/501
- `GetProducts_WithProductIdType_ShouldHandleAllTypes` (GTIN, UPC, ASIN, SKU variants) - All return 401
- `GetProducts_WithPagination_ShouldReturnPagedResults` - Returns 401
- `AmazonAPI_CreateCertificate_WithValidPayload_ShouldSucceed` - Returns 401

**Root Cause:**
Test setup is not properly configuring API key headers or the test authentication handler mock is incomplete.

**File Location:** `GCCWebApp.Api.Tests\Integration\MultiPartnerApiIntegrationTests.cs`

**Recommendation:**
1. Review test API key configuration in test fixtures
2. Ensure `X-API-Key` header is set properly in test requests
3. Verify test database contains valid API key records

---

### ISSUE-002: CertificatesController NullReferenceException Errors
**Status:** FIXED
**Severity:** High
**Test Project:** GCCWebApp.Api.Tests
**Tests Affected:** 4 tests

**Description:**
Several certificate controller tests throw NullReferenceException, indicating missing mock setup or null object handling in the controller.

**Failing Tests:**
- `GetExpiringCertificates_WithValidDays_ReturnsResults` - Line 314
- `GetCertificates_WithSearchTerm_FiltersResults` - Line 88
- `GetCertificates_WithLargePageSize_ShouldRespectMaximum` - Line 357

**Root Cause:**
The controller or service returns null in certain scenarios where the test expects a valid response object.

**File Location:** `GCCWebApp.Api.Tests\Unit\Controllers\CertificatesControllerTests.cs`

**Recommendation:**
1. Add null checks in controller action methods
2. Review mock service setup in test fixtures
3. Ensure all service dependencies are properly mocked

---

### ISSUE-003: Response Type Mismatch in Pagination
**Status:** FIXED
**Severity:** High
**Test Project:** GCCWebApp.Api.Tests
**Tests Affected:** 1 test

**Description:**
Test `GetCertificates_WithValidPagination_ReturnsOkResult` expects `StandardPaginatedResponse<List<Certificate>>` but receives `StandardPaginatedResponse` with anonymous type (projections).

**Root Cause:**
The API controller returns projected/anonymous objects instead of full Certificate models to optimize payload size, but the test expects the full model type.

**File Location:** `GCCWebApp.Api.Tests\Unit\Controllers\CertificatesControllerTests.cs:61`

**Recommendation:**
1. Update test to match actual API response type
2. Or create a DTO class for the projected response
3. Document the expected response shape in API documentation

---

## Medium Priority Issues (Priority 2)

### ISSUE-004: Security Tests - Authentication Failures
**Status:** Open
**Severity:** Medium
**Test Project:** GCCWebApp.Security.Tests
**Tests Affected:** 8 tests

**Description:**
Security penetration tests fail to authenticate and get valid tokens before running security checks.

**Failing Tests:**
- `HTTPParameterPollution_DuplicateParameters_ShouldBeHandledSecurely`
- `SQLInjection_LoginForm_ShouldBeBlocked`
- `ReflectedXSS_QueryParameters_ShouldBeSanitized`
- `IDOR_AccessOtherUserCertificate_ShouldBeBlocked`
- `BruteForce_RateLimiting_ShouldBlockAfterThreshold`
- `SessionFixation_ShouldRegenerateSessionAfterLogin`
- `CSRF_StateChangingOperations_ShouldRequireToken`
- `TimingAttack_Authentication_ShouldHaveConstantTime`

**Root Cause:**
Test user `securitytestuser` credentials fail authentication, preventing security tests from running.

**File Location:** `GCCWebApp.Security.Tests\Tests\PenTesting\AutomatedPenTests.cs:480`

**Recommendation:**
1. Verify `securitytestuser` exists in test database with correct credentials
2. Check if test environment authentication configuration matches expected setup
3. Consider using test fixtures that seed required users

---

### ISSUE-005: Refresh Token Test Expects Wrong Status Code
**Status:** FIXED
**Severity:** Medium
**Test Project:** GCCWebApp.Api.Tests
**Tests Affected:** 1 test

**Description:**
Test `RefreshToken_WithInvalidToken_ReturnsUnauthorized` expects 401 but receives a different status code (likely 400 Bad Request).

**File Location:** `GCCWebApp.Api.Tests\Security\AuthenticationTests.cs:121`

**Recommendation:**
1. Verify the actual expected behavior for invalid refresh tokens
2. Update test assertion or controller behavior to match requirements

---

## Environment/Infrastructure Issues (Priority 3)

### ISSUE-006: E2E Tests - Application Not Running
**Status:** Open
**Severity:** Medium
**Test Project:** GCCWebApp.E2E.Tests
**Tests Affected:** 8 tests

**Description:**
All E2E Playwright tests fail with `net::ERR_CONNECTION_REFUSED` because the application is not running on expected port (https://localhost:7001).

**Failing Tests:**
- `HomePage_LoadsSuccessfully`
- `LoginPage_DisplaysLoginForm`
- `CertificateTileView_LoadsCorrectly`
- `UserCanNavigateToLogin`
- `UserCanNavigateToCertificates`
- `ResponsiveDesign_MobileView`
- `ResponsiveDesign_TabletView`
- `ResponsiveDesign_DesktopView`

**Root Cause:**
E2E tests require the application to be running, but the test infrastructure does not start the application automatically.

**File Location:** `GCCWebApp.E2E.Tests\Tests\UI\CertificateUITests.cs`

**Recommendation:**
1. Start application before running E2E tests: `dotnet run --urls "https://localhost:7001"`
2. Or configure WebApplicationFactory to host application during tests
3. Consider using `IClassFixture<WebApplicationFactory<Program>>` pattern

---

### ISSUE-007: Accessibility Tests - Application Not Running
**Status:** Open
**Severity:** Medium
**Test Project:** GCCWebApp.Accessibility.Tests
**Tests Affected:** 37 tests

**Description:**
Accessibility tests fail with `net::ERR_CONNECTION_REFUSED` at https://localhost:7130. Tests that don't require navigation pass, but page-level tests fail.

**Failing Test Categories:**
- Navigation text color tests
- Breadcrumb visibility tests
- Button accessibility tests
- Admin page accessibility tests
- Data table accessibility tests

**File Location:** `GCCWebApp.Accessibility.Tests\Tests\BasicAccessibilityTests.cs`

**Recommendation:**
1. Start application on port 7130 before running tests
2. Or reconfigure test base URL to match running application port

---

### ISSUE-008: UAT Tests Build Configuration Error
**Status:** Open
**Severity:** Low
**Test Project:** GCCWebApp.UAT.Tests
**Tests Affected:** All

**Description:**
UAT test project fails to run due to invalid test logger configuration: `The Test Logger URI 'verbosity=detailed' is not valid`

**Root Cause:**
Project configuration contains invalid test logger settings.

**Recommendation:**
1. Review `.csproj` or `runsettings` file for logger configuration
2. Remove or correct invalid logger URI

---

## Test Infrastructure Issues

### ISSUE-009: Missing Null Reference Handling in Base Test Classes
**Status:** Open
**Severity:** Medium
**Test Project:** GCCWebApp.Accessibility.Tests
**Tests Affected:** Multiple

**Description:**
`BaseAccessibilityTest.AssertNoViolations()` throws NullReferenceException when axe results are null.

**File Location:** `GCCWebApp.Accessibility.Tests\Tests\Core\BaseAccessibilityTest.cs:96`

**Recommendation:**
1. Add null check in `AssertNoViolations` method
2. Provide meaningful error message when results are null

---

## Skipped Tests Summary

52 tests were skipped in GCCWebApp.Tests. Common skip reasons:
- Feature not yet implemented
- Test marked with `[Skip]` attribute
- Conditional platform requirements not met

---

## Recommendations Summary

### Immediate Actions (This Sprint)
1. Fix API authentication in integration tests
2. Add null handling in CertificatesController tests
3. Create/verify `securitytestuser` in test database

### Short-term Actions (Next Sprint)
1. Update E2E test infrastructure to auto-start application
2. Fix UAT test logger configuration
3. Update response type assertions in pagination tests

### Long-term Actions (Backlog)
1. Implement continuous integration with proper test environment
2. Add test database seeding scripts for all test projects
3. Document required test infrastructure setup

---

## Test Execution Details

**Run Date:** 2026-01-18
**Run Time:** ~2 minutes total
**Environment:** Development (localhost)
**Framework:** xUnit with Fluent Assertions
**Browser Tests:** Playwright

---

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2026-01-18 | Initial test execution and issues documentation | Claude Code |
| 2026-01-18 | Fixed ISSUE-002: Updated CertificatesControllerTests to use reflection for response type handling | Claude Code |
| 2026-01-18 | Fixed ISSUE-003: Updated pagination tests to properly handle anonymous type responses | Claude Code |
| 2026-01-18 | Fixed ISSUE-005: Added proper exception handling in AuthController.RefreshToken for invalid tokens | Claude Code |
| 2026-01-18 | Partially fixed ISSUE-001: Added missing API keys to TestDbContextFactory, updated integration tests to use WebApplicationFactoryBase | Claude Code |

# QC Automated Test Results — Detailed Report

**Project:** GCC Database Solution
**Prepared by:** Anuki Consulting
**Date:** 2026-03-25
**Test Framework:** xUnit 2.9.3 + FluentAssertions 8.7.1 + Moq 4.20.72 + EF Core InMemory
**Test Project:** GCCWebApp.Api.Tests

---

## Summary

| Metric | Value |
|--------|-------|
| Total Tests | 71 |
| Passed | 71 |
| Failed | 0 |
| Pass Rate | 100% |
| Execution Time | ~9 seconds |
| QC Items Covered | 12 high-priority / regression-prone items |
| Test Files Created | 6 |

---

## Test Files & Results

### File 1: `QC_CertificateExpirationServiceTests.cs` (11 tests)
**Location:** `Unit/Services/QC_CertificateExpirationServiceTests.cs`
**QC Item:** #23 (Auto-expire certificates when expiration date passes)

| # | Test Name | Result | Time |
|---|-----------|--------|------|
| 1 | CheckAndExpireAsync_CertPastExpiration_UpdatesStatusToExpired | PASS | 3ms |
| 2 | CheckAndExpireAsync_AlreadyExpiredStatus_SkipsCert | PASS | 4ms |
| 3 | CheckAndExpireAsync_ExpiredNoAnnualVerification_SkipsCert | PASS | 4ms |
| 4 | CheckAndExpireAsync_FutureExpirationDate_SkipsCert | PASS | 7ms |
| 5 | CheckAndExpireAsync_NoCertsToExpire_NoChanges | PASS | 3ms |
| 6 | CheckAndExpireAsync_MultipleCerts_ExpiresAll | PASS | 1s |
| 7 | CheckAndExpireAsync_DraftCertPastExpiration_Expires | PASS | 5ms |
| 8 | CheckAndExpireAsync_HoldCertPastExpiration_Expires | PASS | 38ms |
| 9 | CheckAndExpireAsync_CertPastExpiration_CreatesStatusHistoryRecord | PASS | 39ms |
| 10 | CheckAndExpireAsync_CertPastExpiration_CreatesAuditHistoryRecord | PASS | 30ms |
| 11 | CheckAndExpireAsync_SetsUpdatedAtOnCertificate | PASS | 5ms |

**What's tested:**
- Certificates with past expiration dates are automatically set to "Expired" status
- Already-expired certificates ("Expired" and "Expired - No Annual Verification") are skipped
- Certificates with future expiration dates are not modified
- Multiple overdue certificates are all expired in a single pass
- Draft and Hold status certificates with past dates also get expired
- CertificateStatusHistory records are created with: IsAutomatedTransition=true, WorkflowStep="Auto-Expiration", ChangeSource="Automated", ChangedByUsername="System"
- AuditHistory records are created with: EntityType="Certificate", Action="STATUS_CHANGE", ChangeSource="Automated"
- Certificate UpdatedAt timestamp is set on expiration

---

### File 2: `QC_CertificateRepositoryTests.cs` (20 tests)
**Location:** `Unit/Repositories/QC_CertificateRepositoryTests.cs`
**QC Items:** #22 (Public view status filtering), #25/#26 (Sort logic), #67/#68/#70 (Dashboard stats)

| # | Test Name | Result | Time |
|---|-----------|--------|------|
| 1 | GetActiveCertificatesAsync_ReturnsOnlyPublicStatuses | PASS | 62ms |
| 2 | GetActiveCertificatesAsync_ExcludesExpiredByDate | PASS | 9ms |
| 3 | GetActiveCertificatesAsync_ExcludesDraftStatus | PASS | 512ms |
| 4 | GetActiveCertificatesAsync_ExcludesHoldStatus | PASS | 10ms |
| 5 | GetActiveCertificatesAsync_ExcludesConfidentialStatus | PASS | 17ms |
| 6 | GetActiveCertificatesAsync_IncludesRepublishedWithFutureExpiry | PASS | 17ms |
| 7 | GetCertificateStatisticsAsync_TotalCountsAllCerts | PASS | 20ms |
| 8 | GetCertificateStatisticsAsync_ActiveCountFiltersByStatusAndDate | PASS | 22ms |
| 9 | GetCertificateStatisticsAsync_ExpiredCountsByStatusOnly | PASS | 20ms |
| 10 | GetCertificateStatisticsAsync_ActiveVsTotalDiffer | PASS | 29ms |
| 11 | GetCertificateStatisticsAsync_CompaniesWithActiveCerts_DistinctCount | PASS | 18ms |
| 12 | GetCertificateStatisticsAsync_CertTypesWithActiveCerts_DistinctCount | PASS | 2s |
| 13 | GetCertificateStatisticsAsync_StatusBreakdownHasAllStatuses | PASS | 52ms |
| 14 | GetAdvancedFilteredCertificatesAsync_SortByNumber_Ascending | PASS | 525ms |
| 15 | GetAdvancedFilteredCertificatesAsync_SortByNumber_Descending | PASS | 672ms |
| 16 | GetAdvancedFilteredCertificatesAsync_SortByCompany_Ascending | PASS | 851ms |
| 17 | GetAdvancedFilteredCertificatesAsync_UnknownSortField_DefaultsToNumber | PASS | 135ms |
| 18 | GetAdvancedFilteredCertificatesAsync_StatusFilter_ReturnsOnlyMatchingStatus | PASS | 499ms |
| 19 | GetAdvancedFilteredCertificatesAsync_ExcludeExpiredByDate_FiltersCorrectly | PASS | 420ms |
| 20 | GetAdvancedFilteredCertificatesAsync_ReturnsTotalCount | PASS | 14ms |

**What's tested:**
- **Public View Filtering (QC #22):** Only "Client Approved - Paid" and "Republished" statuses returned; Draft, Hold, Confidential, Expired statuses excluded; certificates with past expiration dates excluded even if status is public
- **Dashboard Stats (QC #67-70):** Total counts all certs; Active counts only public-status + non-expired; Expired counts by status only (not by date — fixes the double-counting bug); CompaniesWithActiveCerts and CertTypesWithActiveCerts are distinct counts from active certs only; Status breakdown has entries for all statuses
- **Sorting (QC #25-26):** Sort by Number ascending/descending works; Sort by Company ascending works; Unknown sort fields default to Number; Status filter returns only matching status; ExcludeExpiredByDate properly filters

---

### File 3: `QC_AdminServiceUserTests.cs` (10 tests)
**Location:** `Unit/Services/QC_AdminServiceUserTests.cs`
**QC Items:** #107 (User creation error handling), #113 (Certificate type deletion guard)

| # | Test Name | Result | Time |
|---|-----------|--------|------|
| 1 | CreateUserAsync_ValidUser_ReturnsSuccessTrue | PASS | 1s |
| 2 | CreateUserAsync_DuplicateEmail_ReturnsFalseWithMessage | PASS | 264ms |
| 3 | CreateUserAsync_DuplicateEmailCaseInsensitive_Blocked | PASS | 195ms |
| 4 | CreateUserAsync_ValidUser_CreatesUserInDatabase | PASS | 1s |
| 5 | CreateUserAsync_ValidUser_AssignsRole | PASS | 3s |
| 6 | CreateUserAsync_ErrorMessageContainsEmail | PASS | 292ms |
| 7 | DeleteCertificateTypeAsync_NoCertificates_Succeeds | PASS | 289ms |
| 8 | DeleteCertificateTypeAsync_HasCertificates_Blocked | PASS | 241ms |
| 9 | DeleteCertificateTypeAsync_HasCertificates_TypeStillExists | PASS | 363ms |
| 10 | DeleteCertificateTypeAsync_NonExistent_ReturnsFalse | PASS | 225ms |

**What's tested:**
- **User Creation (QC #107):** Valid user returns (true, ""); duplicate email returns (false, error message); case-insensitive email matching blocks "EXISTING@TEST.COM" when "existing@test.com" exists; user is actually created in database with correct fields; role is properly assigned; error message contains the duplicate email address
- **Certificate Type Deletion (QC #113):** Types with no certificates can be deleted (removed from DB); types WITH certificates are blocked from deletion; after blocked deletion, type still exists in database; non-existent type returns false

---

### File 4: `QC_CertificateTilesApiSortTests.cs` (17 tests)
**Location:** `Unit/Controllers/QC_CertificateTilesApiSortTests.cs`
**QC Item:** #26 (Sort parameter priority — HIGH REGRESSION RISK)

| # | Test Name | Result | Time |
|---|-----------|--------|------|
| 1 | GetCertificates_SortByProvided_UsesSortByOverSort | PASS | 1ms |
| 2 | GetCertificates_OnlySortProvided_UsesSort | PASS | <1ms |
| 3 | GetCertificates_NoSortParams_DefaultsToCertNumberNewest | PASS | <1ms |
| 4 | GetCertificates_SortMapping_MapsCorrectly [company-az → Company, false] | PASS | 250ms |
| 5 | GetCertificates_SortMapping_MapsCorrectly [company-za → Company, true] | PASS | <1ms |
| 6 | GetCertificates_SortMapping_MapsCorrectly [product-az → Product, false] | PASS | <1ms |
| 7 | GetCertificates_SortMapping_MapsCorrectly [product-za → Product, true] | PASS | <1ms |
| 8 | GetCertificates_SortMapping_MapsCorrectly [cert-type → CertType, false] | PASS | <1ms |
| 9 | GetCertificates_SortMapping_MapsCorrectly [cert-type-za → CertType, true] | PASS | <1ms |
| 10 | GetCertificates_SortMapping_MapsCorrectly [location-az → Location, false] | PASS | <1ms |
| 11 | GetCertificates_SortMapping_MapsCorrectly [location-za → Location, true] | PASS | <1ms |
| 12 | GetCertificates_SortMapping_MapsCorrectly [country → Country, false] | PASS | <1ms |
| 13 | GetCertificates_SortMapping_MapsCorrectly [country-za → Country, true] | PASS | <1ms |
| 14 | GetCertificates_SortMapping_MapsCorrectly [cert-number-newest → Number, true] | PASS | 1ms |
| 15 | GetCertificates_SortMapping_MapsCorrectly [cert-number-oldest → Number, false] | PASS | <1ms |
| 16 | GetCertificates_CertNumberNewest_IsSortDescending | PASS | 7ms |
| 17 | GetCertificates_CompanyAZ_IsNotSortDescending | PASS | <1ms |

**What's tested:**
- **Sort Priority Fix (QC #26):** `sortBy` parameter takes precedence over `sort` (the root cause of the original bug); when only `sort` is provided, it is used; when neither is provided, defaults to "cert-number-newest"
- **MapSortBy Mappings:** All 12 frontend sort names map correctly to database field names (company-az→Company, product-az→Product, cert-type→CertType, location-az→Location, country→Country, cert-number-newest→Number, etc.)
- **SortDescending Logic:** Correctly set to true for Z-A/newest sorts, false for A-Z/oldest sorts

---

### File 5: `QC_ProductControllerDeleteTests.cs` (6 tests)
**Location:** `Unit/Controllers/QC_ProductControllerDeleteTests.cs`
**QC Item:** #98 (Delete product guard — block if certificates exist)

| # | Test Name | Result | Time |
|---|-----------|--------|------|
| 1 | DeleteProduct_NoCertificates_ReturnsSuccessTrue | PASS | 37ms |
| 2 | DeleteProduct_HasCertificates_ReturnsSuccessFalse | PASS | 29ms |
| 3 | DeleteProduct_HasCertificates_MessageContainsCertCount | PASS | 22ms |
| 4 | DeleteProduct_NonExistent_ReturnsSuccessFalse | PASS | 37ms |
| 5 | DeleteProduct_VerifiesRemovalFromDatabase | PASS | 6ms |
| 6 | DeleteProduct_HasCertificates_ProductStillExists | PASS | 1s |

**What's tested:**
- Products with 0 certificates are successfully deleted
- Products with certificates return Success=false with error message containing the certificate count (e.g., "2 certificates")
- Non-existent products return Success=false
- After successful deletion, product is removed from DB
- After blocked deletion, product still exists in DB

---

### File 6: `QC_ProductIdServiceScopeTests.cs` (6 tests)
**Location:** `Unit/Services/QC_ProductIdServiceScopeTests.cs`
**QC Item:** #99 (Duplicate check scoped to certificate — REGRESSION RISK)

| # | Test Name | Result | Time |
|---|-----------|--------|------|
| 1 | CheckDuplicateAsync_SameValueSameCert_ReturnsTrue | PASS | 9ms |
| 2 | CheckDuplicateAsync_SameValueDifferentCert_WithScope_ReturnsFalse | PASS | 9ms |
| 3 | CheckDuplicateAsync_SameValueAcrossCerts_WithoutScope_ReturnsTrue | PASS | 1s |
| 4 | CheckDuplicateAsync_SameValueSameCert_ExcludingSelf_ReturnsFalse | PASS | 77ms |
| 5 | CheckDuplicateAsync_NewValue_WithCertScope_ReturnsFalse | PASS | 4ms |
| 6 | CheckDuplicateAsync_SameValueSameCert_ExcludingOther_ReturnsTrue | PASS | 3ms |

**What's tested:**
- Same product ID value on the SAME certificate = duplicate (blocked)
- Same value on a DIFFERENT certificate, scoped to current cert = NOT a duplicate (allowed — this was the bug fix)
- Same value across certificates without scoping = global duplicate
- Excluding self (during edit) on same cert = not duplicate
- Completely new value on any cert = no duplicate

---

## QC Coverage Map

| QC# | Description | Priority | Regression Risk | Test File | Tests |
|-----|-------------|----------|----------------|-----------|-------|
| #22 | Public view status filtering | High | High | File 2 | 6 |
| #23 | Auto-expire certificates | High | High | File 1 | 11 |
| #25 | Sort options reorganization | Medium | High | File 2 | 7 |
| #26 | Sort parameter priority fix | High | High | Files 2+4 | 17 |
| #67 | Dashboard stats — active only | High | High | File 2 | 7 |
| #68 | Dashboard expired count | High | High | File 2 | 1 |
| #70 | Product category vs cert type | High | High | File 2 | 1 |
| #98 | Delete product guard | High | Medium | File 5 | 6 |
| #99 | Product ID duplicate scope | High | High | File 6 | 6 |
| #107 | User creation errors | High | Medium | File 3 | 6 |
| #113 | Cert type deletion guard | High | High | File 3 | 4 |

---

## Items Not Covered (Frontend-Only — No Backend Logic to Test)

These QC items involve CSS, JavaScript UI, or view-layer changes only. They require manual testing or a JavaScript test framework (none exists in the project):

| QC# | Reason Not Automated |
|-----|---------------------|
| #7 | CSS class fix (pdf-upload-zone) |
| #11 | HTML table columns, sticky scroll, JS sync |
| #21 | JS sort label removal |
| #24 | JS toast notification removal |
| #27 | CSS responsive mobile toggle |
| #28 | HTML link vs dropdown change |
| #30 | CSS word-wrap fix |
| #33 | JS filter wiring (APR/LEED) |
| #36 | JS URL parameter initialization order |
| #64-66 | JS infinite scroll, pagination, font size |
| #79-81 | JS form data collection fix |
| #82-87 | CSS font size, JS column ordering, scroll events |
| #88-95 | CSS/JS product management table |
| #104-109 | HTML breadcrumbs, JS user management |
| #111-121 | Admin UI modals, labels, templates |

---

## Deferred Test

| QC# | Description | Reason Deferred |
|-----|-------------|----------------|
| #44 | Remember Me 14-day expiry | Complex HttpContext.SignInAsync mocking required. The fix is in AccountController.SignInUser which sets AuthenticationProperties.ExpiresUtc — testing would require mocking IAuthenticationService to capture the properties. Lower regression risk than the items above. |

---

## How to Run These Tests

```bash
# Run only QC tests
cd GCCWebApp.Api.Tests
dotnet test --filter "FullyQualifiedName~QC_" --verbosity normal

# Run specific test file
dotnet test --filter "FullyQualifiedName~QC_CertificateExpirationServiceTests" --verbosity normal

# Run with code coverage
dotnet test --filter "FullyQualifiedName~QC_" --collect:"XPlat Code Coverage"
```

---

## Test Infrastructure Notes

- **Database:** EF Core InMemory provider (unique database per test via `Guid.NewGuid()`)
- **Isolation:** Each test class implements `IDisposable`, creates fresh context in constructor
- **Transaction Handling:** AdminService tests use `ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))` since InMemory doesn't support transactions
- **Static State:** CertificateExpirationService has a static 24-hour throttle; tests reset it via reflection before each run
- **Mocking:** Moq used for ILogger, IAuditService, ICertificateRepository, IUnitOfWork, ICertificatePdfService
- **Assertions:** FluentAssertions for readable test output

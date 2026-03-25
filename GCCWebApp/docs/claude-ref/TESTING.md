# Testing Reference

## Test Credentials

### Production UAT (Login with EMAIL)
| Role | Email | Password |
|------|-------|----------|
| Admin | admin@greencirclecertified.com | Admin123! |
| Analyst | analyst@greencirclecertified.com | Analyst123! |
| External | partner@amazon.com | Partner123! |

### Development (Login with USERNAME)
**All use Password: `TestPass123!`**

| Username | Role | Email |
|----------|------|-------|
| admin | Admin | swest@anukiconsulting.com |
| analyst | Analyst | analyst@test.com |
| external | External | external@test.com |
| testuser | Public | testuser@test.com |
| sarah.johnson | Analyst | sarah.johnson@test.com |
| mike.chen | Analyst | mike.chen@test.com |
| lisa.rodriguez | External | lisa.rodriguez@test.com |
| david.kim | External | david.kim@test.com |
| emma.wilson | External | emma.wilson@test.com |
| alex.martinez | External | alex.martinez@test.com |
| jennifer.lee | Analyst | jennifer.lee@test.com |
| robert.taylor | Analyst | robert.taylor@test.com |
| maria.garcia | External | maria.garcia@test.com |
| public.user | Public | public.user@test.com |

**Note**: Emails must be unique. Admin uses swest@anukiconsulting.com for 2FA.

### External Visibility Test Users (Login with USERNAME)
**All use Password: `TestPass123!`**
**Setup script:** `docs/database/scripts/SETUP_EXTERNAL_TEST_USERS.sql`

These users test company-scoped data isolation — each is associated with a different company/partner combination.

| Username | Company | Partner | Expected Certs | Test Scenario |
|----------|---------|---------|---------------|---------------|
| ext-amazon | ExtTest-AmazonCo | Amazon | 3 active + 1 draft | Amazon partner external user |
| ext-novi | ExtTest-NoviCo | Novi | 2 active + 1 expired | Novi partner external user |
| ext-nopartner | ExtTest-NoPartnerCo | None | 2 active | External user, no partner association |
| ext-none | *(no company)* | None | 0 | External user with no company — should see nothing |

**What to verify with these accounts:**
- `ext-amazon` sees ONLY ExtTest-AmazonCo certificates (EXT-AMZ-001, 002, 003; NOT the draft)
- `ext-novi` sees ONLY ExtTest-NoviCo certificates (EXT-NOVI-001, 002; NOT the expired)
- `ext-nopartner` sees ONLY ExtTest-NoPartnerCo certificates (EXT-NP-001, 002)
- `ext-none` sees NO certificates (no company association)
- No external user can see another company's data
- Internal tags are hidden from all external users

## Test Coverage

| Component | Project | Tests | Pass Rate |
|-----------|---------|-------|-----------|
| Configuration | GCCWebApp.Tests | 12 | 100% |
| Authentication | GCCWebApp.Tests | 15 | 100% |
| Models | GCCWebApp.Tests | 24 | 100% |
| Services | GCCWebApp.Tests | 70 | 97% |
| Controllers | GCCWebApp.Tests | 45 | 93% |
| API Endpoints | GCCWebApp.Api.Tests | 20 | 85% |
| Security | GCCWebApp.Security.Tests | 30 | 90% |
| E2E/UI | GCCWebApp.UAT.Tests | 80 | 75% |
| Performance | GCCWebApp.Performance.Tests | 10 | 80% |
| Accessibility | GCCWebApp.Accessibility.Tests | 8 | 100% |
| **TOTAL** | **7 projects** | **624+** | **87%** |

## Running Tests

```bash
# All tests
cd GCCWebApp-Tests/GCCWebApp.Tests
dotnet test

# Specific project
dotnet test GCCWebApp.Tests.csproj

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Playwright Setup
```bash
cd GCCWebApp-Tests/GCCWebApp.Tests/bin/Debug/net8.0
pwsh playwright.ps1 install
```

## Known Test Issues

| Issue | Impact | Fix |
|-------|--------|-----|
| Playwright browsers not installed | 86 E2E tests fail | `pwsh playwright.ps1 install` |
| API Key Handler mock setup | 12 auth tests fail | Replace extension method mocks |

## Test URLs

| Environment | URL |
|-------------|-----|
| Local | http://localhost:5101 |
| Production | https://gccwebapp.com |
| Swagger | http://localhost:5101/swagger |
| Auth Diagnostic | /Account/TestAuth?username=admin&password=TestPass123! |

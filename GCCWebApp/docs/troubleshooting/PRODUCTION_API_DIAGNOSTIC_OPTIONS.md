# Production API Diagnostic Options

## GCC Web Application - Certificate Tile View API Issue

**Date:** December 5, 2025
**Environment:** Test (gccwebapp.com)
**Prepared By:** Development Team

---

## Executive Summary

The Certificate Tile View public API is returning errors in the test environment (gccwebapp.com), preventing certificates from displaying on the main landing page. This document presents three options for diagnosing the root cause, along with risk assessments and recommendations.

---

## Current Situation

### What's Working
| Endpoint | Status | Method |
|----------|--------|--------|
| `/health` | OK | Direct DB query |
| `/api/certificate-tiles/filters` | OK | Direct DbContext |
| Database connectivity | OK | 175 certificates confirmed |

### What's Failing
| Endpoint | Status | Error |
|----------|--------|-------|
| `/api/certificate-tiles` | 400 Bad Request | "Failed to retrieve certificates" |
| `/api/certificate-tiles/autocomplete` | 400 Bad Request | "Failed to retrieve suggestions" |

### Root Cause Hypothesis

The failing endpoints use the **Repository/UnitOfWork pattern**, while working endpoints use **direct DbContext queries**. This suggests:

1. A dependency injection issue with repository services
2. A database query translation problem specific to SQL Server
3. A data integrity issue (null foreign keys) causing navigation property failures
4. A missing database migration or schema mismatch

---

## Option 1: Check Server Logs

### Description
Access the production server's Windows Event Viewer or IIS logs to view the actual exception details without any code changes.

### Implementation Steps
1. Log into AccuWebHosting Plesk panel
2. Navigate to Logs section
3. Filter for Application errors in the last 24 hours
4. Search for "GCCWebApp" or "certificate" related entries

### Advantages
| Benefit | Impact |
|---------|--------|
| No code deployment required | Zero risk to application |
| Immediate results | Minutes to diagnose |
| No application restart needed | Zero downtime |
| Preserves current stable state | No regression risk |

### Disadvantages
| Drawback | Impact |
|----------|--------|
| Requires hosting panel access | May need credentials |
| Logs may not capture full stack trace | Incomplete information |
| Log retention may be limited | Historical data may be lost |
| Shared hosting may limit log access | Platform dependency |

### Risk Level: **LOW**

### Time Estimate: 15-30 minutes

---

## Option 2: Add Diagnostic Endpoint

### Description
Deploy a dedicated `/api/diagnostics/test-repository` endpoint that explicitly tests the repository layer and returns detailed error information in a controlled manner.

### Implementation

```csharp
[Route("api/diagnostics")]
[ApiController]
public class DiagnosticsController : ControllerBase
{
    private readonly ICertificateRepository _certificateRepository;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    [HttpGet("test-repository")]
    public async Task<IActionResult> TestRepository()
    {
        var results = new Dictionary<string, object>();

        // Test 1: Direct context query
        try
        {
            var count = await _context.Certificates.CountAsync();
            results["DirectContext"] = new { Success = true, Count = count };
        }
        catch (Exception ex)
        {
            results["DirectContext"] = new { Success = false, Error = ex.Message };
        }

        // Test 2: Repository query
        try
        {
            var filter = new CertificateSearchFilter { Status = "active", Page = 1, PageSize = 1 };
            var (certs, total) = await _certificateRepository
                .GetAdvancedFilteredCertificatesAsync(filter);
            results["Repository"] = new { Success = true, Count = total };
        }
        catch (Exception ex)
        {
            results["Repository"] = new {
                Success = false,
                Error = ex.Message,
                InnerError = ex.InnerException?.Message,
                StackTrace = ex.StackTrace
            };
        }

        return Ok(results);
    }
}
```

### Advantages
| Benefit | Impact |
|---------|--------|
| Targeted testing | Tests exact failing code path |
| Controlled exposure | Only exposes diagnostic endpoint |
| Detailed error capture | Full exception details |
| Reusable for future issues | Long-term debugging tool |
| Can be secured with auth | Access control possible |

### Disadvantages
| Drawback | Impact |
|----------|--------|
| Requires deployment | Deployment process time |
| Adds code to production | Small code footprint increase |
| Potential security concern | Exposes internal errors (mitigated by auth) |
| Requires application restart | Brief downtime during deploy |

### Risk Level: **LOW-MEDIUM**

### Time Estimate: 1-2 hours (development + deployment)

### Security Considerations
- Endpoint can be protected with `[Authorize(Roles = "Admin")]`
- Can be removed after diagnosis
- Does not expose sensitive data, only error messages

---

## Option 3: Enable Detailed API Error Responses

### Description
Deploy configuration-based detailed error logging that returns full exception information in API error responses when enabled via `appsettings.json`.

### Implementation (Already Completed)

**Configuration (`appsettings.Production.json`):**
```json
{
  "FeatureFlags": {
    "ShowDetailedApiErrors": true  // Set to false for production
  }
}
```

**Controller Update:**
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error retrieving certificates");

    if (_showDetailedErrors)
    {
        return BadRequest(new {
            success = false,
            message = "Failed to retrieve certificates",
            error = ex.Message,
            innerError = ex.InnerException?.Message,
            stackTrace = ex.StackTrace,
            certificates = new List<object>(),
            totalCount = 0
        });
    }

    return BadRequest(new {
        success = false,
        message = "Failed to retrieve certificates",
        certificates = new List<object>(),
        totalCount = 0
    });
}
```

### Advantages
| Benefit | Impact |
|---------|--------|
| Configuration-driven | Easy to enable/disable |
| No code changes to toggle | Just config file update |
| Comprehensive error details | Full debugging information |
| Already implemented | Ready to deploy |
| Follows best practices | Feature flag pattern |

### Disadvantages
| Drawback | Impact |
|----------|--------|
| Exposes errors to all API consumers | Security consideration |
| Requires deployment | Deployment process time |
| Modifies core error handling | Broader code change |
| Must remember to disable | Operational overhead |
| Application restart required | Brief downtime |

### Risk Level: **MEDIUM**

### Time Estimate: 30-60 minutes (deployment only)

### Security Considerations
- Error details visible to any API caller while enabled
- Should be disabled before going live to production
- Stack traces may reveal internal architecture

---

## Comparison Matrix

| Criteria | Option 1: Logs | Option 2: Diagnostic Endpoint | Option 3: Error Responses |
|----------|---------------|------------------------------|---------------------------|
| **Risk Level** | Low | Low-Medium | Medium |
| **Deployment Required** | No | Yes | Yes |
| **Downtime** | None | Brief | Brief |
| **Code Changes** | None | New endpoint | Modified catch blocks |
| **Security Exposure** | None | Controlled | All API callers |
| **Reusability** | N/A | High | Medium |
| **Time to Diagnose** | 15-30 min | 1-2 hours | 30-60 min |
| **Completeness of Info** | Variable | High | High |

---

## Recommendation

### Primary Recommendation: **Option 1 (Check Server Logs)**

**Rationale:**
- Zero risk approach
- No deployment required
- Fastest path to diagnosis if logs are accessible
- Preserves current application state

### Fallback Recommendation: **Option 2 (Diagnostic Endpoint)**

**Rationale:**
- If logs are insufficient, this provides targeted testing
- Lower security risk than exposing errors to all callers
- Can be protected with authentication
- Useful as a permanent debugging tool

### When to Use Option 3

Option 3 (Detailed Error Responses) is appropriate when:
- Quick turnaround is critical
- The environment is truly isolated (test only)
- Other options have been exhausted
- You need to see errors from actual user workflows

---

## Next Steps

### If Proceeding with Option 1:
1. Access AccuWebHosting Plesk panel
2. Navigate to Logs > Application Logs
3. Filter for errors in the last 48 hours
4. Share relevant log entries for analysis

### If Proceeding with Option 2:
1. Review and approve diagnostic endpoint code
2. Deploy to test environment
3. Access `https://gccwebapp.com/api/diagnostics/test-repository`
4. Analyze response and identify root cause
5. Remove or secure endpoint after diagnosis

### If Proceeding with Option 3:
1. Deploy current build with detailed error logging enabled
2. Test `https://gccwebapp.com/api/certificate-tiles?page=1&pageSize=10`
3. Capture and analyze error response
4. Identify and fix root cause
5. Set `ShowDetailedApiErrors: false` before production release

---

## Appendix: Potential Root Causes

Based on the failure pattern, likely causes include:

1. **Null Navigation Properties**
   - Certificates with missing Company, Product, or CertificationType
   - Query attempts to access `.Name` on null objects

2. **EF Core Query Translation**
   - Complex LINQ queries not translating to SQL Server correctly
   - Different behavior between development (ANUKI01) and production SQL Server

3. **Connection Pool Exhaustion**
   - Repository creating connections that aren't being disposed
   - Would affect repository queries but not simple context queries

4. **Schema Mismatch**
   - Missing columns or tables in production
   - Migration not applied correctly

5. **Data Type Mismatch**
   - Date/time format differences
   - String collation issues

---

*Document Version: 1.0*
*Last Updated: December 5, 2025*

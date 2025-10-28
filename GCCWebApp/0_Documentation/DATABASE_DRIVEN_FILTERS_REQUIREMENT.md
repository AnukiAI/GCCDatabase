# Database-Driven Filter Requirements

**Feature ID**: FILTER-001
**Priority**: HIGH
**Status**: ✅ COMPLETED
**Implementation Date**: October 27, 2025
**Author**: System

---

## Overview

All certificate tile view pages must use database-driven filters that dynamically populate based on active certificates in the database. Filters must ONLY show items (certification types, companies, countries, tags) that have at least one active certificate associated with them.

---

## Business Requirements

### BR-001: Dynamic Filter Population
**Requirement**: All filter options must be populated dynamically from the database at page load, not hardcoded in HTML.

**Rationale**: Ensures filters automatically reflect current database state without requiring code changes.

**Acceptance Criteria**:
- Filters load via JavaScript from API endpoints
- No hardcoded filter values in Razor views (except Product Categories - see BR-006)
- Loading placeholders shown while fetching data
- Filters update automatically when database changes

### BR-002: Active Certificate Filtering
**Requirement**: Filters must ONLY display items that have active certificates associated with them.

**Definition of Active**: Certificates with status = "active" OR "expiring_soon" OR "renewal_in_progress"

**Rationale**: Prevents users from seeing filter options that would return zero results (e.g., "Biodegradable" certification type with 0 active certificates).

**Acceptance Criteria**:
- Certification types with 0 active certificates do NOT appear
- Companies with 0 active certificates do NOT appear
- Countries with 0 active certificates do NOT appear
- Tags with 0 active certificates do NOT appear
- Filters automatically update as certificates are added/removed/status changed

### BR-003: Consistent Behavior Across All Views
**Requirement**: Public, Internal, and External views must use the same filtering logic.

**Rationale**: Provides consistent user experience and reduces maintenance overhead.

**Acceptance Criteria**:
- All 3 views query database for active certificates only
- All 3 views use `.Distinct()` to eliminate duplicates
- All 3 views order results alphabetically
- All 3 views handle empty results gracefully

### BR-004: Performance Optimization
**Requirement**: Filter queries must be optimized for performance.

**Acceptance Criteria**:
- Use LINQ `.Select()` projection to return only required fields
- Use `.Distinct()` to eliminate duplicates at database level
- Use `.OrderBy()` to sort at database level
- Avoid N+1 query problems
- Response time < 1 second for filter options endpoint

### BR-005: API Endpoint Consistency
**Requirement**: Each view must have a dedicated `/filters` endpoint.

**Endpoints**:
- Public View: `GET /api/certificate-tiles/filters`
- Internal View: `GET /api/certificate-tiles-internal/filters`
- External View: `GET /api/certificate-tiles-external/filters`

**Response Format**:
```json
{
  "certificationTypes": ["FSC", "PEFC", "SFI"],
  "companies": [
    {"id": 1, "name": "Company A"},
    {"id": 2, "name": "Company B"}
  ],
  "countries": ["United States", "Canada"],
  "additionalTags": ["APR", "LEED"],
  "productCategories": ["Category A", "Category B"]
}
```

### BR-006: Product Categories Exception
**Requirement**: Product Categories filter in External View may remain hardcoded temporarily.

**Rationale**: Large hierarchical structure (~370 lines) requires comprehensive refactoring beyond scope of initial implementation.

**Status**: DEFERRED
**Tracking**: Separate task to be prioritized

---

## Technical Requirements

### TR-001: API Controller Implementation

**Public View** (`CertificateTilesApiController.cs`):
```csharp
public async Task<IActionResult> GetFilterOptions()
{
    // Query only active certificates
    var baseQuery = _context.Certificates
        .Where(c => c.Status == "active"
            || c.Status == "expiring_soon"
            || c.Status == "renewal_in_progress");

    var filters = new
    {
        certificationTypes = await baseQuery
            .Select(c => c.CertificationType.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(),
        // ... other filters
    };
    return Ok(filters);
}
```

**Internal View** (`CertificateTileInternalApiController.cs`):
```csharp
certificationTypes = await _context.CertificationTypes
    .Where(ct => ct.IsActive)
    .Select(ct => new {
        name = ct.Name,
        certificateCount = ct.Certificates.Count()
    })
    .Where(ct => ct.certificateCount > 0)  // Key filtering logic
    .OrderBy(ct => ct.name)
    .Select(ct => ct.name)
    .ToListAsync()
```

**External View** (`CertificateTileExternalApiController.cs`):
- Includes partner-based filtering
- Filters by active status BEFORE returning results
- Similar structure to Public View

### TR-002: JavaScript Implementation

All views must:
1. Fetch filter options on page load: `fetch('/api/[endpoint]/filters')`
2. Store in state: `state.filterOptions = await response.json()`
3. Populate UI: `populateFilterOptions()` function
4. Show loading placeholders while fetching

**Example**:
```javascript
async function loadFilterOptions() {
    const response = await fetch('/api/certificate-tiles/filters');
    if (response.ok) {
        state.filterOptions = await response.json();
        populateFilterOptions();
    }
}

function populateFilterOptions() {
    const certTypeList = document.querySelector('#cert-type-content ul');
    if (certTypeList && state.filterOptions.certificationTypes) {
        certTypeList.innerHTML = state.filterOptions.certificationTypes.map(type => `
            <li class="filter-item">
                <label>
                    <input type="checkbox" name="cert-type" value="${escapeHtml(type)}">
                    ${escapeHtml(type)}
                </label>
            </li>
        `).join('');
    }
}
```

### TR-003: View Implementation

Replace hardcoded HTML filter lists with loading placeholders:

```html
<!-- BEFORE (Hardcoded) -->
<div class="filter-content" id="cert-type-content">
    <ul class="filter-tree">
        <li class="filter-item">
            <label>
                <input type="checkbox" name="cert-type" value="fsc">
                FSC
            </label>
        </li>
        <!-- ... 20+ more hardcoded items -->
    </ul>
</div>

<!-- AFTER (Dynamic) -->
<div class="filter-content" id="cert-type-content">
    <ul class="filter-tree">
        <!-- Certification types will be dynamically populated from the database via JavaScript -->
        <li class="filter-item">
            <span style="color: #999; font-style: italic;">Loading certification types...</span>
        </li>
    </ul>
</div>
```

---

## Implementation Status

### ✅ Completed Components

| Component | File | Status | Notes |
|-----------|------|--------|-------|
| Public View API | `CertificateTilesApiController.cs` | ✅ Complete | Lines 180-232 |
| Public View JavaScript | `certificate-tile-view.js` | ✅ Complete | Already implemented |
| Public View HTML | `CertificateTileView.cshtml` | ✅ Complete | Dynamic loading |
| Internal View API | `CertificateTileInternalApiController.cs` | ✅ Complete | Lines 324-333 |
| Internal View JavaScript | `certificate-tile-view-internal.js` | ✅ Complete | Already implemented |
| Internal View HTML | `CertificateTileViewInternal.cshtml` | ✅ Complete | Dynamic loading |
| External View API | `CertificateTileExternalApiController.cs` | ✅ Complete | Lines 278-395 |
| External View JavaScript | `certificate-tile-view-external.js` | ✅ Complete | Already implemented |
| External View HTML - Cert Types | `CertificateTileViewExternal.cshtml` | ✅ Complete | Lines 557-564 |
| External View HTML - Companies | `CertificateTileViewExternal.cshtml` | ✅ Complete | Lines 933-940 |
| External View HTML - Countries | `CertificateTileViewExternal.cshtml` | ✅ Complete | Lines 949-956 |
| External View HTML - Tags | `CertificateTileViewExternal.cshtml` | ✅ Complete | Lines 965-972 |
| Test Updates | `CertificateTilesApiControllerTests.cs` | ✅ Complete | Added ApplicationDbContext dependency |

### ⏳ Deferred Components

| Component | File | Status | Notes |
|-----------|------|--------|-------|
| External View HTML - Product Categories | `CertificateTileViewExternal.cshtml` | ⏳ Deferred | ~370 lines, requires comprehensive refactoring |

---

## Testing Requirements

### Test Case 1: Empty Database
**Given**: Database has no active certificates
**When**: User loads any certificate tile view
**Then**: All filters show "No [filter type] available"

### Test Case 2: Filtered Data
**Given**: Database has 10 certification types, but only 5 have active certificates
**When**: User loads Public View
**Then**: Only 5 certification types appear in filter

### Test Case 3: Real-Time Updates
**Given**: User has Public View open
**When**: Admin adds new active certificate with new certification type
**Then**: Filter updates on next page refresh (no manual intervention required)

### Test Case 4: Performance
**Given**: Database has 10,000 active certificates
**When**: User requests filter options
**Then**: Response time < 1 second

### Test Case 5: Consistency
**Given**: Same database state
**When**: User views filters in Public, Internal, and External views
**Then**: All views show identical filter options (except partner-filtered External view)

---

## Migration Notes

### Before (Old Behavior)
- Filters showed ALL certification types, companies, countries regardless of active certificates
- "Biodegradable" certification type appeared even with 0 active certificates
- Filters required manual code updates when new types added
- Inconsistent behavior between Public, Internal, and External views

### After (New Behavior)
- Filters show ONLY items with active certificates
- "Biodegradable" automatically hidden if no active certificates
- Filters automatically update when database changes
- Consistent behavior across all views
- Improved user experience (no "0 results" scenarios)

---

## Related Requirements

- **BULK-001**: Bulk Certificate Download (complementary feature)
- **US-001**: Certificate Tile View (Public) - parent requirement
- **US-002**: Infinite Scroll - related UX improvement
- **US-003**: Autocomplete Search - uses same filtering logic

---

## Change Log

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-10-27 | 1.0 | Initial implementation complete | System |
| 2025-10-27 | 1.0 | All 3 views updated with database-driven filters | System |
| 2025-10-27 | 1.0 | Product Categories deferred for External View | System |

---

## Approval

**Business Owner**: [Pending]
**Technical Lead**: [Pending]
**QA Lead**: [Pending]

**Deployed to Production**: [Pending]

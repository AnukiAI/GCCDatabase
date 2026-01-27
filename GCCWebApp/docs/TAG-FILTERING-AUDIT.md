# TAG FILTERING AUDIT - GCCWebApp
**Date:** January 26, 2026
**Status:** FIXES IMPLEMENTED

---

## Executive Summary

This audit identified **5 critical issues** with tag filtering logic that explain why:
1. **External tags are not showing in the Certificate public view**
2. **Internal view doesn't appear to show the right tags**

The root cause was **inconsistent TagType validation** across API endpoints.

### Fixes Applied (January 26, 2026)

| Issue | File | Fix Applied |
|-------|------|-------------|
| #1 Public API | `CertificateTilesApiController.cs:148-157` | Added validation to only accept external tags |
| #2 External API Filter | `CertificateTileExternalApiController.cs:165-170` | Added `t.TagType == "external"` to query |
| #2 External API Display | `CertificateTileExternalApiController.cs:201` | Filter tags to external only in response |
| #3 Internal API | `CertificateTileInternalApiController.cs:450-469` | Added separate `internalTags` and `externalTags` lists |

---

## 1. Tag Model Structure

**File:** `Models/Tag.cs`

| Property | Type | Values |
|----------|------|--------|
| `Id` | int | Primary key |
| `Name` | string | Tag display name |
| `TagType` | string | `"internal"`, `"external"`, `"csf"` |
| `IsActive` | bool | Active/inactive status |

**Subclasses:**
- `InternalTag` - Sets TagType = "internal"
- `ExternalTag` - Sets TagType = "external"
- `CSFAttribute` - Sets TagType = "csf"

---

## 2. Tag Filtering By Endpoint

### Overview Matrix

| Endpoint | Access Level | Main Query Filter | Filter Dropdown | Status |
|----------|--------------|-------------------|-----------------|--------|
| `/api/certificate-tiles` | Public | **NO TagType check** | External only | **BUG** |
| `/api/CertificateTileExternal` | External Auth | **NO TagType check** | External only | **BUG** |
| `/api/certificate-tiles-internal` | Analyst/Admin | No TagType check | All tags | OK |
| CertificateController | Analyst/Admin | Splits by type | Split by type | OK |

---

## 3. Critical Issues

### ISSUE #1: Public View - External Tags Not Showing

**Location:** `CertificateTilesApiController.cs` Lines 320-326

**What's Happening:**
```csharp
// Filter dropdown correctly shows ONLY external tags
additionalTags = await baseQuery
    .SelectMany(c => c.Tags)
    .Where(t => t.IsActive && t.TagType == "external")  // ✓ Correct
    .Select(t => t.Name)
    .ToListAsync()
```

**BUT the actual certificate query (Lines 148-152) does NOT enforce this:**
```csharp
// Main query accepts ANY tag name - NO TagType validation!
if (!string.IsNullOrEmpty(tagsFilter))
{
    filter.Tags = tagsFilter.Split(',').ToList();  // ✗ No type check
}
```

**Result:** The filter dropdown shows external tags, but the query may not properly filter certificates by those tags.

---

### ISSUE #2: External User View - Wrong Tags Displayed

**Location:** `CertificateTileExternalApiController.cs` Lines 165-170

**Problem:**
```csharp
// External user can filter by ANY tag (including internal!)
if (!string.IsNullOrWhiteSpace(tags))
{
    var tagsList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
    query = query.Where(c => c.Tags.Any(t => tagsList.Contains(t.Name)));
    // ✗ MISSING: && t.TagType == "external"
}
```

**Filter options correctly restrict to external (Lines 513-527):**
```csharp
additionalTags = await baseQuery
    .SelectMany(c => c.Tags)
    .Where(t => t.IsActive && t.TagType == "external")  // ✓ Correct
```

**Result:** Filter shows external tags, but certificates with internal tags can still slip through.

---

### ISSUE #3: Internal View - Inconsistent Tag Display

**Location:** `CertificateTileInternalApiController.cs`

**Response correctly splits tags (Lines 178-179):**
```csharp
ExternalTags = string.Join(", ", c.Tags.Where(t => t.TagType == "external").Select(t => t.Name)),
InternalTags = string.Join(", ", c.Tags.Where(t => t.TagType == "internal").Select(t => t.Name)),
```

**BUT filter options show ALL tags together (Lines 450-469):**
```csharp
tags = await _context.Tags
    .Where(t => t.IsActive)  // ALL tags, no type filter
    .Select(t => t.Name)
    .ToListAsync(),
```

**Result:** Confusing UX - tags are split in display but combined in filter.

---

### ISSUE #4: Repository Level - No Type Validation

**Location:** `CertificateRepository.cs` Lines 201-202

```csharp
// Repository accepts tag IDs without checking TagType
if (filter.TagIds != null && filter.TagIds.Any())
    query = query.Where(c => c.Tags.Any(t => filter.TagIds.Contains(t.Id)));
    // ✗ No TagType validation at repository level
```

**Impact:** All controllers rely on this, but none pass TagType restrictions.

---

### ISSUE #5: CSF Tags Not Consistently Handled

**Various Locations:**

- `AdminService.cs` handles CSF tags with `.Where(t => t.TagType == "csf")`
- Certificate views don't explicitly handle CSF tags
- Unclear if CSF should display as external, internal, or separately

---

## 4. Security Implications

| Risk | Severity | Description |
|------|----------|-------------|
| Internal Tag Exposure | **HIGH** | External users can see internal tag names in API responses |
| Tag Filter Bypass | **MEDIUM** | Users can manually construct API requests with internal tags |
| No Server Validation | **MEDIUM** | No defense-in-depth at repository level |

---

## 5. File Locations - All Tag Filtering Points

### Backend (C#)

| File | Lines | Purpose |
|------|-------|---------|
| `Controllers/Api/CertificateTilesApiController.cs` | 70-336 | Public tile API |
| `Controllers/Api/CertificateTileExternalApiController.cs` | 56-544 | External tile API |
| `Controllers/Api/CertificateTileInternalApiController.cs` | 54-479 | Internal tile API |
| `Controllers/CertificateController.cs` | 350, 616, 689, 958 | Certificate CRUD |
| `Repositories/CertificateRepository.cs` | 201-202 | Query building |
| `Services/AdminService.cs` | 967, 982, 1023, 1046 | Admin operations |
| `Models/Tag.cs` | All | Entity definition |

### Frontend (JavaScript)

| File | Purpose |
|------|---------|
| `wwwroot/js/certificate-tile-view.js` | Public view - tag filter population |
| `wwwroot/js/certificate-tile-view-external.js` | External view - tag handling |
| `wwwroot/js/certificate-tile-view-internal.js` | Internal view - tag handling |
| `wwwroot/js/tags.js` | Tag management admin UI |

### Views (Razor)

| File | Purpose |
|------|---------|
| `Views/Certificate/CertificateTileView.cshtml` | Public certificate view |
| `Views/Certificate/CertificateTileViewExternal.cshtml` | External user view (21 columns, NO internal tag column) |
| `Views/Certificate/CertificateTileViewInternal.cshtml` | Internal user view (both tag types) |
| `Views/Admin/Tags.cshtml` | Tag admin page |

---

## 6. Recommended Fixes

### Fix #1: Public API - Add TagType Validation
**File:** `CertificateTilesApiController.cs`

```csharp
// Line ~150 - Add TagType validation
if (!string.IsNullOrEmpty(tagsFilter))
{
    var tagNames = tagsFilter.Split(',').ToList();
    // Validate these are external tags only
    var validExternalTags = await _context.Tags
        .Where(t => tagNames.Contains(t.Name) && t.TagType == "external" && t.IsActive)
        .Select(t => t.Name)
        .ToListAsync();
    filter.Tags = validExternalTags;
}
```

### Fix #2: External API - Enforce External Tags Only
**File:** `CertificateTileExternalApiController.cs`

```csharp
// Line ~169 - Add TagType restriction
if (!string.IsNullOrWhiteSpace(tags))
{
    var tagsList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
    query = query.Where(c => c.Tags.Any(t =>
        tagsList.Contains(t.Name) && t.TagType == "external")); // Added type check
}
```

### Fix #3: Internal API - Split Filter Options
**File:** `CertificateTileInternalApiController.cs`

```csharp
// Return separate filter lists
internalTags = await _context.Tags
    .Where(t => t.IsActive && t.TagType == "internal")
    .Select(t => t.Name).ToListAsync(),

externalTags = await _context.Tags
    .Where(t => t.IsActive && t.TagType == "external")
    .Select(t => t.Name).ToListAsync()
```

### Fix #4: Repository Defense-in-Depth (Optional)
**File:** `CertificateRepository.cs`

Add optional TagType parameter to filtering methods for additional security.

---

## 7. Testing Checklist

After fixes, verify:

- [ ] Public view only shows certificates with external tags
- [ ] Public view filter dropdown only shows external tags
- [ ] External user view only shows external tags
- [ ] External user cannot filter by internal tag names
- [ ] Internal user view shows both tag types properly
- [ ] Internal user filter has separate internal/external sections
- [ ] Admin Tags page can manage all tag types
- [ ] CSF tags handled consistently

---

## 8. Current State Diagram

```
                    ┌─────────────────────────────────────────────────┐
                    │              TAG FILTERING FLOW                 │
                    └─────────────────────────────────────────────────┘
                                           │
            ┌──────────────────────────────┼──────────────────────────────┐
            │                              │                              │
            ▼                              ▼                              ▼
    ┌───────────────┐            ┌─────────────────┐            ┌────────────────┐
    │  PUBLIC VIEW  │            │  EXTERNAL VIEW  │            │ INTERNAL VIEW  │
    │ /api/cert-    │            │ /api/CertTile-  │            │ /api/cert-     │
    │ tiles         │            │ External        │            │ tiles-internal │
    └───────┬───────┘            └────────┬────────┘            └───────┬────────┘
            │                             │                             │
            ▼                             ▼                             ▼
    ┌───────────────┐            ┌─────────────────┐            ┌────────────────┐
    │ Filter Options│            │ Filter Options  │            │ Filter Options │
    │ External Only │            │ External Only   │            │ ALL Tags       │
    │     ✓         │            │       ✓         │            │      ✓         │
    └───────┬───────┘            └────────┬────────┘            └───────┬────────┘
            │                             │                             │
            ▼                             ▼                             ▼
    ┌───────────────┐            ┌─────────────────┐            ┌────────────────┐
    │ Main Query    │            │ Main Query      │            │ Main Query     │
    │ NO TYPE CHECK │            │ NO TYPE CHECK   │            │ NO TYPE CHECK  │
    │     ✗ BUG     │            │     ✗ BUG       │            │      ✓ OK      │
    └───────────────┘            └─────────────────┘            └────────────────┘
```

---

## 9. Priority Action Items

| Priority | Action | Effort |
|----------|--------|--------|
| **P1** | Fix External API tag validation | Low |
| **P1** | Fix Public API tag validation | Low |
| **P2** | Split Internal API filter options | Medium |
| **P3** | Add repository-level validation | Medium |
| **P3** | Define CSF tag handling strategy | Medium |

---

## Document History

| Date | Author | Change |
|------|--------|--------|
| 2026-01-26 | Claude | Initial audit |

# CSS Inline Styles Analysis Report

**Generated**: 2026-01-11
**Issue**: Certificate tile views have inconsistent styling due to inline CSS overriding shared CSS

## Executive Summary

| View | File Lines | Inline CSS Lines | Shared CSS Usage |
|------|------------|------------------|------------------|
| **Public** | 246 | 0 | Uses `certificatetileview.css` only |
| **External** | 877 | 471 (lines 15-486) | Overrides shared CSS with `!important` |
| **Internal** | 916 | 510 (lines 16-526) | Overrides shared CSS with `!important` |

**Root Cause**: ~970 lines of inline CSS in External/Internal views override the shared stylesheet, making it impossible to maintain consistent styling.

---

## Detailed Analysis

### 1. Styles Duplicated Between External & Internal (SHOULD BE IN SHARED CSS)

These styles are **identical** in both views and should be consolidated:

| Style Category | Lines in External | Lines in Internal | Status |
|----------------|-------------------|-------------------|--------|
| Loading spinner animation | 76-89 | 55-69 | **Duplicate** |
| Sidebar toggle tab | 152-192 | 75-116 | **Duplicate** |
| Sidebar collapse button | 194-228 | 118-152 | **Duplicate** |
| Sidebar collapsed state | 230-247 | 154-171 | **Duplicate** |
| Bulk actions bar | 253-271 | 221-238 | **Duplicate** |
| Select cell styling | 278-285 | 246-253 | **Duplicate** |
| Action button hover states | 305-330 | 282-313 | **Duplicate** |
| Reduce spacing styles | 346-383 | 328-366 | **Duplicate** |
| Grid header cells | 390-409 | 373-392 | **Duplicate** |
| Grid body cells | 411-422 | 394-405 | **Duplicate** |
| Zebra striping | 448-459 | 488-499 | **Duplicate** |
| Status badge styling | 461-465 | 501-505 | **Duplicate** |
| Date column monospace | 467-472 | 507-512 | **Duplicate** |
| Certificate number color | 474-480 | 514-520 | **Duplicate** |
| Grid cell text color | 482-485 | 522-525 | **Duplicate** |

**Estimated duplicated lines**: ~350 lines

### 2. View-Specific Styles (SHOULD REMAIN SEPARATE OR USE CSS VARIABLES)

| Style | External View | Internal View | Reason for Difference |
|-------|---------------|---------------|----------------------|
| Grid columns | 21 columns (no Internal Tag) | 22 columns (has Internal Tag) | Column structure differs |
| Actions column width | 80px (2 buttons) | 90px (3 buttons) | Different button count |
| Table container min-width | 2300px | 2450px | Different column total |
| Edit button styling | Not present | Lines 23-42 | Internal-only feature |
| Product name `.certificate-card` | Lines 22-29 | Lines 44-51 | Identical - should be shared |

### 3. Shared CSS Being Overridden

The following styles exist in `certificatetileview.css` but are being overridden by inline CSS with `!important`:

| Selector | Shared CSS Line | Being Overridden By |
|----------|-----------------|---------------------|
| `.loading-spinner` | 12-19 | External:76-89, Internal:55-69 |
| `.sidebar` | 241+ | External:152-247, Internal:75-171 |
| `.grid-cell[data-column="location"]` | 841-843 | External:444-446, Internal:324-326 |
| `.loading-indicator` | 1327-1348 | External:376-383, Internal:358-366 |
| `.table-grid-container` | 1439-1443 | External:104-108, Internal:186-189 |
| `.grid-header-cell` | 1445-1477 | External:390-409, Internal:373-392 |
| `.grid-cell` | 1485-1505 | External:411-485, Internal:394-525 |
| `.actions-column` | 1550-1575 | External:287-343, Internal:255-313 |

### 4. `!important` Usage Analysis

Both inline style blocks use `!important` extensively, which:
- Prevents inheritance from shared CSS
- Makes maintenance difficult
- Causes specificity wars
- Creates inconsistencies when shared CSS is updated

**Count of `!important` declarations**:
- External view: ~85 occurrences
- Internal view: ~92 occurrences

---

## Recommendations

### Priority 1: Immediate Consolidation (High Impact)

Move these shared styles to `certificatetileview.css`:

1. **Loading spinner animation** - Same in both views
2. **Sidebar toggle/collapse buttons** - Same styling, just accessibility
3. **Bulk actions bar** - Same padding and spacing
4. **Grid cell base styling** - Font size, padding, line-height
5. **Zebra striping** - Identical in both
6. **Status badges** - Same styling
7. **Date/certificate number formatting** - Same fonts

### Priority 2: Use CSS Variables for View Differences

```css
/* In certificatetileview.css */
:root {
  --table-grid-columns: 21; /* External default */
  --table-min-width: 2300px;
  --actions-column-width: 80px;
}

/* In Internal view only */
:root {
  --table-grid-columns: 22;
  --table-min-width: 2450px;
  --actions-column-width: 90px;
}
```

### Priority 3: Remove `!important` Declarations

After consolidation, remove all `!important` declarations from inline styles. Rely on proper CSS specificity instead.

---

## Files Affected

| File | Current Lines | After Consolidation |
|------|---------------|---------------------|
| `certificatetileview.css` | ~1650 | ~2000 (+350 shared styles) |
| `CertificateTileViewExternal.cshtml` | 877 | ~450 (-427 inline CSS) |
| `CertificateTileViewInternal.cshtml` | 916 | ~460 (-456 inline CSS) |

---

## Implementation Plan

### Step 1: Add Shared Styles to CSS File
Move duplicated styles to `certificatetileview.css` with proper specificity.

### Step 2: Add View-Specific Classes
Add `.external-view` and `.internal-view` classes to body/container for view-specific overrides.

### Step 3: Update View Files
1. Remove inline `<style>` blocks
2. Add view-specific class to container
3. Keep only truly unique inline styles (grid column definitions)

### Step 4: Test All Views
Verify consistent appearance across:
- Public tile view
- External tile view (tiles and table)
- Internal tile view (tiles and table)

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Breaking existing layout | Medium | High | Test each change incrementally |
| Browser compatibility | Low | Medium | Use standard CSS, avoid new features |
| Performance impact | Low | Low | Fewer inline styles = faster parsing |
| Merge conflicts | Low | Medium | Coordinate with other changes |

---

## Appendix: Side-by-Side Comparison of Key Styles

### Loading Spinner (Identical)

**External (lines 76-89)**:
```css
.loading-spinner {
    width: 24px;
    height: 24px;
    border: 3px solid #f3f3f3;
    border-top: 3px solid #007bff;
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin-right: 12px;
}
```

**Internal (lines 55-69)**:
```css
.loading-spinner {
    width: 24px;
    height: 24px;
    border: 3px solid #f3f3f3;
    border-top: 3px solid #007bff;
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin-right: 12px;
}
```
**Status**: IDENTICAL - should be in shared CSS

### Grid Cell Styling (Identical)

**External (lines 411-422)**:
```css
.table-grid .grid-cell {
    min-height: 36px !important;
    padding: 6px 10px !important;
    font-size: 16px !important;
    line-height: 1.4 !important;
    white-space: normal !important;
    word-wrap: break-word !important;
    overflow-wrap: break-word !important;
    max-width: 100% !important;
}
```

**Internal (lines 394-405)**:
```css
.table-grid .grid-cell {
    min-height: 36px !important;
    padding: 6px 10px !important;
    font-size: 16px !important;
    line-height: 1.4 !important;
    white-space: normal !important;
    word-wrap: break-word !important;
    overflow-wrap: break-word !important;
    max-width: 100% !important;
}
```
**Status**: IDENTICAL - should be in shared CSS

### Grid Template Columns (Different by Design)

**External (21 columns)**:
```css
.table-grid {
    grid-template-columns:
        36px 80px 110px 160px 130px 75px 95px 95px 170px 170px 170px 130px 130px 140px 100px 80px 100px 85px 85px 85px 85px;
}
```

**Internal (22 columns)**:
```css
.table-grid {
    grid-template-columns:
        36px 90px 115px 160px 130px 75px 95px 95px 170px 170px 170px 130px 130px 140px 100px 80px 100px 100px 85px 85px 85px 85px;
}
```
**Status**: DIFFERENT - column 2 (Actions) and column 18 (Internal Tag) differ. Keep view-specific.

---

*End of Report*

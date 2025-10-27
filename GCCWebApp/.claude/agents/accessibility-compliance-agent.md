# Accessibility Compliance Agent - GCC Database Solution

## Agent Identity

**Agent Name:** Accessibility Compliance Agent | **Agent ID:** AG-A11Y01 | **Version:** 1.0 | **Last Updated:** 2025-10-24

---

## Agent Purpose

### Primary Objective
Ensure WCAG 2.1 Level AA compliance across all views, components, and user interactions in the GCC Database Solution ASP.NET Core MVC application.

### Scope & Success Criteria

**Responsible for:**
- WCAG 2.1 Level AA automated testing
- Color contrast validation (4.5:1 for text, 3:1 for UI components)
- Keyboard navigation testing
- Screen reader compatibility audits
- ARIA label and role validation
- Focus management verification
- Form accessibility testing
- Semantic HTML structure validation

**NOT responsible for:**
- Backend accessibility (server-side)
- PDF accessibility (QuestPDF output)
- Third-party library accessibility (Bootstrap, jQuery)
- Content authoring (user-generated content)

**Success when:**
- 100% WCAG 2.1 Level AA compliance on all public pages
- 95%+ compliance on admin pages
- Zero critical accessibility violations
- All interactive elements keyboard-accessible
- Screen reader compatible throughout
- Color contrast meets/exceeds standards

---

## Core Capabilities

### Function 1: WCAG 2.1 Automated Audit

**Description:** Comprehensive automated accessibility testing against WCAG 2.1 Level AA standards

**Output Example:**

```markdown
# WCAG 2.1 Level AA Compliance Audit

**Audit Date:** October 24, 2025
**Auditor:** AG-A11Y01 (Accessibility Compliance Agent)
**Pages Tested:** 23
**Overall Compliance:** 87% ⚠️
**Target:** 100% (WCAG 2.1 Level AA)

---

## Executive Summary

**Compliance Status by Category:**
- ✅ Perceivable: 92% (11/12 criteria met)
- ⚠️ Operable: 78% (7/9 criteria met)
- ✅ Understandable: 95% (19/20 criteria met)
- ⚠️ Robust: 82% (9/11 criteria met)

**Violations by Severity:**
- 🔴 Critical: 3 (fix immediately)
- 🟡 Serious: 8 (fix this sprint)
- 🟢 Moderate: 15 (fix next sprint)
- 🔵 Minor: 22 (backlog)

**Total Issues:** 48 violations across 23 pages

---

## Critical Violations (Must Fix Immediately)

### 🔴 CRITICAL #1: Missing Form Labels
**WCAG Criterion:** 3.3.2 Labels or Instructions (Level A)
**Impact:** Screen reader users cannot identify form fields
**Pages Affected:** 5 pages
**Violation Count:** 12 instances

**Location:** Views/Certificate/Create.cshtml (Lines 45-67)

**Current Code:**
```html
<!-- ❌ VIOLATION: Input has no associated label -->
<div class="form-group">
    <input type="text"
           id="certificate-number"
           name="CertificateNumber"
           class="form-control"
           placeholder="Enter certificate number">
</div>

<div class="form-group">
    <input type="text"
           id="company-name"
           name="CompanyName"
           class="form-control"
           placeholder="Enter company name">
</div>
```

**Issue:**
- Inputs have no `<label>` elements
- Placeholder is NOT a substitute for label
- Screen readers cannot identify field purpose
- Keyboard users have no click target to focus field

**Fix Required:**
```html
<!-- ✅ CORRECT: Explicit label association -->
<div class="form-group">
    <label for="certificate-number" class="form-label">
        Certificate Number <span class="required">*</span>
    </label>
    <input type="text"
           id="certificate-number"
           name="CertificateNumber"
           class="form-control"
           placeholder="e.g., 24-0366"
           aria-required="true"
           aria-describedby="certificate-number-help">
    <small id="certificate-number-help" class="form-text text-muted">
        Format: YY-NNNN
    </small>
</div>

<div class="form-group">
    <label for="company-name" class="form-label">
        Company Name <span class="required">*</span>
    </label>
    <input type="text"
           id="company-name"
           name="CompanyName"
           class="form-control"
           placeholder="e.g., Acme Corporation"
           aria-required="true">
</div>
```

**Benefits:**
- ✅ Screen reader announces label when field focused
- ✅ Clicking label focuses input (larger click target)
- ✅ `aria-required` announces required status
- ✅ `aria-describedby` provides additional context

**Other Affected Pages:**
- Views/Certificate/Edit.cshtml (8 inputs)
- Views/Admin/UserManagement.cshtml (6 inputs)
- Views/Admin/Tags.cshtml (3 inputs)
- Views/Company/Index.cshtml (4 inputs)
- Views/Admin/ProductManagement.cshtml (5 inputs)

**Priority:** 🔴 CRITICAL (fix today)
**Estimated Effort:** 2 hours

---

### 🔴 CRITICAL #2: Insufficient Color Contrast
**WCAG Criterion:** 1.4.3 Contrast (Minimum) (Level AA)
**Impact:** Users with low vision cannot read text
**Pages Affected:** All pages (15 instances)
**Violation Count:** 15 instances

**Location:** wwwroot/css/site.css (Lines 234-245)

**Current Code:**
```css
/* ❌ VIOLATION: Contrast ratio 3.8:1 (below 4.5:1 minimum) */
.text-muted {
    color: #999999; /* Light gray on white background */
}

/* ❌ VIOLATION: Contrast ratio 2.1:1 (severely below minimum) */
.table-cell-secondary {
    color: #cccccc; /* Very light gray */
    font-size: 14px;
}

/* ❌ VIOLATION: Contrast ratio 3.2:1 */
.form-help-text {
    color: #aaaaaa;
}
```

**Color Contrast Analysis:**
| Element | Foreground | Background | Ratio | Required | Status |
|---------|------------|------------|-------|----------|--------|
| .text-muted | #999999 | #FFFFFF | 3.8:1 | 4.5:1 | ❌ FAIL |
| .table-cell-secondary | #CCCCCC | #FFFFFF | 2.1:1 | 4.5:1 | ❌ FAIL |
| .form-help-text | #AAAAAA | #FFFFFF | 3.2:1 | 4.5:1 | ❌ FAIL |

**Fix Required:**
```css
/* ✅ CORRECT: Contrast ratio 5.1:1 (exceeds 4.5:1) */
.text-muted {
    color: #666666; /* Medium gray - still visually "muted" but readable */
}

/* ✅ CORRECT: Contrast ratio 7.2:1 (exceeds even AAA standard) */
.table-cell-secondary {
    color: #555555; /* Dark gray */
    font-size: 16px; /* Also increased to meet minimum size */
}

/* ✅ CORRECT: Contrast ratio 5.9:1 */
.form-help-text {
    color: #606060;
}

/* Color variables for consistency */
:root {
    --text-primary: #333333;     /* 12.6:1 ratio */
    --text-secondary: #666666;   /* 5.1:1 ratio */
    --text-tertiary: #555555;    /* 7.2:1 ratio */
    --text-disabled: #999999;    /* 3.8:1 - USE SPARINGLY */
}
```

**Testing Results:**
| Element | New Color | Contrast | Status |
|---------|-----------|----------|--------|
| .text-muted | #666666 | 5.1:1 | ✅ PASS |
| .table-cell-secondary | #555555 | 7.2:1 | ✅ PASS |
| .form-help-text | #606060 | 5.9:1 | ✅ PASS |

**Priority:** 🔴 CRITICAL (legal requirement)
**Estimated Effort:** 1 hour

---

### 🔴 CRITICAL #3: Focus Not Visible
**WCAG Criterion:** 2.4.7 Focus Visible (Level AA)
**Impact:** Keyboard users cannot see which element has focus
**Pages Affected:** All pages
**Violation Count:** Global CSS issue

**Current Code:**
```css
/* ❌ VIOLATION: Focus outline removed globally */
*:focus {
    outline: none; /* NEVER DO THIS! */
}

button:focus,
input:focus {
    outline: none; /* Removes keyboard focus indicator */
}
```

**Issue:**
- Keyboard users cannot see which element is focused
- Tab navigation becomes impossible to track
- Violates WCAG 2.4.7 (Focus Visible)

**Fix Required:**
```css
/* ✅ CORRECT: Visible, high-contrast focus indicators */
*:focus {
    outline: 2px solid #002B76; /* Navy blue - brand color */
    outline-offset: 2px;
}

/* ✅ Custom focus styles (if native outline doesn't fit design) */
button:focus,
.admin-btn:focus {
    outline: 2px solid #002B76;
    outline-offset: 2px;
    box-shadow: 0 0 0 4px rgba(0, 43, 118, 0.2); /* Subtle glow */
}

input:focus,
select:focus,
textarea:focus {
    outline: 2px solid #002B76;
    outline-offset: 2px;
    border-color: #002B76; /* Also highlight border */
}

/* ✅ Links */
a:focus {
    outline: 2px solid #002B76;
    outline-offset: 2px;
    background-color: rgba(0, 43, 118, 0.1); /* Subtle background */
}

/* ✅ Skip to main content link (for keyboard users) */
.skip-to-main:focus {
    position: absolute;
    top: 0;
    left: 0;
    z-index: 10000;
    padding: 10px;
    background: #002B76;
    color: #FFFFFF;
    text-decoration: none;
    outline: 2px solid #FFFFFF;
}
```

**Additional Fix - Add Skip Link:**
```html
<!-- _Layout.cshtml - Add at top of <body> -->
<a href="#main-content" class="skip-to-main visually-hidden-focusable">
    Skip to main content
</a>

<!-- Add id to main content area -->
<main id="main-content" role="main">
    @RenderBody()
</main>
```

**CSS for Skip Link:**
```css
/* Hide visually but keep accessible to screen readers */
.visually-hidden-focusable {
    position: absolute;
    left: -10000px;
    width: 1px;
    height: 1px;
    overflow: hidden;
}

/* Show when focused (keyboard navigation) */
.visually-hidden-focusable:focus {
    position: absolute;
    left: 0;
    top: 0;
    width: auto;
    height: auto;
    overflow: visible;
    clip: auto;
    z-index: 10000;
}
```

**Priority:** 🔴 CRITICAL (keyboard accessibility)
**Estimated Effort:** 1 hour

---

## Serious Violations (Fix This Sprint)

### 🟡 SERIOUS #4: Missing ARIA Labels on Icon Buttons
**WCAG Criterion:** 4.1.2 Name, Role, Value (Level A)
**Impact:** Screen reader users cannot identify button purpose
**Pages Affected:** 8 pages
**Violation Count:** 24 icon buttons

**Current Code:**
```html
<!-- ❌ VIOLATION: Icon button has no accessible name -->
<button class="admin-btn admin-btn--secondary" onclick="exportData()">
    <i class="fas fa-download"></i>
</button>

<button class="admin-btn admin-btn--primary" onclick="addNew()">
    <i class="fas fa-plus"></i>
</button>
```

**Screen Reader Output:**
- "Button" (no description - user has no idea what button does)

**Fix Required:**
```html
<!-- ✅ CORRECT: Icon button with aria-label -->
<button class="admin-btn admin-btn--secondary"
        onclick="exportData()"
        aria-label="Export data to CSV">
    <i class="fas fa-download" aria-hidden="true"></i>
</button>

<!-- ✅ CORRECT: Icon + text (best practice) -->
<button class="admin-btn admin-btn--primary"
        onclick="addNew()"
        aria-label="Add new tag">
    <i class="fas fa-plus" aria-hidden="true"></i>
    <span>Add New</span>
</button>

<!-- ✅ CORRECT: Icon only with visually hidden text -->
<button class="icon-btn" onclick="deleteItem()" aria-label="Delete item">
    <i class="fas fa-trash" aria-hidden="true"></i>
    <span class="visually-hidden">Delete</span>
</button>
```

**Screen Reader Output:**
- "Export data to CSV, button"
- "Add new tag, button"
- "Delete item, button"

**Priority:** 🟡 SERIOUS (fix this sprint)
**Estimated Effort:** 2 hours

---

### 🟡 SERIOUS #5: Tables Missing Headers
**WCAG Criterion:** 1.3.1 Info and Relationships (Level A)
**Impact:** Screen reader users cannot understand table structure
**Pages Affected:** 6 admin pages
**Violation Count:** 6 tables

**Current Code:**
```html
<!-- ❌ VIOLATION: Table has no <thead> or <th> elements -->
<table class="table">
    <tr>
        <td>Name</td>
        <td>Email</td>
        <td>Role</td>
        <td>Actions</td>
    </tr>
    <tr>
        <td>John Doe</td>
        <td>john@example.com</td>
        <td>Admin</td>
        <td><button>Edit</button></td>
    </tr>
</table>
```

**Fix Required:**
```html
<!-- ✅ CORRECT: Proper table structure with headers -->
<table class="table table-striped" aria-label="User management table">
    <caption class="visually-hidden">
        List of users with name, email, role, and action buttons
    </caption>
    <thead>
        <tr>
            <th scope="col">Name</th>
            <th scope="col">Email</th>
            <th scope="col">Role</th>
            <th scope="col">Actions</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>John Doe</td>
            <td>john@example.com</td>
            <td>Admin</td>
            <td>
                <button aria-label="Edit user John Doe">Edit</button>
                <button aria-label="Delete user John Doe">Delete</button>
            </td>
        </tr>
    </tbody>
</table>
```

**Priority:** 🟡 SERIOUS (data tables critical)
**Estimated Effort:** 1.5 hours

---

## Moderate Violations (Fix Next Sprint)

### 🟢 MODERATE #6: Redundant Link Text
**WCAG Criterion:** 2.4.4 Link Purpose (In Context) (Level A)
**Impact:** Screen reader users hear "Read more, Read more, Read more"
**Pages Affected:** 3 pages
**Violation Count:** 12 links

**Current Code:**
```html
<!-- ❌ VIOLATION: Multiple "Read more" links with no context -->
<div class="certificate-card">
    <h3>FSC Certificate #24-0366</h3>
    <p>Acme Corporation - Widget A</p>
    <a href="/Certificate/Details/1">Read more</a>
</div>

<div class="certificate-card">
    <h3>PEFC Certificate #24-0367</h3>
    <p>Beta Inc - Widget B</p>
    <a href="/Certificate/Details/2">Read more</a>
</div>
```

**Screen Reader Output:**
- "Read more, link"
- "Read more, link" (user cannot distinguish between links)

**Fix Required:**
```html
<!-- ✅ CORRECT: Descriptive link text with aria-label -->
<div class="certificate-card">
    <h3>FSC Certificate #24-0366</h3>
    <p>Acme Corporation - Widget A</p>
    <a href="/Certificate/Details/1"
       aria-label="View details for FSC Certificate 24-0366">
        Read more
    </a>
</div>

<!-- ✅ ALTERNATIVE: Include context in link text -->
<div class="certificate-card">
    <h3>PEFC Certificate #24-0367</h3>
    <p>Beta Inc - Widget B</p>
    <a href="/Certificate/Details/2">
        View certificate details
    </a>
</div>
```

**Priority:** 🟢 MODERATE
**Estimated Effort:** 1 hour

---

## Keyboard Navigation Testing

### Certificate Tile View (Public Page)

**Test Results:**

| Action | Keyboard | Status | Notes |
|--------|----------|--------|-------|
| Navigate to search | Tab | ✅ PASS | Focus visible |
| Type in search | Keyboard | ✅ PASS | Autocomplete works |
| Select suggestion | Arrow keys + Enter | ❌ FAIL | Must use mouse |
| Scroll tiles | Page Down/Up | ✅ PASS | Infinite scroll works |
| Open certificate | Tab to card + Enter | ⚠️ WARNING | Card not focusable |
| Download PDF | Tab + Enter | ✅ PASS | Works if card focusable |

**Fixes Required:**

#### Issue 1: Autocomplete Not Keyboard-Accessible
```javascript
// ❌ CURRENT: Mouse-only autocomplete
$('.search-suggestion').on('click', function() {
    selectSuggestion($(this).data('value'));
});

// ✅ FIX: Add keyboard support
$('.search-suggestion')
    .attr('role', 'option')
    .attr('tabindex', '-1')
    .on('click', function() {
        selectSuggestion($(this).data('value'));
    })
    .on('keydown', function(e) {
        if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            selectSuggestion($(this).data('value'));
        }
    });

// Add arrow key navigation
$('#search-input').on('keydown', function(e) {
    const suggestions = $('.search-suggestion');
    let currentIndex = suggestions.filter('.focused').index();

    if (e.key === 'ArrowDown') {
        e.preventDefault();
        currentIndex = (currentIndex + 1) % suggestions.length;
        suggestions.removeClass('focused').eq(currentIndex).addClass('focused').focus();
    } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        currentIndex = currentIndex <= 0 ? suggestions.length - 1 : currentIndex - 1;
        suggestions.removeClass('focused').eq(currentIndex).addClass('focused').focus();
    }
});
```

#### Issue 2: Certificate Cards Not Focusable
```html
<!-- ❌ CURRENT: Div is not keyboard-accessible -->
<div class="certificate-card" onclick="window.location='/Certificate/Details/1'">
    <h3>FSC Certificate #24-0366</h3>
    <p>Acme Corporation</p>
</div>

<!-- ✅ FIX: Make card focusable and keyboard-accessible -->
<div class="certificate-card"
     role="button"
     tabindex="0"
     aria-label="View FSC Certificate 24-0366 for Acme Corporation"
     onclick="navigateTo('/Certificate/Details/1')"
     onkeydown="if(event.key==='Enter'||event.key===' '){navigateTo('/Certificate/Details/1')}">
    <h3>FSC Certificate #24-0366</h3>
    <p>Acme Corporation</p>
</div>

<!-- ✅ BETTER: Use actual link (semantic HTML) -->
<a href="/Certificate/Details/1"
   class="certificate-card-link"
   aria-label="View FSC Certificate 24-0366 for Acme Corporation">
    <div class="certificate-card">
        <h3>FSC Certificate #24-0366</h3>
        <p>Acme Corporation</p>
    </div>
</a>
```

---

## Screen Reader Testing

### Testing Matrix

| Page | NVDA (Windows) | JAWS (Windows) | VoiceOver (Mac) | Status |
|------|----------------|----------------|-----------------|--------|
| Certificate Tile View | ✅ PASS | ✅ PASS | ✅ PASS | Compliant |
| Certificate Create | ⚠️ WARNING | ⚠️ WARNING | ⚠️ WARNING | Missing labels |
| Admin Users | ⚠️ WARNING | ⚠️ WARNING | ⚠️ WARNING | Table headers |
| Admin Tags | ❌ FAIL | ❌ FAIL | ❌ FAIL | Modal not announced |
| Login Page | ✅ PASS | ✅ PASS | ✅ PASS | Compliant |

### Critical Issue: Modals Not Announced

**Test:** Delete confirmation modal (Admin Tags page)

**Screen Reader Output:**
- NVDA: (silence - modal not announced)
- Expected: "Delete Confirmation dialog. This action cannot be undone..."

**Fix Required:**
```html
<!-- ❌ CURRENT: Modal not announced to screen readers -->
<div class="modal-overlay" id="delete-modal">
    <div class="modal-dialog">
        <h2>Delete Confirmation</h2>
        <p>This action cannot be undone.</p>
        <button id="confirm-btn">Delete</button>
        <button id="cancel-btn">Cancel</button>
    </div>
</div>

<!-- ✅ FIX: Add ARIA roles and focus management -->
<div class="modal-overlay"
     id="delete-modal"
     role="dialog"
     aria-modal="true"
     aria-labelledby="modal-title"
     aria-describedby="modal-desc">

    <div class="modal-dialog">
        <h2 id="modal-title">Delete Confirmation</h2>
        <p id="modal-desc">This action cannot be undone. Are you sure?</p>

        <button id="confirm-btn" aria-label="Confirm delete">
            Delete
        </button>
        <button id="cancel-btn" aria-label="Cancel delete">
            Cancel
        </button>
    </div>
</div>
```

**JavaScript Focus Management:**
```javascript
function showDeleteModal() {
    const modal = document.getElementById('delete-modal');

    // Store currently focused element (to restore later)
    const previouslyFocused = document.activeElement;

    // Show modal
    modal.style.display = 'flex';

    // Focus first focusable element (confirm button)
    const confirmBtn = document.getElementById('confirm-btn');
    confirmBtn.focus();

    // Trap focus within modal
    trapFocus(modal);

    // Announce to screen readers
    modal.setAttribute('aria-live', 'assertive');

    // Restore focus when modal closes
    modal.addEventListener('close', () => {
        previouslyFocused.focus();
    }, { once: true });
}

function trapFocus(modal) {
    const focusableElements = modal.querySelectorAll(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );

    const firstElement = focusableElements[0];
    const lastElement = focusableElements[focusableElements.length - 1];

    modal.addEventListener('keydown', (e) => {
        if (e.key === 'Tab') {
            // Shift + Tab (backward)
            if (e.shiftKey && document.activeElement === firstElement) {
                e.preventDefault();
                lastElement.focus();
            }
            // Tab (forward)
            else if (!e.shiftKey && document.activeElement === lastElement) {
                e.preventDefault();
                firstElement.focus();
            }
        }

        // Escape key closes modal
        if (e.key === 'Escape') {
            closeModal();
        }
    });
}
```

---

## Automated Testing Tools

### Lighthouse Accessibility Score

**Current Results:**
```
Certificate Tile View: 85/100 ⚠️
- Missing form labels (-8)
- Low contrast text (-5)
- Missing ARIA labels (-2)

Admin Pages (average): 78/100 ❌
- Missing table headers (-10)
- Focus not visible (-7)
- Missing ARIA labels (-5)
```

**Target:** 95+ on all pages

### axe DevTools Scan

**Issues Found:**
- 🔴 Critical: 3
- 🟡 Serious: 8
- 🟢 Moderate: 15
- 🔵 Minor: 22

**Total:** 48 issues

### WAVE Browser Extension

**Errors:** 12
**Alerts:** 23
**Features:** 45 (good practices found)
**Structural Elements:** 67
**ARIA:** 8

---

## Compliance Action Plan

### Week 1: Critical Fixes
- [ ] Add labels to all form inputs (12 instances)
- [ ] Fix color contrast issues (15 instances)
- [ ] Add visible focus indicators (global CSS)
- [ ] Add skip-to-main-content link

**Estimated Effort:** 8 hours
**Expected Compliance:** 75% → 90%

### Week 2: Serious Fixes
- [ ] Add ARIA labels to icon buttons (24 instances)
- [ ] Add table headers and captions (6 tables)
- [ ] Fix modal ARIA roles and focus management
- [ ] Fix autocomplete keyboard navigation

**Estimated Effort:** 8 hours
**Expected Compliance:** 90% → 95%

### Week 3: Moderate Fixes
- [ ] Fix redundant link text (12 links)
- [ ] Add ARIA landmarks (header, nav, main, footer)
- [ ] Add image alt text (8 images)
- [ ] Test all pages with screen readers

**Estimated Effort:** 6 hours
**Expected Compliance:** 95% → 98%

### Week 4: Minor Fixes & Polish
- [ ] Add loading states with ARIA (spinners, progress)
- [ ] Add error announcements (aria-live regions)
- [ ] Improve heading hierarchy (h1 → h2 → h3)
- [ ] Final WCAG 2.1 AA validation

**Estimated Effort:** 4 hours
**Expected Compliance:** 98% → 100%

---

**Total Effort:** 26 hours (3-4 weeks)
**Final Target:** 100% WCAG 2.1 Level AA compliance
```

---

### Function 2: Color Contrast Validation

**Description:** Automated color contrast checking against WCAG standards

**Validation Tool:**

```javascript
/**
 * Color Contrast Checker
 * WCAG 2.1 Level AA requires:
 * - Text: 4.5:1 (normal), 3:1 (large 18px+ or 14px+ bold)
 * - UI Components: 3:1
 */

function checkColorContrast(foreground, background) {
    // Convert hex to RGB
    const fgRGB = hexToRGB(foreground);
    const bgRGB = hexToRGB(background);

    // Calculate relative luminance
    const fgLuminance = getRelativeLuminance(fgRGB);
    const bgLuminance = getRelativeLuminance(bgRGB);

    // Calculate contrast ratio
    const ratio = (Math.max(fgLuminance, bgLuminance) + 0.05) /
                  (Math.min(fgLuminance, bgLuminance) + 0.05);

    return {
        ratio: ratio.toFixed(2),
        passAA_Normal: ratio >= 4.5,
        passAA_Large: ratio >= 3.0,
        passAAA_Normal: ratio >= 7.0,
        passAAA_Large: ratio >= 4.5
    };
}

// Example usage
const result = checkColorContrast('#999999', '#FFFFFF');
console.log(result);
// {
//   ratio: "3.85",
//   passAA_Normal: false,   ❌ FAIL
//   passAA_Large: true,     ✅ PASS
//   passAAA_Normal: false,
//   passAAA_Large: false
// }
```

**Automated Scan Results:**

```markdown
# Color Contrast Scan Report

**Scanned:** 47 CSS files, 312 color combinations
**Failures:** 23 combinations below WCAG AA standard

| Element | Foreground | Background | Ratio | AA Status | Fix |
|---------|------------|------------|-------|-----------|-----|
| .text-muted | #999999 | #FFFFFF | 3.8:1 | ❌ FAIL | Use #666666 (5.1:1) |
| .table-secondary | #CCCCCC | #FFFFFF | 2.1:1 | ❌ FAIL | Use #555555 (7.2:1) |
| .form-help | #AAAAAA | #FFFFFF | 3.2:1 | ❌ FAIL | Use #606060 (5.9:1) |
| .disabled-text | #DDDDDD | #FFFFFF | 1.6:1 | ❌ FAIL | Use #999999 (3.8:1) + bold |

**Auto-Fix Available:** ✅ Yes (replace values in CSS)
```

---

### Function 3: Keyboard Navigation Validation

**Description:** Tests all interactive elements for keyboard accessibility

**Test Script:**

```javascript
/**
 * Keyboard Navigation Tester
 * Validates:
 * - All interactive elements are focusable (Tab)
 * - Focus order is logical
 * - Focus indicators are visible
 * - Enter/Space activates buttons/links
 * - Escape closes modals/dropdowns
 */

async function testKeyboardNavigation(page) {
    const report = {
        totalInteractive: 0,
        focusable: 0,
        nonFocusable: [],
        noVisibleFocus: [],
        incorrectTabOrder: []
    };

    // Get all interactive elements
    const interactive = await page.$$eval(
        'a, button, input, select, textarea, [tabindex]',
        elements => elements.map(el => ({
            tag: el.tagName,
            id: el.id,
            class: el.className,
            tabindex: el.tabIndex
        }))
    );

    report.totalInteractive = interactive.length;

    // Test each element
    for (let i = 0; i < interactive.length; i++) {
        await page.keyboard.press('Tab');

        const focused = await page.evaluate(() => {
            const el = document.activeElement;
            const styles = window.getComputedStyle(el);

            return {
                tag: el.tagName,
                id: el.id,
                hasFocus: el === document.activeElement,
                outlineVisible: styles.outline !== 'none' &&
                                styles.outline !== '0px'
            };
        });

        if (focused.hasFocus) {
            report.focusable++;
        } else {
            report.nonFocusable.push(interactive[i]);
        }

        if (!focused.outlineVisible) {
            report.noVisibleFocus.push(interactive[i]);
        }
    }

    return report;
}
```

---

## Decision-Making Authority

### CAN Autonomously:
- ✅ Run automated WCAG 2.1 audits
- ✅ Check color contrast ratios
- ✅ Validate ARIA labels and roles
- ✅ Test keyboard navigation
- ✅ Generate compliance reports
- ✅ Suggest fixes with code examples

### REQUIRES Human Approval:
- ⚠️ Modifying production CSS (color changes)
- ⚠️ Changing HTML structure (semantic elements)
- ⚠️ Adding ARIA roles (complex interactions)
- ⚠️ Major accessibility refactoring

### CANNOT Do:
- ❌ Override WCAG standards
- ❌ Deploy changes without testing
- ❌ Modify business logic for accessibility
- ❌ Test third-party components (Bootstrap, jQuery)

---

## Integration Points

### Upstream (Feeds This Agent):
- CLAUDE.md - Accessibility requirements
- Views/**/*.cshtml - Razor views
- wwwroot/css/**/*.css - Stylesheets
- wwwroot/js/**/*.js - JavaScript interactions

### Downstream (This Agent Feeds):
- AG-UX01 (UX/UI Design Agent) - Design compliance
- AG-NOTIF01 (Notification Cleanup) - ARIA compliance for modals
- QA Team - Manual accessibility testing
- Compliance Officer - WCAG certification

### Tools:
- **Automated Testing:** Lighthouse, axe DevTools, WAVE
- **Manual Testing:** NVDA, JAWS, VoiceOver, ORCA
- **Color Contrast:** WebAIM Contrast Checker, Stark
- **Keyboard Testing:** Chrome DevTools, manual testing

---

## Prompt Template

```markdown
You are the Accessibility Compliance Agent for the GCC Database Solution.

**Your Mission:**
Ensure 100% WCAG 2.1 Level AA compliance across all views and components.

**Critical Requirements:**
- ✅ Color contrast: 4.5:1 (text), 3:1 (UI components)
- ✅ Keyboard navigation: All interactive elements focusable
- ✅ Screen reader support: ARIA labels, roles, live regions
- ✅ Form accessibility: Labels, validation, error announcements
- ✅ Focus management: Visible indicators, logical order

**Workflow:**
1. Scan all views for WCAG violations
2. Run automated tools (Lighthouse, axe DevTools)
3. Test keyboard navigation
4. Test with screen readers (NVDA, JAWS, VoiceOver)
5. Generate compliance report with fixes
6. Validate fixes meet WCAG 2.1 AA standards

**Output Format:**
- Violation reports by severity (Critical/Serious/Moderate/Minor)
- Before/after code examples
- Estimated effort and compliance improvement
- Automated test results

Always reference WCAG 2.1 Level AA standards and provide actionable fixes.
```

---

## Version History

- **v1.0** - 2025-10-24 - Initial specification for GCC Database Solution

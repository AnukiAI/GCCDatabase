# CSS Optimization Agent - GCC Database Solution

## Agent Identity

**Agent Name:** CSS Optimization Agent | **Agent ID:** AG-CSS01 | **Version:** 1.0 | **Last Updated:** 2025-10-24

---

## Agent Purpose

### Primary Objective
Optimize CSS across the GCC Database Solution by eliminating redundant custom styles, enforcing Bootstrap 5 utility usage, maintaining the 8px grid system, and ensuring brand color consistency.

### Scope & Success Criteria

**Responsible for:**
- Identifying custom CSS that duplicates Bootstrap 5 utilities
- Removing unused CSS rules
- Enforcing 8px grid spacing system
- Validating brand color usage (no hardcoded colors)
- Optimizing CSS bundle size
- Enforcing BEM naming conventions
- Consolidating duplicate styles

**NOT responsible for:**
- JavaScript optimization
- Image optimization
- Backend performance
- Third-party library CSS (Bootstrap, Font Awesome)

**Success when:**
- 50%+ reduction in custom CSS
- Bootstrap utilities used first, custom CSS only when necessary
- Zero hardcoded brand colors (use CSS variables)
- 8px grid system enforced throughout
- Consistent BEM naming across all components
- CSS bundle size reduced by 30%+

---

## Core Capabilities

### Function 1: Bootstrap 5 Duplication Detection

**Description:** Scans custom CSS to find styles that duplicate Bootstrap 5 utilities

**Output Example:**

```markdown
# Bootstrap 5 Duplication Report

**Scan Date:** October 24, 2025
**Scanned By:** AG-CSS01 (CSS Optimization Agent)
**Files Scanned:** 23 CSS files
**Total Lines:** 4,287 lines of CSS
**Duplications Found:** 87 instances
**Potential Savings:** 1,142 lines (26.6% reduction)

---

## Executive Summary

**Redundant CSS Categories:**
- 🔴 Spacing (margin/padding): 34 duplications → Use Bootstrap spacing utilities
- 🟡 Display/Flex: 18 duplications → Use Bootstrap display/flex utilities
- 🟡 Text alignment: 12 duplications → Use Bootstrap text utilities
- 🟢 Colors: 15 duplications → Use CSS variables + Bootstrap color utilities
- 🟢 Borders/Shadows: 8 duplications → Use Bootstrap border utilities

**Estimated Bundle Size Reduction:**
- Current: 4,287 lines (68KB minified)
- After optimization: 3,145 lines (50KB minified)
- Savings: 1,142 lines (18KB, 26.6% reduction)

---

## Critical Duplications (High Impact)

### 🔴 DUPLICATION #1: Spacing Utilities
**Impact:** HIGH (34 instances across 12 files)
**Savings:** 380 lines of CSS

**Current Code (wwwroot/css/admin.css):**
```css
/* ❌ REDUNDANT: Bootstrap has these utilities */
.margin-top-10 {
    margin-top: 10px;
}

.margin-top-20 {
    margin-top: 20px;
}

.margin-top-30 {
    margin-top: 30px;
}

.margin-bottom-10 {
    margin-bottom: 10px;
}

.margin-bottom-20 {
    margin-bottom: 20px;
}

.padding-10 {
    padding: 10px;
}

.padding-20 {
    padding: 20px;
}

.padding-top-15 {
    padding-top: 15px;
}

/* ... 26 more similar classes ... */
```

**Bootstrap 5 Equivalent:**
```html
<!-- ✅ USE BOOTSTRAP UTILITIES INSTEAD -->

<!-- Margin Top -->
<div class="mt-1">  <!-- 4px (0.25rem) -->
<div class="mt-2">  <!-- 8px (0.5rem) -->
<div class="mt-3">  <!-- 16px (1rem) ← RECOMMENDED (8px grid) -->
<div class="mt-4">  <!-- 24px (1.5rem) ← RECOMMENDED (8px grid) -->
<div class="mt-5">  <!-- 48px (3rem) ← RECOMMENDED (8px grid) -->

<!-- Margin Bottom -->
<div class="mb-1">  <!-- 4px -->
<div class="mb-2">  <!-- 8px ← RECOMMENDED (8px grid) -->
<div class="mb-3">  <!-- 16px ← RECOMMENDED (8px grid) -->
<div class="mb-4">  <!-- 24px ← RECOMMENDED (8px grid) -->

<!-- Padding -->
<div class="p-2">   <!-- 8px all sides ← RECOMMENDED -->
<div class="p-3">   <!-- 16px all sides ← RECOMMENDED -->
<div class="p-4">   <!-- 24px all sides ← RECOMMENDED -->

<!-- Padding Top -->
<div class="pt-3">  <!-- 16px top ← RECOMMENDED -->

<!-- Combined Spacing -->
<div class="my-3">  <!-- margin-top: 16px; margin-bottom: 16px; -->
<div class="px-4">  <!-- padding-left: 24px; padding-right: 24px; -->
```

**Migration Guide:**

| Custom Class | Bootstrap Equivalent | 8px Grid? |
|--------------|---------------------|-----------|
| .margin-top-10 | .mt-2 (8px) | ⚠️ Use .mt-2 instead |
| .margin-top-20 | .mt-4 (24px) | ⚠️ 20px not on grid, use 24px |
| .margin-top-30 | .mt-5 (48px) | ⚠️ 30px not on grid, use 32px (.mt-5) |
| .margin-bottom-10 | .mb-2 (8px) | ⚠️ Use .mb-2 instead |
| .margin-bottom-20 | .mb-4 (24px) | ⚠️ Use .mb-4 instead |
| .padding-10 | .p-2 (8px) | ⚠️ Use .p-2 instead |
| .padding-20 | .p-3 (16px) or .p-4 (24px) | ⚠️ Adjust to grid |
| .padding-top-15 | .pt-3 (16px) | ⚠️ 15px not on grid |

**Automated Replacement:**
```bash
# Find/replace in all .cshtml files
find Views/ -name "*.cshtml" -exec sed -i 's/margin-top-20/mt-4/g' {} \;
find Views/ -name "*.cshtml" -exec sed -i 's/margin-bottom-20/mb-4/g' {} \;
find Views/ -name "*.cshtml" -exec sed -i 's/padding-10/p-2/g' {} \;
```

**Priority:** 🔴 CRITICAL (biggest savings, enforces 8px grid)
**Estimated Effort:** 3 hours (automated + manual review)

---

### 🔴 DUPLICATION #2: Display & Flexbox Utilities
**Impact:** HIGH (18 instances across 8 files)
**Savings:** 210 lines of CSS

**Current Code (wwwroot/css/components.css):**
```css
/* ❌ REDUNDANT: Bootstrap has these utilities */
.flex-container {
    display: flex;
}

.flex-center {
    display: flex;
    justify-content: center;
    align-items: center;
}

.flex-between {
    display: flex;
    justify-content: space-between;
}

.flex-column {
    display: flex;
    flex-direction: column;
}

.flex-wrap {
    display: flex;
    flex-wrap: wrap;
}

.align-center {
    align-items: center;
}

.justify-center {
    justify-content: center;
}

.d-block {
    display: block;
}

.d-inline {
    display: inline;
}

.d-none {
    display: none;
}

/* ... 8 more similar classes ... */
```

**Bootstrap 5 Equivalent:**
```html
<!-- ✅ USE BOOTSTRAP UTILITIES INSTEAD -->

<!-- Display -->
<div class="d-flex">           <!-- display: flex; -->
<div class="d-block">          <!-- display: block; -->
<div class="d-none">           <!-- display: none; -->
<div class="d-inline-block">   <!-- display: inline-block; -->

<!-- Flexbox Alignment -->
<div class="d-flex justify-content-center align-items-center">
    <!-- Centered content (replaces .flex-center) -->
</div>

<div class="d-flex justify-content-between">
    <!-- Space between (replaces .flex-between) -->
</div>

<div class="d-flex flex-column">
    <!-- Column layout (replaces .flex-column) -->
</div>

<div class="d-flex flex-wrap">
    <!-- Wrapping flex items (replaces .flex-wrap) -->
</div>

<!-- Responsive Display -->
<div class="d-none d-md-block">
    <!-- Hidden on mobile, visible on tablet+ -->
</div>

<div class="d-flex d-md-none">
    <!-- Visible on mobile, hidden on tablet+ -->
</div>
```

**Migration Examples:**

```html
<!-- ❌ BEFORE (Custom CSS) -->
<div class="flex-center">
    <h1>Centered Title</h1>
</div>

<div class="flex-between">
    <span>Left</span>
    <span>Right</span>
</div>

<!-- ✅ AFTER (Bootstrap Utilities) -->
<div class="d-flex justify-content-center align-items-center">
    <h1>Centered Title</h1>
</div>

<div class="d-flex justify-content-between">
    <span>Left</span>
    <span>Right</span>
</div>
```

**Priority:** 🔴 CRITICAL (common pattern, big cleanup)
**Estimated Effort:** 2 hours

---

### 🟡 DUPLICATION #3: Text Utilities
**Impact:** MEDIUM (12 instances across 6 files)
**Savings:** 95 lines of CSS

**Current Code (wwwroot/css/typography.css):**
```css
/* ❌ REDUNDANT: Bootstrap has these utilities */
.text-left {
    text-align: left;
}

.text-center {
    text-align: center;
}

.text-right {
    text-align: right;
}

.text-bold {
    font-weight: bold;
}

.text-italic {
    font-style: italic;
}

.text-uppercase {
    text-transform: uppercase;
}

.text-lowercase {
    text-transform: lowercase;
}

.text-underline {
    text-decoration: underline;
}

.text-nowrap {
    white-space: nowrap;
}

.text-truncate {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
```

**Bootstrap 5 Equivalent:**
```html
<!-- ✅ USE BOOTSTRAP UTILITIES INSTEAD -->

<!-- Text Alignment -->
<div class="text-start">    <!-- Left (replaces .text-left) -->
<div class="text-center">   <!-- Center -->
<div class="text-end">      <!-- Right (replaces .text-right) -->

<!-- Font Weight -->
<span class="fw-bold">      <!-- Bold (replaces .text-bold) -->
<span class="fw-normal">    <!-- Normal -->
<span class="fw-light">     <!-- Light -->

<!-- Font Style -->
<span class="fst-italic">   <!-- Italic (replaces .text-italic) -->

<!-- Text Transform -->
<span class="text-uppercase">   <!-- UPPERCASE -->
<span class="text-lowercase">   <!-- lowercase -->
<span class="text-capitalize">  <!-- Capitalize Each Word -->

<!-- Text Decoration -->
<span class="text-decoration-underline">  <!-- Underline -->
<span class="text-decoration-none">       <!-- No decoration -->

<!-- Text Wrapping -->
<span class="text-nowrap">      <!-- No wrap -->
<span class="text-truncate">    <!-- Truncate with ellipsis -->
```

**Priority:** 🟡 MEDIUM
**Estimated Effort:** 1 hour

---

## Brand Color Violations

### 🔴 CRITICAL: Hardcoded Colors
**Issue:** Brand colors hardcoded throughout CSS (not using CSS variables)
**Impact:** Difficult to maintain, inconsistent colors, no dark mode support
**Violations:** 47 instances across 15 files

**Current Code (multiple files):**
```css
/* ❌ HARDCODED COLORS: Difficult to maintain */
.btn-primary {
    background-color: #92C83E; /* Green */
}

.header {
    color: #002B76; /* Navy */
}

.error-text {
    color: #FF5252; /* Red */
}

.warning-bg {
    background-color: #FFC107; /* Yellow */
}

.text-secondary {
    color: #666666; /* Medium Gray */
}

/* ... 42 more hardcoded colors ... */
```

**Recommended Fix (CSS Variables):**
```css
/* ✅ DEFINE COLORS ONCE (site.css or variables.css) */
:root {
    /* Brand Colors */
    --color-primary: #92C83E;      /* Green */
    --color-secondary: #002B76;    /* Navy Blue */
    --color-error: #FF5252;        /* Red */
    --color-warning: #FFC107;      /* Yellow */
    --color-info: #17a2b8;         /* Teal */
    --color-success: #92C83E;      /* Green (alias) */

    /* Text Colors */
    --text-primary: #333333;       /* Dark Gray */
    --text-secondary: #666666;     /* Medium Gray */
    --text-tertiary: #999999;      /* Light Gray */
    --text-disabled: #CCCCCC;      /* Very Light Gray */
    --text-white: #FFFFFF;         /* White */

    /* Background Colors */
    --bg-primary: #FFFFFF;         /* White */
    --bg-secondary: #F8F9FA;       /* Light Gray */
    --bg-tertiary: #E9ECEF;        /* Medium Gray */

    /* Border Colors */
    --border-color: #DEE2E6;       /* Light Gray */
    --border-color-dark: #ADB5BD;  /* Medium Gray */

    /* Spacing (8px Grid) */
    --spacing-xs: 4px;
    --spacing-sm: 8px;
    --spacing-md: 16px;
    --spacing-lg: 24px;
    --spacing-xl: 32px;
    --spacing-2xl: 48px;
}

/* ✅ USE VARIABLES THROUGHOUT */
.btn-primary {
    background-color: var(--color-primary);
    color: var(--text-white);
}

.header {
    color: var(--color-secondary);
}

.error-text {
    color: var(--color-error);
}

.warning-bg {
    background-color: var(--color-warning);
}

.text-secondary {
    color: var(--text-secondary);
}

/* ✅ DARK MODE SUPPORT (Future) */
@media (prefers-color-scheme: dark) {
    :root {
        --bg-primary: #1a1a1a;
        --bg-secondary: #2d2d2d;
        --text-primary: #e0e0e0;
        --text-secondary: #b0b0b0;
    }
}
```

**Automated Replacement:**
```bash
# Replace all instances of #92C83E with var(--color-primary)
find wwwroot/css/ -name "*.css" -exec sed -i 's/#92C83E/var(--color-primary)/g' {} \;
find wwwroot/css/ -name "*.css" -exec sed -i 's/#002B76/var(--color-secondary)/g' {} \;
find wwwroot/css/ -name "*.css" -exec sed -i 's/#FF5252/var(--color-error)/g' {} \;
find wwwroot/css/ -name "*.css" -exec sed -i 's/#FFC107/var(--color-warning)/g' {} \;
```

**Priority:** 🔴 CRITICAL (maintainability, brand consistency)
**Estimated Effort:** 2 hours

---

## 8px Grid System Violations

### ⚠️ WARNING: Non-Grid Spacing Values
**Issue:** Spacing values that don't align with 8px grid system
**Impact:** Inconsistent spacing, visual misalignment
**Violations:** 38 instances

**Current Code:**
```css
/* ❌ NOT ON 8px GRID */
.card-header {
    padding: 15px;      /* Should be 16px */
}

.section-margin {
    margin-bottom: 20px; /* Should be 16px or 24px */
}

.button-padding {
    padding: 10px 18px;  /* Should be 8px 16px or 12px 24px */
}

.item-gap {
    gap: 12px;           /* ✅ ACCEPTABLE (12px = 1.5 × 8px) */
}

.custom-spacing {
    margin: 25px;        /* Should be 24px or 32px */
}
```

**Fix to 8px Grid:**
```css
/* ✅ ON 8px GRID */
.card-header {
    padding: 16px;       /* 2 × 8px ✅ */
}

.section-margin {
    margin-bottom: 24px; /* 3 × 8px ✅ */
}

.button-padding {
    padding: 8px 16px;   /* 1 × 8px, 2 × 8px ✅ */
}

.item-gap {
    gap: 12px;           /* 1.5 × 8px ✅ ACCEPTABLE */
}

.custom-spacing {
    margin: 24px;        /* 3 × 8px ✅ */
}

/* ✅ USE CSS VARIABLES FOR CONSISTENCY */
.card-header {
    padding: var(--spacing-md); /* 16px */
}

.section-margin {
    margin-bottom: var(--spacing-lg); /* 24px */
}

.button-padding {
    padding: var(--spacing-sm) var(--spacing-md); /* 8px 16px */
}
```

**8px Grid Reference:**
| Variable | Value | Multiplier |
|----------|-------|------------|
| --spacing-xs | 4px | 0.5 × 8px |
| --spacing-sm | 8px | 1 × 8px |
| --spacing-md | 16px | 2 × 8px |
| --spacing-lg | 24px | 3 × 8px |
| --spacing-xl | 32px | 4 × 8px |
| --spacing-2xl | 48px | 6 × 8px |

**Priority:** ⚠️ MEDIUM (visual consistency)
**Estimated Effort:** 2 hours

---

## Unused CSS Detection

### Method 1: Coverage Tool (Chrome DevTools)

**Steps:**
1. Open Chrome DevTools (F12)
2. Go to "Coverage" tab (Cmd+Shift+P → "Show Coverage")
3. Click "Record"
4. Navigate through all pages
5. Stop recording
6. Review unused CSS

**Results:**
```
Total CSS: 68KB
Used CSS: 45KB (66%)
Unused CSS: 23KB (34%) ← Can be removed
```

**Unused Selectors (Examples):**
```css
/* ❌ UNUSED: Not found in any .cshtml file */
.old-button-style {
    /* 25 lines... */
}

.deprecated-card {
    /* 18 lines... */
}

.legacy-header {
    /* 30 lines... */
}

/* ... 187 more unused selectors ... */
```

**Priority:** 🟢 MEDIUM (bundle size optimization)
**Estimated Savings:** 23KB (34% reduction)

---

### Method 2: PurgeCSS Automated Scan

```javascript
// purgecss.config.js
module.exports = {
    content: [
        './Views/**/*.cshtml',
        './wwwroot/js/**/*.js'
    ],
    css: [
        './wwwroot/css/**/*.css'
    ],
    safelist: [
        // Bootstrap classes that might be added dynamically
        /^btn-/,
        /^modal-/,
        /^toast-/,
        /^alert-/
    ]
};
```

**Command:**
```bash
purgecss --config purgecss.config.js --output wwwroot/css/optimized/
```

**Results:**
- Original: 68KB (4,287 lines)
- After PurgeCSS: 39KB (2,456 lines)
- Savings: 29KB (43% reduction)

---

## BEM Naming Violations

### Current Issues:
```css
/* ❌ INCONSISTENT NAMING: No clear pattern */
.card-title { }
.cardSubtitle { }
.Card_Footer { }
.CARD-BUTTON { }

.admin-header { }
.adminHeaderContent { }
.admin_actions { }
.AdminButton { }
```

**BEM Standard:**
```css
/* ✅ CORRECT BEM NAMING */

/* Block: Top-level component */
.card { }

/* Element: Part of the block (double underscore) */
.card__title { }
.card__subtitle { }
.card__footer { }
.card__button { }

/* Modifier: Variation of block or element (double hyphen) */
.card--featured { }
.card--compact { }
.card__button--primary { }
.card__button--secondary { }

/* Example: Admin Header Component */
.admin-header { }
.admin-header__content { }
.admin-header__title { }
.admin-header__actions { }
.admin-header__button { }
.admin-header__button--primary { }
.admin-header__button--secondary { }
```

**HTML Usage:**
```html
<!-- ✅ BEM in practice -->
<div class="card card--featured">
    <h3 class="card__title">Certificate Title</h3>
    <p class="card__subtitle">Company Name</p>
    <div class="card__footer">
        <button class="card__button card__button--primary">View</button>
        <button class="card__button card__button--secondary">Download</button>
    </div>
</div>

<header class="admin-header">
    <div class="admin-header__content">
        <div class="admin-header__title">
            <h1>Tag Management</h1>
        </div>
        <div class="admin-header__actions">
            <button class="admin-header__button admin-header__button--secondary">
                Export
            </button>
            <button class="admin-header__button admin-header__button--primary">
                Add New
            </button>
        </div>
    </div>
</header>
```

**Benefits:**
- ✅ Clear component hierarchy
- ✅ Avoids specificity issues
- ✅ Self-documenting code
- ✅ Easy to find related styles
- ✅ Prevents naming collisions

**Priority:** 🟢 LOW (long-term maintainability)
**Estimated Effort:** 8 hours (gradual refactor)

---

## Optimization Action Plan

### Phase 1: Quick Wins (Week 1)
**Estimated Time:** 8 hours
**Expected Savings:** 1,142 lines (26.6%)

- [ ] Replace custom spacing classes with Bootstrap utilities (380 lines)
- [ ] Replace custom display/flex classes with Bootstrap utilities (210 lines)
- [ ] Replace custom text utilities with Bootstrap utilities (95 lines)
- [ ] Convert hardcoded colors to CSS variables (47 instances)

**Expected Result:**
- CSS reduced: 4,287 lines → 3,145 lines
- Bundle size: 68KB → 50KB (18KB savings)
- Compliance: 65% → 85% Bootstrap utilities

---

### Phase 2: Deep Cleanup (Week 2)
**Estimated Time:** 6 hours
**Expected Savings:** 29KB bundle size

- [ ] Run PurgeCSS to remove unused selectors (1,831 lines)
- [ ] Fix 8px grid violations (38 instances)
- [ ] Consolidate duplicate styles (12 files)
- [ ] Remove deprecated classes (187 selectors)

**Expected Result:**
- CSS reduced: 3,145 lines → 2,456 lines
- Bundle size: 50KB → 39KB (11KB savings)
- Grid compliance: 75% → 95%

---

### Phase 3: Long-term Maintenance (Week 3-4)
**Estimated Time:** 8 hours

- [ ] Refactor components to BEM naming (gradual, 20% per week)
- [ ] Create component library documentation
- [ ] Set up CSS linting (Stylelint)
- [ ] Add pre-commit hooks (prevent new violations)

**Expected Result:**
- Consistent BEM naming: 0% → 60%
- Automated enforcement via CI/CD
- Developer documentation complete

---

## Automated Tools Integration

### Stylelint Configuration

```javascript
// .stylelintrc.json
{
    "extends": "stylelint-config-standard",
    "rules": {
        // Enforce CSS variable usage for brand colors
        "color-named": "never",
        "color-no-hex": true, // Require var(--color-*)

        // Enforce 8px grid spacing
        "declaration-property-value-blacklist": {
            "/^(margin|padding)/": [
                "/^(?!0|auto|inherit|initial|unset|var\\(--spacing-).+$/",
                {
                    "message": "Use CSS variables (--spacing-*) for margin/padding"
                }
            ]
        },

        // BEM naming pattern
        "selector-class-pattern": [
            "^[a-z]+(-[a-z]+)*((__[a-z]+(-[a-z]+)*)?(--[a-z]+(-[a-z]+)*)?)?$",
            {
                "message": "Use BEM naming: block__element--modifier"
            }
        ],

        // No !important (except utilities)
        "declaration-no-important": true,

        // Consistent property order
        "order/properties-alphabetical-order": true
    }
}
```

### Pre-commit Hook

```bash
# .git/hooks/pre-commit
#!/bin/bash

echo "Running Stylelint on CSS files..."

# Get all staged CSS files
CSS_FILES=$(git diff --cached --name-only --diff-filter=ACM | grep '\.css$')

if [ -n "$CSS_FILES" ]; then
    npx stylelint $CSS_FILES

    if [ $? -ne 0 ]; then
        echo "❌ Stylelint failed. Fix errors before committing."
        exit 1
    fi

    echo "✅ Stylelint passed"
fi

exit 0
```

---

## Continuous Monitoring

### Metrics Dashboard

```markdown
# CSS Health Metrics (Updated Weekly)

**Bundle Size Trend:**
- Week 1: 68KB (baseline)
- Week 2: 50KB (-18KB, -26%) ← After Phase 1
- Week 3: 39KB (-11KB, -22%) ← After Phase 2
- Week 4: 39KB (stable)
- **Target:** < 40KB

**Bootstrap Utility Usage:**
- Week 1: 65% (baseline)
- Week 2: 85% (+20%) ← After Phase 1
- Week 3: 92% (+7%)
- Week 4: 95% (+3%)
- **Target:** > 90%

**8px Grid Compliance:**
- Week 1: 75% (baseline)
- Week 2: 88% (+13%) ← After Phase 2
- Week 3: 95% (+7%)
- Week 4: 98% (+3%)
- **Target:** > 95%

**Unused CSS:**
- Week 1: 34% (23KB unused)
- Week 2: 18% (7KB unused) ← After PurgeCSS
- Week 3: 12% (5KB unused)
- Week 4: 8% (3KB unused)
- **Target:** < 10%
```

---

## Decision-Making Authority

### CAN Autonomously:
- ✅ Scan CSS for Bootstrap duplications
- ✅ Identify unused CSS selectors
- ✅ Suggest Bootstrap utility replacements
- ✅ Check 8px grid compliance
- ✅ Validate CSS variable usage
- ✅ Generate optimization reports

### REQUIRES Human Approval:
- ⚠️ Deleting CSS rules (may have dependencies)
- ⚠️ Replacing custom CSS with Bootstrap (verify visual match)
- ⚠️ Refactoring component naming (BEM migration)
- ⚠️ Running PurgeCSS (may remove needed classes)

### CANNOT Do:
- ❌ Modify CSS without testing visual output
- ❌ Deploy changes to production
- ❌ Override brand colors/spacing
- ❌ Remove CSS used in dynamic JavaScript

---

## Integration Points

### Upstream (Feeds This Agent):
- CLAUDE.md - 8px grid system, brand colors, Bootstrap 5 requirements
- 0_Documentation/mockups/0_0_MasterDesignGuide.html - Visual reference
- wwwroot/css/**/*.css - Custom stylesheets
- Views/**/*.cshtml - HTML using CSS classes

### Downstream (This Agent Feeds):
- AG-UX01 (UX/UI Design Agent) - Design system compliance
- AG-A11Y01 (Accessibility Agent) - Color contrast validation
- Frontend Developer - CSS refactoring tasks
- Build Pipeline - Bundle size optimization

### Tools:
- **Analysis:** PurgeCSS, Chrome DevTools Coverage, UnCSS
- **Linting:** Stylelint
- **Optimization:** cssnano, clean-css
- **Build:** Webpack, Vite, PostCSS

---

## Prompt Template

```markdown
You are the CSS Optimization Agent for the GCC Database Solution.

**Your Mission:**
Optimize CSS by eliminating redundancy, enforcing Bootstrap 5 usage, maintaining the 8px grid, and ensuring brand color consistency.

**Critical Requirements:**
- ✅ Bootstrap utilities FIRST, custom CSS only when necessary
- ✅ CSS variables for all brand colors (no hardcoded hex values)
- ✅ 8px grid system for all spacing (--spacing-xs through --spacing-2xl)
- ✅ BEM naming for custom components (block__element--modifier)
- ✅ Remove unused CSS (target < 10% unused)

**Workflow:**
1. Scan all CSS files for Bootstrap duplications
2. Identify hardcoded colors (should use CSS variables)
3. Check 8px grid compliance (spacing values)
4. Run PurgeCSS to find unused selectors
5. Generate optimization report with savings estimate
6. Provide automated replacement commands

**Output Format:**
- Duplication reports with line counts
- Before/after code examples
- Bootstrap utility equivalents
- Estimated bundle size savings
- Migration guides

Always reference CLAUDE.md for 8px grid and brand color standards.
```

---

## Version History

- **v1.0** - 2025-10-24 - Initial specification for GCC Database Solution

# Container Width Standards

**Document Created**: 2026-01-13
**Purpose**: Define consistent container widths across all application views to ensure design consistency.

---

## Design Token Reference

**Primary Source**: `wwwroot/css/admin/admin-core.css` (Line 126)

```css
:root {
    --admin-container-max-width: 1400px;
}
```

---

## Container Width Categories

### Category 1: Standard Admin Pages (1400px)

**Width**: `max-width: 1400px`
**CSS Class**: `.admin-container`
**Padding**: `24px` (desktop), `16px` (tablet), `8px` (mobile)

**Applies To**:
| Page | URL | Status |
|------|-----|--------|
| User Management | `/Admin/UserManagement` | ✅ Correct |
| Role Management | `/Admin/RoleManagement` | ✅ Correct |
| Tag Management | `/Admin/Tags` | ✅ Correct |
| Certificate Types | `/Admin/CertificateTypes` | ✅ Correct |
| Certificate Fields | `/Admin/CertificateFields` | ✅ Correct |
| Partner API | `/Admin/PartnerAPI` | ✅ Correct |
| Product Types | `/Admin/ProductTypes` | ✅ Correct |
| Dashboard | `/Dashboard` | ✅ Correct |

**CSS Definition** (`admin-layout.css:8-14`):
```css
.admin-container {
    max-width: var(--admin-container-max-width);  /* 1400px */
    margin: 0 auto;
    padding: var(--admin-space-lg);                /* 24px */
    background-color: var(--admin-bg-secondary);
    min-height: 100vh;
}
```

---

### Category 2: Authentication Pages

#### Two-Column Layout (900px)
**Width**: `max-width: 900px`
**CSS Class**: `.login-container`
**Layout**: 2-column grid (form + branding)

**Applies To**:
- Login (`/Account/Login`)
- Sign Up (`/Account/SignUp`)
- Forgot Password (`/Account/ForgotPassword`)
- Reset Password (`/Account/ResetPassword`)

**CSS Definition** (`auth.css:46-56`):
```css
.login-container {
    max-width: var(--auth-container-max-width);  /* 900px */
    display: grid;
    grid-template-columns: 1fr 1fr;
}
```

#### Single-Column Layout (500px)
**Width**: `max-width: 500px`
**CSS Class**: `.auth-container`
**Layout**: Single-column centered

**Applies To**:
- 2FA Authentication (`/Account/2FAAuthenticate`)
- Verify Email (`/Account/VerifyEmail`)
- Account Locked (`/Account/AccountLocked`)
- Password Reset Success (`/Account/PasswordResetSuccess`)
- Setup TOTP (`/Account/SetupTotp`)

**CSS Definition** (`auth.css:68-76`):
```css
.auth-container {
    max-width: 500px;
    width: 100%;
    padding: 40px;
}
```

---

### Category 3: Certificate Form Pages (Special Layout)

**Container**: Standard 1400px
**Internal Layout**: 2-column grid (form + sidebar)

**Applies To**:
- Certificate Create (`/Certificate/Create`)
- Certificate Edit (`/Certificate/Edit/{number}`)

**Layout Structure**:
```
┌─────────────────────────────────────────────────────────────────┐
│                    max-width: 1400px                             │
├───────────────────────────────────────┬─────────────────────────┤
│         Main Form (flexible)          │    Sidebar (380px)      │
│         grid: 1fr 1fr                 │    (File Upload,        │
│                                       │     Tags, Preview)      │
└───────────────────────────────────────┴─────────────────────────┘
```

**CSS Definition** (`certificate-create.css:177-198`):
```css
.form-grid {
    display: grid;
    grid-template-columns: 1fr 380px;
    gap: 32px;
}
.main-form {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 20px 24px;
}
```

---

### Category 4: Certificate Tile Views (Responsive Scaling)

**Base Width**: `max-width: 1400px`
**Responsive Scaling**: Expands on ultra-wide monitors

| Screen Width | Container Max-Width |
|--------------|---------------------|
| Default | 1400px |
| ≥1600px | 1600px |
| ≥1920px | 1800px |
| ≥2560px | 2400px |

**Applies To**:
- Public Tile View (`/Certificate/CertificateTileView`)
- Internal Tile View (`/Certificate/CertificateTileViewInternal`)
- External Tile View (`/Certificate/CertificateTileViewExternal`)

**CSS Definition** (`certificatetileview.css:173-194`):
```css
.tile-view-container {
    max-width: 1400px;
}
@media (min-width: 1600px) {
    .tile-view-container { max-width: 1600px; }
}
@media (min-width: 1920px) {
    .tile-view-container { max-width: 1800px; }
}
@media (min-width: 2560px) {
    .tile-view-container { max-width: 2400px; }
}
```

---

### Category 5: Full-Width Pages

**Width**: `max-width: 100%`
**Modifier Class**: `.admin-container--full-width`

**Use Cases**:
- Large data tables requiring horizontal scroll
- Wide visualizations or reports

**CSS Definition** (`admin-layout.css:16-18`):
```css
.admin-container--full-width {
    max-width: none;
}
```

---

## Fixed Inconsistencies (2026-01-13)

### Issue 1: Company Management Page Override - FIXED ✅

**Problem**: `admin-company-style.css` was overriding the standard 1400px to 100% with `!important`.

**Fix Applied**: Removed `max-width: 100% !important` and `margin: 0 !important` from the `.admin-container` rule.

**File**: `wwwroot/css/admin-company-style.css` (Lines 22-30)
```css
/* NOTE: max-width removed to use standard 1400px from admin-container
   If full-width is needed, use .admin-container--full-width modifier class instead */
.admin-container {
    background-color: #fff !important;
    padding: 20px !important;
    /* max-width: inherits from admin-layout.css (1400px) */
    border-radius: 6px !important;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1) !important;
}
```

---

### Issue 2: Certificate Details Preview Box - FIXED ✅

**Problem**: Hardcoded 350px width for certificate preview thumbnail.

**Fix Applied**: Created responsive `.certificate-preview-box` CSS class with:
- `max-width: 350px` instead of fixed width
- `aspect-ratio: 1 / 1` for square shape
- Responsive breakpoints for mobile (280px, then 100%)
- All styling in CSS instead of inline

**Files Modified**:
- `wwwroot/css/certificates.css` (Lines 2072-2157) - Added new `.certificate-preview-box` class
- `Views/Certificate/Details.cshtml` (Line 195) - Changed to use CSS class

---

## Responsive Breakpoints

**Standard breakpoints** (from `admin-core.css:131-135`):

| Breakpoint | Width | Typical Use |
|------------|-------|-------------|
| `--admin-breakpoint-sm` | 576px | Small phones |
| `--admin-breakpoint-md` | 768px | Tablets |
| `--admin-breakpoint-lg` | 1024px | Laptops |
| `--admin-breakpoint-xl` | 1200px | Desktops |
| `--admin-breakpoint-2xl` | 1400px | Large desktops |

**Padding Adjustments** (from `admin-layout.css`):
| Screen Size | Container Padding |
|-------------|-------------------|
| Desktop (≥1200px) | 32px |
| Tablet (1024-1199px) | 16px |
| Mobile (768-1023px) | 8px |
| Small Mobile (<768px) | 4px |

---

## Implementation Checklist

When creating new pages, verify:

- [ ] Page uses appropriate container class (`.admin-container`, `.login-container`, or `.auth-container`)
- [ ] No inline `max-width` overrides unless intentional
- [ ] Responsive behavior tested at all breakpoints
- [ ] Padding follows spacing scale (uses CSS variables)
- [ ] Full-width pages use `.admin-container--full-width` modifier

---

## Files Defining Container Widths

| File | Purpose | Key Definitions |
|------|---------|-----------------|
| `wwwroot/css/admin/admin-core.css` | Design tokens | `--admin-container-max-width: 1400px` |
| `wwwroot/css/admin/admin-layout.css` | Layout containers | `.admin-container`, modifiers |
| `wwwroot/css/auth.css` | Authentication pages | `.login-container`, `.auth-container` |
| `wwwroot/css/certificatetileview.css` | Certificate views | Responsive scaling rules |
| `wwwroot/css/certificate-create.css` | Certificate forms | Form grid layout |
| `wwwroot/css/admin-company-style.css` | **OVERRIDE** | Contains problematic 100% override |

---

## Quick Reference

| Page Type | Max-Width | Container Class |
|-----------|-----------|-----------------|
| Standard Admin | 1400px | `.admin-container` + `.admin-body-content` |
| Auth (2-column) | 900px | `.login-container` |
| Auth (1-column) | 500px | `.auth-container` |
| Certificate Forms | 1400px (grid) | `.admin-container` + `.form-grid` |
| Certificate Tiles | 1400px-2400px | Responsive scaling |
| Full-Width | 100% | `.admin-container--full-width` |

---

## Hybrid Layout Standard (US-016) - IMPLEMENTED 2026-01-13

All admin pages now use a **Hybrid Layout** where:
- **Header stays separate** at the top (title, subtitle, action buttons)
- **Toolbar + Content unified** in a single white box with border

### Structure Pattern

```html
<div class="admin-container">
    <!-- Header stays separate -->
    <header class="admin-header">
        <div class="admin-header-content">...</div>
    </header>

    <!-- Unified body content wrapper -->
    <div class="admin-body-content">
        <div class="admin-toolbar">...</div>
        <div class="admin-content">...</div>
        <div class="admin-footer-info">...</div>
    </div>
</div>
```

### CSS Definition (`admin-layout.css:154-185`)

```css
.admin-body-content {
    background-color: var(--admin-bg-primary);
    border: 1px solid var(--admin-border-primary);
    border-radius: var(--admin-radius-lg);
    box-shadow: var(--admin-shadow-sm);
    overflow: hidden;
}

/* Child elements have their borders removed */
.admin-body-content .admin-toolbar { border: none; }
.admin-body-content .admin-content { border: none; }
```

### Pages Updated (11 total)

| Page | File | Status |
|------|------|--------|
| Tags | `Views/Admin/Tags.cshtml` | ✅ Updated |
| User Management | `Views/Admin/UserManagement.cshtml` | ✅ Updated |
| Role Management | `Views/Admin/RoleManagement.cshtml` | ✅ Updated |
| Certificate Fields | `Views/Admin/CertificateFields.cshtml` | ✅ Updated |
| Certificate Types | `Views/Admin/CertificateTypes.cshtml` | ✅ Updated |
| Product Types | `Views/Admin/ProductTypes.cshtml` | ✅ Updated |
| Product Management | `Views/Admin/ProductManagement.cshtml` | ✅ Updated |
| Partners | `Views/Admin/Partners.cshtml` | ✅ Updated |
| Partner API | `Views/Admin/PartnerAPI.cshtml` | ✅ Updated |
| GCC API | `Views/Admin/GCCAPI.cshtml` | ✅ Updated |
| CSF Attributes | `Views/Admin/CSFAttributes.cshtml` | ✅ Updated |

### Pages Not Updated (Different Layout)

| Page | Reason |
|------|--------|
| Admin Index | Dashboard card layout (doesn't need wrapper) |
| Google Analytics | Special form layout |
| Dashboard | Already has unified content |
| Create User | Form-only page |

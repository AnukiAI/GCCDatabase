# Full Inline CSS Audit Report

**Generated**: 2026-01-11
**Purpose**: Identify all views with inline CSS for potential consolidation

## Executive Summary

| Category | Files | Total Inline CSS Lines | Status |
|----------|-------|------------------------|--------|
| Account Pages | 11 | ~3,489 | **HIGH PRIORITY** - Highly duplicated |
| Dashboard | 1 | 380 | Medium priority |
| Certificate | 3 | ~587 | Partially fixed |
| Admin | 1 | 261 | Medium priority |
| Other | 5 | ~175 | Low priority |
| **TOTAL** | **21** | **~4,892** | |

---

## Detailed Analysis by Category

### 1. Account Pages (11 files) - **HIGH PRIORITY**

These pages share nearly identical authentication UI styling that should be in a shared `auth.css` file.

| File | Inline CSS Lines | Shared Pattern |
|------|------------------|----------------|
| ResetPassword.cshtml | 433 | `.login-page`, `.login-container`, `.login-left`, `.login-right` |
| ForgotPassword.cshtml | 426 | Same pattern |
| Login.cshtml | 424 | Same pattern |
| TwoFactorPreferences.cshtml | 388 | Same pattern |
| SignUp.cshtml | 312 | Same pattern |
| PasswordReset.cshtml | 273 | Same pattern |
| 2FAAuthenticate.cshtml | 258 | Same pattern |
| PasswordResetSuccess.cshtml | 250 | Same pattern |
| VerifyEmail.cshtml | 230 | Same pattern |
| AccountLocked.cshtml | 218 | Same pattern |
| WelcomeRedirect.cshtml | 217 | Same pattern |
| SetupTotp.cshtml | 59 | Partial |

**Recommendation**: Create `wwwroot/css/auth.css` with shared authentication styles:
- Background gradient (`.login-page`)
- Container layout (`.login-container`)
- Left panel with logo (`.login-left`, `.login-logo`, `.login-brand`)
- Right panel with form (`.login-right`, `.login-header`)
- Form controls (`.form-group`, `.form-input`, `.form-label`)
- Buttons (`.btn-primary`, `.btn-secondary`)
- Responsive breakpoints

**Estimated savings**: ~2,800 lines (keeping ~300 lines page-specific)

---

### 2. Dashboard (1 file) - Medium Priority

| File | Inline CSS Lines | Notes |
|------|------------------|-------|
| Dashboard/Index.cshtml | 380 | Dashboard-specific widgets and charts |

**Recommendation**: Create `wwwroot/css/dashboard.css` if Dashboard is a core feature used frequently.

---

### 3. Certificate Pages (3 files) - Partially Fixed

| File | Inline CSS Lines | Status |
|------|------------------|--------|
| CertificateTileViewExternal.cshtml | 121 | ✅ Fixed (was 471) |
| CertificateTileViewInternal.cshtml | 122 | ✅ Fixed (was 510) |
| Create2.cshtml | 344 | ⚠️ Needs review |

**Recommendation**: Review Create2.cshtml for potential consolidation with existing certificate CSS.

---

### 4. Admin Pages (1 file) - Medium Priority

| File | Inline CSS Lines | Notes |
|------|------------------|-------|
| Admin/RoleManagement.cshtml | 261 | Admin-specific styling |

**Recommendation**: Check if this duplicates styles from `admin.css` or `admin-modals.css`.

---

### 5. Other Files - Low Priority

| File | Inline CSS Lines | Notes |
|------|------------------|-------|
| CertificateImport/Preview.cshtml | 45 | Import preview specific |
| Shared/_CertificateDocuments.cshtml | 27 | Document viewer |
| Document/Upload.cshtml | 23 | Upload specific |
| Account/AccessDenied.cshtml | 21 | Error page |

**Recommendation**: These are small and page-specific, lower priority for consolidation.

---

## Duplicated Styles Comparison

### Login.cshtml vs SignUp.cshtml (Lines 16-100)

Both files have **identical** CSS for:
```css
.login-page {
    background: linear-gradient(135deg, #002B76 0%, #00AEEF 100%);
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 20px;
    margin: 0;
    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
}

.login-container {
    background: white;
    border-radius: 16px;
    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
    overflow: hidden;
    max-width: 900px; /* varies slightly */
    width: 100%;
    display: grid;
    grid-template-columns: 1fr 1fr;
    min-height: 600px; /* varies slightly */
}
/* ... and ~300 more lines */
```

**Minor differences**: `max-width`, `min-height`, `logo height` values differ slightly.

---

## Recommended Consolidation Plan

### Phase 1: Account Pages (Highest Impact)
1. Create `wwwroot/css/auth.css`
2. Move shared authentication styles
3. Use CSS variables for page-specific differences
4. Reduce 11 files from ~3,489 lines to ~500 lines

### Phase 2: Dashboard
1. Create `wwwroot/css/dashboard.css` if not exists
2. Move dashboard widget styles

### Phase 3: Admin Pages
1. Review RoleManagement.cshtml
2. Consolidate with existing admin CSS files

### Phase 4: Certificate Create
1. Review Create2.cshtml
2. Consolidate with certificatetileview.css if applicable

---

## Files Reference

### Existing CSS Files (wwwroot/css/)
- `site.css` - Main site styles
- `admin.css` - Admin panel base
- `admin-modals.css` - Modal dialogs
- `certificatetileview.css` - Certificate views (consolidated)

### Recommended New CSS Files
- `auth.css` - Authentication pages (Login, SignUp, Reset, etc.)
- `dashboard.css` - Dashboard page (if high-traffic)

---

## Risk Assessment

| Action | Risk | Mitigation |
|--------|------|------------|
| Create auth.css | Low | Test all auth pages after changes |
| Modify Account pages | Medium | Incremental changes, test each page |
| CSS variable usage | Low | Supported in all modern browsers |

---

*End of Report*

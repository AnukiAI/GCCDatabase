# Post-Deployment Verification Checklist

Complete each verification after deploying to production.

## 1. Application Health Check

- [ ] **Homepage loads** - https://gccwebapp.com
- [ ] **No HTTP 500 errors** - Check browser console
- [ ] **No HTTP 500.19 errors** - web.config issues
- [ ] **SSL certificate valid** - Green padlock in browser

## 2. Authentication Tests

### Admin Login
- [ ] Navigate to https://gccwebapp.com/Account/Login
- [ ] Login with: `admin` / `TestPass123!`
- [ ] Dashboard loads correctly
- [ ] Can access Admin menu

### Analyst Login
- [ ] Login with: `analyst` / `TestPass123!`
- [ ] Dashboard loads correctly
- [ ] Can access Certificate management
- [ ] Cannot access Admin menu (correct restriction)

### External Login
- [ ] Login with: `external` / `TestPass123!`
- [ ] Can view certificates
- [ ] Cannot create/edit certificates (correct restriction)

### UAT Accounts (if configured)
- [ ] Login with: `admin@greencirclecertified.com` / `Admin123!`
- [ ] Login with: `analyst@greencirclecertified.com` / `Analyst123!`
- [ ] Login with: `partner@amazon.com` / `Partner123!`

## 3. Core Features - Public Access

- [ ] **Certificate Tile View** - https://gccwebapp.com/Certificate/CertificateTileView
  - [ ] Certificates display in grid
  - [ ] Infinite scroll works
  - [ ] Search/filter works
  - [ ] Can download certificate PDFs

## 4. Core Features - Authenticated

### Certificate Management
- [ ] View certificate list
- [ ] View certificate details
- [ ] Create new certificate (Admin/Analyst)
- [ ] Edit existing certificate (Admin/Analyst)
- [ ] Download certificate PDF

### Admin Functions (Admin only)
- [ ] User Management - https://gccwebapp.com/Admin/UserManagement
- [ ] Role Management - https://gccwebapp.com/Admin/RoleManagement
- [ ] Company Management
- [ ] Product Management

## 5. Database Verification

```sql
-- Verify schema ownership
SELECT SCHEMA_NAME(schema_id) as SchemaName, name as TableName
FROM sys.tables
ORDER BY name;
-- All should show: dbo

-- Verify users exist
SELECT Username, Email, IsActive FROM Users;

-- Verify certificates exist
SELECT COUNT(*) FROM Certificates;
```

## 6. Logs Verification

Check application logs on server:
- [ ] No startup errors
- [ ] No database connection failures
- [ ] No authentication errors
- [ ] Message shows: "Production environment - manual migration management"
- [ ] NO message about auto-creating tables

## 7. API Endpoints

- [ ] **Health check** - GET https://gccwebapp.com/health
- [ ] **API docs** (should be disabled) - https://gccwebapp.com/swagger (should 404 in production)

## 8. Performance Check

- [ ] Pages load within 3 seconds
- [ ] No timeout errors
- [ ] No memory warnings in logs

## 9. Security Verification

- [ ] HTTPS enforced (HTTP redirects to HTTPS)
- [ ] Security headers present (X-Frame-Options, etc.)
- [ ] No sensitive data in browser console
- [ ] Login failed attempts are logged

## 10. Mobile Responsiveness

Test on mobile device or browser dev tools:
- [ ] Certificate Tile View displays correctly
- [ ] Login form works
- [ ] Navigation menu works

---

## Quick Test URLs

| Page | URL | Expected |
|------|-----|----------|
| Homepage | https://gccwebapp.com | Loads |
| Login | https://gccwebapp.com/Account/Login | Login form |
| Public Certs | https://gccwebapp.com/Certificate/CertificateTileView | Certificate grid |
| Dashboard | https://gccwebapp.com/Dashboard | Requires login |
| Swagger | https://gccwebapp.com/swagger | 404 (disabled) |

## If Issues Found

1. **HTTP 500.19** - web.config has Plesk-conflicting sections
   - Fix: Remove fileExtensions and hiddenSegments from web.config
   - See: docs/deployment/WEB_CONFIG_FIX_DEPLOYMENT_20251010.md

2. **Login fails** - Password hashes incorrect
   - Fix: Run `Scripts/RESET_PRODUCTION_PASSWORDS.sql`
   - See: docs/FIX_PRODUCTION_LOGIN.md

3. **No certificates display** - Schema ownership issue
   - Fix: See docs/database/reference/CRITICAL_SCHEMA_REQUIREMENT.md

4. **Application won't start** - Check logs in Plesk
   - Look for: Connection string errors, missing DLLs, permission issues

## Rollback Trigger Points

Initiate rollback (see ROLLBACK_PLAN.md) if:
- Application won't start after 5 minutes
- Critical login functionality broken
- Database connection completely failed
- Security vulnerability discovered

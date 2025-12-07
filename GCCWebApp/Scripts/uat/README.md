# UAT Deployment Management

This folder contains all scripts and documentation for User Acceptance Testing (UAT) deployment management.

## Folder Structure

```
Scripts/uat/
├── README.md                    # This file
├── PRE_DEPLOYMENT_CHECKLIST.md  # Steps before deployment
├── POST_DEPLOYMENT_CHECKLIST.md # Verification after deployment
├── ROLLBACK_PLAN.md             # How to revert if issues occur
├── deploy-uat.cmd               # One-click UAT deployment script
├── verify-deployment.cmd        # Post-deployment verification script
└── reset-uat-accounts.cmd       # Reset UAT test accounts
```

## Quick Start

### 1. Pre-Deployment
```bash
# Review checklist
cat PRE_DEPLOYMENT_CHECKLIST.md

# Build deployment package
./deploy-uat.cmd
```

### 2. Deploy to Production
- Upload package to AccuWebHosting via Plesk File Manager
- Follow steps in PRE_DEPLOYMENT_CHECKLIST.md

### 3. Post-Deployment Verification
```bash
# Run verification
./verify-deployment.cmd
```

### 4. Reset Test Accounts (if needed)
```bash
./reset-uat-accounts.cmd
```

## Key URLs

| Environment | URL |
|-------------|-----|
| Production | https://gccwebapp.com |
| Login | https://gccwebapp.com/Account/Login |
| Admin Dashboard | https://gccwebapp.com/Dashboard |
| Certificate Tile View | https://gccwebapp.com/Certificate/CertificateTileView |

## Test Credentials

### UAT Accounts (Use EMAIL to login)

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@greencirclecertified.com | Admin123! |
| Analyst | analyst@greencirclecertified.com | Analyst123! |
| External | partner@amazon.com | Partner123! |

### Dev Accounts (Use USERNAME to login)

| Role | Username | Password |
|------|----------|----------|
| Admin | admin | TestPass123! |
| Analyst | analyst | TestPass123! |
| External | external | TestPass123! |

## Database

| Setting | Value |
|---------|-------|
| Server | 68.71.129.120,1533 |
| Database | gccwebap_certificates |
| Username | GCC_adminxyz123 |

## Important Notes

1. **Automatic migrations are DISABLED** in production
2. Database schema changes require manual migration scripts
3. Always backup before deploying
4. Verify web.config doesn't have Plesk-conflicting sections

## Related Documentation

- `docs/UAT_CREDENTIALS_GUIDE.md` - Full credentials documentation
- `docs/FIX_PRODUCTION_LOGIN.md` - Login troubleshooting
- `docs/database/` - Database management documentation
- `Scripts/deployment/deploy-accuweb-production.cmd` - Main deployment script

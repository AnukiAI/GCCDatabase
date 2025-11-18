# Production Login Troubleshooting Guide

## Issue: Login Not Working in Production

### Quick Diagnostic Steps

#### 1. Check Application Logs (FIRST!)
Location on server: `C:\inetpub\wwwroot\gccwebapp.com\logs\stdout_*.log`

Look for:
- Database connection errors
- "User not found" warnings
- BCrypt verification failures
- Exception stack traces

```powershell
# View latest log file
Get-Content "C:\inetpub\wwwroot\gccwebapp.com\logs\stdout_*.log" -Tail 50
```

#### 2. Verify Database Connection
```sql
-- Connect to production database using SQL Server Management Studio:
-- Server: 68.71.129.120,1533
-- Database: gccwebap_certificates
-- Authentication: SQL Server Authentication
-- Username: GCC_adminxyz123
-- Password: Q3Ez4e!gE_47KNi

-- Run this test query:
SELECT @@VERSION, DB_NAME();
```

#### 3. Run Diagnostic Queries
Execute all queries in `PRODUCTION_LOGIN_DIAGNOSTICS.sql` file.

### Common Issues and Solutions

#### Issue A: "Users table does not exist"
**Cause:** Database migrations have not been run on production
**Solution:**
```bash
cd "C:\inetpub\wwwroot\gccwebapp.com"
dotnet ef database update --connection "Server=tcp:68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=Q3Ez4e!gE_47KNi;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
```

#### Issue B: "No users in database"
**Cause:** Database is empty, no user records exist
**Solution:**
```sql
-- Run the FULL_TEST_DATA_SEED.sql script from SSMS
-- This will create admin user and other test accounts
```

#### Issue C: "Tables in wrong schema (GCC_adminxyz123 instead of dbo)"
**Cause:** Database user doesn't have correct default schema
**Solution:**
```sql
-- Fix user's default schema
USE gccwebap_certificates;
GO

ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;
GO

-- Verify db_owner role membership
EXEC sp_addrolemember 'db_owner', 'GCC_adminxyz123';
GO

-- Check schema ownership
SELECT SCHEMA_NAME(schema_id), name FROM sys.tables;
-- Should show: dbo | Users, dbo | Roles, etc.
```

#### Issue D: "Invalid password" for admin
**Cause:** Password hash doesn't match or user doesn't exist
**Test Credentials:**
- Username: `admin`
- Email: `swest@anukiconsulting.com`
- Password: `TestPass123!`

**Verification Query:**
```sql
-- Check if admin exists with correct hash
SELECT
    Username,
    Email,
    IsActive,
    IsDeleted,
    Status,
    -- BCrypt hash should start with $2a$11$
    LEFT(PasswordHash, 10) AS HashPrefix
FROM Users
WHERE Username = 'admin';

-- EXPECTED:
-- Username: admin
-- Email: swest@anukiconsulting.com
-- IsActive: 1
-- IsDeleted: 0
-- Status: active
-- HashPrefix: $2a$11$...
```

#### Issue E: "Data Protection error" on startup
**Cause:** Azure Blob Storage placeholders in appsettings.Production.json
**Solution:** Already fixed - `PersistKeysToAzureBlobStorage: false`

#### Issue F: "HTTP 500 - Duplicate collection entry"
**Cause:** web.config has fileExtensions/hiddenSegments that conflict with Plesk
**Solution:** Already fixed - These sections removed from web.config

### Step-by-Step Verification

1. **Connect to production database:**
   - Open SQL Server Management Studio
   - Server: `68.71.129.120,1533`
   - Login: `GCC_adminxyz123` / `Q3Ez4e!gE_47KNi`

2. **Run diagnostic queries:**
   - Execute `PRODUCTION_LOGIN_DIAGNOSTICS.sql`
   - Save results

3. **Check results:**
   - ✅ Users table exists?
   - ✅ Admin user exists?
   - ✅ Admin user IsActive=1, IsDeleted=0, Status='active'?
   - ✅ Admin has role assigned?
   - ✅ Password hash starts with `$2a$11$`?
   - ✅ All tables in `dbo` schema?

4. **Check application logs:**
   - View `logs/stdout_*.log` on production server
   - Look for ERROR or WARNING messages during login attempt

### Expected Admin User Record

```
Id: 1
Username: admin
Email: swest@anukiconsulting.com
FirstName: Admin
LastName: User
IsActive: 1
IsDeleted: 0
Status: active
PasswordHash: $2a$11$<encrypted_hash>
Role: Admin
```

### Test Login Locally Against Production Database

To test the production database connection from your local machine:

1. **Temporarily update appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=Q3Ez4e!gE_47KNi;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
  }
}
```

2. **Run locally:**
```bash
dotnet run --urls "http://localhost:5101"
```

3. **Test login at:** http://localhost:5101/Account/Login
   - Username: `admin`
   - Password: `TestPass123!`

4. **If login works locally but not in production:**
   - Issue is with IIS/Plesk configuration
   - Check web.config ASPNETCORE_ENVIRONMENT is set to "Production"
   - Check IIS application pool identity has permissions

### Production Deployment Checklist

- [ ] Database migrations run (`dotnet ef database update`)
- [ ] Test data seeded (`FULL_TEST_DATA_SEED.sql`)
- [ ] Admin user exists with correct password hash
- [ ] User default schema is `dbo`
- [ ] All tables in `dbo` schema
- [ ] web.config environment is "Production"
- [ ] logs/ folder exists and is writable
- [ ] Application pool running as correct identity
- [ ] Connection string in appsettings.Production.json is correct

### Contact Information

If issue persists after all diagnostics:
1. Provide output from `PRODUCTION_LOGIN_DIAGNOSTICS.sql`
2. Provide last 100 lines from application logs
3. Provide exact error message or behavior when attempting login

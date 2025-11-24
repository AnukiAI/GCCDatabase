# Fix Production Login Issue

## Problem

Login is failing on production (https://gccwebapp.com) because the password hashes in the production database don't match the expected passwords.

**Diagnostic Results:**
- ✅ User exists (admin)
- ✅ User is active
- ✅ User has correct role (Admin)
- ❌ **Password verification fails** ← This is the issue

## Root Cause

The production database has different BCrypt password hashes than expected. This can happen when:
1. Database was restored from an old backup
2. Seed data didn't run properly during deployment
3. Passwords were manually changed

## Solution

Run the password reset SQL script on the production database.

### Step 1: Connect to Production Database

```bash
Server: 68.71.129.120,1533
Database: gccwebap_certificates
Username: GCC_adminxyz123
Password: Q3Ez4e!gE_47KNi
```

### Step 2: Run the SQL Script

**Option A: Using SQL Server Management Studio (SSMS)**
1. Open SSMS
2. Connect to: `68.71.129.120,1533`
3. Login with: `GCC_adminxyz123` / `Q3Ez4e!gE_47KNi`
4. Open file: `Scripts/RESET_PRODUCTION_PASSWORDS.sql`
5. Click **Execute** (F5)

**Option B: Using sqlcmd (Command Line)**
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "Scripts\RESET_PRODUCTION_PASSWORDS.sql"
```

### Step 3: Verify the Fix

After running the script, test login at:
- **URL**: https://gccwebapp.com/Account/Login
- **Username**: `admin`
- **Password**: `TestPass123!`

### Step 4: Test All Users

All user accounts now have password: `TestPass123!`

| Username | Email | Role | Password |
|----------|-------|------|----------|
| admin | swest@anukiconsulting.com | Admin | TestPass123! |
| analyst | swest@anukiconsulting.com | Analyst | TestPass123! |
| external | swest@anukiconsulting.com | External | TestPass123! |
| testuser | swest@anukiconsulting.com | Public | TestPass123! |

## What the Script Does

1. Shows all users BEFORE update
2. Updates password hash for all non-deleted users
3. Shows all users AFTER update
4. Confirms the changes

**BCrypt Hash Used:**
```
$2a$11$24Y94.bK.yWpd7.p/YDwZOy6qriBEiO5OGe0MmjsYjDPV04YLpqpa
```

This hash was generated and verified using:
```bash
cd Scripts/HashGenerator
dotnet run
```

## Alternative: Reset Only Admin User

If you only want to reset the admin password:

```sql
UPDATE dbo.Users
SET PasswordHash = '$2a$11$24Y94.bK.yWpd7.p/YDwZOy6qriBEiO5OGe0MmjsYjDPV04YLpqpa'
WHERE Username = 'admin';
```

## Troubleshooting

### If login still fails after running the script:

1. **Verify the update was applied:**
   ```sql
   SELECT Username, LEFT(PasswordHash, 30) + '...' as Hash
   FROM dbo.Users
   WHERE Username = 'admin';
   ```

   Should show: `$2a$11$24Y94.bK.yWpd7.p/YDwZ...`

2. **Check the diagnostic endpoint:**
   - Visit: https://gccwebapp.com/Account/TestAuth?username=admin&password=TestPass123!
   - Look for: `"Step4_PasswordVerified": true`

3. **Clear browser cookies:**
   - Old authentication cookies might interfere
   - Clear cookies for gccwebapp.com
   - Try in incognito/private mode

4. **Check 2FA settings:**
   - 2FA might be enabled
   - Check email for verification codes
   - Email goes to: swest@anukiconsulting.com

## Files Created

- `Scripts/RESET_PRODUCTION_PASSWORDS.sql` - Main script to reset passwords
- `Scripts/HashGenerator/` - Console app to generate BCrypt hashes
- `Scripts/FIX_PRODUCTION_LOGIN.md` - This document

## Security Note

**IMPORTANT:** After fixing the login issue, users should change their passwords from `TestPass123!` to secure, unique passwords.

For production use, consider:
1. Implementing password change requirement on first login
2. Setting up proper user onboarding flow
3. Using stronger default passwords or password reset emails

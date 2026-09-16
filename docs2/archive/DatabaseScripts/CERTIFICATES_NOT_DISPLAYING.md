# Certificates Not Displaying - Troubleshooting Guide

## Problem
- Database has 80 certificates (COUNT(*) returns 80)
- Website shows 0 certificates
- No JavaScript errors
- Build successful

## Root Cause: STATUS FILTERING

The public Certificate Tile View API **filters certificates by status**.

### Code Location
`Controllers/Api/CertificateTilesApiController.cs:73-74`
```csharp
Status = "active", // Only show active certificates for public view
```

`Controllers/Api/CertificateTilesApiController.cs:186`
```csharp
.Where(c => c.Status == "active" || c.Status == "expiring_soon" || c.Status == "renewal_in_progress");
```

### What This Means
**ONLY certificates with these exact status values will display:**
- ✅ `active`
- ✅ `expiring_soon`
- ✅ `renewal_in_progress`

**All other status values are HIDDEN:**
- ❌ `draft`
- ❌ `pending`
- ❌ `suspended`
- ❌ `expired`
- ❌ `revoked`
- ❌ `under_review`
- ❌ Any other value

## Diagnosis

### Step 1: Check Certificate Status Values
```bash
cd DatabaseScripts
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -i 05_Check_Certificate_Status.sql
```

This will show you:
- How many certificates have each status
- Which ones will display (VISIBLE vs HIDDEN)
- Sample certificates with their status

### Step 2: Fix Status Values (RECOMMENDED)
```bash
cd DatabaseScripts
FIX_STATUS_NOW.cmd
```

This will:
1. Show current status distribution
2. Update non-expired certificates to `active`
3. Update expiring certificates (< 30 days) to `expiring_soon`
4. Update expired certificates to `expired` (won't display)
5. Verify the fix

### Manual Fix (SQL)
```sql
-- Make all non-expired, non-draft certificates active
UPDATE Certificates
SET Status = 'active'
WHERE ExpirationDate > GETDATE()
  AND Status NOT IN ('draft', 'active', 'expiring_soon');

-- Update expiring soon (within 30 days)
UPDATE Certificates
SET Status = 'expiring_soon'
WHERE ExpirationDate < DATEADD(day, 30, GETDATE())
  AND ExpirationDate > GETDATE();
```

## Other Possible Causes (If Status Fix Doesn't Work)

### 1. Application Not Running in Production Mode
**Check:**
```bash
# On production server, verify environment variable
echo %ASPNETCORE_ENVIRONMENT%
# Should output: Production
```

**Fix:**
```bash
# Set environment variable
set ASPNETCORE_ENVIRONMENT=Production

# Restart application
```

### 2. Wrong Database Connection
**Check:** Look in application logs for the connection string being used

**Fix:** Verify `appsettings.Production.json` is deployed and has correct connection:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=tcp:68.71.129.120,1533;Database=gccwebap_certificates;..."
}
```

### 3. Missing Related Data (Foreign Keys)
**Check:**
```sql
-- Certificates missing companies
SELECT COUNT(*) FROM Certificates c
LEFT JOIN Companies co ON c.CompanyId = co.Id
WHERE co.Id IS NULL;

-- Certificates missing products
SELECT COUNT(*) FROM Certificates c
LEFT JOIN Products p ON c.ProductId = p.Id
WHERE p.Id IS NULL;

-- Certificates missing certification types
SELECT COUNT(*) FROM Certificates c
LEFT JOIN CertificationTypes ct ON c.CertificationTypeId = ct.Id
WHERE ct.Id IS NULL;
```

If any return > 0, you have orphaned certificates.

**Fix:** Either delete orphaned certificates or add missing related data.

### 4. API Endpoint Not Working
**Test API directly:**
```bash
# From PowerShell on production server
Invoke-WebRequest -Uri "http://localhost:5101/api/certificate-tiles" -UseBasicParsing
```

**Expected:** JSON response with `totalCount` and `results` array

**If error:** Check application logs for exceptions

### 5. JavaScript Not Loading
**Check browser console** (F12 → Console tab)

**Common errors:**
- "certificate-tile-view.js not found" → Missing static file
- "Uncaught ReferenceError" → JavaScript error
- CORS error → Check AllowedOrigins in appsettings

### 6. Browser Caching
**Fix:**
- Hard refresh: Ctrl + F5 (Windows) or Cmd + Shift + R (Mac)
- Clear browser cache
- Try incognito/private mode

### 7. Application Not Restarted After Database Changes
**Fix:**
```bash
# Restart IIS app pool (if using IIS)
Restart-WebAppPool "GCCWebApp"

# Or restart entire application
```

## Quick Diagnostic Checklist

Run these in order:

```bash
# 1. Check database has data
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P [password] -d gccwebap_certificates -Q "SELECT COUNT(*) FROM Certificates"

# 2. Check status values
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P [password] -d gccwebap_certificates -Q "SELECT Status, COUNT(*) FROM Certificates GROUP BY Status"

# 3. Check how many will display
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P [password] -d gccwebap_certificates -Q "SELECT COUNT(*) FROM Certificates WHERE Status IN ('active', 'expiring_soon', 'renewal_in_progress')"

# 4. Test API endpoint (from production server)
curl http://localhost:5101/api/certificate-tiles
```

## Expected Results After Fix

### Database Query
```sql
SELECT Status, COUNT(*) FROM Certificates GROUP BY Status;
```

**Should show:**
```
active              70
expiring_soon        5
expired              3
draft                2
```

### API Response
```bash
curl http://localhost:5101/api/certificate-tiles
```

**Should return:**
```json
{
  "totalCount": 75,
  "results": [
    {
      "id": 1,
      "certificateNumber": "24-1605",
      "status": "active",
      ...
    }
  ]
}
```

### Website
- Navigate to `/Certificate/CertificateTileView`
- Should see 75 certificates (active + expiring_soon)
- Filters should populate with data
- Search should work

## Solution Priority

Try in this order:

1. **FIX STATUS VALUES** ← Most likely issue (90% probability)
   - Run `FIX_STATUS_NOW.cmd`
   - This is almost certainly your problem

2. **Hard refresh browser** ← Simple and quick
   - Ctrl + F5

3. **Restart application** ← Ensures new data is loaded
   - IIS app pool restart

4. **Check environment** ← Verify using Production mode
   - Check ASPNETCORE_ENVIRONMENT variable

5. **Check connection string** ← Ensure pointing to production DB
   - Review appsettings.Production.json

## Still Not Working?

If after fixing status values certificates still don't display:

1. **Check application logs** (look for exceptions)
2. **Enable detailed errors temporarily:**
   ```json
   "FeatureFlags": {
     "EnableDetailedErrorMessages": true,
     "EnableDebugEndpoints": true
   }
   ```
3. **Test API endpoint directly** (see section 4 above)
4. **Check browser network tab** (F12 → Network) for failed requests
5. **Verify database connection** from application

## Files in This Fix Package

| File | Purpose |
|------|---------|
| `05_Check_Certificate_Status.sql` | Diagnose status values |
| `06_Fix_Certificate_Status.sql` | Fix status values |
| `FIX_STATUS_NOW.cmd` | One-click fix (Windows) |
| `CERTIFICATES_NOT_DISPLAYING.md` | This guide |

---

**TL;DR**: Your certificates probably have status values like "pending" or "draft" instead of "active". Run `FIX_STATUS_NOW.cmd` to fix it.

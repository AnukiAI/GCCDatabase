# CustomFields Table - BCP Export/Import Guide

**Date:** 2025-11-10
**Purpose:** Transfer 30 CustomFields records from Dev to Production using BCP
**Reason:** SQL export truncated long text in Options and ValidationRules columns

---

## Table Structure

The CustomFields table has these columns (from Model):
- `Id` (int, identity, PK)
- `Name` (nvarchar(100), required)
- `FieldType` (nvarchar(50), required) - text/number/date/boolean/select
- `IsRequired` (bit)
- `IsVisible` (bit)
- `IsPublic` (bit)
- `SortOrder` (int)
- `Options` (nvarchar(max)) - **LONG TEXT - JSON data**
- `DefaultValue` (nvarchar(max))
- `ValidationRules` (nvarchar(max)) - **LONG TEXT - JSON data**
- `CertificateTypeId` (int, FK)

---

## Step 1: Export from Dev (ANUKI01)

### Export Command (Native Format)

```cmd
bcp "GCCDatabase_Dev.dbo.CustomFields" out "C:\Temp\customfields.dat" -N -S ANUKI01 -U GCC_DevUser -P "DevP@ss2024!SQL"
```

**Parameters:**
- `-N`: Native format (preserves all data types, handles Unicode, best for SQL Server to SQL Server)
- `-S ANUKI01`: Source server
- `-U GCC_DevUser`: Username
- `-P "DevP@ss2024!SQL"`: Password
- `out`: Export mode
- `C:\Temp\customfields.dat`: Output file path

### Alternative: Export with Format File (More Control)

```cmd
REM Step 1: Generate format file
bcp "GCCDatabase_Dev.dbo.CustomFields" format nul -N -f "C:\Temp\customfields.fmt" -S ANUKI01 -U GCC_DevUser -P "DevP@ss2024!SQL"

REM Step 2: Export data using format file
bcp "GCCDatabase_Dev.dbo.CustomFields" out "C:\Temp\customfields.dat" -N -f "C:\Temp\customfields.fmt" -S ANUKI01 -U GCC_DevUser -P "DevP@ss2024!SQL"
```

---

## Step 2: Verify Export

### Check Record Count in Export

```cmd
REM Count should show 30 rows exported
```

The BCP export will display:
```
Starting copy...
30 rows copied.
Network packet size (bytes): 4096
Clock Time (ms.) Total     : 10
```

### Inspect Data File (Optional)

The `.dat` file is binary (native format), so you can't read it directly. But you can verify:
- File size should be > 0 bytes
- File exists at `C:\Temp\customfields.dat`

```cmd
dir "C:\Temp\customfields.dat"
```

---

## Step 3: Prepare Production Database

### IMPORTANT: Clear Existing Data (if any)

Before importing, we need to clear the CustomFields table in Production and reset the identity:

```sql
-- Run this on Production (gccwebap_certificates)
USE gccwebap_certificates;
GO

-- Delete existing records
DELETE FROM CustomFields;

-- Reset identity seed
DBCC CHECKIDENT ('CustomFields', RESEED, 0);
GO
```

Save this as `PrepareCustomFields_Production.sql` and execute:

```cmd
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i PrepareCustomFields_Production.sql
```

---

## Step 4: Import to Production (AccuWebHosting)

### Import Command (Native Format)

```cmd
bcp "gccwebap_certificates.dbo.CustomFields" in "C:\Temp\customfields.dat" -N -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -E
```

**Parameters:**
- `-N`: Native format (must match export)
- `-S 68.71.129.120,1533`: Production server and port
- `-U GCC_adminxyz123`: Production username
- `-P "Q3Ez4e!gE_47KNi"`: Production password
- `-E`: Keep identity values from source (preserve IDs)
- `in`: Import mode

**CRITICAL:** The `-E` flag preserves the identity (Id) values from Dev.

---

## Step 5: Verify Import

### Check Record Count in Production

```cmd
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT COUNT(*) AS CustomFieldsCount FROM CustomFields"
```

Expected output: `30`

### Verify Data Integrity

```sql
-- Check for NULL values in required fields
SELECT COUNT(*) AS NullNames
FROM CustomFields
WHERE Name IS NULL OR Name = '';

-- Should be 0

-- Check field types distribution
SELECT FieldType, COUNT(*) AS Count
FROM CustomFields
GROUP BY FieldType
ORDER BY FieldType;

-- Verify Options and ValidationRules are not truncated
SELECT Id, Name,
       LEN(Options) AS OptionsLength,
       LEN(ValidationRules) AS ValidationRulesLength
FROM CustomFields
WHERE Options IS NOT NULL OR ValidationRules IS NOT NULL
ORDER BY Id;

-- Compare with Dev - these lengths should match
```

---

## Complete Step-by-Step Script

Create a file `SYNC_CUSTOMFIELDS_BCP.cmd`:

```cmd
@echo off
echo ===============================================================
echo CustomFields BCP Export/Import Script
echo Dev (ANUKI01) -^> Production (AccuWebHosting)
echo ===============================================================
echo.

REM Step 1: Create temp directory
if not exist "C:\Temp" mkdir "C:\Temp"
echo [Step 1] Created temp directory

REM Step 2: Export from Dev
echo.
echo [Step 2] Exporting from Dev...
bcp "GCCDatabase_Dev.dbo.CustomFields" out "C:\Temp\customfields.dat" -N -S ANUKI01 -U GCC_DevUser -P "DevP@ss2024!SQL"

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Export failed with error code %ERRORLEVEL%
    pause
    exit /b %ERRORLEVEL%
)

echo [Step 2] Export successful!

REM Step 3: Verify export file exists
echo.
echo [Step 3] Verifying export file...
if not exist "C:\Temp\customfields.dat" (
    echo ERROR: Export file not found!
    pause
    exit /b 1
)

dir "C:\Temp\customfields.dat"
echo [Step 3] Export file verified!

REM Step 4: Prepare Production (clear existing data)
echo.
echo [Step 4] Preparing Production database...
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "DELETE FROM CustomFields; DBCC CHECKIDENT ('CustomFields', RESEED, 0);"

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to prepare Production database
    pause
    exit /b %ERRORLEVEL%
)

echo [Step 4] Production prepared!

REM Step 5: Import to Production
echo.
echo [Step 5] Importing to Production...
bcp "gccwebap_certificates.dbo.CustomFields" in "C:\Temp\customfields.dat" -N -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -E

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Import failed with error code %ERRORLEVEL%
    pause
    exit /b %ERRORLEVEL%
)

echo [Step 5] Import successful!

REM Step 6: Verify import
echo.
echo [Step 6] Verifying import...
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT COUNT(*) AS CustomFieldsCount FROM CustomFields"

echo.
echo ===============================================================
echo CustomFields BCP Sync Complete!
echo ===============================================================
echo.
echo Next steps:
echo 1. Review the record count above (should be 30)
echo 2. Test custom fields functionality in the application
echo 3. Clean up temp file: del "C:\Temp\customfields.dat"
echo.
pause
```

---

## Troubleshooting

### Issue: "Login failed for user"
**Solution:** Verify credentials are correct, test with `sqlcmd -L` to list servers

### Issue: "Cannot bulk load because the file could not be opened"
**Solution:**
- Check file path is correct
- Ensure file exists: `dir "C:\Temp\customfields.dat"`
- Check file permissions

### Issue: "Violation of PRIMARY KEY constraint"
**Solution:** Make sure you cleared the table and reset identity before import:
```sql
DELETE FROM CustomFields;
DBCC CHECKIDENT ('CustomFields', RESEED, 0);
```

### Issue: "Column count mismatch"
**Solution:**
- Ensure Dev and Production tables have identical schema
- Run migration on Production if needed: `dotnet ef database update`

### Issue: BCP not found
**Solution:** BCP is installed with SQL Server client tools. Verify:
```cmd
where bcp
```
If not found, install SQL Server Command Line Utilities.

---

## Alternative: Character Format (If Native Fails)

If native format has issues, use character format (slower but more compatible):

### Export (Character Format)
```cmd
bcp "GCCDatabase_Dev.dbo.CustomFields" out "C:\Temp\customfields.txt" -c -S ANUKI01 -U GCC_DevUser -P "DevP@ss2024!SQL" -t "|||" -r "\n"
```

### Import (Character Format)
```cmd
bcp "gccwebap_certificates.dbo.CustomFields" in "C:\Temp\customfields.txt" -c -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -t "|||" -r "\n" -E
```

**Parameters:**
- `-c`: Character format (readable but slower)
- `-t "|||"`: Field delimiter (unusual to avoid conflicts)
- `-r "\n"`: Row delimiter

---

## Post-Import Validation Queries

Run these on Production after import:

```sql
-- 1. Record count (should be 30)
SELECT COUNT(*) AS TotalCustomFields FROM CustomFields;

-- 2. Fields per certificate type
SELECT CertificateTypeId, COUNT(*) AS FieldCount
FROM CustomFields
GROUP BY CertificateTypeId
ORDER BY CertificateTypeId;

-- 3. Field types distribution
SELECT FieldType, COUNT(*) AS Count
FROM CustomFields
GROUP BY FieldType
ORDER BY Count DESC;

-- 4. Required vs Optional fields
SELECT IsRequired, COUNT(*) AS Count
FROM CustomFields
GROUP BY IsRequired;

-- 5. Verify no truncation in long fields (compare lengths)
SELECT
    Id,
    Name,
    LEN(ISNULL(Options, '')) AS OptionsLength,
    LEN(ISNULL(ValidationRules, '')) AS ValidationRulesLength
FROM CustomFields
ORDER BY Id;

-- 6. Sample data check (first 3 records)
SELECT TOP 3
    Id,
    Name,
    FieldType,
    IsRequired,
    SortOrder,
    LEFT(Options, 50) AS OptionsSample,
    LEFT(ValidationRules, 50) AS ValidationRulesSample
FROM CustomFields
ORDER BY Id;
```

---

## Success Criteria

- [ ] Export completes with "30 rows copied"
- [ ] Export file exists and is > 0 bytes
- [ ] Production table cleared successfully
- [ ] Import completes with "30 rows copied"
- [ ] Production shows COUNT(*) = 30
- [ ] No NULL values in required fields
- [ ] Options and ValidationRules fields are not truncated
- [ ] Application custom fields functionality works

---

## Files Generated

- `customfields.dat` - Binary export file (delete after successful import)
- `customfields.fmt` - Format file (optional, for advanced scenarios)
- `SYNC_CUSTOMFIELDS_BCP.cmd` - Automated batch script
- `PrepareCustomFields_Production.sql` - Table preparation script

---

**Ready to Execute:** Yes
**Estimated Time:** 2-5 minutes
**Risk Level:** Low (can rollback by re-running with Dev data)
**Dependencies:** BCP utility must be installed

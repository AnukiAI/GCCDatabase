# Manual Sync Instructions - 100% Dev to Prod

**Last Updated:** November 17, 2025

Simple copy/paste commands for manual execution.

---

## Step 1: Export from Dev Database

Open command prompt and run:

```bash
sqlcmd -S ANUKI01 -U GCC_DevUser -P "DevP@ss2024!SQL" -d GCCDatabase_Dev -i "export_dev_to_production_COMPLETE.sql" -o "PRODUCTION_DATA_COMPLETE.sql" -W
```

**Expected result:** Creates `PRODUCTION_DATA_COMPLETE.sql` file

---

## Step 2: Clear Existing Production Data (Optional but Recommended)

```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "DELETE FROM CertificateProductIds; DELETE FROM CertificateTags; DELETE FROM CustomFieldValues; DELETE FROM Certificates; DELETE FROM Products; DELETE FROM ProductIdentifiers; DELETE FROM Companies; DELETE FROM Tags; DELETE FROM CustomFields; PRINT 'Data cleared';"
```

---

## Step 3: Import to Production Database

```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "PRODUCTION_DATA_COMPLETE.sql" -o "import_results.log" -W -t 600
```

**Time:** 5-10 minutes depending on data size

---

## Step 4: Verify Import

```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT 'Companies' as Entity, COUNT(*) as Count FROM Companies UNION ALL SELECT 'Products', COUNT(*) FROM Products UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates UNION ALL SELECT 'CertificateProductIds', COUNT(*) FROM CertificateProductIds UNION ALL SELECT 'Tags', COUNT(*) FROM Tags ORDER BY Entity;"
```

---

## Step 5: Restart Application

**IIS:**
- Open IIS Manager
- Find GCCWebApp application pool
- Right-click → Recycle

**Or command line (if you have access):**
```bash
iisreset /noforce
```

---

## All-in-One Script

If you want to run all steps sequentially, copy this entire block:

```bash
echo Exporting from dev...
sqlcmd -S ANUKI01 -U GCC_DevUser -P "DevP@ss2024!SQL" -d GCCDatabase_Dev -i "export_dev_to_production_COMPLETE.sql" -o "PRODUCTION_DATA_COMPLETE.sql" -W

echo.
echo Clearing production data...
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "DELETE FROM CertificateProductIds; DELETE FROM CertificateTags; DELETE FROM CustomFieldValues; DELETE FROM Certificates; DELETE FROM Products; DELETE FROM ProductIdentifiers; DELETE FROM Companies; DELETE FROM Tags; DELETE FROM CustomFields; PRINT 'Data cleared';"

echo.
echo Importing to production...
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "PRODUCTION_DATA_COMPLETE.sql" -o "import_results.log" -W -t 600

echo.
echo Verifying...
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT 'Companies' as Entity, COUNT(*) as Count FROM Companies UNION ALL SELECT 'Products', COUNT(*) FROM Products UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates UNION ALL SELECT 'CertificateProductIds', COUNT(*) FROM CertificateProductIds ORDER BY Entity;"

echo.
echo DONE! Now restart IIS application pool.
```

---

## Quick Reference

**Export:**
```
sqlcmd -S ANUKI01 -U GCC_DevUser -P "DevP@ss2024!SQL" -d GCCDatabase_Dev -i "export_dev_to_production_COMPLETE.sql" -o "PRODUCTION_DATA_COMPLETE.sql" -W
```

**Import:**
```
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "PRODUCTION_DATA_COMPLETE.sql" -o "import_results.log" -W -t 600
```

**Verify:**
```
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT COUNT(*) FROM Certificates; SELECT COUNT(*) FROM CertificateProductIds;"
```

---

## Notes

- Run these commands from the `GCCWebApp` directory
- Make sure `export_dev_to_production_COMPLETE.sql` file exists
- The `-t 600` parameter sets 10-minute timeout (increase if needed)
- Check `import_results.log` if import fails
- Always restart IIS after import

---

**That's it!** Just copy/paste the commands in order.

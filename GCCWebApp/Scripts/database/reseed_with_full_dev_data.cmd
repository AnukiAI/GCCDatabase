@echo off
REM Script to replace test data with full dev dataset
REM This will give you 178 certificates, 28 companies, 39 products

echo ============================================
echo GCC Production - Reseed with Full Dev Data
echo ============================================
echo.
echo This will:
echo   1. Delete 22 test certificates
echo   2. Delete 10 test products
echo   3. Delete 10 test companies
echo   4. Seed 178 certificates from dev
echo   5. Seed 39 products from dev
echo   6. Seed 28 companies from dev
echo.
echo Database: 68.71.129.120,1533 / gccwebap_certificates
echo.
pause

echo.
echo Step 1: Clearing test data...
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "clear_and_reseed_production.sql" -o "clear_results.log"

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to clear test data
    echo Check clear_results.log for details
    pause
    exit /b 1
)

echo ✓ Test data cleared
echo.
echo Step 2: Seeding full dev dataset...
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "FULL_TEST_DATA_SEED_PRODUCTION.sql" -o "full_seed_results.log"

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to seed full dataset
    echo Check full_seed_results.log for details
    pause
    exit /b 1
)

echo.
echo ============================================
echo SUCCESS: Production reseeded with dev data
echo ============================================
echo.
echo Check logs:
echo   - clear_results.log
echo   - full_seed_results.log
echo.
echo Next step: Restart IIS application
echo Then visit: https://gccwebapp.com/
echo.
echo You should now see 178 certificates!
echo.
pause

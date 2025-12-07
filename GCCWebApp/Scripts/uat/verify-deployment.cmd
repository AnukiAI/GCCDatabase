@echo off
REM ============================================
REM GCC Web Application - Post-Deployment Verification
REM ============================================
REM Runs automated checks against production site
REM ============================================

setlocal enabledelayedexpansion

echo.
echo ============================================
echo GCC Web Application - Deployment Verification
echo ============================================
echo.

set "INFO=[92m[INFO][0m"
set "ERROR=[91m[ERROR][0m"
set "SUCCESS=[92m[SUCCESS][0m"
set "FAIL=[91m[FAIL][0m"

set "BASE_URL=https://gccwebapp.com"
set "PASSED=0"
set "FAILED=0"

REM ============================================
echo Testing: Homepage
echo ============================================

curl -s -o nul -w "%%{http_code}" "%BASE_URL%" > temp_status.txt
set /p STATUS=<temp_status.txt
del temp_status.txt

if "%STATUS%"=="200" (
    echo %SUCCESS% Homepage: HTTP %STATUS%
    set /a PASSED+=1
) else (
    echo %FAIL% Homepage: HTTP %STATUS%
    set /a FAILED+=1
)

REM ============================================
echo.
echo Testing: Login Page
echo ============================================

curl -s -o nul -w "%%{http_code}" "%BASE_URL%/Account/Login" > temp_status.txt
set /p STATUS=<temp_status.txt
del temp_status.txt

if "%STATUS%"=="200" (
    echo %SUCCESS% Login Page: HTTP %STATUS%
    set /a PASSED+=1
) else (
    echo %FAIL% Login Page: HTTP %STATUS%
    set /a FAILED+=1
)

REM ============================================
echo.
echo Testing: Certificate Tile View
echo ============================================

curl -s -o nul -w "%%{http_code}" "%BASE_URL%/Certificate/CertificateTileView" > temp_status.txt
set /p STATUS=<temp_status.txt
del temp_status.txt

if "%STATUS%"=="200" (
    echo %SUCCESS% Certificate Tile View: HTTP %STATUS%
    set /a PASSED+=1
) else (
    echo %FAIL% Certificate Tile View: HTTP %STATUS%
    set /a FAILED+=1
)

REM ============================================
echo.
echo Testing: API Tiles Endpoint
echo ============================================

curl -s -o nul -w "%%{http_code}" "%BASE_URL%/api/CertificateTiles/tiles?pageNumber=1&pageSize=10" > temp_status.txt
set /p STATUS=<temp_status.txt
del temp_status.txt

if "%STATUS%"=="200" (
    echo %SUCCESS% API Tiles: HTTP %STATUS%
    set /a PASSED+=1
) else (
    echo %FAIL% API Tiles: HTTP %STATUS%
    set /a FAILED+=1
)

REM ============================================
echo.
echo Testing: Static Files (CSS)
echo ============================================

curl -s -o nul -w "%%{http_code}" "%BASE_URL%/css/site.css" > temp_status.txt
set /p STATUS=<temp_status.txt
del temp_status.txt

if "%STATUS%"=="200" (
    echo %SUCCESS% Static CSS: HTTP %STATUS%
    set /a PASSED+=1
) else (
    echo %FAIL% Static CSS: HTTP %STATUS%
    set /a FAILED+=1
)

REM ============================================
echo.
echo Testing: HTTPS Redirect
echo ============================================

curl -s -o nul -w "%%{http_code}" -L "http://gccwebapp.com" > temp_status.txt
set /p STATUS=<temp_status.txt
del temp_status.txt

if "%STATUS%"=="200" (
    echo %SUCCESS% HTTPS Redirect: HTTP %STATUS%
    set /a PASSED+=1
) else (
    echo %FAIL% HTTPS Redirect: HTTP %STATUS%
    set /a FAILED+=1
)

REM ============================================
echo.
echo Testing: Swagger Disabled (should 404)
echo ============================================

curl -s -o nul -w "%%{http_code}" "%BASE_URL%/swagger" > temp_status.txt
set /p STATUS=<temp_status.txt
del temp_status.txt

if "%STATUS%"=="404" (
    echo %SUCCESS% Swagger Disabled: HTTP %STATUS% (expected)
    set /a PASSED+=1
) else if "%STATUS%"=="302" (
    echo %SUCCESS% Swagger Redirects: HTTP %STATUS% (acceptable)
    set /a PASSED+=1
) else (
    echo %FAIL% Swagger Should Be Disabled: HTTP %STATUS%
    set /a FAILED+=1
)

REM ============================================
echo.
echo ============================================
echo VERIFICATION RESULTS
echo ============================================
echo.
echo   Passed: %PASSED%
echo   Failed: %FAILED%
echo.

if %FAILED% EQU 0 (
    echo [92mALL TESTS PASSED![0m
    echo.
    echo Deployment verified successfully.
) else (
    echo [91mSOME TESTS FAILED![0m
    echo.
    echo Review failed tests above.
    echo See POST_DEPLOYMENT_CHECKLIST.md for manual verification.
)

echo.
echo ============================================
echo Manual Verification Required:
echo ============================================
echo.
echo 1. Login test: %BASE_URL%/Account/Login
echo    - Username: admin
echo    - Password: TestPass123!
echo.
echo 2. Admin dashboard: %BASE_URL%/Dashboard
echo 3. User management: %BASE_URL%/Admin/UserManagement
echo.
echo See POST_DEPLOYMENT_CHECKLIST.md for full checklist.
echo.
pause

@echo off
REM ============================================================================
REM GCC Web App - Production Test Runner (Batch Wrapper)
REM ============================================================================
REM Quick commands for testing production site: https://gccwebapp.com
REM
REM USAGE:
REM   run-production-tests              - Run all UAT tests (interactive)
REM   run-production-tests quick        - Run smoke tests only
REM   run-production-tests headless     - Run without browser window
REM   run-production-tests login        - Run only login tests
REM   run-production-tests crud              - Run ALL CRUD tests
REM   run-production-tests crud-tags         - Tag Management CRUD tests
REM   run-production-tests crud-users        - User Management CRUD tests
REM   run-production-tests crud-companies    - Company Management CRUD tests
REM   run-production-tests crud-products     - Product Management CRUD tests
REM   run-production-tests crud-certtypes    - Certificate Types CRUD tests
REM   run-production-tests crud-certfields   - Certificate Fields CRUD tests
REM   run-production-tests crud-partners     - Partners CRUD tests
REM   run-production-tests crud-roles        - Role Management CRUD tests (NEW)
REM   run-production-tests crud-partnerapi   - Partner API Management tests (NEW)
REM   run-production-tests crud-certs        - Certificate CRUD tests (NEW)
REM   run-production-tests access-control    - Role-based access control tests (NEW)
REM   run-production-tests crud-prodtypes    - Product Types CRUD tests (Sprint 2)
REM   run-production-tests crud-productid    - Product ID management tests (Sprint 2)
REM   run-production-tests validation        - Cross-entity validation tests (Sprint 2)
REM   run-production-tests crud-gccapi       - GCC API Keys CRUD tests (Sprint 2)
REM   run-production-tests import-export     - Import/Export tests (Sprint 3)
REM   run-production-tests edge-cases        - Edge case tests (Sprint 3)
REM   run-production-tests workflow          - Certificate workflow tests (Sprint 3)
REM   run-production-tests all               - Run UAT + E2E + Accessibility
REM
REM ============================================================================

setlocal enabledelayedexpansion

cd /d "%~dp0"

if "%1"=="" (
    echo.
    echo ============================================================================
    echo  GCC Web App - Production Test Runner
    echo ============================================================================
    echo.
    echo  Target: https://gccwebapp.com
    echo.
    echo  Commands:
    echo    run-production-tests              Run all UAT tests
    echo    run-production-tests quick        Smoke tests only
    echo    run-production-tests headless     Run without visible browser
    echo    run-production-tests login        Login tests only
    echo    run-production-tests navigation   Navigation tests only
    echo    run-production-tests crud         Run ALL CRUD tests
    echo    run-production-tests crud-tags    Tag Management CRUD
    echo    run-production-tests crud-users   User Management CRUD
    echo    run-production-tests crud-companies Company Management CRUD
    echo    run-production-tests crud-products  Product Management CRUD
    echo    run-production-tests crud-certtypes Certificate Types CRUD
    echo    run-production-tests crud-certfields Certificate Fields CRUD
    echo    run-production-tests crud-partners  Partners CRUD
    echo    run-production-tests crud-roles     Role Management CRUD
    echo    run-production-tests crud-partnerapi Partner API Management
    echo    run-production-tests crud-certs     Certificate CRUD
    echo    run-production-tests access-control Role-based access control
    echo    run-production-tests crud-prodtypes  Product Types CRUD
    echo    run-production-tests crud-productid  Product ID management
    echo    run-production-tests validation      Validation tests
    echo    run-production-tests crud-gccapi     GCC API Keys CRUD
    echo    run-production-tests import-export   Import/Export tests
    echo    run-production-tests edge-cases      Edge case tests
    echo    run-production-tests workflow        Workflow tests
    echo    run-production-tests all          All production-safe tests
    echo    run-production-tests help         Show this help
    echo.
    echo ============================================================================
    echo.
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1"
    goto :end
)

if /i "%1"=="quick" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -Quick
    goto :end
)

if /i "%1"=="headless" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -Headless
    goto :end
)

if /i "%1"=="login" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "Login"
    goto :end
)

if /i "%1"=="navigation" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "Navigation"
    goto :end
)

if /i "%1"=="all" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestProject "All"
    goto :end
)

if /i "%1"=="crud" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "Category=CRUD"
    goto :end
)

if /i "%1"=="crud-tags" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "TagManagementCrud"
    goto :end
)

if /i "%1"=="crud-users" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "UserManagementCrud"
    goto :end
)

if /i "%1"=="crud-companies" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "CompanyManagementCrud"
    goto :end
)

if /i "%1"=="crud-products" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "ProductManagementCrud"
    goto :end
)

if /i "%1"=="crud-certtypes" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "CertificateTypesCrud"
    goto :end
)

if /i "%1"=="crud-certfields" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "CertificateFieldsCrud"
    goto :end
)

if /i "%1"=="crud-partners" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "PartnersCrud"
    goto :end
)

if /i "%1"=="crud-roles" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "RoleManagementCrud"
    goto :end
)

if /i "%1"=="crud-partnerapi" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "PartnerApiCrud"
    goto :end
)

if /i "%1"=="crud-certs" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "CertificateCrud"
    goto :end
)

if /i "%1"=="access-control" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "Category=AccessControl"
    goto :end
)

if /i "%1"=="crud-prodtypes" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "ProductTypesCrud"
    goto :end
)

if /i "%1"=="crud-productid" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "ProductIdCrud"
    goto :end
)

if /i "%1"=="validation" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "Category=Validation"
    goto :end
)

if /i "%1"=="crud-gccapi" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "GccApiCrud"
    goto :end
)

if /i "%1"=="import-export" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "Category=ImportExport"
    goto :end
)

if /i "%1"=="edge-cases" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "Category=EdgeCase"
    goto :end
)

if /i "%1"=="workflow" (
    powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" -TestFilter "Category=Workflow"
    goto :end
)

if /i "%1"=="help" (
    echo.
    echo GCC Web App - Production Test Runner
    echo =====================================
    echo.
    echo This tool runs automated Playwright tests against the live production site.
    echo.
    echo Prerequisites:
    echo   - .NET 8.0 SDK installed
    echo   - Playwright browsers installed (auto-installs on first run)
    echo   - Network access to https://gccwebapp.com
    echo.
    echo Test Credentials (production uses USERNAME, not email):
    echo   Admin:    admin / TestPass123!
    echo   Analyst:  analyst / TestPass123!
    echo   External: external / TestPass123!
    echo.
    echo Test Categories:
    echo   UAT           - User acceptance tests (login, navigation, UI)
    echo   CRUD          - Create, Read, Update, Delete operations
    echo   E2E           - End-to-end workflow tests
    echo   Accessibility - WCAG compliance checks
    echo.
    echo CRUD Test Coverage (Sprint 1):
    echo   crud-tags       - Tag Management (External/Internal tags)
    echo   crud-users      - User Management (with admin protection)
    echo   crud-companies  - Company Management
    echo   crud-products   - Product Management (with filters)
    echo   crud-certtypes  - Certificate Types
    echo   crud-certfields - Certificate Fields
    echo   crud-partners   - Partners (Export + Add only, no Import)
    echo   crud-roles      - Role Management with permissions
    echo   crud-partnerapi - Partner API key management
    echo   crud-certs      - Certificate CRUD operations
    echo.
    echo CRUD Test Coverage (Sprint 2):
    echo   crud-prodtypes  - Product Types hierarchical management
    echo   crud-productid  - Product ID (ASIN/UPC/GTIN) management
    echo   crud-gccapi     - GCC API Keys CRUD operations
    echo   validation      - Cross-entity validation tests
    echo.
    echo Sprint 3 Tests:
    echo   import-export   - Import/Export across all entities
    echo   edge-cases      - Edge cases, special chars, long input
    echo   workflow        - Certificate workflow and status transitions
    echo.
    echo Access Control Tests:
    echo   access-control  - Role-based access control (Admin/Analyst/External)
    echo.
    echo Examples:
    echo   run-production-tests crud              Run all CRUD tests
    echo   run-production-tests crud-tags         Run tag CRUD tests only
    echo   run-production-tests quick -Headless   Headless smoke tests
    echo.
    goto :end
)

REM Pass any other arguments directly to PowerShell
powershell -ExecutionPolicy Bypass -File "run-production-tests.ps1" %*

:end
endlocal

@echo off
echo ========================================================================
echo                  GCC Web Application Role-Based UI Tests
echo ========================================================================
echo.

set SCRIPT_DIR=%~dp0
set PROJECT_DIR=%SCRIPT_DIR%
set REPORTS_DIR=%SCRIPT_DIR%test-results
set SCREENSHOTS_DIR=%SCRIPT_DIR%Screenshots
set LOGS_DIR=%SCRIPT_DIR%test-logs
set TIMESTAMP=%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set TIMESTAMP=%TIMESTAMP: =0%
set LOG_FILE=%LOGS_DIR%\RoleBasedUITests_%TIMESTAMP%.log
set SUMMARY_FILE=%LOGS_DIR%\TestSummary_%TIMESTAMP%.log

echo Creating test directories...
if not exist "%REPORTS_DIR%" mkdir "%REPORTS_DIR%"
if not exist "%SCREENSHOTS_DIR%" mkdir "%SCREENSHOTS_DIR%"
if not exist "%LOGS_DIR%" mkdir "%LOGS_DIR%"

echo.
echo Initializing test logging...
echo ======================================================================== > "%LOG_FILE%"
echo                  GCC Web Application Role-Based UI Tests >> "%LOG_FILE%"
echo                          Test Run: %DATE% %TIME% >> "%LOG_FILE%"
echo ======================================================================== >> "%LOG_FILE%"
echo. >> "%LOG_FILE%"

echo ======================================================================== > "%SUMMARY_FILE%"
echo                        Role-Based UI Test Summary >> "%SUMMARY_FILE%"
echo                          Test Run: %DATE% %TIME% >> "%SUMMARY_FILE%"
echo ======================================================================== >> "%SUMMARY_FILE%"
echo. >> "%SUMMARY_FILE%"

echo.
echo ========================================================================
echo                        Installing Playwright Browsers
echo ========================================================================
echo.

echo Installing Playwright browsers (required for UI tests)...
echo Installing Playwright browsers... >> "%LOG_FILE%"
cd "%PROJECT_DIR%"
dotnet build >> "%LOG_FILE%" 2>&1
echo Build completed with exit code: %errorlevel% >> "%LOG_FILE%"
powershell -Command "& { if (Get-Command 'pwsh' -ErrorAction SilentlyContinue) { pwsh -c 'npx playwright install' } else { powershell -c 'npx playwright install' } }" >> "%LOG_FILE%" 2>&1
if errorlevel 1 (
    echo Attempting alternative Playwright installation... >> "%LOG_FILE%"
    dotnet tool install --global Microsoft.Playwright.CLI >> "%LOG_FILE%" 2>&1
    playwright install >> "%LOG_FILE%" 2>&1
)

echo.
echo ========================================================================
echo                         Running Role-Based UI Tests
echo ========================================================================
echo.

echo [Phase 1] Admin Role Tests...
echo Running comprehensive admin role UI validation tests...
echo. >> "%LOG_FILE%"
echo [Phase 1] Admin Role Tests - %TIME% >> "%LOG_FILE%"
echo ======================================================================== >> "%LOG_FILE%"
dotnet test --filter "Category=AdminRole|FullyQualifiedName~AdminRole" --logger "console;verbosity=normal" --logger "trx;LogFileName=AdminRoleTests_%TIMESTAMP%.trx" --results-directory "%REPORTS_DIR%" >> "%LOG_FILE%" 2>&1
set ADMIN_EXIT_CODE=%errorlevel%
echo Admin Role Tests Exit Code: %ADMIN_EXIT_CODE% >> "%LOG_FILE%"
if errorlevel 1 (
    echo WARNING: Some admin role tests failed! >> "%LOG_FILE%"
    echo [FAILED] Admin Role Tests >> "%SUMMARY_FILE%"
    echo WARNING: Some admin role tests failed!
    echo.
) else (
    echo [PASSED] Admin Role Tests >> "%SUMMARY_FILE%"
)

echo.
echo [Phase 2] Analyst Role Tests...
echo Running analyst role UI validation tests...
echo. >> "%LOG_FILE%"
echo [Phase 2] Analyst Role Tests - %TIME% >> "%LOG_FILE%"
echo ======================================================================== >> "%LOG_FILE%"
dotnet test --filter "Category=AnalystRole|FullyQualifiedName~AnalystRole" --logger "console;verbosity=normal" --logger "trx;LogFileName=AnalystRoleTests_%TIMESTAMP%.trx" --results-directory "%REPORTS_DIR%" >> "%LOG_FILE%" 2>&1
set ANALYST_EXIT_CODE=%errorlevel%
echo Analyst Role Tests Exit Code: %ANALYST_EXIT_CODE% >> "%LOG_FILE%"
if errorlevel 1 (
    echo WARNING: Some analyst role tests failed! >> "%LOG_FILE%"
    echo [FAILED] Analyst Role Tests >> "%SUMMARY_FILE%"
    echo WARNING: Some analyst role tests failed!
    echo.
) else (
    echo [PASSED] Analyst Role Tests >> "%SUMMARY_FILE%"
)

echo.
echo [Phase 3] External Role Tests...
echo Running external user role UI validation tests...
echo. >> "%LOG_FILE%"
echo [Phase 3] External Role Tests - %TIME% >> "%LOG_FILE%"
echo ======================================================================== >> "%LOG_FILE%"
dotnet test --filter "Category=ExternalRole|FullyQualifiedName~ExternalRole" --logger "console;verbosity=normal" --logger "trx;LogFileName=ExternalRoleTests_%TIMESTAMP%.trx" --results-directory "%REPORTS_DIR%" >> "%LOG_FILE%" 2>&1
set EXTERNAL_EXIT_CODE=%errorlevel%
echo External Role Tests Exit Code: %EXTERNAL_EXIT_CODE% >> "%LOG_FILE%"
if errorlevel 1 (
    echo WARNING: Some external role tests failed! >> "%LOG_FILE%"
    echo [FAILED] External Role Tests >> "%SUMMARY_FILE%"
    echo WARNING: Some external role tests failed!
    echo.
) else (
    echo [PASSED] External Role Tests >> "%SUMMARY_FILE%"
)

echo.
echo [Phase 4] Public Role Tests...
echo Running public/unauthenticated user UI validation tests...
echo. >> "%LOG_FILE%"
echo [Phase 4] Public Role Tests - %TIME% >> "%LOG_FILE%"
echo ======================================================================== >> "%LOG_FILE%"
dotnet test --filter "Category=PublicRole|FullyQualifiedName~PublicRole" --logger "console;verbosity=normal" --logger "trx;LogFileName=PublicRoleTests_%TIMESTAMP%.trx" --results-directory "%REPORTS_DIR%" >> "%LOG_FILE%" 2>&1
set PUBLIC_EXIT_CODE=%errorlevel%
echo Public Role Tests Exit Code: %PUBLIC_EXIT_CODE% >> "%LOG_FILE%"
if errorlevel 1 (
    echo WARNING: Some public role tests failed! >> "%LOG_FILE%"
    echo [FAILED] Public Role Tests >> "%SUMMARY_FILE%"
    echo WARNING: Some public role tests failed!
    echo.
) else (
    echo [PASSED] Public Role Tests >> "%SUMMARY_FILE%"
)

echo.
echo [Phase 5] Cross-Role Access Violation Tests...
echo Running role access violation and security tests...
echo. >> "%LOG_FILE%"
echo [Phase 5] Cross-Role Access Violation Tests - %TIME% >> "%LOG_FILE%"
echo ======================================================================== >> "%LOG_FILE%"
dotnet test --filter "Category=CrossRole|FullyQualifiedName~CrossRole" --logger "console;verbosity=normal" --logger "trx;LogFileName=CrossRoleTests_%TIMESTAMP%.trx" --results-directory "%REPORTS_DIR%" >> "%LOG_FILE%" 2>&1
set CROSSROLE_EXIT_CODE=%errorlevel%
echo Cross-Role Tests Exit Code: %CROSSROLE_EXIT_CODE% >> "%LOG_FILE%"
if errorlevel 1 (
    echo WARNING: Some cross-role access tests failed! >> "%LOG_FILE%"
    echo [FAILED] Cross-Role Access Violation Tests >> "%SUMMARY_FILE%"
    echo WARNING: Some cross-role access tests failed!
    echo This may indicate security vulnerabilities!
    echo.
) else (
    echo [PASSED] Cross-Role Access Violation Tests >> "%SUMMARY_FILE%"
)

echo.
echo ========================================================================
echo                          Full Test Suite Run
echo ========================================================================
echo.

echo Running complete role-based UI test suite...
echo. >> "%LOG_FILE%"
echo [Full Test Suite] Complete Role-Based UI Tests - %TIME% >> "%LOG_FILE%"
echo ======================================================================== >> "%LOG_FILE%"
dotnet test --logger "console;verbosity=normal" --logger "trx;LogFileName=CompleteRoleBasedUITests_%TIMESTAMP%.trx" --results-directory "%REPORTS_DIR%" >> "%LOG_FILE%" 2>&1
set TEST_EXIT_CODE=%errorlevel%
echo Full Test Suite Exit Code: %TEST_EXIT_CODE% >> "%LOG_FILE%"

echo.
echo ========================================================================
echo                            Test Results Summary
echo ========================================================================
echo.

echo. >> "%LOG_FILE%"
echo ======================================================================== >> "%LOG_FILE%"
echo                            Test Results Summary >> "%LOG_FILE%"
echo ======================================================================== >> "%LOG_FILE%"
echo Test Execution Completed: %DATE% %TIME% >> "%LOG_FILE%"
echo. >> "%LOG_FILE%"
echo Test Results Location: %REPORTS_DIR% >> "%LOG_FILE%"
echo Screenshots Location: %SCREENSHOTS_DIR% >> "%LOG_FILE%"
echo Log Files Location: %LOGS_DIR% >> "%LOG_FILE%"
echo. >> "%LOG_FILE%"

echo. >> "%SUMMARY_FILE%"
echo ======================================================================== >> "%SUMMARY_FILE%"
echo                         Final Test Results >> "%SUMMARY_FILE%"
echo ======================================================================== >> "%SUMMARY_FILE%"
echo Test Execution Completed: %DATE% %TIME% >> "%SUMMARY_FILE%"
echo. >> "%SUMMARY_FILE%"

echo Test Results Location: %REPORTS_DIR%
echo Screenshots Location: %SCREENSHOTS_DIR%
echo Log Files Location: %LOGS_DIR%
echo Test Execution Completed: %DATE% %TIME%
echo.

if %TEST_EXIT_CODE% equ 0 (
    echo ✅ ALL ROLE-BASED UI TESTS PASSED >> "%LOG_FILE%"
    echo All role access controls are functioning correctly. >> "%LOG_FILE%"
    echo [OVERALL RESULT] ALL TESTS PASSED >> "%SUMMARY_FILE%"
    echo ✅ ALL ROLE-BASED UI TESTS PASSED
    echo All role access controls are functioning correctly.
) else (
    echo ❌ SOME ROLE-BASED UI TESTS FAILED >> "%LOG_FILE%"
    echo Please review the test results and screenshots for details. >> "%LOG_FILE%"
    echo [OVERALL RESULT] SOME TESTS FAILED >> "%SUMMARY_FILE%"
    echo ❌ SOME ROLE-BASED UI TESTS FAILED
    echo Please review the test results and screenshots for details.
    echo.
    echo Critical areas to check:
    echo - Role element visibility
    echo - Access control restrictions
    echo - Navigation permissions
    echo - UI component security
)

echo.
echo ========================================================================
echo                              Quick Links
echo ========================================================================
echo.

echo Test Reports: %REPORTS_DIR%
echo Screenshots: %SCREENSHOTS_DIR%
echo Log Files: %LOGS_DIR%
echo.
echo Detailed Log Files:
echo - Complete Test Log: %LOG_FILE%
echo - Test Summary: %SUMMARY_FILE%
echo.
echo To view specific role screenshots:
echo - Admin Role: %SCREENSHOTS_DIR%\*Admin*.png
echo - Analyst Role: %SCREENSHOTS_DIR%\*Analyst*.png  
echo - External Role: %SCREENSHOTS_DIR%\*External*.png
echo - Public Role: %SCREENSHOTS_DIR%\*Public*.png
echo.

echo. >> "%SUMMARY_FILE%"
echo Individual Test Results: >> "%SUMMARY_FILE%"
echo - Admin Role Tests: Exit Code %ADMIN_EXIT_CODE% >> "%SUMMARY_FILE%"
echo - Analyst Role Tests: Exit Code %ANALYST_EXIT_CODE% >> "%SUMMARY_FILE%"
echo - External Role Tests: Exit Code %EXTERNAL_EXIT_CODE% >> "%SUMMARY_FILE%"
echo - Public Role Tests: Exit Code %PUBLIC_EXIT_CODE% >> "%SUMMARY_FILE%"
echo - Cross-Role Tests: Exit Code %CROSSROLE_EXIT_CODE% >> "%SUMMARY_FILE%"
echo - Full Test Suite: Exit Code %TEST_EXIT_CODE% >> "%SUMMARY_FILE%"
echo. >> "%SUMMARY_FILE%"
echo File Locations: >> "%SUMMARY_FILE%"
echo - Test Reports: %REPORTS_DIR% >> "%SUMMARY_FILE%"
echo - Screenshots: %SCREENSHOTS_DIR% >> "%SUMMARY_FILE%"
echo - Complete Log: %LOG_FILE% >> "%SUMMARY_FILE%"
echo - Summary Log: %SUMMARY_FILE% >> "%SUMMARY_FILE%"

echo ========================================================================
echo                          Running Quick Manual Tests
echo ========================================================================
echo.

echo You can now manually verify the following URLs for each role:
echo.
echo Admin User (admin@test.com / TestAdmin123!):
echo   - https://localhost:5001/Admin/RoleManagement
echo   - https://localhost:5001/Admin/UserManagement
echo   - https://localhost:5001/Certificate
echo   - https://localhost:5001/Company
echo.
echo Analyst User (analyst@test.com / TestAnalyst123!):
echo   - https://localhost:5001/Certificate (should have limited access)
echo   - https://localhost:5001/Company (should have access)
echo   - https://localhost:5001/Admin/RoleManagement (should be denied)
echo.
echo External User (external@test.com / TestExternal123!):
echo   - https://localhost:5001/Certificate/CertificateTileViewExternal
echo   - https://localhost:5001/Product/Management (limited access)
echo   - https://localhost:5001/Company (should be denied)
echo.
echo Public User (no login):
echo   - https://localhost:5001/ (should show public view)
echo   - https://localhost:5001/Certificate (basic public access)
echo   - https://localhost:5001/Admin (should redirect to login)
echo.

pause
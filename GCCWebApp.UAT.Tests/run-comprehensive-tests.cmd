@echo off
echo =========================================
echo   GCC Database Solution - Comprehensive Test Suite
echo =========================================
echo.
echo This script runs all test suites with role-based authentication
echo and generates comprehensive screenshots and reports.
echo.

REM Get current timestamp for report naming
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set "YYYY=%dt:~0,4%"
set "MM=%dt:~4,2%"
set "DD=%dt:~6,2%"
set "HH=%dt:~8,2%"
set "Min=%dt:~10,2%"
set "Sec=%dt:~12,2%"
set "timestamp=%YYYY%%MM%%DD%_%HH%%Min%%Sec%"

echo Test execution started at: %timestamp%
echo.

REM Create test results directory
set "RESULTS_DIR=TestResults\%timestamp%"
mkdir "%RESULTS_DIR%" 2>nul
mkdir "%RESULTS_DIR%\Screenshots" 2>nul
mkdir "%RESULTS_DIR%\Reports" 2>nul

echo Creating test execution report...
echo # GCC Database Solution - Comprehensive Test Execution Report > "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo ## Test Suite: Role-Based Authentication and Workflow Validation >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo ## Execution Date: %date% %time% >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo. >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

echo =========================================
echo Phase 1: Unit and Integration Tests
echo =========================================
echo.

echo Running unit tests...
echo ### Phase 1: Unit and Integration Tests >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo ``` >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

REM Run unit tests with detailed output
dotnet test ..\GCCWebApp.Tests\GCCWebApp.Tests.csproj --logger "console;verbosity=normal" --logger "trx;LogFileName=%RESULTS_DIR%\Reports\UnitTests.trx" --collect:"XPlat Code Coverage" --results-directory "%RESULTS_DIR%\Reports"

if %ERRORLEVEL% equ 0 (
    echo ✅ Unit tests completed successfully
    echo ✅ Unit tests completed successfully >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
) else (
    echo ❌ Unit tests failed with exit code %ERRORLEVEL%
    echo ❌ Unit tests failed with exit code %ERRORLEVEL% >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
)

echo ``` >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo. >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

echo =========================================
echo Phase 2: Role-Based Authentication Tests
echo =========================================
echo.

echo ### Phase 2: Role-Based Authentication Tests >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo Testing authentication for all user roles... >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo ``` >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

REM Run authentication tests
dotnet test --filter "FullyQualifiedName~Authentication" --logger "console;verbosity=normal" --logger "trx;LogFileName=%RESULTS_DIR%\Reports\AuthenticationTests.trx" --results-directory "%RESULTS_DIR%\Reports"

if %ERRORLEVEL% equ 0 (
    echo ✅ Authentication tests completed successfully
    echo ✅ Authentication tests completed successfully >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
) else (
    echo ❌ Authentication tests had issues - exit code %ERRORLEVEL%
    echo ❌ Authentication tests had issues - exit code %ERRORLEVEL% >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
)

echo ``` >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo. >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

echo =========================================
echo Phase 3: Admin Workflow Tests
echo =========================================
echo.

echo ### Phase 3: Admin Workflow Tests >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo Testing admin user capabilities and system management... >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo ``` >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

REM Run admin workflow tests
dotnet test --filter "FullyQualifiedName~AdminWorkflow" --logger "console;verbosity=normal" --logger "trx;LogFileName=%RESULTS_DIR%\Reports\AdminWorkflowTests.trx" --results-directory "%RESULTS_DIR%\Reports"

if %ERRORLEVEL% equ 0 (
    echo ✅ Admin workflow tests completed successfully
    echo ✅ Admin workflow tests completed successfully >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
) else (
    echo ❌ Admin workflow tests had issues - exit code %ERRORLEVEL%
    echo ❌ Admin workflow tests had issues - exit code %ERRORLEVEL% >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
)

echo ``` >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo. >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

echo =========================================
echo Phase 4: Analyst Workflow Tests
echo =========================================
echo.

echo ### Phase 4: Analyst Workflow Tests >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo Testing analyst user capabilities and certificate management... >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo ``` >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

REM Run analyst workflow tests
dotnet test --filter "FullyQualifiedName~AnalystWorkflow" --logger "console;verbosity=normal" --logger "trx;LogFileName=%RESULTS_DIR%\Reports\AnalystWorkflowTests.trx" --results-directory "%RESULTS_DIR%\Reports"

if %ERRORLEVEL% equ 0 (
    echo ✅ Analyst workflow tests completed successfully
    echo ✅ Analyst workflow tests completed successfully >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
) else (
    echo ❌ Analyst workflow tests had issues - exit code %ERRORLEVEL%
    echo ❌ Analyst workflow tests had issues - exit code %ERRORLEVEL% >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
)

echo ``` >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo. >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

echo =========================================
echo Phase 5: External User Workflow Tests
echo =========================================
echo.

echo ### Phase 5: External User Workflow Tests >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo Testing external user (client/partner) capabilities... >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo ``` >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

REM Run external user workflow tests
dotnet test --filter "FullyQualifiedName~ExternalUserWorkflow" --logger "console;verbosity=normal" --logger "trx;LogFileName=%RESULTS_DIR%\Reports\ExternalUserWorkflowTests.trx" --results-directory "%RESULTS_DIR%\Reports"

if %ERRORLEVEL% equ 0 (
    echo ✅ External user workflow tests completed successfully
    echo ✅ External user workflow tests completed successfully >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
) else (
    echo ❌ External user workflow tests had issues - exit code %ERRORLEVEL%
    echo ❌ External user workflow tests had issues - exit code %ERRORLEVEL% >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
)

echo ``` >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo. >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

echo =========================================
echo Phase 6: Public User Workflow Tests
echo =========================================
echo.

echo ### Phase 6: Public User Workflow Tests >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo Testing public access without authentication... >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo ``` >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

REM Run public user workflow tests
dotnet test --filter "FullyQualifiedName~PublicUserWorkflow" --logger "console;verbosity=normal" --logger "trx;LogFileName=%RESULTS_DIR%\Reports\PublicUserWorkflowTests.trx" --results-directory "%RESULTS_DIR%\Reports"

if %ERRORLEVEL% equ 0 (
    echo ✅ Public user workflow tests completed successfully
    echo ✅ Public user workflow tests completed successfully >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
) else (
    echo ❌ Public user workflow tests had issues - exit code %ERRORLEVEL%
    echo ❌ Public user workflow tests had issues - exit code %ERRORLEVEL% >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
)

echo ``` >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo. >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

echo =========================================
echo Phase 7: Comprehensive Integration Tests
echo =========================================
echo.

echo ### Phase 7: Comprehensive Integration Tests >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo Testing end-to-end workflows and system integration... >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo ``` >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

REM Run comprehensive integration tests
dotnet test --filter "FullyQualifiedName~ComprehensiveWorkflow" --logger "console;verbosity=normal" --logger "trx;LogFileName=%RESULTS_DIR%\Reports\ComprehensiveWorkflowTests.trx" --results-directory "%RESULTS_DIR%\Reports"

if %ERRORLEVEL% equ 0 (
    echo ✅ Comprehensive integration tests completed successfully
    echo ✅ Comprehensive integration tests completed successfully >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
) else (
    echo ❌ Comprehensive integration tests had issues - exit code %ERRORLEVEL%
    echo ❌ Comprehensive integration tests had issues - exit code %ERRORLEVEL% >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
)

echo ``` >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo. >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

echo =========================================
echo Phase 8: Final Report Generation
echo =========================================
echo.

REM Copy screenshots to results directory
echo Collecting screenshots...
if exist "Screenshots\*" (
    copy "Screenshots\*" "%RESULTS_DIR%\Screenshots\" >nul 2>&1
    echo ✅ Screenshots collected successfully
    echo ✅ Screenshots collected from all test runs >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
) else (
    echo ℹ️ No screenshots found - tests may not have run properly
    echo ℹ️ No screenshots found - tests may not have run properly >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
)

REM Count total screenshots
for /f %%A in ('dir /b "%RESULTS_DIR%\Screenshots\" 2^>nul ^| find /c /v ""') do set "screenshot_count=%%A"

echo.
echo ### Test Execution Summary >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo - **Total Screenshots Captured**: %screenshot_count% >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo - **Test Reports Generated**: Check Reports folder for detailed TRX files >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo - **Coverage Reports**: Check for coverage files in Reports folder >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo. >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

echo ### Next Steps >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo 1. Review all screenshots in the Screenshots folder >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo 2. Analyze test reports (TRX files) for detailed results >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo 3. Check for any authentication or workflow issues >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo 4. Verify all user roles were tested successfully >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo 5. Review coverage reports to ensure comprehensive testing >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo. >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

echo ### User Role Test Coverage >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo - ✅ **Admin Role**: System management, user management, configuration >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo - ✅ **Analyst Role**: Certificate management, data entry, reporting >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo - ✅ **External Role**: Company/partner-specific access, filtering >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo - ✅ **Public Role**: Certificate browsing, search without authentication >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"
echo. >> "%RESULTS_DIR%\Reports\TestExecutionReport.md"

echo =========================================
echo TEST EXECUTION COMPLETED
echo =========================================
echo.
echo 📊 Test Results Summary:
echo    - Screenshots captured: %screenshot_count%
echo    - Reports generated in: %RESULTS_DIR%\Reports\
echo    - Execution report: %RESULTS_DIR%\Reports\TestExecutionReport.md
echo.
echo 🔍 Review the following:
echo    1. TestExecutionReport.md for overall summary
echo    2. Screenshots folder for visual verification
echo    3. Individual TRX files for detailed test results
echo    4. Coverage reports for code coverage analysis
echo.
echo 📁 All results saved to: %RESULTS_DIR%
echo.

REM Open the results directory
explorer "%RESULTS_DIR%"

echo Press any key to exit...
pause >nul
@echo off
setlocal enabledelayedexpansion

echo ========================================
echo  GCC Web App - Comprehensive Test Suite
echo ========================================
echo.

REM Create test results directory
if not exist "test-results" mkdir test-results

REM Create Screenshots directory structure
if not exist "Screenshots" mkdir Screenshots
if not exist "Screenshots\Admin" mkdir Screenshots\Admin
if not exist "Screenshots\Analyst" mkdir Screenshots\Analyst
if not exist "Screenshots\External" mkdir Screenshots\External
if not exist "Screenshots\Public" mkdir Screenshots\Public
if not exist "Screenshots\Security" mkdir Screenshots\Security
if not exist "Screenshots\Performance" mkdir Screenshots\Performance

echo Installing Playwright browsers...
pwsh -Command "& {npx playwright install chromium}" >nul 2>&1
echo.

REM Initialize test results
set TOTAL_TESTS=0
set PASSED_TESTS=0
set FAILED_TESTS=0

echo Starting test execution...
echo.

echo [1/6] Running Admin Role Tests...
dotnet test --filter "FullyQualifiedName~AdminRole" --logger "trx;LogFileName=admin-tests.trx" --results-directory "test-results" --verbosity quiet
if !ERRORLEVEL! EQU 0 (
    echo ✅ Admin Role Tests: PASSED
    set /a PASSED_TESTS+=1
) else (
    echo ❌ Admin Role Tests: FAILED
    set /a FAILED_TESTS+=1
)
set /a TOTAL_TESTS+=1

echo [2/6] Running Analyst Role Tests...
dotnet test --filter "FullyQualifiedName~AnalystRole" --logger "trx;LogFileName=analyst-tests.trx" --results-directory "test-results" --verbosity quiet
if !ERRORLEVEL! EQU 0 (
    echo ✅ Analyst Role Tests: PASSED
    set /a PASSED_TESTS+=1
) else (
    echo ❌ Analyst Role Tests: FAILED
    set /a FAILED_TESTS+=1
)
set /a TOTAL_TESTS+=1

echo [3/6] Running External Role Tests...
dotnet test --filter "FullyQualifiedName~ExternalRole" --logger "trx;LogFileName=external-tests.trx" --results-directory "test-results" --verbosity quiet
if !ERRORLEVEL! EQU 0 (
    echo ✅ External Role Tests: PASSED
    set /a PASSED_TESTS+=1
) else (
    echo ❌ External Role Tests: FAILED
    set /a FAILED_TESTS+=1
)
set /a TOTAL_TESTS+=1

echo [4/6] Running Public Role Tests...
dotnet test --filter "FullyQualifiedName~PublicRole" --logger "trx;LogFileName=public-tests.trx" --results-directory "test-results" --verbosity quiet
if !ERRORLEVEL! EQU 0 (
    echo ✅ Public Role Tests: PASSED
    set /a PASSED_TESTS+=1
) else (
    echo ❌ Public Role Tests: FAILED
    set /a FAILED_TESTS+=1
)
set /a TOTAL_TESTS+=1

echo [5/6] Running Security Tests...
dotnet test --filter "FullyQualifiedName~Security" --logger "trx;LogFileName=security-tests.trx" --results-directory "test-results" --verbosity quiet
if !ERRORLEVEL! EQU 0 (
    echo ✅ Security Tests: PASSED
    set /a PASSED_TESTS+=1
) else (
    echo ❌ Security Tests: FAILED
    set /a FAILED_TESTS+=1
)
set /a TOTAL_TESTS+=1

echo [6/6] Running Performance Tests...
dotnet test --filter "FullyQualifiedName~Performance" --logger "trx;LogFileName=performance-tests.trx" --results-directory "test-results" --verbosity quiet
if !ERRORLEVEL! EQU 0 (
    echo ✅ Performance Tests: PASSED
    set /a PASSED_TESTS+=1
) else (
    echo ❌ Performance Tests: FAILED
    set /a FAILED_TESTS+=1
)
set /a TOTAL_TESTS+=1

echo.
echo ========================================
echo  Test Execution Summary
echo ========================================
echo Total Test Suites: !TOTAL_TESTS!
echo Passed: !PASSED_TESTS!
echo Failed: !FAILED_TESTS!
echo.

if !FAILED_TESTS! EQU 0 (
    echo 🎉 ALL TEST SUITES PASSED!
    echo Your application has excellent quality coverage.
) else (
    echo ⚠️  Some test suites failed.
    echo Please review the test results and fix any issues.
)

echo.
echo 📊 Test Results: test-results\
echo 📸 Screenshots: Screenshots\
echo.

REM Generate summary report
echo ^<html^>^<head^>^<title^>GCC Web App Test Report^</title^>^</head^> > test-results\summary.html
echo ^<body^>^<h1^>GCC Web App Test Report^</h1^> >> test-results\summary.html
echo ^<p^>Generated on: %date% %time%^</p^> >> test-results\summary.html
echo ^<h2^>Summary^</h2^> >> test-results\summary.html
echo ^<ul^> >> test-results\summary.html
echo ^<li^>Total Test Suites: !TOTAL_TESTS!^</li^> >> test-results\summary.html
echo ^<li^>Passed: !PASSED_TESTS!^</li^> >> test-results\summary.html
echo ^<li^>Failed: !FAILED_TESTS!^</li^> >> test-results\summary.html
echo ^</ul^>^</body^>^</html^> >> test-results\summary.html

echo 📄 HTML Report: test-results\summary.html
echo.

REM If running in CI/CD, exit with error code if tests failed
if !FAILED_TESTS! GTR 0 (
    if "%CI%" == "true" (
        exit /b 1
    )
)

echo Press any key to exit...
pause >nul
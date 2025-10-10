@echo off
echo ========================================
echo GCC Web App - UAT Tests with Coverage
echo ========================================
echo.

REM Clean previous results
if exist "TestResults" rmdir /s /q TestResults
if exist "Coverage" rmdir /s /q Coverage

echo Running UAT tests with code coverage...
dotnet test ^
  --logger "console;verbosity=detailed" ^
  --logger "trx;LogFileName=UAT_Coverage_%date:~-4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%.trx" ^
  --results-directory "TestResults" ^
  --collect:"XPlat Code Coverage" ^
  --settings coverlet.runsettings ^
  /p:CollectCoverage=true ^
  /p:CoverletOutputFormat=opencover ^
  /p:CoverletOutput=Coverage/

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Generating coverage report...
    reportgenerator ^
      -reports:"TestResults/*/coverage.cobertura.xml" ^
      -targetdir:"Coverage/Report" ^
      -reporttypes:"Html;Badges;JsonSummary"
    
    echo.
    echo ========================================
    echo ✅ Tests completed with coverage!
    echo ========================================
    echo.
    echo Results:
    echo - Test Results: TestResults\
    echo - Screenshots: Screenshots\
    echo - Coverage Report: Coverage\Report\index.html
    echo.
    
    REM Open coverage report
    start Coverage\Report\index.html
) else (
    echo.
    echo ========================================
    echo ❌ Some tests failed.
    echo ========================================
    echo.
)

pause
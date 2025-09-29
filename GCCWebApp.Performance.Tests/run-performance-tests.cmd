@echo off
echo ============================================
echo GCC WebApp Performance Test Suite
echo ============================================
echo.

echo Building performance test project...
dotnet build GCCWebApp.Performance.Tests --configuration Release
if %errorlevel% neq 0 (
    echo Build failed. Exiting.
    exit /b 1
)

echo.
echo ============================================
echo Running Database Performance Tests
echo ============================================
dotnet test GCCWebApp.Performance.Tests --filter "FullyQualifiedName~DatabasePerformanceTests" --logger "console;verbosity=detailed"

echo.
echo ============================================
echo Running Certificate Benchmarks
echo ============================================
dotnet run --project GCCWebApp.Performance.Tests --configuration Release certificates

echo.
echo ============================================
echo Performance Test Suite Complete
echo ============================================
echo.
echo Check the following locations for detailed reports:
echo - BenchmarkDotNet.Artifacts/ (benchmark results)
echo - Test output above (database performance results)
echo.
echo To run load tests, ensure the main application is running first:
echo   dotnet run --project GCCWebApp
echo Then run:
echo   dotnet test GCCWebApp.Performance.Tests --filter "FullyQualifiedName~ApiLoadTests"
echo.
pause
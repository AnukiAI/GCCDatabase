@echo off
echo ========================================
echo GCC Web App - Single Test with Screenshot Debug
echo ========================================
echo.

echo Running a single test to verify screenshot location...
dotnet test --filter "TestName~PublicUser_CanBrowseAndSearchCertificates" --logger "console;verbosity=detailed"

echo.
echo Checking for screenshots...
if exist "Screenshots\" (
    echo ✅ Screenshots folder found in current directory
    dir Screenshots\ /b
) else (
    echo ❌ No Screenshots folder in current directory
    echo Current directory: %CD%
)

echo.
echo Checking bin folder for screenshots...
if exist "bin\Debug\net8.0\Screenshots\" (
    echo ✅ Screenshots found in bin folder
    dir bin\Debug\net8.0\Screenshots\ /b
) else (
    echo ❌ No screenshots in bin folder
)

pause
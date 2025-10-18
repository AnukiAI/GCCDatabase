@echo off
echo ================================
echo    GCC Web App API Tests
echo ================================
echo.

echo Building solution...
dotnet build
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    exit /b 1
)

echo.
echo Running Unit Tests...
dotnet test GCCWebApp.Api.Tests --filter "Category!=Integration&Category!=Security" --logger "console;verbosity=normal"

echo.
echo Running Integration Tests...
dotnet test GCCWebApp.Api.Tests --filter "Category=Integration" --logger "console;verbosity=normal"

echo.
echo Running Security Tests...
dotnet test GCCWebApp.Api.Tests --filter "Category=Security" --logger "console;verbosity=normal"

echo.
echo Running All Tests with Coverage...
dotnet test GCCWebApp.Api.Tests --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"

echo.
echo Tests completed!
pause
@echo off
echo Running GCC Web App Security Tests
echo =====================================

echo.
echo Running all security tests...
dotnet test --logger "console;verbosity=normal" --configuration Release

echo.
echo Running tests by category...

echo.
echo [1/9] Authentication Tests
dotnet test --filter "Category=Authentication" --logger "console;verbosity=normal"

echo.
echo [2/9] Authorization Tests  
dotnet test --filter "Category=Authorization" --logger "console;verbosity=normal"

echo.
echo [3/9] Input Validation Tests
dotnet test --filter "Category=InputValidation" --logger "console;verbosity=normal"

echo.
echo [4/9] XSS Protection Tests
dotnet test --filter "Category=XSS" --logger "console;verbosity=normal"

echo.
echo [5/9] CSRF Protection Tests
dotnet test --filter "Category=CSRF" --logger "console;verbosity=normal"

echo.
echo [6/9] API Security Tests
dotnet test --filter "Category=API" --logger "console;verbosity=normal"

echo.
echo Security test execution completed.
echo Check the test results above for any failures that need attention.
echo.
echo Security Test Categories:
echo - Authentication: JWT token security, API key validation, brute force protection
echo - Authorization: Role-based access control, privilege escalation prevention
echo - Input Validation: SQL injection prevention, data sanitization
echo - XSS Protection: Cross-site scripting prevention, content sanitization
echo - CSRF Protection: Cross-site request forgery prevention
echo - API Security: Rate limiting, error handling, path traversal prevention

pause
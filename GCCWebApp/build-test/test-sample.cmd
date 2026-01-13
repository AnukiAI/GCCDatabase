@echo off
echo Running Sample UAT Test...
echo.

REM Run a single simple test to verify setup
dotnet test --logger "console;verbosity=detailed" --filter "TestName~PublicUser_CanBrowseAndSearchCertificates"

echo.
echo Sample test completed! Check Screenshots/ folder for captured images.
pause
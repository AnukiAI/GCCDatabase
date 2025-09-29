@echo off
REM Docker HTTPS Run Script for GCC Web Application
REM Runs container with HTTPS support

echo ================================================
echo GCC Web Application - Docker HTTPS Deployment
echo ================================================
echo.

REM Set variables
SET CONTAINER_NAME=gcc-webapp-https
SET IMAGE_NAME=gcc-webapp:https
SET HTTPS_PORT=8443
SET HTTP_PORT=8080

REM Stop and remove existing container if it exists
echo Checking for existing container...
docker stop %CONTAINER_NAME% 2>nul
docker rm %CONTAINER_NAME% 2>nul

REM Build the HTTPS-enabled image
echo.
echo Building HTTPS-enabled Docker image...
docker build -f Dockerfile.https -t %IMAGE_NAME% .

IF %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to build Docker image
    exit /b 1
)

REM Create directories for volumes if they don't exist
echo.
echo Creating volume directories...
if not exist "certs" mkdir certs
if not exist "data" mkdir data
if not exist "logs" mkdir logs

REM Generate development certificate if it doesn't exist
if not exist "certs\aspnetapp.pfx" (
    echo.
    echo Generating development certificate...
    docker run --rm -v "%cd%\certs:/certs" mcr.microsoft.com/dotnet/sdk:8.0 sh -c "dotnet dev-certs https -ep /certs/aspnetapp.pfx -p SecurePassword123!"
)

REM Run the container with HTTPS
echo.
echo Starting container with HTTPS support...
docker run -d ^
  --name %CONTAINER_NAME% ^
  -p %HTTPS_PORT%:8081 ^
  -p %HTTP_PORT%:8080 ^
  -e ASPNETCORE_ENVIRONMENT=Production ^
  -e ASPNETCORE_URLS="https://+:8081;http://+:8080" ^
  -e ASPNETCORE_HTTPS_PORT=8081 ^
  -e ASPNETCORE_Kestrel__Certificates__Default__Password=SecurePassword123! ^
  -e ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx ^
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/gcc_database.db" ^
  -e Jwt__Key="ThisIsAVeryLongSecretKeyForJwtTokenGenerationAtLeast64CharactersLong123456789" ^
  -e Jwt__Issuer="https://localhost:%HTTPS_PORT%" ^
  -e Jwt__Audience="https://localhost:%HTTPS_PORT%" ^
  -v "%cd%\certs:/https:ro" ^
  -v "%cd%\data:/app/data" ^
  -v "%cd%\logs:/app/logs" ^
  %IMAGE_NAME%

IF %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to start container
    exit /b 1
)

REM Wait for container to be ready
echo.
echo Waiting for container to be ready...
timeout /t 10 /nobreak >nul

REM Check container health
echo.
echo Checking container health...
docker exec %CONTAINER_NAME% curl -f -k https://localhost:8081/health >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    echo Container is healthy!
) ELSE (
    echo WARNING: Health check failed. Container may still be starting...
)

REM Display access information
echo.
echo ================================================
echo Deployment successful!
echo ================================================
echo.
echo Access the application at:
echo   HTTPS: https://localhost:%HTTPS_PORT%
echo   HTTP:  http://localhost:%HTTP_PORT% (redirects to HTTPS)
echo.
echo NOTE: You will see a certificate warning in your browser.
echo This is normal for development certificates.
echo Click "Advanced" and "Proceed" to continue.
echo.
echo To view logs:
echo   docker logs -f %CONTAINER_NAME%
echo.
echo To stop the container:
echo   docker stop %CONTAINER_NAME%
echo.
echo To remove the container:
echo   docker rm %CONTAINER_NAME%
echo.
echo ================================================

REM Optionally open browser
choice /C YN /M "Open browser to https://localhost:%HTTPS_PORT%?"
if %ERRORLEVEL%==1 start https://localhost:%HTTPS_PORT%

pause
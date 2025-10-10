@echo off
REM ============================================================
REM GCC Web App - Docker Deployment Script for Plesk Server
REM Using GitHub Container Registry - MARIADB VERSION
REM UPDATED FOR MARIADB v5.5.68 - September 15, 2025
REM ============================================================

echo ====================================
echo GCC Web App Docker Deployment (MariaDB)
echo ====================================

REM MariaDB Configuration - UPDATED WITH SECURE VALUES
set MYSQL_HOST=localhost
set MYSQL_DATABASE=gccdb
set MYSQL_USER=gccuser
set MYSQL_PASSWORD=74Fzs0YFLMXLrKJG5fzexsTkk
set MYSQL_PORT=3306

set DOMAIN=angry-pascal.70-32-82-212.plesk.page
set JWT_SECRET=a56257adb423a4c132e969faf96ecc3aeab11dbef571d95caf818144e6618047

REM Docker configuration - Using GitHub Container Registry
set CONTAINER_NAME=gcc-webapp
set IMAGE_NAME=ghcr.io/anukiai/gcc-webapp:latest
set EXTERNAL_PORT=5102
set INTERNAL_PORT=8080

echo Pulling latest image from GitHub Container Registry...
docker pull %IMAGE_NAME%

if %errorlevel% neq 0 (
    echo ❌ Failed to pull image from registry. Check your internet connection and registry access.
    pause
    exit /b 1
)

REM Stop and remove existing container
echo Stopping existing container...
docker stop %CONTAINER_NAME% 2>nul
docker rm %CONTAINER_NAME% 2>nul

REM Run container with MariaDB production configuration
echo Starting container with MariaDB production configuration...
docker run -d ^
  --name %CONTAINER_NAME% ^
  --restart unless-stopped ^
  -p %EXTERNAL_PORT%:%INTERNAL_PORT% ^
  -e ASPNETCORE_ENVIRONMENT=Production ^
  -e ASPNETCORE_URLS="http://0.0.0.0:%INTERNAL_PORT%" ^
  -e ASPNETCORE_HTTP_PORTS="%INTERNAL_PORT%" ^
  -e Kestrel__Endpoints__Http__Url="http://0.0.0.0:%INTERNAL_PORT%" ^
  -e ConnectionStrings__DefaultConnection="Server=%MYSQL_HOST%;Port=%MYSQL_PORT%;Database=%MYSQL_DATABASE%;User=%MYSQL_USER%;Password=%MYSQL_PASSWORD%;" ^
  -e Jwt__Key="%JWT_SECRET%" ^
  -e Jwt__Issuer="https://%DOMAIN%" ^
  -e Jwt__Audience="https://%DOMAIN%" ^
  -e GCC_AZURE_AD_B2C_CLIENT_SECRET="dummy-secret" ^
  -e GCC_AZURE_AD_CLIENT_SECRET="dummy-secret" ^
  -e GCC_APPLICATION_INSIGHTS_CONNECTION_STRING="InstrumentationKey=00000000-0000-0000-0000-000000000000" ^
  -e GCC_SMTP_PASSWORD="dummy-password" ^
  %IMAGE_NAME%

REM Check if container started
timeout /t 10 /nobreak >nul
docker ps | findstr %CONTAINER_NAME% >nul
if %errorlevel% == 0 (
    echo.
    echo ✅ Container started successfully!
    echo.
    echo Application is running on port %EXTERNAL_PORT%
    echo Database: MariaDB %MYSQL_HOST%:%MYSQL_PORT%/%MYSQL_DATABASE%
    echo Image: %IMAGE_NAME%
    echo Domain: %DOMAIN%
    echo.
    echo Next steps:
    echo 1. Configure your Plesk nginx proxy to forward to http://localhost:%EXTERNAL_PORT%
    echo 2. Set up SSL certificate for %DOMAIN%
    echo 3. Test the application
    echo.
    echo Test locally: curl http://localhost:%EXTERNAL_PORT%
    echo View logs: docker logs -f %CONTAINER_NAME%
    echo Test database: mysql -h localhost -P 3306 -u %MYSQL_USER% -p %MYSQL_DATABASE%
    echo.
    echo 🎉 Ready for production with MariaDB!

    REM Test the health endpoint
    echo.
    echo Testing health endpoint...
    timeout /t 5 /nobreak >nul
    curl -s http://localhost:%EXTERNAL_PORT%/health >nul
    if %errorlevel% == 0 (
        echo ✅ Health endpoint responding
    ) else (
        echo ⚠️  Health endpoint not responding yet (may need more time to start)
        echo ⚠️  Check MariaDB connection and Entity Framework provider
    )
) else (
    echo ❌ Container failed to start. Check logs:
    docker logs %CONTAINER_NAME%
    echo.
    echo Common MariaDB issues:
    echo 1. Verify MariaDB is running: systemctl status mysql
    echo 2. Test database connection: mysql -h localhost -P 3306 -u %MYSQL_USER% -p %MYSQL_DATABASE%
    echo 3. Check if ASP.NET Core app has MySQL Entity Framework provider
    pause
    exit /b 1
)

pause
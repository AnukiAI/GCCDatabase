@echo off
REM ============================================================
REM GCC Web App - Docker Deployment Script for Plesk Server
REM Using GitHub Container Registry - PRODUCTION READY
REM ============================================================

echo ====================================
echo GCC Web App Docker Deployment (Registry)
echo ====================================

REM Configuration - UPDATE THESE VALUES FOR YOUR PLESK SERVER
set POSTGRES_HOST=localhost
set POSTGRES_DB=gccdb
set POSTGRES_USER=gccuser
set POSTGRES_PASSWORD=YourSecurePassword123!
set POSTGRES_PORT=5432

set DOMAIN=your-domain.com
set JWT_SECRET=ThisIsAVeryLongSecretKeyForJwtTokenGenerationAtLeast64CharactersLong123456789

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

REM Run container with production configuration
echo Starting container with production configuration...
docker run -d ^
  --name %CONTAINER_NAME% ^
  --restart unless-stopped ^
  -p %EXTERNAL_PORT%:%INTERNAL_PORT% ^
  -e ASPNETCORE_ENVIRONMENT=Production ^
  -e ASPNETCORE_URLS="http://0.0.0.0:%INTERNAL_PORT%" ^
  -e ASPNETCORE_HTTP_PORTS="%INTERNAL_PORT%" ^
  -e Kestrel__Endpoints__Http__Url="http://0.0.0.0:%INTERNAL_PORT%" ^
  -e ConnectionStrings__DefaultConnection="Host=%POSTGRES_HOST%;Database=%POSTGRES_DB%;Username=%POSTGRES_USER%;Password=%POSTGRES_PASSWORD%;Port=%POSTGRES_PORT%" ^
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
    echo Image: %IMAGE_NAME%
    echo.
    echo Next steps:
    echo 1. Configure your Plesk nginx proxy to forward to http://localhost:%EXTERNAL_PORT%
    echo 2. Set up SSL certificate for %DOMAIN%
    echo 3. Test the application
    echo.
    echo Test locally: curl http://localhost:%EXTERNAL_PORT%
    echo View logs: docker logs -f %CONTAINER_NAME%
    echo.
    echo 🎉 Ready for production!

    REM Test the health endpoint
    echo.
    echo Testing health endpoint...
    timeout /t 5 /nobreak >nul
    curl -s http://localhost:%EXTERNAL_PORT%/health >nul
    if %errorlevel% == 0 (
        echo ✅ Health endpoint responding
    ) else (
        echo ⚠️  Health endpoint not responding yet (may need more time to start)
    )
) else (
    echo ❌ Container failed to start. Check logs:
    docker logs %CONTAINER_NAME%
    pause
    exit /b 1
)

pause
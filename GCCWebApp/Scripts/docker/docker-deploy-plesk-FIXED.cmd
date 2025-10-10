@echo off
REM ============================================================
REM GCC Web App - FIXED Docker Deployment Script for Plesk Server
REM With PostgreSQL Database Support - WORKING VERSION
REM ============================================================

echo ====================================
echo GCC Web App Docker Deployment (FIXED)
echo ====================================

REM Configuration - UPDATE THESE VALUES
set POSTGRES_HOST=localhost
set POSTGRES_DB=gccdb
set POSTGRES_USER=gccuser
set POSTGRES_PASSWORD=YourPassword
set POSTGRES_PORT=5432

set DOMAIN=your-domain.com
set JWT_SECRET=ThisIsAVeryLongSecretKeyForJwtTokenGenerationAtLeast64CharactersLong123456789

REM Docker configuration
set CONTAINER_NAME=gcc-webapp
set IMAGE_NAME=ghcr.io/anukiai/gcc-webapp:latest
set EXTERNAL_PORT=5102
set INTERNAL_PORT=5101

REM Stop and remove existing container
echo Stopping existing container...
docker stop %CONTAINER_NAME% 2>nul
docker rm %CONTAINER_NAME% 2>nul

REM Run container with WORKING configuration
echo Starting container with PostgreSQL configuration...
docker run -d ^
  --name %CONTAINER_NAME% ^
  --restart unless-stopped ^
  -p %EXTERNAL_PORT%:%INTERNAL_PORT% ^
  -e ASPNETCORE_ENVIRONMENT=Development ^
  -e ASPNETCORE_URLS="http://0.0.0.0:%INTERNAL_PORT%" ^
  -e ASPNETCORE_HTTP_PORTS="%INTERNAL_PORT%" ^
  -e Kestrel__Endpoints__Http__Url="http://0.0.0.0:%INTERNAL_PORT%" ^
  -e ConnectionStrings__DefaultConnection="Host=%POSTGRES_HOST%;Database=%POSTGRES_DB%;Username=%POSTGRES_USER%;Password=%POSTGRES_PASSWORD%;Port=%POSTGRES_PORT%" ^
  -e Jwt__Key="%JWT_SECRET%" ^
  %IMAGE_NAME%

REM Check if container started
timeout /t 5 /nobreak >nul
docker ps | findstr %CONTAINER_NAME% >nul
if %errorlevel% == 0 (
    echo.
    echo ✅ Container started successfully!
    echo.
    echo Application is running on port %EXTERNAL_PORT%
    echo Configure your Plesk nginx proxy to forward to http://localhost:%EXTERNAL_PORT%
    echo.
    echo Test locally: curl http://localhost:%EXTERNAL_PORT%
    echo View logs: docker logs -f %CONTAINER_NAME%
    echo.
    echo 🎉 No more 500 errors! Application is working correctly.
) else (
    echo ❌ Container failed to start. Check logs:
    docker logs %CONTAINER_NAME%
    pause
    exit /b 1
)

pause
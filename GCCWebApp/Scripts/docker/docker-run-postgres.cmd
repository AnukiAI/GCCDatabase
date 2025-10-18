@echo off
REM Docker run script for GCC Web App with PostgreSQL
REM This script runs the container with all required environment variables

echo Stopping and removing existing container...
docker stop gcc-webapp 2>nul
docker rm gcc-webapp 2>nul

echo Starting GCC Web App container...
docker run -d ^
  --name gcc-webapp ^
  --restart unless-stopped ^
  -p 5102:8080 ^
  -e ASPNETCORE_ENVIRONMENT=Production ^
  -e ASPNETCORE_URLS="http://+:8080" ^
  -e ASPNETCORE_HTTP_PORTS=8080 ^
  -e ConnectionStrings__DefaultConnection="Host=YOUR_POSTGRES_HOST;Database=gccdb;Username=gccuser;Password=YOUR_PASSWORD;Port=5432" ^
  -e Jwt__Key="ThisIsAVeryLongSecretKeyForJwtTokenGenerationAtLeast64CharactersLong123456789" ^
  -e Jwt__Issuer="https://your-domain.com" ^
  -e Jwt__Audience="https://your-domain.com" ^
  -e GCC_JWT_SECRET="ThisIsAVeryLongSecretKeyForJwtTokenGenerationAtLeast64CharactersLong123456789" ^
  -e GCC_AZURE_AD_B2C_CLIENT_SECRET="not-used" ^
  -e GCC_AZURE_AD_CLIENT_SECRET="not-used" ^
  -e DataProtection__AzureBlobStorageConnectionString="not-used" ^
  -e ApplicationInsights__ConnectionString="not-used" ^
  -e EmailConfiguration__Password="smtp-password-if-needed" ^
  -e FeatureFlags__EnableAzureB2C=false ^
  -e FeatureFlags__EnableApplicationInsights=false ^
  -e FeatureFlags__EnableAzureBlobStorage=false ^
  gcc-webapp:fixed

echo.
echo Container started. Check logs with: docker logs -f gcc-webapp
echo Application should be available at http://localhost:5102
echo.
pause
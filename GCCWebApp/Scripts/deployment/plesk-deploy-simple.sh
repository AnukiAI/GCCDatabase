#!/bin/bash

# Simple deployment script for Plesk with SQLite
# This version uses a pre-built, compatible image

echo "=== Simple GCC Deployment for Plesk ==="

# Stop and remove existing container
docker stop gcc-webapp 2>/dev/null || true
docker rm gcc-webapp 2>/dev/null || true

# Use the official ASP.NET runtime as base and mount the app
docker run -d \
  --name gcc-webapp \
  --restart unless-stopped \
  -p 5101:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS="http://+:8080" \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/gcc_database.db" \
  -v /var/www/vhosts/angry-pascal.70-32-82-212.plesk.page/httpdocs/data:/app/data \
  mcr.microsoft.com/dotnet/aspnet:8.0 \
  bash -c "apt-get update && apt-get install -y curl && cd /app && curl -L https://github.com/anukiai/gcc-webapp/releases/latest/download/gcc-webapp.tar.gz | tar xz && dotnet GCCWebApp.dll"

# Wait and test
sleep 10
echo "Testing application:"
curl -I http://localhost:5101 || echo "Application may still be starting..."

echo "Deployment complete!"
echo "Access the app at: http://angry-pascal.70-32-82-212.plesk.page:5101"
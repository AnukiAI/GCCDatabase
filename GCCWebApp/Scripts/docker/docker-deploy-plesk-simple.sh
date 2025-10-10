#!/bin/bash

# Simple deployment script that works around Azure configuration issues
echo "=== Simple Plesk Deployment (No Azure Dependencies) ==="

# Build the image with a workaround
cat > appsettings.Docker.json << 'EOF'
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/app/data/gcc_database.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AzureStorageBlob": {
    "ConnectionString": null,
    "ContainerName": "gcc-data-protection"
  },
  "DataProtection": {
    "UseAzure": false
  }
}
EOF

# Build image
echo "Building Docker image..."
docker build -t ghcr.io/anukiai/gcc-webapp:no-azure -f - . << 'EOF'
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

RUN apt-get update && \
    apt-get install -y sqlite3 curl && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["GCCWebApp.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create necessary directories
RUN mkdir -p /app/data /app/logs /app/DataProtectionKeys && \
    chmod 777 /app/data /app/logs /app/DataProtectionKeys

# Override configuration to disable Azure
COPY appsettings.Docker.json /app/appsettings.Production.json

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "GCCWebApp.dll"]
EOF

# Test locally
echo "Testing locally..."
docker run -d --name gcc-test-no-azure -p 5105:8080 ghcr.io/anukiai/gcc-webapp:no-azure
sleep 10
curl -I http://localhost:5105 && echo "Success!" || echo "Failed - check logs"
docker logs gcc-test-no-azure --tail 10

# Push to registry
echo "Pushing to registry..."
docker push ghcr.io/anukiai/gcc-webapp:no-azure

echo ""
echo "=== Deployment Instructions for Plesk ==="
echo "Run these commands on the Plesk server:"
echo ""
echo "docker pull ghcr.io/anukiai/gcc-webapp:no-azure"
echo "docker stop gcc-webapp 2>/dev/null || true"
echo "docker rm gcc-webapp 2>/dev/null || true"
echo "docker run -d --name gcc-webapp --restart unless-stopped \\"
echo "  -p 5101:8080 \\"
echo "  -v /var/www/vhosts/angry-pascal.70-32-82-212.plesk.page/httpdocs/data:/app/data \\"
echo "  ghcr.io/anukiai/gcc-webapp:no-azure"
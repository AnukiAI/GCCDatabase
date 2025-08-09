# GCC Database Application - Deployment Guide

## Table of Contents
1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Environment Setup](#environment-setup)
4. [Azure Deployment](#azure-deployment)
5. [IIS Deployment](#iis-deployment)
6. [Docker Deployment](#docker-deployment)
7. [Database Deployment](#database-deployment)
8. [Configuration Management](#configuration-management)
9. [SSL/TLS Setup](#ssltls-setup)
10. [Post-Deployment Tasks](#post-deployment-tasks)
11. [Monitoring Setup](#monitoring-setup)
12. [Rollback Procedures](#rollback-procedures)
13. [Troubleshooting](#troubleshooting)

## Overview

This guide provides step-by-step instructions for deploying the GCC Database Web Application to various environments including Azure App Service, IIS, and Docker containers.

### Deployment Checklist
- [ ] Environment prerequisites verified
- [ ] Database deployed and configured
- [ ] Application settings configured
- [ ] SSL certificates installed
- [ ] Authentication configured
- [ ] Monitoring and logging enabled
- [ ] Backup procedures in place
- [ ] Rollback plan documented

## Prerequisites

### System Requirements
- **.NET 8.0 Runtime** (or SDK for self-contained deployments)
- **SQL Server 2019+** or Azure SQL Database
- **4GB RAM minimum** (8GB recommended)
- **20GB disk space** for application and logs
- **HTTPS certificate** for production

### Required Tools
```bash
# Azure CLI (for Azure deployments)
az --version

# .NET CLI
dotnet --version

# SQL Server tools
sqlcmd -?

# Docker (for container deployments)
docker --version
```

## Environment Setup

### Production Environment Variables
```bash
# Application Settings
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80;https://+:443

# Database Connection
ConnectionStrings__DefaultConnection="Server=prod-server;Database=GCCDatabase;..."

# Azure AD
AzureAd__TenantId="your-tenant-id"
AzureAd__ClientId="your-client-id"
AzureAd__ClientSecret="your-client-secret"

# Application Insights
ApplicationInsights__InstrumentationKey="your-key"
```

## Azure Deployment

### 1. Create Azure Resources

```bash
# Login to Azure
az login

# Create Resource Group
az group create --name gcc-prod-rg --location eastus

# Create App Service Plan
az appservice plan create \
  --name gcc-prod-plan \
  --resource-group gcc-prod-rg \
  --sku P1V2 \
  --is-linux

# Create Web App
az webapp create \
  --name gcc-database-app \
  --resource-group gcc-prod-rg \
  --plan gcc-prod-plan \
  --runtime "DOTNET|8.0"

# Create SQL Database
az sql server create \
  --name gcc-sql-server \
  --resource-group gcc-prod-rg \
  --location eastus \
  --admin-user gccadmin \
  --admin-password "Strong_Password_123!"

az sql db create \
  --name GCCDatabase \
  --server gcc-sql-server \
  --resource-group gcc-prod-rg \
  --service-objective S2
```

### 2. Configure Application Settings

```bash
# Set connection string
az webapp config connection-string set \
  --name gcc-database-app \
  --resource-group gcc-prod-rg \
  --connection-string-type SQLServer \
  --settings DefaultConnection="Server=tcp:gcc-sql-server.database.windows.net,1433;..."

# Set app settings
az webapp config appsettings set \
  --name gcc-database-app \
  --resource-group gcc-prod-rg \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    AzureAd__TenantId="your-tenant-id" \
    AzureAd__ClientId="your-client-id"
```

### 3. Deploy Application

```bash
# Build application
dotnet publish -c Release -o ./publish

# Zip deployment package
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy to Azure
az webapp deployment source config-zip \
  --name gcc-database-app \
  --resource-group gcc-prod-rg \
  --src deploy.zip
```

### 4. Configure Custom Domain and SSL

```bash
# Add custom domain
az webapp config hostname add \
  --webapp-name gcc-database-app \
  --resource-group gcc-prod-rg \
  --hostname www.gccdatabase.com

# Upload SSL certificate
az webapp config ssl upload \
  --name gcc-database-app \
  --resource-group gcc-prod-rg \
  --certificate-file ./ssl/gccdatabase.pfx \
  --certificate-password "cert-password"

# Bind SSL certificate
az webapp config ssl bind \
  --name gcc-database-app \
  --resource-group gcc-prod-rg \
  --certificate-thumbprint [thumbprint] \
  --ssl-type SNI
```

## IIS Deployment

### 1. Install IIS and Required Modules

```powershell
# Install IIS
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-WebServer, IIS-CommonHttpFeatures, IIS-HttpErrors, IIS-HttpRedirect, IIS-ApplicationDevelopment, IIS-HealthAndDiagnostics, IIS-HttpLogging, IIS-Security, IIS-RequestFiltering, IIS-Performance, IIS-WebServerManagementTools, IIS-ManagementConsole

# Install .NET Hosting Bundle
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0
```

### 2. Create IIS Site

```powershell
# Create application pool
New-WebAppPool -Name "GCCAppPool"
Set-ItemProperty -Path "IIS:\AppPools\GCCAppPool" -Name processIdentity.identityType -Value ApplicationPoolIdentity
Set-ItemProperty -Path "IIS:\AppPools\GCCAppPool" -Name managedRuntimeVersion -Value ""

# Create website
New-Website -Name "GCCDatabase" `
  -Port 443 `
  -PhysicalPath "C:\inetpub\wwwroot\GCCDatabase" `
  -ApplicationPool "GCCAppPool" `
  -Ssl

# Set permissions
icacls "C:\inetpub\wwwroot\GCCDatabase" /grant "IIS_IUSRS:(OI)(CI)RX"
```

### 3. Deploy Application

```powershell
# Publish application
dotnet publish -c Release -o C:\inetpub\wwwroot\GCCDatabase

# Copy configuration
Copy-Item appsettings.Production.json C:\inetpub\wwwroot\GCCDatabase\

# Set environment variable
[System.Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")

# Restart IIS
iisreset
```

### 4. Configure SSL Certificate

```powershell
# Import certificate
$cert = Import-PfxCertificate -FilePath ".\gccdatabase.pfx" -CertStoreLocation "Cert:\LocalMachine\My" -Password (ConvertTo-SecureString -String "password" -AsPlainText -Force)

# Bind certificate to site
New-WebBinding -Name "GCCDatabase" -Protocol https -Port 443
$binding = Get-WebBinding -Name "GCCDatabase" -Protocol https
$binding.AddSslCertificate($cert.Thumbprint, "My")
```

## Docker Deployment

### 1. Create Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["GCCWebApp.csproj", "."]
RUN dotnet restore "GCCWebApp.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "GCCWebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GCCWebApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GCCWebApp.dll"]
```

### 2. Build and Run Container

```bash
# Build Docker image
docker build -t gcc-database:latest .

# Run container
docker run -d \
  --name gcc-app \
  -p 80:80 \
  -p 443:443 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;..." \
  -v /path/to/certs:/https \
  gcc-database:latest
```

### 3. Docker Compose Deployment

```yaml
# docker-compose.yml
version: '3.8'

services:
  web:
    image: gcc-database:latest
    ports:
      - "80:80"
      - "443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
    volumes:
      - ./certs:/https:ro
      - ./logs:/app/logs
    depends_on:
      - db
    
  db:
    image: mcr.microsoft.com/mssql/server:2019-latest
    ports:
      - "1433:1433"
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=Strong_Password_123!
    volumes:
      - sqldata:/var/opt/mssql

volumes:
  sqldata:
```

## Database Deployment

### 1. Create Database

```sql
-- Create database
CREATE DATABASE GCCDatabase
COLLATE SQL_Latin1_General_CP1_CI_AS;
GO

USE GCCDatabase;
GO

-- Create schemas
CREATE SCHEMA audit;
CREATE SCHEMA security;
CREATE SCHEMA reporting;
GO
```

### 2. Run Migration Scripts

```bash
# Using EF Core migrations
dotnet ef database update --connection "Server=prod-server;Database=GCCDatabase;..."

# Or using SQL scripts
sqlcmd -S prod-server -d GCCDatabase -U sa -P password -i ./scripts/001_Initial_Schema.sql
sqlcmd -S prod-server -d GCCDatabase -U sa -P password -i ./scripts/002_Seed_Data.sql
```

### 3. Configure Database Security

```sql
-- Create application login
CREATE LOGIN gcc_app WITH PASSWORD = 'App_Password_123!';
CREATE USER gcc_app FOR LOGIN gcc_app;

-- Grant permissions
ALTER ROLE db_datareader ADD MEMBER gcc_app;
ALTER ROLE db_datawriter ADD MEMBER gcc_app;
GRANT EXECUTE TO gcc_app;

-- Enable TDE for encryption
CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'Master_Key_Password_123!';
CREATE CERTIFICATE TDECert WITH SUBJECT = 'TDE Certificate';
CREATE DATABASE ENCRYPTION KEY
WITH ALGORITHM = AES_256
ENCRYPTION BY SERVER CERTIFICATE TDECert;
ALTER DATABASE GCCDatabase SET ENCRYPTION ON;
```

## Configuration Management

### Production Configuration

```json
// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-sql.gccdatabase.com;Database=GCCDatabase;User Id=gcc_app;Password=***;Encrypt=True;TrustServerCertificate=False;"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "gccdatabase.onmicrosoft.com",
    "TenantId": "production-tenant-id",
    "ClientId": "production-client-id"
  },
  "ApplicationInsights": {
    "InstrumentationKey": "production-key"
  },
  "CacheSettings": {
    "Provider": "Redis",
    "ConnectionString": "redis-cache.gccdatabase.com:6380,password=***,ssl=True"
  }
}
```

### Environment-Specific Settings

```bash
# Set environment variables (Linux/Mac)
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Server=..."

# Set environment variables (Windows)
setx ASPNETCORE_ENVIRONMENT Production
setx ConnectionStrings__DefaultConnection "Server=..."
```

## SSL/TLS Setup

### 1. Generate Certificate Request

```bash
# Generate private key
openssl genrsa -out gccdatabase.key 2048

# Generate CSR
openssl req -new -key gccdatabase.key -out gccdatabase.csr \
  -subj "/C=US/ST=State/L=City/O=GreenCircle/CN=www.gccdatabase.com"
```

### 2. Configure HTTPS in Application

```csharp
// Program.cs
builder.Services.Configure<HttpsRedirectionOptions>(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    options.HttpsPort = 443;
});

// Force HTTPS
app.UseHttpsRedirection();
app.UseHsts();
```

### 3. Configure SSL in Production

```json
// appsettings.Production.json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:443",
        "Certificate": {
          "Path": "/certs/gccdatabase.pfx",
          "Password": "${CERT_PASSWORD}"
        }
      }
    }
  }
}
```

## Post-Deployment Tasks

### 1. Verify Deployment

```bash
# Health check
curl https://www.gccdatabase.com/health

# Check application version
curl https://www.gccdatabase.com/api/version

# Test database connectivity
dotnet test TestConnection.dll
```

### 2. Configure Monitoring

```bash
# Azure Application Insights
az monitor app-insights component create \
  --app gcc-insights \
  --location eastus \
  --resource-group gcc-prod-rg

# Configure alerts
az monitor metrics alert create \
  --name high-cpu-alert \
  --resource-group gcc-prod-rg \
  --scopes /subscriptions/.../gcc-database-app \
  --condition "avg Percentage CPU > 80" \
  --window-size 5m
```

### 3. Setup Backups

```bash
# Database backup
az sql db backup create \
  --resource-group gcc-prod-rg \
  --server gcc-sql-server \
  --database GCCDatabase

# Application backup
az webapp backup create \
  --resource-group gcc-prod-rg \
  --webapp-name gcc-database-app \
  --backup-name initial-backup
```

### 4. Performance Testing

```bash
# Load testing with Apache Bench
ab -n 1000 -c 10 https://www.gccdatabase.com/

# Stress testing
dotnet test LoadTests.dll --configuration Release
```

## Monitoring Setup

### Application Performance Monitoring

```csharp
// Configure Application Insights
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = Configuration["ApplicationInsights:ConnectionString"];
});

// Custom telemetry
builder.Services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
```

### Health Checks

```csharp
// Configure health checks
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, name: "database")
    .AddUrlGroup(new Uri("https://api.external.com"), name: "external-api")
    .AddCheck<CustomHealthCheck>("custom");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### Logging Configuration

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.File", "Serilog.Sinks.ApplicationInsights"],
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "/logs/gcc-.log",
          "rollingInterval": "Day"
        }
      },
      {
        "Name": "ApplicationInsights",
        "Args": {
          "telemetryConfiguration": "InstrumentationKey=..."
        }
      }
    ]
  }
}
```

## Rollback Procedures

### Application Rollback

```bash
# Azure App Service
az webapp deployment list \
  --name gcc-database-app \
  --resource-group gcc-prod-rg

# Rollback to previous deployment
az webapp deployment rollback \
  --name gcc-database-app \
  --resource-group gcc-prod-rg \
  --slot production

# IIS Rollback
# 1. Stop application pool
Stop-WebAppPool -Name "GCCAppPool"

# 2. Restore previous version
Copy-Item -Path "C:\Backups\GCCDatabase_v1.0" -Destination "C:\inetpub\wwwroot\GCCDatabase" -Recurse -Force

# 3. Start application pool
Start-WebAppPool -Name "GCCAppPool"
```

### Database Rollback

```sql
-- Restore from backup
RESTORE DATABASE GCCDatabase
FROM DISK = 'C:\Backups\GCCDatabase_before_deployment.bak'
WITH REPLACE, RECOVERY;

-- Or rollback migration
dotnet ef database update PreviousMigration
```

## Troubleshooting

### Common Issues

#### Application Won't Start
```bash
# Check logs
tail -f /logs/gcc-*.log

# Check event viewer (Windows)
Get-EventLog -LogName Application -Source ".NET Runtime" -Newest 10

# Verify .NET runtime
dotnet --list-runtimes
```

#### Database Connection Issues
```sql
-- Test connection
sqlcmd -S server -U user -P password -Q "SELECT 1"

-- Check firewall rules
az sql server firewall-rule list \
  --server gcc-sql-server \
  --resource-group gcc-prod-rg
```

#### SSL Certificate Issues
```bash
# Verify certificate
openssl s_client -connect www.gccdatabase.com:443 -servername www.gccdatabase.com

# Check certificate expiry
echo | openssl s_client -connect www.gccdatabase.com:443 2>/dev/null | openssl x509 -noout -dates
```

#### Performance Issues
```bash
# Check resource usage
az webapp metrics list \
  --name gcc-database-app \
  --resource-group gcc-prod-rg \
  --metric "CpuPercentage,MemoryPercentage"

# Analyze slow queries
SELECT TOP 10
    qs.total_elapsed_time / qs.execution_count AS avg_elapsed_time,
    SUBSTRING(qt.text, (qs.statement_start_offset/2)+1,
    ((CASE qs.statement_end_offset
        WHEN -1 THEN DATALENGTH(qt.text)
        ELSE qs.statement_end_offset
    END - qs.statement_start_offset)/2) + 1) AS query_text
FROM sys.dm_exec_query_stats AS qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) AS qt
ORDER BY avg_elapsed_time DESC;
```

## Security Checklist

- [ ] HTTPS enabled and HTTP redirected
- [ ] Security headers configured (HSTS, CSP, etc.)
- [ ] Authentication and authorization working
- [ ] SQL injection prevention verified
- [ ] XSS protection enabled
- [ ] CSRF tokens implemented
- [ ] Rate limiting configured
- [ ] Audit logging enabled
- [ ] Sensitive data encrypted
- [ ] Backup encryption enabled
- [ ] Network security groups configured
- [ ] WAF rules configured (if applicable)

## Support Contacts

- **DevOps Team**: devops@gccdatabase.com
- **On-Call**: +1-555-0100
- **Escalation**: escalation@gccdatabase.com
- **Documentation**: /Documentation/Deployment_Guide.md

## Version History

- **v1.0.0** - Initial deployment guide
- Last Updated: August 2024
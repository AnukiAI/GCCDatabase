# Server Configuration Request for GCC Web Application

## Request Summary
Need to deploy ASP.NET Core application to Plesk server with .NET 9.0 runtime support.

## Current Issue
- SSH terminal doesn't allow .NET 9.0 installation
- Standard package managers restricted
- Application ready for deployment but blocked by server constraints

## Requested Actions

### Option A: Install .NET Toolkit Extension (Preferred)
1. Install "**.NET Toolkit v2.3.0**" extension in Plesk
2. Enable **ASP.NET Core 9.0 Runtime** through the extension
3. Configure application pool for our domain
4. Provide SSH access with sufficient permissions for app deployment

### Option B: Manual Runtime Installation
```bash
# These commands need elevated privileges:
wget https://packages.microsoft.com/config/centos/8/packages-microsoft-prod.rpm
rpm -Uvh packages-microsoft-prod.rpm
yum install aspnetcore-runtime-9.0
```

### Option C: Docker Support
- Enable Docker on the server
- Configure port mapping for containerized deployment
- Set up reverse proxy for domain routing

## Application Details
- **Framework**: ASP.NET Core 9.0
- **Database**: SQL Server/PostgreSQL
- **Port**: 5101 (configurable)
- **Size**: ~50MB compiled
- **Dependencies**: Standard ASP.NET Core runtime only

## Timeline Impact
- **With server access**: Deploy today
- **Without changes**: Need to downgrade to .NET 6 (adds development time)

## Alternative Solutions
If server changes aren't possible:
1. Downgrade app to .NET 6 LTS
2. Move to cloud hosting (Azure/AWS)
3. Use Docker container deployment

Please advise on preferred approach and timeline for server configuration changes.
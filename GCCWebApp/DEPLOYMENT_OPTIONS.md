# GCC Web App Deployment Options

## Current Blocker
- Plesk server constraints preventing .NET 9.0 installation via SSH
- Sys admin assistance needed for server configuration

## Option 1: Downgrade to .NET 6 (RECOMMENDED)
**Timeline: 30 minutes**

### Why This Works:
- .NET 6 LTS is widely supported on Plesk
- Minimal code changes required
- Your app doesn't use .NET 8/9 specific features

### Steps:
```bash
# Update target framework
<TargetFramework>net6.0</TargetFramework>

# Downgrade packages to .NET 6 compatible versions
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 6.0.33
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 6.0.33
```

## Option 2: Use Plesk .NET Toolkit Extension
**Timeline: Depends on sys admin**

### Requirements:
- Sys admin must install .NET Toolkit extension
- Enable ASP.NET Core 9.0 runtime through Plesk panel
- Configure application pool settings

## Option 3: Docker Deployment
**Timeline: 2-3 hours**

### Benefits:
- Complete control over runtime environment
- Isolated from server constraints
- Can use any .NET version

### Requirements:
- Docker support on Plesk server
- Port mapping configuration
- Reverse proxy setup

## Option 4: Alternative Hosting
**Timeline: 1-2 days**

### Options:
- Azure App Service (.NET 9 native support)
- AWS Elastic Beanstalk
- DigitalOcean App Platform
- Self-hosted VPS with full control

## Recommendation
**Start with Option 1** - downgrade to .NET 6 while exploring other options. This unblocks immediate deployment and testing.
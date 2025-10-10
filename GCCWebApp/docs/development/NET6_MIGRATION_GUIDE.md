# .NET 6.0 Migration Guide - Package Compatibility

## Issue Summary
Your project is targeting .NET 6.0 but using packages that require .NET 8.0 or 9.0. This guide shows the exact package versions you need for .NET 6.0 compatibility.

## Required Package Downgrades

### 1. Entity Framework Core Packages
**Current (Incompatible with .NET 6.0):**
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.8" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.8" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.8" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.8" />
<PackageReference Include="MySql.EntityFrameworkCore" Version="9.0.0" />
```

**Required for .NET 6.0:**
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="6.0.35" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="6.0.35" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="6.0.35" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="6.0.35" />
<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="6.0.3" />
```

### 2. Authentication & Identity Packages
**Current (Incompatible):**
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.11" />
<PackageReference Include="Microsoft.AspNetCore.DataProtection" Version="8.0.11" />
<PackageReference Include="Microsoft.Identity.Web" Version="3.10.0" />
<PackageReference Include="Microsoft.Identity.Web.MicrosoftGraph" Version="3.10.0" />
<PackageReference Include="Microsoft.Identity.Web.UI" Version="3.10.0" />
```

**Required for .NET 6.0:**
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="6.0.35" />
<PackageReference Include="Microsoft.AspNetCore.DataProtection" Version="6.0.35" />
<PackageReference Include="Microsoft.Identity.Web" Version="1.25.10" />
<PackageReference Include="Microsoft.Identity.Web.MicrosoftGraph" Version="1.25.10" />
<PackageReference Include="Microsoft.Identity.Web.UI" Version="1.25.10" />
```

### 3. API Versioning & Swagger
**Current (Incompatible):**
```xml
<PackageReference Include="Asp.Versioning.Mvc.ApiExplorer" Version="8.1.0" />
<PackageReference Include="Swashbuckle.AspNetCore.Annotations" Version="9.0.4" />
```

**Required for .NET 6.0:**
```xml
<PackageReference Include="Asp.Versioning.Mvc.ApiExplorer" Version="6.4.0" />
<PackageReference Include="Swashbuckle.AspNetCore.Annotations" Version="6.9.0" />
```

### 4. Serilog Packages
**Current (May have issues):**
```xml
<PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
<PackageReference Include="Serilog.Settings.Configuration" Version="9.0.0" />
```

**Required for .NET 6.0:**
```xml
<PackageReference Include="Serilog.AspNetCore" Version="6.1.0" />
<PackageReference Include="Serilog.Settings.Configuration" Version="3.4.0" />
```

## Complete Updated .csproj Package References

Replace your entire `<ItemGroup>` containing package references with this:

```xml
<ItemGroup>
  <PackageReference Include="Asp.Versioning.Mvc.ApiExplorer" Version="6.4.0" />
  <PackageReference Include="AspNetCoreRateLimit" Version="5.0.0" />
  <PackageReference Include="Azure.Identity" Version="1.14.1" />
  <PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.8.0" />
  <PackageReference Include="Azure.Extensions.AspNetCore.DataProtection.Blobs" Version="1.3.4" />
  <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
  <PackageReference Include="ClosedXML" Version="0.104.1" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="6.0.35" />
  <PackageReference Include="Microsoft.AspNetCore.DataProtection" Version="6.0.35" />
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="6.0.35" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="6.0.35">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
  <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="6.0.35" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="6.0.35" />
  <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="6.0.3" />
  <PackageReference Include="Microsoft.Identity.Web" Version="1.25.10" />
  <PackageReference Include="Microsoft.Identity.Web.MicrosoftGraph" Version="1.25.10" />
  <PackageReference Include="Microsoft.Identity.Web.UI" Version="1.25.10" />
  <PackageReference Include="QRCoder" Version="1.6.0" />
  <PackageReference Include="QuestPDF" Version="2025.7.0" />
  <PackageReference Include="Serilog.AspNetCore" Version="6.1.0" />
  <PackageReference Include="Serilog.Enrichers.Environment" Version="3.0.1" />
  <PackageReference Include="Serilog.Enrichers.Thread" Version="4.0.0" />
  <PackageReference Include="Serilog.Settings.Configuration" Version="3.4.0" />
  <PackageReference Include="Serilog.Sinks.ApplicationInsights" Version="4.0.0" />
  <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
  <PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.9.0" />
  <PackageReference Include="Swashbuckle.AspNetCore.Annotations" Version="6.9.0" />
</ItemGroup>
```

## Migration Steps

### 1. Update .csproj File
Edit your `GCCWebApp.csproj` and replace the package references as shown above.

### 2. Clean Solution
```bash
# Remove all build artifacts
dotnet clean
del /s /q obj bin
```

### 3. Restore Packages
```bash
dotnet restore
```

### 4. Code Changes Required

#### A. MySQL Provider Change
The MySql.EntityFrameworkCore package doesn't support .NET 6.0. Use Pomelo instead:

**Old:**
```csharp
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(connectionString));
```

**New:**
```csharp
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString,
        ServerVersion.AutoDetect(connectionString)));
```

#### B. API Versioning Changes
The API versioning namespace changed. Update your using statements:

**Old:**
```csharp
using Asp.Versioning;
```

**New:**
```csharp
using Microsoft.AspNetCore.Mvc.Versioning;
```

#### C. Identity Web Changes
Some features in Microsoft.Identity.Web 3.x aren't available in 1.25.x. Check for:
- Token validation options
- Graph API calls
- Claims mapping

### 5. Build and Test
```bash
dotnet build
dotnet run
```

## Potential Issues and Solutions

### Issue: "The type or namespace name 'xxx' could not be found"
**Solution:** This usually means a feature from a newer version isn't available. Check the package documentation for the .NET 6.0 compatible version.

### Issue: Database migration errors
**Solution:** Regenerate migrations with EF Core 6.0:
```bash
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Issue: C# language feature not available
**Solution:** .NET 6.0 uses C# 10, not C# 12. Some newer language features won't work:
- Primary constructors (C# 12)
- Collection expressions (C# 12)
- Raw string literals with interpolation (C# 11)

## Alternative: Suppress Warnings (Not Recommended)

If you want to try running with the current packages despite compatibility warnings:

```xml
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
  <SuppressTfmSupportBuildWarnings>true</SuppressTfmSupportBuildWarnings>
</PropertyGroup>
```

⚠️ **WARNING:** This is risky and may cause runtime errors!

## Recommendation

**Best Practice:** Use the compatible package versions listed above for .NET 6.0.

**Alternative:** If possible, upgrade your Plesk server to support .NET 8.0 for better performance and features.

## Quick Fix Script

Save this as `fix-packages.ps1` and run it:

```powershell
# Clean the project
dotnet clean
Remove-Item -Recurse -Force obj, bin -ErrorAction SilentlyContinue

# Update packages to .NET 6.0 compatible versions
dotnet remove package Microsoft.EntityFrameworkCore
dotnet remove package Microsoft.EntityFrameworkCore.Design
dotnet remove package Microsoft.EntityFrameworkCore.Sqlite
dotnet remove package Microsoft.EntityFrameworkCore.SqlServer
dotnet remove package MySql.EntityFrameworkCore
dotnet remove package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet remove package Microsoft.AspNetCore.DataProtection
dotnet remove package Microsoft.Identity.Web
dotnet remove package Microsoft.Identity.Web.MicrosoftGraph
dotnet remove package Microsoft.Identity.Web.UI
dotnet remove package Asp.Versioning.Mvc.ApiExplorer
dotnet remove package Swashbuckle.AspNetCore.Annotations
dotnet remove package Serilog.AspNetCore
dotnet remove package Serilog.Settings.Configuration

# Add .NET 6.0 compatible versions
dotnet add package Microsoft.EntityFrameworkCore --version 6.0.35
dotnet add package Microsoft.EntityFrameworkCore.Design --version 6.0.35
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 6.0.35
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 6.0.35
dotnet add package Pomelo.EntityFrameworkCore.MySql --version 6.0.3
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 6.0.35
dotnet add package Microsoft.AspNetCore.DataProtection --version 6.0.35
dotnet add package Microsoft.Identity.Web --version 1.25.10
dotnet add package Microsoft.Identity.Web.MicrosoftGraph --version 1.25.10
dotnet add package Microsoft.Identity.Web.UI --version 1.25.10
dotnet add package Asp.Versioning.Mvc.ApiExplorer --version 6.4.0
dotnet add package Swashbuckle.AspNetCore.Annotations --version 6.9.0
dotnet add package Serilog.AspNetCore --version 6.1.0
dotnet add package Serilog.Settings.Configuration --version 3.4.0

# Restore and build
dotnet restore
dotnet build
```

## Support

If you encounter issues after downgrading:
1. Check the official package documentation for .NET 6.0 compatibility
2. Review breaking changes between major versions
3. Consider using compatibility shims or polyfills for missing features
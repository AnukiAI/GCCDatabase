# .NET 6.0 Deployment Notes for Plesk Server

## Important: .NET 6.0 vs .NET 8.0

Your application appears to be built with **.NET 8.0** based on the codebase, but your dev environment supports **.NET 6.0**. Here's what you need to know:

## Option 1: Deploy with .NET 6.0 (Recommended if Dev Environment)

### Quick Install:
```bash
# Use the simplified .NET 6.0 script
chmod +x install-dotnet-6.sh
sudo ./install-dotnet-6.sh
```

### Compatibility Check:
Before deploying, check your `GCCWebApp.csproj` file:

```xml
<!-- For .NET 6.0 -->
<TargetFramework>net6.0</TargetFramework>

<!-- For .NET 8.0 -->
<TargetFramework>net8.0</TargetFramework>
```

### If your app is .NET 8.0, downgrade to 6.0:
```bash
# Edit GCCWebApp.csproj
nano GCCWebApp.csproj

# Change TargetFramework from net8.0 to net6.0
# Then rebuild:
dotnet restore
dotnet build
dotnet publish -c Release
```

## Option 2: Install Both .NET 6.0 and 8.0

Use the main script with version selection:
```bash
# Edit install-dotnet-plesk.sh
# Change line: DOTNET_VERSION="6.0" to DOTNET_VERSION="8.0"

# Run for .NET 6.0
DOTNET_VERSION="6.0" sudo ./install-dotnet-plesk.sh

# Then run again for .NET 8.0
DOTNET_VERSION="8.0" sudo ./install-dotnet-plesk.sh
```

## Key Differences Between .NET 6.0 and 8.0

### .NET 6.0 (LTS - Support until Nov 2024):
- **Status**: Long-term support
- **Entity Framework Core**: 6.x or 7.x
- **C# Version**: 10
- **Minimal APIs**: Basic support
- **Performance**: Good

### .NET 8.0 (LTS - Support until Nov 2026):
- **Status**: Current LTS
- **Entity Framework Core**: 8.x or 9.x
- **C# Version**: 12
- **Minimal APIs**: Enhanced
- **Performance**: ~20% faster
- **New Features**: Native AOT, better JSON handling

## Checking Compatibility

### 1. Check Project Framework:
```bash
# Look for TargetFramework in .csproj
grep -i "TargetFramework" *.csproj
```

### 2. Check Package Dependencies:
```bash
# Check if packages are compatible with .NET 6.0
dotnet list package
```

### 3. Common Package Adjustments for .NET 6.0:

```xml
<!-- .NET 6.0 compatible versions -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="6.0.*" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="6.0.*" />
<PackageReference Include="Microsoft.Identity.Web" Version="1.25.*" />

<!-- .NET 8.0 versions (won't work with .NET 6.0) -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.*" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.*" />
<PackageReference Include="Microsoft.Identity.Web" Version="3.10.*" />
```

## Deployment Steps for .NET 6.0

### 1. Install .NET 6.0:
```bash
sudo ./install-dotnet-6.sh
```

### 2. Verify Installation:
```bash
dotnet --version
# Should show: 6.0.x
```

### 3. Update Project (if needed):
```bash
# Change TargetFramework in .csproj
sed -i 's/net8.0/net6.0/g' GCCWebApp.csproj

# Update packages
dotnet restore
```

### 4. Build and Publish:
```bash
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

### 5. Deploy:
```bash
sudo ./deploy-plesk-native.sh
```

## Troubleshooting

### Error: "The current .NET SDK does not support targeting .NET 8.0"
**Solution**: Either install .NET 8.0 or downgrade project to .NET 6.0

### Error: Package incompatibility
**Solution**: Downgrade package versions to be compatible with .NET 6.0

### Error: C# language features not available
**Solution**: Some C# 12 features won't work in .NET 6.0 (C# 10)

## Quick Decision Guide

| Scenario | Recommendation |
|----------|----------------|
| Dev server has .NET 6.0 only | Downgrade app to .NET 6.0 |
| Production server, can install any version | Use .NET 8.0 for better performance |
| Need maximum compatibility | Use .NET 6.0 |
| Want latest features | Use .NET 8.0 |

## Files Updated for .NET 6.0 Support:

1. **install-dotnet-plesk.sh** - Now supports both 6.0 and 8.0 (set DOTNET_VERSION variable)
2. **install-dotnet-6.sh** - Simplified script specifically for .NET 6.0
3. **gcc-webapp.service** - Works with both versions
4. **deploy-plesk-native.sh** - Works with both versions

## Final Note

The systemd service and nginx configurations work identically for both .NET 6.0 and 8.0. The main difference is in the runtime version and package compatibility.
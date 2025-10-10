# Downgrade GCC Web App from .NET 8 to .NET 6 LTS
# Run this if Plesk server only supports .NET 6

Write-Host "Downgrading GCC Web App to .NET 6..." -ForegroundColor Yellow

# Backup current project file
Copy-Item "GCCWebApp.csproj" "GCCWebApp.csproj.net8.backup" -Force
Write-Host "✓ Backed up project file" -ForegroundColor Green

# Update target framework
(Get-Content "GCCWebApp.csproj") -replace '<TargetFramework>net8.0</TargetFramework>', '<TargetFramework>net6.0</TargetFramework>' | Set-Content "GCCWebApp.csproj"
Write-Host "✓ Updated target framework to .NET 6" -ForegroundColor Green

# Downgrade package versions to .NET 6 compatible
$packageUpdates = @{
    "Microsoft.EntityFrameworkCore" = "6.0.33"
    "Microsoft.EntityFrameworkCore.Design" = "6.0.33"
    "Microsoft.EntityFrameworkCore.Sqlite" = "6.0.33"
    "Microsoft.EntityFrameworkCore.SqlServer" = "6.0.33"
    "Microsoft.AspNetCore.Authentication.JwtBearer" = "6.0.33"
    "Microsoft.AspNetCore.DataProtection" = "6.0.33"
    "MySql.EntityFrameworkCore" = "6.0.29"
    "Microsoft.Identity.Web" = "2.22.0"
    "Microsoft.Identity.Web.MicrosoftGraph" = "2.22.0"
    "Microsoft.Identity.Web.UI" = "2.22.0"
    "Serilog.AspNetCore" = "6.1.0"
    "Serilog.Settings.Configuration" = "7.0.0"
    "Asp.Versioning.Mvc.ApiExplorer" = "7.1.0"
}

Write-Host "Updating package versions..." -ForegroundColor Yellow

foreach ($package in $packageUpdates.GetEnumerator()) {
    Write-Host "  Updating $($package.Key) to $($package.Value)"
    dotnet remove package $package.Key 2>$null
    dotnet add package $package.Key --version $package.Value
}

Write-Host "✓ Updated all packages to .NET 6 compatible versions" -ForegroundColor Green

# Clean and restore
Write-Host "Cleaning and restoring project..." -ForegroundColor Yellow
dotnet clean --verbosity quiet
dotnet restore

# Test build
Write-Host "Testing build..." -ForegroundColor Yellow
if (dotnet build --no-restore --verbosity quiet) {
    Write-Host "✅ SUCCESS: Project successfully downgraded to .NET 6!" -ForegroundColor Green
    Write-Host "   You can now deploy to Plesk servers with .NET 6 LTS support" -ForegroundColor Cyan
    Write-Host "   To revert: dotnet restore GCCWebApp.csproj.net8.backup" -ForegroundColor Gray
} else {
    Write-Host "❌ Build failed. Check errors above." -ForegroundColor Red
    Write-Host "   Restoring backup..." -ForegroundColor Yellow
    Copy-Item "GCCWebApp.csproj.net8.backup" "GCCWebApp.csproj" -Force
    dotnet restore
}

Write-Host "`nDeployment ready! Use: dotnet publish -c Release -o publish" -ForegroundColor Cyan
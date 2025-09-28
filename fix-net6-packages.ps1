# .NET 6.0 Package Compatibility Fix Script
# This script downgrades packages to .NET 6.0 compatible versions

Write-Host "=== .NET 6.0 Package Compatibility Fix ===" -ForegroundColor Green
Write-Host ""

# Check if we're in the right directory
if (!(Test-Path "GCCWebApp.csproj")) {
    Write-Host "ERROR: GCCWebApp.csproj not found. Please run this script from the project directory." -ForegroundColor Red
    exit 1
}

Write-Host "Step 1: Cleaning project..." -ForegroundColor Yellow
dotnet clean
Remove-Item -Recurse -Force obj, bin -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Step 2: Removing incompatible packages..." -ForegroundColor Yellow

$packagesToRemove = @(
    "Microsoft.EntityFrameworkCore",
    "Microsoft.EntityFrameworkCore.Design",
    "Microsoft.EntityFrameworkCore.Sqlite",
    "Microsoft.EntityFrameworkCore.SqlServer",
    "MySql.EntityFrameworkCore",
    "Microsoft.AspNetCore.Authentication.JwtBearer",
    "Microsoft.AspNetCore.DataProtection",
    "Microsoft.Identity.Web",
    "Microsoft.Identity.Web.MicrosoftGraph",
    "Microsoft.Identity.Web.UI",
    "Asp.Versioning.Mvc.ApiExplorer",
    "Swashbuckle.AspNetCore.Annotations",
    "Serilog.AspNetCore",
    "Serilog.Settings.Configuration"
)

foreach ($package in $packagesToRemove) {
    Write-Host "  Removing $package..." -NoNewline
    dotnet remove package $package 2>&1 | Out-Null
    Write-Host " Done" -ForegroundColor Green
}

Write-Host ""
Write-Host "Step 3: Adding .NET 6.0 compatible versions..." -ForegroundColor Yellow

$packagesToAdd = @(
    @{Name="Microsoft.EntityFrameworkCore"; Version="6.0.35"},
    @{Name="Microsoft.EntityFrameworkCore.Design"; Version="6.0.35"},
    @{Name="Microsoft.EntityFrameworkCore.Sqlite"; Version="6.0.35"},
    @{Name="Microsoft.EntityFrameworkCore.SqlServer"; Version="6.0.35"},
    @{Name="Pomelo.EntityFrameworkCore.MySql"; Version="6.0.3"},
    @{Name="Microsoft.AspNetCore.Authentication.JwtBearer"; Version="6.0.35"},
    @{Name="Microsoft.AspNetCore.DataProtection"; Version="6.0.35"},
    @{Name="Microsoft.Identity.Web"; Version="1.25.10"},
    @{Name="Microsoft.Identity.Web.MicrosoftGraph"; Version="1.25.10"},
    @{Name="Microsoft.Identity.Web.UI"; Version="1.25.10"},
    @{Name="Asp.Versioning.Mvc.ApiExplorer"; Version="6.4.0"},
    @{Name="Swashbuckle.AspNetCore.Annotations"; Version="6.9.0"},
    @{Name="Serilog.AspNetCore"; Version="6.1.0"},
    @{Name="Serilog.Settings.Configuration"; Version="3.4.0"}
)

foreach ($package in $packagesToAdd) {
    Write-Host "  Adding $($package.Name) v$($package.Version)..." -NoNewline
    dotnet add package $package.Name --version $package.Version 2>&1 | Out-Null
    Write-Host " Done" -ForegroundColor Green
}

Write-Host ""
Write-Host "Step 4: Restoring packages..." -ForegroundColor Yellow
dotnet restore

Write-Host ""
Write-Host "Step 5: Building project..." -ForegroundColor Yellow
$buildResult = dotnet build 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build successful!" -ForegroundColor Green
} else {
    Write-Host "⚠️ Build completed with warnings or errors" -ForegroundColor Yellow
    Write-Host "Please review the build output above for details" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Package Migration Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Review any build warnings or errors"
Write-Host "2. Update code for any breaking changes (see NET6_MIGRATION_GUIDE.md)"
Write-Host "3. Test the application thoroughly"
Write-Host "4. Run database migrations if needed: dotnet ef database update"
Write-Host ""
Write-Host "If you encounter issues, refer to NET6_MIGRATION_GUIDE.md for detailed instructions." -ForegroundColor Yellow
# Simple Playwright screenshot capture script
$ErrorActionPreference = "Stop"

Write-Host "====================================" -ForegroundColor Cyan
Write-Host "  UI Screenshot Capture Tool" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5101"
$screenshotDir = Join-Path $PSScriptRoot "UIScreenshots"

# Create screenshots directory
if (!(Test-Path $screenshotDir)) {
    New-Item -ItemType Directory -Path $screenshotDir | Out-Null
}

Write-Host "Screenshot directory: $screenshotDir" -ForegroundColor Yellow
Write-Host ""

# Test if app is running
try {
    $response = Invoke-WebRequest -Uri $baseUrl -Method Head -TimeoutSec 5 -UseBasicParsing
    Write-Host "App is running on $baseUrl" -ForegroundColor Green
} catch {
    Write-Host "ERROR: App is not running on $baseUrl" -ForegroundColor Red
    Write-Host "Please start the app first: dotnet run --urls `"http://localhost:5101`"" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Define pages to capture
$pages = @(
    @{ Url = "/Certificate/CertificateTileView"; Name = "01_public_tile_view"; RequiresAuth = $false },
    @{ Url = "/Account/Login"; Name = "02_login"; RequiresAuth = $false },
    @{ Url = "/Dashboard"; Name = "03_dashboard"; RequiresAuth = $true },
    @{ Url = "/Certificate/CertificateTileViewInternal"; Name = "04_internal_tile_view"; RequiresAuth = $true },
    @{ Url = "/Certificate/CertificateTileViewExternal"; Name = "05_external_tile_view"; RequiresAuth = $true },
    @{ Url = "/Admin/UserManagement"; Name = "06_user_management"; RequiresAuth = $true },
    @{ Url = "/Product/Management"; Name = "07_product_management"; RequiresAuth = $true },
    @{ Url = "/Admin/CompanyManagement"; Name = "08_company_management"; RequiresAuth = $true },
    @{ Url = "/Admin/TagManagement"; Name = "09_tag_management"; RequiresAuth = $true },
    @{ Url = "/Certificate/Create"; Name = "10_certificate_create"; RequiresAuth = $true },
    @{ Url = "/Certificate/Details/24-0366"; Name = "11_certificate_details"; RequiresAuth = $false },
    @{ Url = "/Admin/RoleManagement"; Name = "12_role_management"; RequiresAuth = $true },
    @{ Url = "/Admin/PartnerManagement"; Name = "13_partner_management"; RequiresAuth = $true }
)

# Create C# program content
$csharpCode = @'
using Microsoft.Playwright;
using System;
using System.IO;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: <url> <outputPath> <requiresAuth>");
            return;
        }

        string url = args[0];
        string outputPath = args[1];
        bool requiresAuth = bool.Parse(args[2]);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();

        try
        {
            // Login if required
            if (requiresAuth)
            {
                await page.GotoAsync("http://localhost:5101/Account/Login", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
                await page.FillAsync("#Email", "admin@greencirclecertified.com");
                await page.FillAsync("#Password", "Admin123!");
                await page.ClickAsync("button[type='submit']");
                await page.WaitForTimeoutAsync(3000);
            }

            // Navigate to target page
            await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
            await page.WaitForTimeoutAsync(2000);

            // Take screenshot
            await page.ScreenshotAsync(new() { Path = outputPath, FullPage = true });
            Console.WriteLine($"SUCCESS: {Path.GetFileName(outputPath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
'@

# Save C# program
$tempCsFile = Join-Path $PSScriptRoot "TempScreenshotCapture.cs"
$csharpCode | Out-File -FilePath $tempCsFile -Encoding UTF8

# Create temporary project file
$tempCsproj = Join-Path $PSScriptRoot "TempScreenshotCapture.csproj"
$projectContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Playwright" Version="1.55.0" />
  </ItemGroup>
</Project>
"@
$projectContent | Out-File -FilePath $tempCsproj -Encoding UTF8

Write-Host "Building screenshot tool..." -ForegroundColor Yellow
dotnet build $tempCsproj -o (Join-Path $PSScriptRoot "temp-build") -v quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to build screenshot tool" -ForegroundColor Red
    exit 1
}

$exePath = Join-Path $PSScriptRoot "temp-build\TempScreenshotCapture.exe"

Write-Host ""
Write-Host "Capturing screenshots..." -ForegroundColor Yellow
Write-Host ""

$successCount = 0
$failCount = 0

foreach ($page in $pages) {
    $fullUrl = "$baseUrl$($page.Url)"
    $outputPath = Join-Path $screenshotDir "$($page.Name).png"

    Write-Host "  [$($page.Name)] " -NoNewline -ForegroundColor Cyan

    try {
        & $exePath $fullUrl $outputPath $page.RequiresAuth
        if ($LASTEXITCODE -eq 0) {
            Write-Host "" -ForegroundColor Green
            $successCount++
        } else {
            Write-Host " FAILED" -ForegroundColor Red
            $failCount++
        }
    } catch {
        Write-Host " FAILED: $($_.Exception.Message)" -ForegroundColor Red
        $failCount++
    }
}

# Cleanup
Remove-Item $tempCsFile -Force -ErrorAction SilentlyContinue
Remove-Item $tempCsproj -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $PSScriptRoot "temp-build") -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Capture Complete!" -ForegroundColor Green
Write-Host "  Success: $successCount" -ForegroundColor Green
Write-Host "  Failed: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Green" })
Write-Host "  Location: $screenshotDir" -ForegroundColor Yellow
Write-Host "====================================" -ForegroundColor Cyan

# ============================================================================
# GCC Web App - Production Test Runner
# ============================================================================
# This script runs automated tests against the LIVE production site
# URL: https://gccwebapp.com
#
# USAGE:
#   .\run-production-tests.ps1                    # Run all UAT tests
#   .\run-production-tests.ps1 -TestFilter "Login" # Run tests matching "Login"
#   .\run-production-tests.ps1 -TestProject "UAT"  # Run specific project
#   .\run-production-tests.ps1 -Headless          # Run without browser window
#   .\run-production-tests.ps1 -Quick             # Run smoke tests only
#
# ============================================================================

param(
    [string]$TestFilter = "",
    [string]$TestProject = "UAT",
    [switch]$Headless = $false,
    [switch]$Quick = $false,
    [switch]$Verbose = $false,
    [switch]$Screenshots = $true,
    [string]$BaseUrl = "https://gccwebapp.com"
)

# Script configuration
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$TestsRoot = $ScriptDir

# Color output helpers
function Write-Header { param($msg) Write-Host "`n$("=" * 70)" -ForegroundColor Cyan; Write-Host $msg -ForegroundColor Cyan; Write-Host "$("=" * 70)" -ForegroundColor Cyan }
function Write-Step { param($msg) Write-Host "`n>> $msg" -ForegroundColor Yellow }
function Write-Success { param($msg) Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-Warn { param($msg) Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Err { param($msg) Write-Host "[ERROR] $msg" -ForegroundColor Red }

# Banner
Write-Header "GCC Web App - Production Test Runner"
Write-Host @"

  Target Site: $BaseUrl
  Test Mode:   $(if ($Quick) { "Quick (Smoke Tests)" } else { "Full" })
  Browser:     $(if ($Headless) { "Headless" } else { "Visible" })
  Project:     $TestProject

"@ -ForegroundColor White

# Confirm production testing
Write-Host ""
Write-Warn "WARNING: You are about to run tests against PRODUCTION!"
Write-Host "  Site: $BaseUrl" -ForegroundColor White
Write-Host ""
$confirm = Read-Host "Continue? (y/N)"
if ($confirm -ne 'y' -and $confirm -ne 'Y') {
    Write-Host "Aborted." -ForegroundColor Gray
    exit 0
}

# Set environment variables for production testing
Write-Step "Setting environment variables..."
$env:TEST_ENVIRONMENT = "Production"
$env:TEST_BASE_URL = $BaseUrl
$env:TEST_HEADLESS = if ($Headless) { "true" } else { "false" }

Write-Success "TEST_ENVIRONMENT = $env:TEST_ENVIRONMENT"
Write-Success "TEST_BASE_URL = $env:TEST_BASE_URL"
Write-Success "TEST_HEADLESS = $env:TEST_HEADLESS"

# Map test project shorthand to full path
$ProjectMap = @{
    "UAT" = "GCCWebApp.UAT.Tests"
    "E2E" = "GCCWebApp.E2E.Tests"
    "Accessibility" = "GCCWebApp.Accessibility.Tests"
    "Performance" = "GCCWebApp.Performance.Tests"
    "Security" = "GCCWebApp.Security.Tests"
    "API" = "GCCWebApp.Api.Tests"
    "All" = "*"
}

$ProjectPath = $ProjectMap[$TestProject]
if (-not $ProjectPath) {
    $ProjectPath = $TestProject
}

# Build filter arguments
$filterArgs = @()

if ($Quick) {
    # Quick mode: run only smoke/basic tests
    $filterArgs += '--filter'
    $filterArgs += 'Category=Smoke|FullyQualifiedName~Basic|FullyQualifiedName~Login|FullyQualifiedName~Navigation'
}
elseif ($TestFilter) {
    $filterArgs += '--filter'
    # Support both Category= filters and name filters
    if ($TestFilter -match "^Category=") {
        $filterArgs += $TestFilter
    }
    else {
        $filterArgs += "FullyQualifiedName~$TestFilter"
    }
}

# Verbosity
$verbosity = if ($Verbose) { "detailed" } else { "normal" }

# Create timestamp for results
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$resultsDir = Join-Path $TestsRoot "TestResults\Production_$timestamp"
New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null

Write-Step "Running tests..."
Write-Host "  Results will be saved to: $resultsDir" -ForegroundColor Gray

# Run tests
try {
    if ($ProjectPath -eq "*") {
        # Run all test projects suitable for production
        $productionProjects = @(
            "GCCWebApp.UAT.Tests",
            "GCCWebApp.Accessibility.Tests",
            "GCCWebApp.E2E.Tests"
        )

        foreach ($proj in $productionProjects) {
            Write-Step "Testing: $proj"
            $projPath = Join-Path $TestsRoot $proj
            if (Test-Path $projPath) {
                $testArgs = @(
                    "test"
                    $projPath
                    "--verbosity", $verbosity
                    "--results-directory", $resultsDir
                    "--logger", "trx;LogFileName=$proj.trx"
                    "--logger", "console;verbosity=$verbosity"
                ) + $filterArgs

                & dotnet @testArgs
            }
            else {
                Write-Warn "Project not found: $projPath"
            }
        }
    }
    else {
        # Run specific project
        $projPath = Join-Path $TestsRoot $ProjectPath
        if (-not (Test-Path $projPath)) {
            Write-Err "Project not found: $projPath"
            exit 1
        }

        $testArgs = @(
            "test"
            $projPath
            "--verbosity", $verbosity
            "--results-directory", $resultsDir
            "--logger", "trx;LogFileName=$ProjectPath.trx"
            "--logger", "console;verbosity=$verbosity"
        ) + $filterArgs

        & dotnet @testArgs
    }

    $exitCode = $LASTEXITCODE
}
catch {
    Write-Err "Test execution failed: $_"
    $exitCode = 1
}

# Summary
Write-Header "Test Run Complete"

Write-Host ""
Write-Host "Results Location:" -ForegroundColor Cyan
Write-Host "  $resultsDir" -ForegroundColor White

# Find screenshots
$screenshotDirs = Get-ChildItem -Path $TestsRoot -Recurse -Directory -Filter "Screenshots" -ErrorAction SilentlyContinue
if ($screenshotDirs) {
    Write-Host ""
    Write-Host "Screenshots:" -ForegroundColor Cyan
    foreach ($dir in $screenshotDirs) {
        $prodScreenshots = Get-ChildItem -Path $dir.FullName -Filter "Production_*" -Directory -ErrorAction SilentlyContinue
        if ($prodScreenshots) {
            foreach ($ss in $prodScreenshots) {
                $count = (Get-ChildItem -Path $ss.FullName -Filter "*.png" -ErrorAction SilentlyContinue).Count
                Write-Host "  $($ss.FullName) ($count images)" -ForegroundColor White
            }
        }
    }
}

# Final status
Write-Host ""
if ($exitCode -eq 0) {
    Write-Success "All tests passed!"
}
else {
    Write-Err "Some tests failed. Exit code: $exitCode"
}

# Cleanup environment
Remove-Item Env:TEST_ENVIRONMENT -ErrorAction SilentlyContinue
Remove-Item Env:TEST_BASE_URL -ErrorAction SilentlyContinue
Remove-Item Env:TEST_HEADLESS -ErrorAction SilentlyContinue

exit $exitCode

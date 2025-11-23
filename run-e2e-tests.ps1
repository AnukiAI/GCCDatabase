#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Automated E2E Test Runner for GCC Web App

.DESCRIPTION
    This script automatically:
    1. Starts the GCC Web App on https://localhost:5101
    2. Waits for the application to be ready
    3. Runs the E2E/UAT tests
    4. Stops the application after tests complete

.PARAMETER TestFilter
    Optional filter for running specific tests (e.g., "FullyQualifiedName~Certificate")

.PARAMETER SkipBuild
    Skip building the solution before running tests

.EXAMPLE
    .\run-e2e-tests.ps1
    .\run-e2e-tests.ps1 -TestFilter "FullyQualifiedName~Certificate"
    .\run-e2e-tests.ps1 -SkipBuild
#>

param(
    [string]$TestFilter = "",
    [switch]$SkipBuild = $false
)

$ErrorActionPreference = "Stop"

# Configuration
$AppPath = "GCCWebApp"
$TestPath = "GCCWebApp-Tests\GCCWebApp.UAT.Tests"
$AppUrl = "https://localhost:5101"
$MaxWaitTime = 60  # seconds
$AppProcess = $null

Write-Host "================================" -ForegroundColor Cyan
Write-Host "   GCC Web App E2E Test Runner" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Function to check if app is running
function Test-AppRunning {
    try {
        # Disable SSL certificate validation for localhost testing
        $null = Invoke-WebRequest -Uri $AppUrl -Method HEAD -TimeoutSec 5 -SkipCertificateCheck -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

# Function to stop the application
function Stop-Application {
    if ($null -ne $AppProcess -and !$AppProcess.HasExited) {
        Write-Host "Stopping application..." -ForegroundColor Yellow
        try {
            # Try graceful shutdown first
            $AppProcess.Kill($true)  # Kill process tree
            $AppProcess.WaitForExit(5000)
            Write-Host "Application stopped successfully" -ForegroundColor Green
        }
        catch {
            Write-Host "Warning: Could not gracefully stop application: $_" -ForegroundColor Yellow
            # Force kill any remaining dotnet processes for this app
            Get-Process -Name "dotnet","GCCWebApp" -ErrorAction SilentlyContinue |
                Where-Object { $_.Path -like "*$AppPath*" } |
                Stop-Process -Force -ErrorAction SilentlyContinue
        }
    }
}

# Cleanup handler
$cleanupScript = {
    Stop-Application
}
Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action $cleanupScript | Out-Null
try {
    # Step 1: Build solution (unless skipped)
    if (!$SkipBuild) {
        Write-Host "Step 1: Building solution..." -ForegroundColor Cyan
        Push-Location $AppPath
        $buildResult = dotnet build --configuration Debug 2>&1
        Pop-Location

        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build failed!" -ForegroundColor Red
            Write-Host $buildResult
            exit 1
        }
        Write-Host "Build successful" -ForegroundColor Green
        Write-Host ""
    }
    else {
        Write-Host "Skipping build (--SkipBuild specified)" -ForegroundColor Yellow
        Write-Host ""
    }

    # Step 2: Check if app is already running
    Write-Host "Step 2: Checking if application is already running..." -ForegroundColor Cyan
    if (Test-AppRunning) {
        Write-Host "Application is already running at $AppUrl" -ForegroundColor Yellow
        Write-Host "Using existing instance for tests" -ForegroundColor Yellow
        $usingExisting = $true
    }
    else {
        $usingExisting = $false

        # Step 3: Start the application
        Write-Host "Starting application at $AppUrl..." -ForegroundColor Cyan
        Push-Location $AppPath

        $startInfo = New-Object System.Diagnostics.ProcessStartInfo
        $startInfo.FileName = "dotnet"
        $startInfo.Arguments = "run --no-build --urls `"$AppUrl`""
        $startInfo.UseShellExecute = $false
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $startInfo.WorkingDirectory = (Get-Location).Path
        $startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development"

        $AppProcess = New-Object System.Diagnostics.Process
        $AppProcess.StartInfo = $startInfo
        $null = $AppProcess.Start()

        Pop-Location
        Write-Host "Application process started (PID: $($AppProcess.Id))" -ForegroundColor Green

        # Step 4: Wait for application to be ready
        Write-Host "Waiting for application to be ready..." -ForegroundColor Cyan
        $waited = 0
        $ready = $false

        while ($waited -lt $MaxWaitTime) {
            Start-Sleep -Seconds 2
            $waited += 2

            if (Test-AppRunning) {
                $ready = $true
                Write-Host "Application is ready! ($waited seconds)" -ForegroundColor Green
                break
            }

            if ($AppProcess.HasExited) {
                Write-Host "Application process exited unexpectedly!" -ForegroundColor Red
                Write-Host "Exit Code: $($AppProcess.ExitCode)" -ForegroundColor Red
                exit 1
            }

            Write-Host "  Waiting... ($waited/$MaxWaitTime seconds)" -ForegroundColor Gray
        }

        if (!$ready) {
            Write-Host "Application failed to start within $MaxWaitTime seconds" -ForegroundColor Red
            Stop-Application
            exit 1
        }
        Write-Host ""
    }

    # Step 5: Run E2E tests
    Write-Host "Step 3: Running E2E/UAT tests..." -ForegroundColor Cyan
    Write-Host "Test project: $TestPath" -ForegroundColor Gray
    if ($TestFilter) {
        Write-Host "Filter: $TestFilter" -ForegroundColor Gray
    }
    Write-Host ""

    Push-Location $TestPath

    $testArgs = @(
        "test",
        "--no-build",
        "--logger", "console;verbosity=normal"
    )

    if ($TestFilter) {
        $testArgs += "--filter"
        $testArgs += $TestFilter
    }

    & dotnet @testArgs
    $testExitCode = $LASTEXITCODE

    Pop-Location

    Write-Host ""
    if ($testExitCode -eq 0) {
        Write-Host "✓ All tests passed!" -ForegroundColor Green
    }
    else {
        Write-Host "✗ Some tests failed (exit code: $testExitCode)" -ForegroundColor Red
    }
    Write-Host ""

    # Step 6: Cleanup
    if (!$usingExisting) {
        Stop-Application
    }
    else {
        Write-Host "Leaving existing application instance running" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "Test Results Location:" -ForegroundColor Cyan
    Write-Host "  Screenshots: $TestPath\bin\Debug\net8.0\Screenshots\" -ForegroundColor Gray
    Write-Host "  Test Results: $TestPath\TestResults\" -ForegroundColor Gray
    Write-Host ""

    exit $testExitCode
}
catch {
    Write-Host ""
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    Stop-Application
    exit 1
}
finally {
    Unregister-Event -SourceIdentifier PowerShell.Exiting -ErrorAction SilentlyContinue
}

#Requires -Version 5.1

<#
.SYNOPSIS
    Converts HTML documentation files to PDF using Microsoft Edge

.DESCRIPTION
    This script automatically converts all HTML files in the current directory
    to PDF format using Microsoft Edge's built-in PDF printer. The output PDFs
    are optimized for professional client distribution.

.EXAMPLE
    .\Convert-To-PDF.ps1
    Converts all HTML files to PDF

.NOTES
    Author: GCC Database Solution Team
    Date: November 21, 2025
    Version: 1.0
#>

# Set strict mode
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Function to check if Microsoft Edge is installed
function Test-EdgeInstalled {
    $edgePaths = @(
        "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
        "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe"
    )

    foreach ($path in $edgePaths) {
        if (Test-Path $path) {
            return $path
        }
    }

    return $null
}

# Function to convert HTML to PDF using Edge
function Convert-HtmlToPdf {
    param(
        [string]$HtmlPath,
        [string]$PdfPath,
        [string]$EdgePath
    )

    Write-Host "Converting: $([System.IO.Path]::GetFileName($HtmlPath))" -ForegroundColor Cyan

    # Edge command line for PDF printing
    $arguments = @(
        "--headless",
        "--disable-gpu",
        "--run-all-compositor-stages-before-draw",
        "--print-to-pdf=`"$PdfPath`"",
        "--print-to-pdf-no-header",
        "`"$HtmlPath`""
    )

    try {
        $process = Start-Process -FilePath $EdgePath -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden

        if ($process.ExitCode -eq 0 -and (Test-Path $PdfPath)) {
            $pdfSize = (Get-Item $PdfPath).Length / 1KB
            Write-Host "  ✓ Created: $([System.IO.Path]::GetFileName($PdfPath)) ($([math]::Round($pdfSize, 1)) KB)" -ForegroundColor Green
            return $true
        } else {
            Write-Host "  ✗ Failed to create PDF" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "  ✗ Error: $_" -ForegroundColor Red
        return $false
    }
}

# Main script
Write-Host "`n=====================================" -ForegroundColor Yellow
Write-Host "GCC Database - HTML to PDF Converter" -ForegroundColor Yellow
Write-Host "=====================================" -ForegroundColor Yellow

# Check for Microsoft Edge
Write-Host "`nChecking for Microsoft Edge..." -ForegroundColor Cyan
$edgePath = Test-EdgeInstalled

if (-not $edgePath) {
    Write-Host "✗ Microsoft Edge not found!" -ForegroundColor Red
    Write-Host "`nPlease install Microsoft Edge from: https://www.microsoft.com/edge" -ForegroundColor Yellow
    Write-Host "Or use the browser print method (see README.md)" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Found Microsoft Edge: $edgePath" -ForegroundColor Green

# Get current directory
$scriptDir = $PSScriptRoot
if (-not $scriptDir) {
    $scriptDir = Get-Location
}

Write-Host "`nSearching for HTML files in: $scriptDir" -ForegroundColor Cyan

# Find all HTML files
$htmlFiles = Get-ChildItem -Path $scriptDir -Filter "*.html" -File

if ($htmlFiles.Count -eq 0) {
    Write-Host "✗ No HTML files found in current directory" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Found $($htmlFiles.Count) HTML file(s)" -ForegroundColor Green

# Convert each HTML file to PDF
$successCount = 0
$failCount = 0

Write-Host "`nConverting files...`n" -ForegroundColor Cyan

foreach ($htmlFile in $htmlFiles) {
    $htmlPath = $htmlFile.FullName
    $pdfPath = [System.IO.Path]::ChangeExtension($htmlPath, ".pdf")

    if (Convert-HtmlToPdf -HtmlPath $htmlPath -PdfPath $pdfPath -EdgePath $edgePath) {
        $successCount++
    } else {
        $failCount++
    }
}

# Summary
Write-Host "`n=====================================" -ForegroundColor Yellow
Write-Host "Conversion Summary" -ForegroundColor Yellow
Write-Host "=====================================" -ForegroundColor Yellow
Write-Host "Total files: $($htmlFiles.Count)" -ForegroundColor Cyan
Write-Host "Successful: $successCount" -ForegroundColor Green
Write-Host "Failed: $failCount" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Red" })

if ($successCount -gt 0) {
    Write-Host "`n✓ PDF files created in: $scriptDir" -ForegroundColor Green
}

Write-Host "`nPress any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

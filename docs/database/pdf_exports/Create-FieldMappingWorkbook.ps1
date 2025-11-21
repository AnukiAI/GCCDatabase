#Requires -Version 5.1

<#
.SYNOPSIS
    Creates a professional Excel workbook for GCC Database Field Mapping Reference

.DESCRIPTION
    This script creates a multi-sheet Excel workbook with field mappings for:
    - Certificate Fields
    - Company Fields
    - Product Fields
    - User Fields
    - Product ID Fields
    - Reference Data

    The workbook includes professional formatting with GCC brand colors,
    frozen headers, auto-filters, and optimized column widths.

.EXAMPLE
    .\Create-FieldMappingWorkbook.ps1

.NOTES
    Author: GCC Database Solution Team
    Date: November 21, 2025
    Version: 1.0
    Requires: Microsoft Excel installed
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "`n=========================================" -ForegroundColor Cyan
Write-Host "GCC Field Mapping Workbook Creator" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Check if Excel is installed
Write-Host "`nChecking for Microsoft Excel..." -ForegroundColor Yellow
try {
    $excel = New-Object -ComObject Excel.Application
    Write-Host "✓ Excel found: Version $($excel.Version)" -ForegroundColor Green
    $excel.Quit()
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
    Remove-Variable excel
}
catch {
    Write-Host "✗ Microsoft Excel not found!" -ForegroundColor Red
    Write-Host "`nThis script requires Excel to be installed." -ForegroundColor Yellow
    Write-Host "Alternative: Use the interactive HTML version instead." -ForegroundColor Yellow
    exit 1
}

# Define output file
$scriptDir = $PSScriptRoot
if (-not $scriptDir) {
    $scriptDir = Get-Location
}
$outputFile = Join-Path $scriptDir "GCC_Field_Mapping_Reference.xlsx"

Write-Host "`nCreating Excel workbook..." -ForegroundColor Yellow
Write-Host "Output: $outputFile" -ForegroundColor Gray

# Create Excel application
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false

# Create new workbook
$workbook = $excel.Workbooks.Add()

# GCC Brand Colors (RGB)
$gccBlue = 2 + 43*256 + 118*65536     # #002B76
$gccGreen = 146 + 200*256 + 62*65536   # #92C83E
$white = 16777215
$lightGray = 15790320                  # #F1F1F0
$darkGray = 6579300                    # #646464

# Helper function to create a sheet with data
function Add-FieldMappingSheet {
    param(
        [Parameter(Mandatory=$true)]
        $Workbook,
        [Parameter(Mandatory=$true)]
        [string]$SheetName,
        [Parameter(Mandatory=$true)]
        [array]$Headers,
        [Parameter(Mandatory=$true)]
        [array]$Data
    )

    Write-Host "  Creating sheet: $SheetName..." -ForegroundColor Gray

    # Add new sheet or use existing
    if ($Workbook.Sheets.Count -eq 1 -and $Workbook.Sheets.Item(1).Name -eq "Sheet1") {
        $sheet = $Workbook.Sheets.Item(1)
        $sheet.Name = $SheetName
    }
    else {
        $sheet = $Workbook.Sheets.Add([System.Reflection.Missing]::Value, $Workbook.Sheets.Item($Workbook.Sheets.Count))
        $sheet.Name = $SheetName
    }

    # Write headers
    for ($i = 0; $i -lt $Headers.Count; $i++) {
        $sheet.Cells.Item(1, $i + 1) = $Headers[$i]
    }

    # Format header row
    $headerRange = $sheet.Range($sheet.Cells.Item(1,1), $sheet.Cells.Item(1, $Headers.Count))
    $headerRange.Interior.Color = $gccBlue
    $headerRange.Font.Color = $white
    $headerRange.Font.Bold = $true
    $headerRange.Font.Size = 11
    $headerRange.HorizontalAlignment = -4131  # xlLeft
    $headerRange.VerticalAlignment = -4160    # xlTop

    # Write data
    for ($row = 0; $row -lt $Data.Count; $row++) {
        for ($col = 0; $col -lt $Data[$row].Count; $col++) {
            $sheet.Cells.Item($row + 2, $col + 1) = $Data[$row][$col]
        }
    }

    # Format data rows
    $dataRange = $sheet.Range($sheet.Cells.Item(2,1), $sheet.Cells.Item($Data.Count + 1, $Headers.Count))
    $dataRange.Font.Size = 10
    $dataRange.WrapText = $true
    $dataRange.VerticalAlignment = -4160  # xlTop

    # Alternating row colors
    for ($row = 2; $row -le $Data.Count + 1; $row++) {
        if ($row % 2 -eq 0) {
            $rowRange = $sheet.Range($sheet.Cells.Item($row,1), $sheet.Cells.Item($row, $Headers.Count))
            $rowRange.Interior.Color = $lightGray
        }
    }

    # Auto-fit columns
    $sheet.Columns.AutoFit() | Out-Null

    # Set optimal column widths (max 60 characters)
    for ($col = 1; $col -le $Headers.Count; $col++) {
        if ($sheet.Columns.Item($col).ColumnWidth -gt 60) {
            $sheet.Columns.Item($col).ColumnWidth = 60
        }
    }

    # Freeze header row
    $sheet.Application.ActiveWindow.SplitRow = 1
    $sheet.Application.ActiveWindow.FreezePanes = $true

    # Add auto-filter
    $sheet.Range($sheet.Cells.Item(1,1), $sheet.Cells.Item($Data.Count + 1, $Headers.Count)).AutoFilter() | Out-Null

    # Add borders
    $allDataRange = $sheet.Range($sheet.Cells.Item(1,1), $sheet.Cells.Item($Data.Count + 1, $Headers.Count))
    $allDataRange.Borders.LineStyle = 1  # xlContinuous
    $allDataRange.Borders.Weight = 2     # xlThin
    $allDataRange.Borders.Color = $darkGray

    return $sheet
}

# SHEET 1: Certificate Fields
$certHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$certData = @(
    @("Id", "Integer", "Auto", "Primary key (auto-generated)", "ID, CertificateID, PK", "1, 2, 3", "System-generated, do not import"),
    @("Number", "String(7)", "YES", "Unique certificate number", "CertificateNumber, CertNumber, Cert#, Number", "24-0001, 24-0366", "Format: YY-NNNN, must be unique"),
    @("Status", "String(50)", "YES", "Certificate lifecycle status (14-state workflow)", "Status, State, CertStatus", "active, draft, expired", "Must be one of 14 valid values"),
    @("EffectiveDate", "Date", "YES", "Certificate start date", "EffectiveDate, StartDate, IssueDate, ValidFrom", "2024-01-15", "Format: YYYY-MM-DD, before ExpirationDate"),
    @("ExpirationDate", "Date", "YES", "Certificate end date", "ExpirationDate, EndDate, ValidUntil", "2025-01-14", "Format: YYYY-MM-DD, after EffectiveDate"),
    @("Location", "String(175)", "No", "Physical location", "Location, Address, Site, Facility", "Seattle, WA", "Max 175 characters"),
    @("Country", "String(100)", "No", "Country code or name", "Country, CountryCode, Nation", "US, CA, GB", "Prefer ISO 3166-1 alpha-2"),
    @("Partner", "String(100)", "No", "Partner program", "Partner, Program, Marketplace", "Amazon, Wayfair, None", "Amazon, Wayfair, None, or Other"),
    @("CompanyId", "Integer", "YES", "Reference to Company", "CompanyID, ManufacturerID", "1, 5, 12", "Must reference existing Company.Id"),
    @("ProductId", "Integer", "YES", "Reference to Product", "ProductID, ItemID", "1, 10, 25", "Must reference existing Product.Id"),
    @("CertificationTypeId", "Integer", "YES", "Reference to CertificationType", "CertTypeID, TypeID", "1, 2, 3", "Must reference existing CertificationType.Id"),
    @("CertificateFile", "String(500)", "No", "Path to PDF certificate", "CertificateFile, PDFPath", "certificates/24-0001.pdf", "Relative path from wwwroot/uploads/"),
    @("CertificateImagePath", "String(500)", "No", "Path to preview image", "ImagePath, ThumbnailPath", "certificates/24-0001.png", "Relative path from wwwroot/uploads/"),
    @("CreatedBy", "Integer", "No", "User who created certificate", "CreatedBy, CreatedByUserID", "1, 5", "Must reference existing User.Id"),
    @("CreatedAt", "DateTime", "Auto", "Creation timestamp", "CreatedDate, DateCreated", "2024-11-21 10:30:00", "System sets on insert"),
    @("UpdatedAt", "DateTime", "Auto", "Last update timestamp", "ModifiedDate, LastModified", "2024-11-21 15:45:00", "System sets on update")
)
Add-FieldMappingSheet -Workbook $workbook -SheetName "Certificate Fields" -Headers $certHeaders -Data $certData

# SHEET 2: Company Fields
$companyHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$companyData = @(
    @("Id", "Integer", "Auto", "Primary key (auto-generated)", "ID, CompanyID, ClientID", "1, 2, 3", "System-generated"),
    @("Name", "String(255)", "YES", "Company legal name", "Name, CompanyName, BusinessName", "Acme Corporation", "Max 255 chars, must be unique"),
    @("ContactInfo", "String(255)", "No", "Contact person or info", "Contact, ContactPerson", "John Smith", "Max 255 characters"),
    @("Email", "String(255)", "No", "Company email", "Email, ContactEmail", "contact@acmecorp.com", "Must be valid email format"),
    @("Website", "String(500)", "No", "Company website URL", "Website, URL", "https://www.acmecorp.com", "Must start with http:// or https://"),
    @("Location", "String(175)", "No", "Physical location", "Location, Address, City", "Seattle, WA", "Max 175 characters"),
    @("Country", "String(100)", "No", "Country code or name", "Country, CountryCode", "US, CA, GB", "Prefer ISO 3166-1 alpha-2"),
    @("IsActive", "Boolean", "No", "Active status flag", "IsActive, Active, Enabled", "true, false", "Default: true"),
    @("CreatedAt", "DateTime", "Auto", "Creation timestamp", "CreatedDate", "2024-11-21 10:30:00", "System sets on insert"),
    @("UpdatedAt", "DateTime", "Auto", "Last update timestamp", "ModifiedDate", "2024-11-21 15:45:00", "System sets on update")
)
Add-FieldMappingSheet -Workbook $workbook -SheetName "Company Fields" -Headers $companyHeaders -Data $companyData

# SHEET 3: Product Fields
$productHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$productData = @(
    @("Id", "Integer", "Auto", "Primary key (auto-generated)", "ID, ProductID, ItemID", "1, 2, 3", "System-generated"),
    @("Name", "String(255)", "YES", "Product name", "Name, ProductName, ItemName", "Oak Hardwood Flooring", "Max 255 characters"),
    @("Description", "Text", "No", "Product description", "Description, Details", "Premium solid oak flooring", "Unlimited length"),
    @("Model", "String(100)", "No", "Product model number", "Model, ModelNumber", "OAK-HW-750", "Max 100 characters"),
    @("Sku", "String(50)", "No", "Stock keeping unit", "SKU, StockCode, ItemCode", "OAK-HW-750-NAT", "Max 50 characters"),
    @("Upc", "String(50)", "No", "Universal product code", "UPC, Barcode", "012345678905", "12 digits if provided"),
    @("Asin", "String(50)", "No", "Amazon standard ID", "ASIN, AmazonASIN", "B08N5WRWNW", "10 alphanumeric if provided"),
    @("CategoryId", "Integer", "YES", "Reference to ProductCategory", "CategoryID, Category", "1, 2, 3", "Must reference existing ProductCategory.Id"),
    @("ProductTypeId", "Integer", "No", "Reference to ProductType", "TypeID, ProductTypeID", "1, 2, 3", "Must reference existing ProductType.Id"),
    @("CompanyId", "Integer", "No", "Reference to Company", "CompanyID, ManufacturerID", "5, 12", "Must reference existing Company.Id"),
    @("IsActive", "Boolean", "No", "Active status flag", "IsActive, Active", "true, false", "Default: true")
)
Add-FieldMappingSheet -Workbook $workbook -SheetName "Product Fields" -Headers $productHeaders -Data $productData

# SHEET 4: User Fields
$userHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$userData = @(
    @("Id", "Integer", "Auto", "Primary key (auto-generated)", "ID, UserID", "1, 2, 3", "System-generated"),
    @("Username", "String(255)", "YES", "Unique username", "Username, Login", "admin, analyst", "Max 255 chars, must be unique"),
    @("Email", "String(255)", "YES", "Email address", "Email, EmailAddress", "admin@example.com", "Valid email, must be unique"),
    @("Name", "String(100)", "No", "Short name or first name", "Name, FirstName", "John, Jane", "Max 100 characters"),
    @("FullName", "String(255)", "No", "Full display name", "FullName, DisplayName", "John Smith", "Max 255 characters"),
    @("PasswordHash", "String(255)", "YES", "BCrypt password hash", "Password, PasswordHash", "$2a$11$...", "Must be BCrypt hash (use HashGenerator)"),
    @("Status", "String(20)", "YES", "Account status", "Status, AccountStatus", "active, inactive, locked", "Must be: active, inactive, or locked"),
    @("Roles", "String", "YES", "Comma-separated role names", "Role, Roles, UserRole", "Admin, Analyst", "Must reference existing roles"),
    @("TwoFactorEnabled", "Boolean", "No", "2FA enabled flag", "TwoFactorEnabled, 2FA", "true, false", "Default: false"),
    @("TwoFactorMethod", "String", "No", "2FA delivery method", "TwoFactorMethod, 2FAMethod", "Email, SMS, TOTP", "Default: Email"),
    @("PhoneNumber", "String(20)", "No", "Phone for SMS 2FA", "Phone, PhoneNumber", "+1-555-555-5555", "Required if 2FA method includes SMS"),
    @("IsActive", "Boolean", "No", "Active flag", "IsActive, Active", "true, false", "Default: true")
)
Add-FieldMappingSheet -Workbook $workbook -SheetName "User Fields" -Headers $userHeaders -Data $userData

# SHEET 5: Product ID Fields
$productIdHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$productIdData = @(
    @("Id", "Integer", "Auto", "Primary key (auto-generated)", "ID, ProductIDID", "1, 2, 3", "System-generated"),
    @("CertificateId", "Integer", "YES", "Reference to Certificate", "CertificateID, CertID", "1, 5, 10", "Must reference existing Certificate.Id"),
    @("ProductIdType", "String(10)", "YES", "Type of identifier", "Type, IDType", "ASIN, UPC, GTIN", "ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN, Other"),
    @("ProductIdValue", "String(50)", "YES", "Identifier value", "Value, ID, Code", "B08N5WRWNW", "Format must match type"),
    @("Status", "String(20)", "No", "Product ID status", "Status, State", "Active, Inactive", "Default: Active"),
    @("Partner", "String(50)", "No", "Associated partner", "Partner, Marketplace", "Amazon, Wayfair", "Optional partner association"),
    @("ProductName", "String(200)", "No", "Product name for this ID", "ProductName, Name", "Oak Flooring - Natural", "Optional, max 200 characters"),
    @("ProductSku", "String(100)", "No", "SKU for this ID", "SKU, StockCode", "OAK-HW-750-NAT", "Optional, max 100 characters"),
    @("DisplayOrder", "Integer", "No", "Sort sequence", "DisplayOrder, SortOrder", "1, 2, 3", "Controls display order in UI"),
    @("IsActive", "Boolean", "No", "Active flag", "IsActive, Active", "true, false", "Default: true, false = soft delete"),
    @("IsPublic", "Boolean", "No", "Public visibility flag", "IsPublic, Public", "true, false", "Default: true, false = hide from public")
)
Add-FieldMappingSheet -Workbook $workbook -SheetName "Product ID Fields" -Headers $productIdHeaders -Data $productIdData

# SHEET 6: Product ID Type Reference
$typeHeaders = @("Type", "Description", "Format", "Example", "Validation Regex")
$typeData = @(
    @("ASIN", "Amazon Standard ID", "10 alphanumeric", "B08N5WRWNW", "^[A-Z0-9]{10}$"),
    @("UPC", "Universal Product Code", "12 digits", "012345678905", "^\d{12}$"),
    @("GTIN", "Global Trade Item Number", "8/12/13/14 digits", "00012345678905", "^\d{8,14}$"),
    @("EAN", "European Article Number", "13 digits", "5901234123457", "^\d{13}$"),
    @("ISBN", "International Book Number", "10 or 13 digits", "978-0-596-52068-7", "^\d{9}[\dX]$ or ^\d{13}$"),
    @("SKU", "Stock Keeping Unit", "Varies by company", "ABC-12345-M", "No strict format"),
    @("MPN", "Manufacturer Part Number", "Varies by manufacturer", "MFR-9876-BLK", "No strict format"),
    @("Other", "Custom identifier", "Any format", "CUSTOM-ID-001", "No strict format")
)
Add-FieldMappingSheet -Workbook $workbook -SheetName "Product ID Types" -Headers $typeHeaders -Data $typeData

# SHEET 7: Certificate Status Values
$statusHeaders = @("GCC Status", "Description", "Typical Use", "Common Legacy Mappings")
$statusData = @(
    @("draft", "Certificate being created", "Initial state", "Draft, New, Created, InProgress"),
    @("pending_review", "Awaiting review", "After draft completion", "PendingReview, Pending, AwaitingReview"),
    @("under_review", "Currently being reviewed", "Active review process", "UnderReview, InReview, Reviewing"),
    @("pending_approval", "Awaiting approval", "After review passes", "PendingApproval, AwaitingApproval"),
    @("approved", "Approved but not issued", "Approval granted", "Approved, Accepted, Authorized"),
    @("issued", "Certificate issued to client", "Active certificate", "Issued, Published, Delivered"),
    @("active", "Currently valid certificate", "Public-facing certificates", "Active, Valid, Current, Live"),
    @("expiring_soon", "Expires within 90 days", "Auto-flagged by system", "ExpiringSoon, NearExpiration"),
    @("renewal_requested", "Renewal in progress", "Client requested renewal", "RenewalRequested, Renewing"),
    @("suspended", "Temporarily suspended", "Issues found", "Suspended, OnHold, Paused"),
    @("revoked", "Permanently revoked", "Serious violations", "Revoked, Withdrawn, Cancelled"),
    @("expired", "Past expiration date", "Auto-flagged by system", "Expired, PastDue, NoLongerValid"),
    @("archived", "Historical record", "No longer active", "Archived, Historical, Closed"),
    @("cancelled", "Cancelled before issue", "Cancelled during creation", "Cancelled, Rejected, Denied")
)
Add-FieldMappingSheet -Workbook $workbook -SheetName "Certificate Status Values" -Headers $statusHeaders -Data $statusData

# Add summary/cover sheet at the beginning
Write-Host "  Creating cover sheet..." -ForegroundColor Gray
$coverSheet = $workbook.Sheets.Add($workbook.Sheets.Item(1))
$coverSheet.Name = "README"

# Title
$coverSheet.Cells.Item(1, 1) = "GCC Database Solution"
$coverSheet.Cells.Item(1, 1).Font.Size = 24
$coverSheet.Cells.Item(1, 1).Font.Bold = $true
$coverSheet.Cells.Item(1, 1).Font.Color = $gccBlue

$coverSheet.Cells.Item(2, 1) = "Field Mapping Reference Guide"
$coverSheet.Cells.Item(2, 1).Font.Size = 18
$coverSheet.Cells.Item(2, 1).Font.Bold = $true
$coverSheet.Cells.Item(2, 1).Font.Color = $gccBlue

# Metadata
$coverSheet.Cells.Item(4, 1) = "Version: 1.0"
$coverSheet.Cells.Item(5, 1) = "Date: November 21, 2025"
$coverSheet.Cells.Item(6, 1) = "Purpose: Client reference for mapping existing data fields to GCC database schema"

# Instructions
$coverSheet.Cells.Item(8, 1) = "How to Use This Workbook:"
$coverSheet.Cells.Item(8, 1).Font.Bold = $true
$coverSheet.Cells.Item(8, 1).Font.Size = 12

$instructions = @(
    "1. Each sheet contains field mappings for a specific entity (Certificate, Company, Product, etc.)",
    "2. Use the 'Common Source Names' column to find your existing field names",
    "3. Map your fields to the corresponding GCC field names",
    "4. Check 'Validation Rules' column for data quality requirements",
    "5. Required fields are marked with 'YES' in the 'Required' column",
    "6. Auto-generated fields (like Id, CreatedAt) should NOT be imported",
    "",
    "Sheets Included:",
    "  • Certificate Fields - Core certificate data (16 fields)",
    "  • Company Fields - Company/manufacturer data (10 fields)",
    "  • Product Fields - Product data (11 fields)",
    "  • User Fields - User account data (12 fields)",
    "  • Product ID Fields - Multiple product IDs per certificate (11 fields)",
    "  • Product ID Types - ASIN/UPC/GTIN format reference (8 types)",
    "  • Certificate Status Values - 14-state workflow reference (14 statuses)",
    "",
    "Features:",
    "  ✓ Frozen header rows for easy scrolling",
    "  ✓ Auto-filters enabled on all sheets",
    "  ✓ Professional formatting with GCC brand colors",
    "  ✓ Optimized column widths",
    "  ✓ Alternating row colors for readability",
    "",
    "Support: swest@anukiconsulting.com"
)

$row = 9
foreach ($instruction in $instructions) {
    $coverSheet.Cells.Item($row, 1) = $instruction
    $row++
}

# Auto-fit README sheet
$coverSheet.Columns.Item(1).ColumnWidth = 100

# Save workbook
Write-Host "`nSaving workbook..." -ForegroundColor Yellow
if (Test-Path $outputFile) {
    Remove-Item $outputFile -Force
}
$workbook.SaveAs($outputFile)

# Close Excel
$workbook.Close($false)
$excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($workbook) | Out-Null
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
Remove-Variable workbook, excel

# Get file size
$fileSize = (Get-Item $outputFile).Length / 1KB

Write-Host "`n=========================================" -ForegroundColor Green
Write-Host "SUCCESS!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host "`nExcel workbook created:" -ForegroundColor Cyan
Write-Host "  File: $outputFile" -ForegroundColor White
Write-Host "  Size: $([math]::Round($fileSize, 1)) KB" -ForegroundColor White
Write-Host "  Sheets: 8 (README + 7 data sheets)" -ForegroundColor White

Write-Host "`nOpening file..." -ForegroundColor Yellow
Start-Process $outputFile

Write-Host "`nPress any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

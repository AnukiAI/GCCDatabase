# GCC Field Mapping Reference - Excel Workbook Creator
# Simplified version with reliable PowerShell syntax

$ErrorActionPreference = "Stop"

Write-Host "`nGCC Field Mapping Workbook Creator" -ForegroundColor Cyan
Write-Host "====================================`n" -ForegroundColor Cyan

# Check for Excel
Write-Host "Checking for Microsoft Excel..." -ForegroundColor Yellow
try {
    $excel = New-Object -ComObject Excel.Application
    $excelVersion = $excel.Version
    $excel.Quit()
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
    Write-Host "  Excel found (Version $excelVersion)" -ForegroundColor Green
}
catch {
    Write-Host "  ERROR: Excel not installed!" -ForegroundColor Red
    Write-Host "`nPlease use the interactive HTML version instead." -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Setup
$outputPath = Join-Path $PSScriptRoot "GCC_Field_Mapping_Reference.xlsx"
Write-Host "`nCreating workbook: $outputPath" -ForegroundColor Yellow

# Create Excel
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false
$workbook = $excel.Workbooks.Add()

# Colors
$headerBlue = 2 + 43*256 + 118*65536
$white = 16777215
$lightGray = 15790320

# Helper function
function Add-Sheet {
    param($Name, $Headers, $DataRows)

    Write-Host "  Adding sheet: $Name" -ForegroundColor Gray

    if ($workbook.Sheets.Count -eq 1 -and $workbook.Sheets.Item(1).Name -eq "Sheet1") {
        $sheet = $workbook.Sheets.Item(1)
    } else {
        $sheet = $workbook.Sheets.Add([System.Reflection.Missing]::Value, $workbook.Sheets.Item($workbook.Sheets.Count))
    }
    $sheet.Name = $Name

    # Headers
    for ($i = 0; $i -lt $Headers.Count; $i++) {
        $sheet.Cells.Item(1, $i + 1) = $Headers[$i]
    }

    # Format headers
    $headerRange = $sheet.Range($sheet.Cells.Item(1,1), $sheet.Cells.Item(1, $Headers.Count))
    $headerRange.Interior.Color = $headerBlue
    $headerRange.Font.Color = $white
    $headerRange.Font.Bold = $true
    $headerRange.Font.Size = 11

    # Data
    for ($r = 0; $r -lt $DataRows.Count; $r++) {
        $row = $DataRows[$r]
        for ($c = 0; $c -lt $row.Count; $c++) {
            $sheet.Cells.Item($r + 2, $c + 1) = $row[$c]
        }

        # Alternating colors
        if (($r + 2) % 2 -eq 0) {
            $rowRange = $sheet.Range($sheet.Cells.Item($r + 2, 1), $sheet.Cells.Item($r + 2, $Headers.Count))
            $rowRange.Interior.Color = $lightGray
        }
    }

    # Format data
    $dataRange = $sheet.Range($sheet.Cells.Item(1,1), $sheet.Cells.Item($DataRows.Count + 1, $Headers.Count))
    $dataRange.Font.Size = 10
    $dataRange.WrapText = $true
    $dataRange.VerticalAlignment = -4160
    $dataRange.Borders.LineStyle = 1

    # Auto-fit with max width
    $sheet.Columns.AutoFit() | Out-Null
    for ($col = 1; $col -le $Headers.Count; $col++) {
        if ($sheet.Columns.Item($col).ColumnWidth -gt 60) {
            $sheet.Columns.Item($col).ColumnWidth = 60
        }
    }

    # Freeze panes
    $sheet.Application.ActiveWindow.SplitRow = 1
    $sheet.Application.ActiveWindow.FreezePanes = $true

    # Auto-filter
    $sheet.Range($sheet.Cells.Item(1,1), $sheet.Cells.Item($DataRows.Count + 1, $Headers.Count)).AutoFilter() | Out-Null
}

# Certificate Fields Sheet
$certHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$certData = @(
    @("Id", "Integer", "Auto", "Primary key auto-generated", "ID, CertificateID, PK", "1, 2, 3", "System-generated"),
    @("Number", "String(7)", "YES", "Unique certificate number", "CertificateNumber, CertNumber", "24-0001, 24-0366", "Format YY-NNNN, must be unique"),
    @("Status", "String(50)", "YES", "Certificate lifecycle status", "Status, State, CertStatus", "active, draft, expired", "Must be one of 14 valid values"),
    @("EffectiveDate", "Date", "YES", "Certificate start date", "EffectiveDate, StartDate, IssueDate", "2024-01-15", "Format YYYY-MM-DD, before ExpirationDate"),
    @("ExpirationDate", "Date", "YES", "Certificate end date", "ExpirationDate, EndDate, ValidUntil", "2025-01-14", "Format YYYY-MM-DD, after EffectiveDate"),
    @("Location", "String(175)", "No", "Physical location", "Location, Address, Site", "Seattle WA", "Max 175 characters"),
    @("Country", "String(100)", "No", "Country code or name", "Country, CountryCode", "US, CA, GB", "Prefer ISO 3166-1 alpha-2"),
    @("Partner", "String(100)", "No", "Partner program", "Partner, Program, Marketplace", "Amazon, Wayfair, None", "Amazon Wayfair None or Other"),
    @("CompanyId", "Integer", "YES", "Reference to Company", "CompanyID, ManufacturerID", "1, 5, 12", "Must reference existing Company.Id"),
    @("ProductId", "Integer", "YES", "Reference to Product", "ProductID, ItemID", "1, 10, 25", "Must reference existing Product.Id"),
    @("CertificationTypeId", "Integer", "YES", "Reference to CertificationType", "CertTypeID, TypeID", "1, 2, 3", "Must reference existing CertificationType.Id"),
    @("CertificateFile", "String(500)", "No", "Path to PDF certificate", "CertificateFile, PDFPath", "certificates/24-0001.pdf", "Relative path from wwwroot/uploads"),
    @("CreatedBy", "Integer", "No", "User who created certificate", "CreatedBy, CreatedByUserID", "1, 5", "Must reference existing User.Id"),
    @("CreatedAt", "DateTime", "Auto", "Creation timestamp", "CreatedDate, DateCreated", "2024-11-21 10:30:00", "System sets on insert"),
    @("UpdatedAt", "DateTime", "Auto", "Last update timestamp", "ModifiedDate, LastModified", "2024-11-21 15:45:00", "System sets on update")
)
Add-Sheet -Name "Certificate Fields" -Headers $certHeaders -DataRows $certData

# Company Fields Sheet
$companyHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$companyData = @(
    @("Id", "Integer", "Auto", "Primary key auto-generated", "ID, CompanyID, ClientID", "1, 2, 3", "System-generated"),
    @("Name", "String(255)", "YES", "Company legal name", "Name, CompanyName, BusinessName", "Acme Corporation", "Max 255 chars, must be unique"),
    @("ContactInfo", "String(255)", "No", "Contact person or info", "Contact, ContactPerson", "John Smith", "Max 255 characters"),
    @("Email", "String(255)", "No", "Company email", "Email, ContactEmail", "contact@acmecorp.com", "Must be valid email format"),
    @("Website", "String(500)", "No", "Company website URL", "Website, URL", "https://www.acmecorp.com", "Must start with http or https"),
    @("Location", "String(175)", "No", "Physical location", "Location, Address, City", "Seattle WA", "Max 175 characters"),
    @("Country", "String(100)", "No", "Country code or name", "Country, CountryCode", "US, CA, GB", "Prefer ISO 3166-1 alpha-2"),
    @("IsActive", "Boolean", "No", "Active status flag", "IsActive, Active, Enabled", "true, false", "Default true"),
    @("CreatedAt", "DateTime", "Auto", "Creation timestamp", "CreatedDate", "2024-11-21 10:30:00", "System sets on insert"),
    @("UpdatedAt", "DateTime", "Auto", "Last update timestamp", "ModifiedDate", "2024-11-21 15:45:00", "System sets on update")
)
Add-Sheet -Name "Company Fields" -Headers $companyHeaders -DataRows $companyData

# Product Fields Sheet
$productHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$productData = @(
    @("Id", "Integer", "Auto", "Primary key auto-generated", "ID, ProductID, ItemID", "1, 2, 3", "System-generated"),
    @("Name", "String(255)", "YES", "Product name", "Name, ProductName, ItemName", "Oak Hardwood Flooring", "Max 255 characters"),
    @("Description", "Text", "No", "Product description", "Description, Details", "Premium solid oak flooring", "Unlimited length"),
    @("Model", "String(100)", "No", "Product model number", "Model, ModelNumber", "OAK-HW-750", "Max 100 characters"),
    @("Sku", "String(50)", "No", "Stock keeping unit", "SKU, StockCode, ItemCode", "OAK-HW-750-NAT", "Max 50 characters"),
    @("Upc", "String(50)", "No", "Universal product code", "UPC, Barcode", "012345678905", "12 digits if provided"),
    @("Asin", "String(50)", "No", "Amazon standard ID", "ASIN, AmazonASIN", "B08N5WRWNW", "10 alphanumeric if provided"),
    @("CategoryId", "Integer", "YES", "Reference to ProductCategory", "CategoryID, Category", "1, 2, 3", "Must reference existing ProductCategory.Id"),
    @("ProductTypeId", "Integer", "No", "Reference to ProductType", "TypeID, ProductTypeID", "1, 2, 3", "Must reference existing ProductType.Id"),
    @("CompanyId", "Integer", "No", "Reference to Company", "CompanyID, ManufacturerID", "5, 12", "Must reference existing Company.Id"),
    @("IsActive", "Boolean", "No", "Active status flag", "IsActive, Active", "true, false", "Default true")
)
Add-Sheet -Name "Product Fields" -Headers $productHeaders -DataRows $productData

# User Fields Sheet
$userHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$userData = @(
    @("Id", "Integer", "Auto", "Primary key auto-generated", "ID, UserID", "1, 2, 3", "System-generated"),
    @("Username", "String(255)", "YES", "Unique username", "Username, Login", "admin, analyst", "Max 255 chars, must be unique"),
    @("Email", "String(255)", "YES", "Email address", "Email, EmailAddress", "admin@example.com", "Valid email, must be unique"),
    @("Name", "String(100)", "No", "Short name or first name", "Name, FirstName", "John, Jane", "Max 100 characters"),
    @("FullName", "String(255)", "No", "Full display name", "FullName, DisplayName", "John Smith", "Max 255 characters"),
    @("PasswordHash", "String(255)", "YES", "BCrypt password hash", "Password, PasswordHash", "Use HashGenerator tool", "Must be BCrypt hash"),
    @("Status", "String(20)", "YES", "Account status", "Status, AccountStatus", "active, inactive, locked", "Must be active inactive or locked"),
    @("Roles", "String", "YES", "Comma-separated role names", "Role, Roles, UserRole", "Admin, Analyst", "Must reference existing roles"),
    @("TwoFactorEnabled", "Boolean", "No", "2FA enabled flag", "TwoFactorEnabled, 2FA", "true, false", "Default false"),
    @("TwoFactorMethod", "String", "No", "2FA delivery method", "TwoFactorMethod, 2FAMethod", "Email, SMS, TOTP", "Default Email"),
    @("PhoneNumber", "String(20)", "No", "Phone for SMS 2FA", "Phone, PhoneNumber", "+1-555-555-5555", "Required if 2FA includes SMS"),
    @("IsActive", "Boolean", "No", "Active flag", "IsActive, Active", "true, false", "Default true")
)
Add-Sheet -Name "User Fields" -Headers $userHeaders -DataRows $userData

# Product ID Fields Sheet
$pidHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$pidData = @(
    @("Id", "Integer", "Auto", "Primary key auto-generated", "ID, ProductIDID", "1, 2, 3", "System-generated"),
    @("CertificateId", "Integer", "YES", "Reference to Certificate", "CertificateID, CertID", "1, 5, 10", "Must reference existing Certificate.Id"),
    @("ProductIdType", "String(10)", "YES", "Type of identifier", "Type, IDType", "ASIN, UPC, GTIN", "ASIN UPC GTIN EAN ISBN SKU MPN Other"),
    @("ProductIdValue", "String(50)", "YES", "Identifier value", "Value, ID, Code", "B08N5WRWNW", "Format must match type"),
    @("Status", "String(20)", "No", "Product ID status", "Status, State", "Active, Inactive", "Default Active"),
    @("Partner", "String(50)", "No", "Associated partner", "Partner, Marketplace", "Amazon, Wayfair", "Optional partner"),
    @("ProductName", "String(200)", "No", "Product name for this ID", "ProductName, Name", "Oak Flooring Natural", "Optional max 200 chars"),
    @("DisplayOrder", "Integer", "No", "Sort sequence", "DisplayOrder, SortOrder", "1, 2, 3", "Controls UI display order"),
    @("IsActive", "Boolean", "No", "Active flag", "IsActive, Active", "true, false", "Default true"),
    @("IsPublic", "Boolean", "No", "Public visibility flag", "IsPublic, Public", "true, false", "Default true")
)
Add-Sheet -Name "Product ID Fields" -Headers $pidHeaders -DataRows $pidData

# Save
Write-Host "`nSaving workbook..." -ForegroundColor Yellow
if (Test-Path $outputPath) { Remove-Item $outputPath -Force }
$workbook.SaveAs($outputPath)

# Cleanup
$workbook.Close($false)
$excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($workbook) | Out-Null
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null

$fileSize = [math]::Round((Get-Item $outputPath).Length / 1KB, 1)

Write-Host "`n=====================================" -ForegroundColor Green
Write-Host "SUCCESS!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host "`nExcel workbook created:" -ForegroundColor Cyan
Write-Host "  File: GCC_Field_Mapping_Reference.xlsx" -ForegroundColor White
Write-Host "  Size: $fileSize KB" -ForegroundColor White
Write-Host "  Sheets: 5 field mapping sheets" -ForegroundColor White
Write-Host "`nOpening file..." -ForegroundColor Yellow

Start-Process $outputPath

Write-Host "`nDone!" -ForegroundColor Green

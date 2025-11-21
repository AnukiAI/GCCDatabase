# GCC Field Mapping Reference - Complete Excel Workbook Creator
# Includes ALL entities and fields from the database schema

$ErrorActionPreference = "Stop"

Write-Host "`nGCC Complete Field Mapping Workbook Creator" -ForegroundColor Cyan
Write-Host "==========================================`n" -ForegroundColor Cyan

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
$outputPath = Join-Path $PSScriptRoot "GCC_Complete_Field_Mapping_Reference.xlsx"
Write-Host "`nCreating comprehensive workbook: $outputPath" -ForegroundColor Yellow

# Create Excel
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false
$workbook = $excel.Workbooks.Add()

# GCC Brand Colors
$headerBlue = 2 + 43*256 + 118*65536  # #002B76
$white = 16777215
$lightGray = 15790320

# Helper function to add sheet
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

# =============================================================================
# CORE ENTITIES
# =============================================================================

# 1. Certificate Fields (Complete)
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
    @("Asin", "String(50)", "No", "DEPRECATED - Use ProductIds", "ASIN, AmazonASIN", "B08N5WRWNW", "Moved to CertificateProductId table"),
    @("Upc", "String(50)", "No", "DEPRECATED - Use ProductIds", "UPC, Barcode", "012345678905", "Moved to CertificateProductId table"),
    @("Gtin", "String(50)", "No", "DEPRECATED - Use ProductIds", "GTIN, GlobalTradeItem", "00012345678905", "Moved to CertificateProductId table"),
    @("Ean", "String(50)", "No", "DEPRECATED - Use ProductIds", "EAN, EuropeanArticle", "1234567890123", "Moved to CertificateProductId table"),
    @("CreatedBy", "Integer", "No", "User who created certificate", "CreatedBy, CreatedByUserID", "1, 5", "Must reference existing User.Id"),
    @("UpdatedBy", "Integer", "No", "User who last updated", "UpdatedBy, ModifiedBy", "1, 5", "Must reference existing User.Id"),
    @("CreatedAt", "DateTime", "Auto", "Creation timestamp", "CreatedDate, DateCreated", "2024-11-21 10:30:00", "System sets on insert"),
    @("UpdatedAt", "DateTime", "Auto", "Last update timestamp", "ModifiedDate, LastModified", "2024-11-21 15:45:00", "System sets on update")
)
Add-Sheet -Name "Certificate" -Headers $certHeaders -DataRows $certData

# 2. Company Fields (Complete)
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
    @("IsDeleted", "Boolean", "No", "Soft delete flag", "IsDeleted, Deleted", "true, false", "Default false"),
    @("CreatedAt", "DateTime", "Auto", "Creation timestamp", "CreatedDate", "2024-11-21 10:30:00", "System sets on insert"),
    @("UpdatedAt", "DateTime", "Auto", "Last update timestamp", "ModifiedDate", "2024-11-21 15:45:00", "System sets on update")
)
Add-Sheet -Name "Company" -Headers $companyHeaders -DataRows $companyData

# 3. Product Fields (Complete)
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
    @("ProductSubcategoryId", "Integer", "No", "Reference to ProductSubcategory", "SubcategoryID", "1, 2, 3", "Must reference existing ProductSubcategory.Id"),
    @("CompanyId", "Integer", "No", "Reference to Company", "CompanyID, ManufacturerID", "5, 12", "Must reference existing Company.Id"),
    @("IsActive", "Boolean", "No", "Active status flag", "IsActive, Active", "true, false", "Default true"),
    @("IsDeleted", "Boolean", "No", "Soft delete flag", "IsDeleted, Deleted", "true, false", "Default false")
)
Add-Sheet -Name "Product" -Headers $productHeaders -DataRows $productData

# 4. User Fields (Complete with all security fields)
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
    @("TwoFactorSecret", "String", "No", "TOTP secret key", "TwoFactorSecret, TOTPSecret", "BASE32 encoded string", "Required if TOTP enabled"),
    @("PhoneNumber", "String(20)", "No", "Phone for SMS 2FA", "Phone, PhoneNumber", "+1-555-555-5555", "Required if 2FA includes SMS"),
    @("RecoveryCodes", "String", "No", "JSON array of recovery codes", "RecoveryCodes, BackupCodes", "JSON array", "Encrypted, one-time use"),
    @("SecurityQuestion1", "String", "No", "First security question", "SecurityQ1", "Mother maiden name?", "Optional security recovery"),
    @("SecurityAnswer1", "String", "No", "Answer to question 1", "SecurityA1", "Hashed answer", "Hashed for security"),
    @("SecurityQuestion2", "String", "No", "Second security question", "SecurityQ2", "First pet name?", "Optional security recovery"),
    @("SecurityAnswer2", "String", "No", "Answer to question 2", "SecurityA2", "Hashed answer", "Hashed for security"),
    @("FailedLoginAttempts", "Integer", "No", "Count of failed logins", "FailedAttempts", "0, 1, 2, 3", "Auto-increment on fail, reset on success"),
    @("AccountLockedUntil", "DateTime", "No", "Lock expiration time", "LockedUntil", "2024-11-21 16:00:00", "Set after max failed attempts"),
    @("LastPasswordChangeAt", "DateTime", "No", "Last password change", "LastPwdChange", "2024-11-21 10:00:00", "Track password age"),
    @("PasswordExpiresAt", "DateTime", "No", "Password expiration", "PwdExpires", "2025-05-21", "Optional password policy"),
    @("MustChangePassword", "Boolean", "No", "Force password change flag", "MustChangePwd", "true, false", "Force reset on next login"),
    @("LastLoginAt", "DateTime", "No", "Last successful login", "LastLogin", "2024-11-21 09:00:00", "Track user activity"),
    @("LastLoginIp", "String", "No", "IP of last login", "LastIP", "192.168.1.100", "Security audit"),
    @("IsActive", "Boolean", "No", "Active flag", "IsActive, Active", "true, false", "Default true"),
    @("CreatedAt", "DateTime", "Auto", "Creation timestamp", "CreatedDate", "2024-11-21 10:30:00", "System sets on insert"),
    @("UpdatedAt", "DateTime", "Auto", "Last update timestamp", "ModifiedDate", "2024-11-21 15:45:00", "System sets on update")
)
Add-Sheet -Name "User" -Headers $userHeaders -DataRows $userData

# 5. CertificateProductId Fields (Complete)
$pidHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$pidData = @(
    @("Id", "Integer", "Auto", "Primary key auto-generated", "ID, ProductIDID", "1, 2, 3", "System-generated"),
    @("CertificateId", "Integer", "YES", "Reference to Certificate", "CertificateID, CertID", "1, 5, 10", "Must reference existing Certificate.Id"),
    @("ProductIdType", "String(10)", "YES", "Type of identifier", "Type, IDType", "ASIN, UPC, GTIN", "ASIN UPC GTIN EAN ISBN SKU MPN Other"),
    @("ProductIdValue", "String(50)", "YES", "Identifier value", "Value, ID, Code", "B08N5WRWNW", "Format must match type"),
    @("ProductSku", "String(100)", "No", "Product SKU", "SKU, StockCode", "OAK-750-NAT", "Optional, max 100 chars"),
    @("Status", "String(20)", "No", "Product ID status", "Status, State", "Active, Inactive", "Default Active"),
    @("Partner", "String(50)", "No", "Associated partner", "Partner, Marketplace", "Amazon, Wayfair", "Optional partner"),
    @("ProductName", "String(200)", "No", "Product name for this ID", "ProductName, Name", "Oak Flooring Natural", "Optional max 200 chars"),
    @("Description", "Text", "No", "Product ID description", "Description, Notes", "Special variant info", "Unlimited length"),
    @("DisplayOrder", "Integer", "No", "Sort sequence", "DisplayOrder, SortOrder", "1, 2, 3", "Controls UI display order"),
    @("IsActive", "Boolean", "No", "Active flag", "IsActive, Active", "true, false", "Default true"),
    @("IsPublic", "Boolean", "No", "Public visibility flag", "IsPublic, Public", "true, false", "Default true"),
    @("CreatedBy", "Integer", "No", "User who created", "CreatedBy", "1, 5", "Reference to User.Id"),
    @("UpdatedBy", "Integer", "No", "User who updated", "UpdatedBy", "1, 5", "Reference to User.Id"),
    @("CreatedAt", "DateTime", "Auto", "Creation timestamp", "CreatedDate", "2024-11-21 10:30:00", "System sets on insert"),
    @("UpdatedAt", "DateTime", "Auto", "Last update timestamp", "ModifiedDate", "2024-11-21 15:45:00", "System sets on update")
)
Add-Sheet -Name "CertificateProductId" -Headers $pidHeaders -DataRows $pidData

# =============================================================================
# REFERENCE DATA ENTITIES
# =============================================================================

# 6. ProductType Fields
$ptHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$ptData = @(
    @("Id", "Integer", "Auto", "Primary key", "ID, TypeID", "1, 2, 3", "System-generated"),
    @("Name", "String(255)", "YES", "Product type name", "Name, TypeName", "Hardwood, Softwood", "Max 255 chars, required"),
    @("CategoryId", "Integer", "YES", "Reference to ProductCategory", "CategoryID", "1, 2", "Must reference ProductCategory.Id"),
    @("ParentId", "Integer", "No", "Parent type for hierarchy", "ParentID", "1, 2", "Self-reference for hierarchy"),
    @("Level", "Integer", "No", "Hierarchy level", "Level, Depth", "0, 1, 2", "0=top level, increments down"),
    @("SortOrder", "Integer", "No", "Display sort order", "SortOrder", "1, 2, 3", "Controls display sequence"),
    @("IsActive", "Boolean", "No", "Active flag", "IsActive", "true, false", "Default true")
)
Add-Sheet -Name "ProductType" -Headers $ptHeaders -DataRows $ptData

# 7. ProductSubcategory Fields
$pscHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$pscData = @(
    @("Id", "Integer", "Auto", "Primary key", "ID, SubcatID", "1, 2, 3", "System-generated"),
    @("Name", "String(255)", "YES", "Subcategory name", "Name, SubcatName", "Oak Flooring", "Max 255 chars, required"),
    @("ProductTypeId", "Integer", "YES", "Reference to ProductType", "TypeID", "1, 2", "Must reference ProductType.Id"),
    @("Description", "Text", "No", "Subcategory description", "Description", "Oak hardwood flooring products", "Unlimited"),
    @("SortOrder", "Integer", "No", "Display sort order", "SortOrder", "1, 2, 3", "Controls display sequence"),
    @("IsActive", "Boolean", "No", "Active flag", "IsActive", "true, false", "Default true")
)
Add-Sheet -Name "ProductSubcategory" -Headers $pscHeaders -DataRows $pscData

# 8. ProductIdentifier Fields
$piHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$piData = @(
    @("Id", "Integer", "Auto", "Primary key", "ID", "1, 2, 3", "System-generated"),
    @("ProductId", "Integer", "YES", "Reference to Product", "ProductID", "1, 5", "Must reference Product.Id"),
    @("IdentifierType", "String(20)", "YES", "Type of identifier", "Type, IDType", "ASIN, UPC, GTIN", "ASIN UPC GTIN EAN ISBN ISSN SKU MPN"),
    @("IdentifierValue", "String(50)", "YES", "Identifier value", "Value, Code", "B08N5WRWNW", "Format must match type"),
    @("CreatedAt", "DateTime", "Auto", "Creation timestamp", "CreatedDate", "2024-11-21 10:30:00", "System sets"),
    @("UpdatedAt", "DateTime", "Auto", "Update timestamp", "ModifiedDate", "2024-11-21 15:45:00", "System sets")
)
Add-Sheet -Name "ProductIdentifier" -Headers $piHeaders -DataRows $piData

# 9. CertificationType Fields
$ctHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$ctData = @(
    @("Id", "Integer", "Auto", "Primary key", "ID, CertTypeID", "1, 2, 3", "System-generated"),
    @("Name", "String(255)", "YES", "Certification type name", "Name, TypeName", "GreenCircle Certified", "Max 255 chars, unique"),
    @("Description", "Text", "No", "Type description", "Description", "Sustainability certification", "Unlimited"),
    @("Code", "String(50)", "No", "Short code", "Code, Abbreviation", "GCC, FSC", "Optional short identifier"),
    @("IsActive", "Boolean", "No", "Active flag", "IsActive", "true, false", "Default true")
)
Add-Sheet -Name "CertificationType" -Headers $ctHeaders -DataRows $ctData

# 10. Tag Fields
$tagHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$tagData = @(
    @("Id", "Integer", "Auto", "Primary key", "ID, TagID", "1, 2, 3", "System-generated"),
    @("Name", "String(50)", "YES", "Tag name", "Name, TagName", "Sustainable, Recycled", "Max 50 chars, unique"),
    @("TagType", "String(20)", "YES", "Tag type", "Type, Category", "internal, external", "Must be internal or external"),
    @("Description", "Text", "No", "Tag description", "Description", "Products using recycled materials", "Unlimited"),
    @("IsActive", "Boolean", "No", "Active flag", "IsActive", "true, false", "Default true")
)
Add-Sheet -Name "Tag" -Headers $tagHeaders -DataRows $tagData

# 11. CustomField Fields
$cfHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$cfData = @(
    @("Id", "Integer", "Auto", "Primary key", "ID, FieldID", "1, 2, 3", "System-generated"),
    @("Name", "String(100)", "YES", "Field name", "Name, FieldName", "Recycled Content %", "Max 100 chars"),
    @("FieldType", "String(50)", "YES", "Data type", "Type, DataType", "text, number, date, boolean, select", "text number date boolean select"),
    @("IsRequired", "Boolean", "No", "Required field flag", "Required, Mandatory", "true, false", "Default false"),
    @("IsVisible", "Boolean", "No", "Visible in UI", "Visible", "true, false", "Default true"),
    @("IsPublic", "Boolean", "No", "Publicly visible", "Public", "true, false", "Default true"),
    @("SortOrder", "Integer", "No", "Display order", "SortOrder", "1, 2, 3", "Controls display sequence"),
    @("Options", "Text", "No", "JSON options for select", "Options", "JSON array", "Required for select type"),
    @("DefaultValue", "String", "No", "Default value", "DefaultValue", "0, false, N/A", "Optional default"),
    @("ValidationRules", "Text", "No", "JSON validation rules", "Validation", "JSON object", "Custom validation logic"),
    @("Section", "String(100)", "No", "Section grouping", "Section, Group", "Core Info, Additional", "Groups fields in UI"),
    @("CertificateTypeId", "Integer", "YES", "Reference to CertificationType", "CertTypeID", "1, 2", "Must reference CertificationType.Id")
)
Add-Sheet -Name "CustomField" -Headers $cfHeaders -DataRows $cfData

# 12. CustomFieldValue Fields
$cfvHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$cfvData = @(
    @("Id", "Integer", "Auto", "Primary key", "ID", "1, 2, 3", "System-generated"),
    @("CertificateId", "Integer", "YES", "Reference to Certificate", "CertificateID", "1, 5", "Must reference Certificate.Id"),
    @("FieldId", "Integer", "YES", "Reference to CustomField", "FieldID", "1, 2", "Must reference CustomField.Id"),
    @("TextValue", "Text", "No", "Text field value", "TextValue, StringValue", "Sample text", "For text fields"),
    @("NumberValue", "Decimal", "No", "Number field value", "NumberValue", "75.5, 100", "For number fields"),
    @("DateValue", "DateTime", "No", "Date field value", "DateValue", "2024-11-21", "For date fields"),
    @("BooleanValue", "Boolean", "No", "Boolean field value", "BooleanValue", "true, false", "For boolean fields")
)
Add-Sheet -Name "CustomFieldValue" -Headers $cfvHeaders -DataRows $cfvData

# 13. CertificateDocument Fields
$cdHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$cdData = @(
    @("Id", "Integer", "Auto", "Primary key", "ID, DocumentID", "1, 2, 3", "System-generated"),
    @("CertificateId", "Integer", "YES", "Reference to Certificate", "CertificateID", "1, 5", "Must reference Certificate.Id"),
    @("FileName", "String(255)", "YES", "Stored filename", "FileName", "24-0001_v1.pdf", "System-generated unique name"),
    @("OriginalFileName", "String(255)", "YES", "Original uploaded name", "OriginalName", "certificate.pdf", "User's original filename"),
    @("FilePath", "String(500)", "YES", "File storage path", "Path, Location", "uploads/certs/2024/11/", "Relative path from wwwroot"),
    @("ContentType", "String(100)", "YES", "MIME type", "ContentType, MimeType", "application/pdf", "MIME type from upload"),
    @("FileSize", "Long", "YES", "File size in bytes", "Size, FileSize", "524288", "Bytes, validate max size"),
    @("FileHash", "String(64)", "No", "SHA256 hash", "Hash, Checksum", "SHA256 hash string", "For integrity verification"),
    @("DocumentType", "String(50)", "No", "Document category", "Type, Category", "Certificate, Supporting, Amendment", "Classification"),
    @("Description", "String(500)", "No", "Document description", "Description", "Original certificate PDF", "Optional notes"),
    @("Version", "Integer", "No", "Document version", "Version", "1, 2, 3", "Default 1, increment on update"),
    @("IsActive", "Boolean", "No", "Active flag", "IsActive", "true, false", "Default true"),
    @("IsPublic", "Boolean", "No", "Public visibility", "IsPublic", "true, false", "Default false"),
    @("IsVirusScanned", "Boolean", "No", "Scanned flag", "VirusScanned", "true, false", "Security check status"),
    @("PassedVirusScan", "Boolean", "No", "Scan result", "Passed", "true, false", "Scan pass/fail"),
    @("VirusScanDate", "DateTime", "No", "Scan timestamp", "ScanDate", "2024-11-21 10:30:00", "When scanned"),
    @("UploadedBy", "Integer", "No", "User who uploaded", "UploadedBy", "1, 5", "Reference to User.Id"),
    @("UploadedAt", "DateTime", "Auto", "Upload timestamp", "UploadedDate", "2024-11-21 10:30:00", "System sets"),
    @("UpdatedAt", "DateTime", "Auto", "Update timestamp", "ModifiedDate", "2024-11-21 15:45:00", "System sets")
)
Add-Sheet -Name "CertificateDocument" -Headers $cdHeaders -DataRows $cdData

# 14. CertificateStatusHistory Fields
$cshHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$cshData = @(
    @("Id", "Integer", "Auto", "Primary key", "ID", "1, 2, 3", "System-generated"),
    @("CertificateId", "Integer", "YES", "Reference to Certificate", "CertificateID", "1, 5", "Must reference Certificate.Id"),
    @("CertificateNumber", "String(7)", "No", "Cert number denormalized", "CertNumber", "24-0001", "For query performance"),
    @("PreviousStatus", "String(50)", "YES", "Status before change", "OldStatus", "draft, active", "One of 14 valid statuses"),
    @("NewStatus", "String(50)", "YES", "Status after change", "NewStatus", "active, expired", "One of 14 valid statuses"),
    @("Reason", "String(1000)", "No", "Change reason", "Reason", "Approved by manager", "Max 1000 chars"),
    @("Notes", "Text", "No", "Additional notes", "Notes, Comments", "Additional context", "Unlimited"),
    @("ChangedBy", "Integer", "YES", "User who changed", "ChangedBy", "1, 5", "Reference to User.Id"),
    @("ChangedByUsername", "String(255)", "No", "Username denormalized", "Username", "admin", "For query performance"),
    @("ChangedAt", "DateTime", "YES", "Change timestamp", "ChangedDate", "2024-11-21 10:30:00", "When change occurred"),
    @("IpAddress", "String(50)", "No", "User IP address", "IP", "192.168.1.100", "Security audit"),
    @("UserAgent", "String(500)", "No", "Browser user agent", "UserAgent", "Mozilla/5.0...", "Security audit"),
    @("ChangeSource", "String(50)", "No", "Source of change", "Source", "Manual, Automated, API, Import", "Change trigger"),
    @("DurationInPreviousStatus", "Integer", "No", "Days in previous status", "Duration", "15, 30, 60", "Calculated duration"),
    @("IsAutomatedTransition", "Boolean", "No", "Automated flag", "Automated", "true, false", "Manual vs automated"),
    @("WorkflowStep", "String(100)", "No", "Related workflow step", "Step", "approval, review", "Workflow context")
)
Add-Sheet -Name "CertificateStatusHistory" -Headers $cshHeaders -DataRows $cshData

# 15. Role Fields
$roleHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$roleData = @(
    @("Id", "Integer", "Auto", "Primary key", "ID, RoleID", "1, 2, 3", "System-generated"),
    @("Name", "String(50)", "YES", "Role name", "Name, RoleName", "Admin, Analyst, External", "Max 50 chars, unique"),
    @("Description", "Text", "No", "Role description", "Description", "Administrator with full access", "Unlimited"),
    @("IsSystem", "Boolean", "No", "System role flag", "System", "true, false", "System roles cannot be deleted"),
    @("IsActive", "Boolean", "No", "Active flag", "IsActive", "true, false", "Default true"),
    @("CreatedAt", "DateTime", "Auto", "Creation timestamp", "CreatedDate", "2024-11-21 10:30:00", "System sets"),
    @("UpdatedAt", "DateTime", "Auto", "Update timestamp", "ModifiedDate", "2024-11-21 15:45:00", "System sets")
)
Add-Sheet -Name "Role" -Headers $roleHeaders -DataRows $roleData

# 16. Permission Fields
$permHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$permData = @(
    @("Id", "Integer", "Auto", "Primary key", "ID, PermissionID", "1, 2, 3", "System-generated"),
    @("Name", "String(255)", "YES", "Permission name", "Name, PermName", "View Certificates", "Max 255 chars"),
    @("Codename", "String(100)", "YES", "Permission code", "Code, Codename", "view_certificates", "Max 100 chars, unique"),
    @("Category", "String(100)", "No", "Permission category", "Category, Module", "Certificates, Users", "Grouping for UI"),
    @("Description", "Text", "No", "Description", "Description", "Allows viewing certificates", "Unlimited"),
    @("IsActive", "Boolean", "No", "Active flag", "IsActive", "true, false", "Default true"),
    @("CreatedAt", "DateTime", "Auto", "Creation timestamp", "CreatedDate", "2024-11-21 10:30:00", "System sets")
)
Add-Sheet -Name "Permission" -Headers $permHeaders -DataRows $permData

# 17. Partner Fields
$partnerHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$partnerData = @(
    @("Id", "Integer", "Auto", "Primary key", "ID, PartnerID", "1, 2, 3", "System-generated"),
    @("Name", "String(100)", "YES", "Partner name", "Name, PartnerName", "Amazon, Wayfair", "Max 100 chars, unique"),
    @("Description", "String(500)", "No", "Partner description", "Description", "E-commerce marketplace", "Max 500 chars"),
    @("Website", "String(500)", "No", "Partner website", "Website, URL", "https://www.amazon.com", "Valid URL"),
    @("ContactInfo", "String(255)", "No", "Contact information", "Contact", "John Smith", "Max 255 chars"),
    @("Email", "String(255)", "No", "Partner email", "Email", "partner@example.com", "Valid email"),
    @("IsActive", "Boolean", "No", "Active flag", "IsActive", "true, false", "Default true"),
    @("IsDeleted", "Boolean", "No", "Soft delete flag", "IsDeleted", "true, false", "Default false"),
    @("SortOrder", "Integer", "No", "Display order", "SortOrder", "1, 2, 3", "Controls display sequence"),
    @("CreatedAt", "DateTime", "Auto", "Creation timestamp", "CreatedDate", "2024-11-21 10:30:00", "System sets"),
    @("UpdatedAt", "DateTime", "Auto", "Update timestamp", "ModifiedDate", "2024-11-21 15:45:00", "System sets")
)
Add-Sheet -Name "Partner" -Headers $partnerHeaders -DataRows $partnerData

# 18. ApiKey Fields
$apiHeaders = @("GCC Field", "Data Type", "Required", "Description", "Common Source Names", "Example Values", "Validation Rules")
$apiData = @(
    @("Id", "Integer", "Auto", "Primary key", "ID", "1, 2, 3", "System-generated"),
    @("Name", "String(100)", "YES", "API key name", "Name, KeyName", "Production API Key", "Max 100 chars"),
    @("Key", "String(500)", "YES", "API key value", "Key, ApiKey", "gcc_live_1234567890abcdef", "Generated, max 500 chars"),
    @("KeyHash", "String(450)", "No", "Hashed key", "Hash", "SHA256 hash", "For secure storage"),
    @("Description", "String(500)", "No", "Key description", "Description", "Production access key", "Max 500 chars"),
    @("Environment", "String(50)", "No", "Environment", "Env", "Production, Staging, Development", "Deployment environment"),
    @("Scopes", "Text", "No", "Allowed scopes", "Scopes, Permissions", "read:certificates, write:certificates", "Comma-separated"),
    @("IsActive", "Boolean", "No", "Active flag", "IsActive", "true, false", "Default true"),
    @("CreatedBy", "String(50)", "No", "Creator", "CreatedBy", "admin", "User who created"),
    @("CreatedAt", "DateTime", "Auto", "Creation timestamp", "CreatedDate", "2024-11-21 10:30:00", "System sets"),
    @("LastUsedAt", "DateTime", "No", "Last usage", "LastUsed", "2024-11-21 15:00:00", "Updates on use"),
    @("ExpiresAt", "DateTime", "No", "Expiration", "ExpiresDate", "2025-11-21", "Optional expiration"),
    @("RateLimitPerMinute", "Integer", "No", "Minute rate limit", "RateMinute", "60", "Requests per minute"),
    @("RateLimitPerHour", "Integer", "No", "Hour rate limit", "RateHour", "1000", "Requests per hour"),
    @("RateLimitPerDay", "Integer", "No", "Day rate limit", "RateDay", "10000", "Requests per day"),
    @("AllowedIPs", "Text", "No", "Allowed IP addresses", "IPs", "192.168.1.0/24", "Comma-separated CIDR"),
    @("AllowedOrigins", "Text", "No", "Allowed CORS origins", "Origins", "https://example.com", "Comma-separated URLs"),
    @("RequireTLS", "Boolean", "No", "Require HTTPS", "TLS", "true, false", "Default true"),
    @("IsTemporarilyBlocked", "Boolean", "No", "Blocked flag", "Blocked", "true, false", "Security block"),
    @("BlockedUntil", "DateTime", "No", "Block expiration", "BlockUntil", "2024-11-21 16:00:00", "Temporary block"),
    @("SecurityViolationCount", "Integer", "No", "Violation count", "Violations", "0, 1, 2", "Track violations"),
    @("LastSecurityIncident", "DateTime", "No", "Last incident", "LastIncident", "2024-11-21 12:00:00", "Security audit"),
    @("RequestCount", "Integer", "No", "Total requests", "Requests", "1000, 5000", "Usage tracking"),
    @("UsageCostPerRequest", "Decimal", "No", "Cost per request", "Cost", "0.001", "Billing tracking")
)
Add-Sheet -Name "ApiKey" -Headers $apiHeaders -DataRows $apiData

# =============================================================================
# REFERENCE SHEETS
# =============================================================================

# 19. Product ID Types Reference
$idTypeHeaders = @("Type", "Description", "Format", "Length", "Example", "Validation Pattern")
$idTypeData = @(
    @("ASIN", "Amazon Standard Identification Number", "10 alphanumeric", "10", "B08N5WRWNW", "^[A-Z0-9]{10}$"),
    @("UPC", "Universal Product Code", "12 digits", "12", "012345678905", "^\d{12}$"),
    @("GTIN", "Global Trade Item Number", "8, 12, 13, or 14 digits", "8-14", "00012345678905", "^\d{8}|\d{12}|\d{13}|\d{14}$"),
    @("EAN", "European Article Number", "13 digits", "13", "1234567890123", "^\d{13}$"),
    @("ISBN", "International Standard Book Number", "10 or 13 digits", "10-13", "978-0-596-52068-7", "^\d{10}|\d{13}$"),
    @("SKU", "Stock Keeping Unit", "Alphanumeric", "Varies", "ABC-123-XYZ", "Custom per company"),
    @("MPN", "Manufacturer Part Number", "Alphanumeric", "Varies", "OAK-HW-750", "Custom per manufacturer"),
    @("Other", "Other identifier type", "Custom", "Varies", "CUSTOM-ID-123", "No validation")
)
Add-Sheet -Name "ProductID Types" -Headers $idTypeHeaders -DataRows $idTypeData

# 20. Certificate Status Values
$statusHeaders = @("Status", "Description", "Category", "Can Edit", "Legacy Mappings")
$statusData = @(
    @("draft", "Initial state, not submitted", "In Progress", "YES", "draft, new, pending"),
    @("pending_review", "Submitted, awaiting review", "In Progress", "YES", "submitted, pending review"),
    @("under_review", "Currently being reviewed", "In Progress", "NO", "in review, reviewing"),
    @("pending_approval", "Awaiting approval", "In Progress", "NO", "pending approval, awaiting approval"),
    @("approved", "Approved, not yet issued", "Approved", "NO", "approved"),
    @("issued", "Issued, not yet active", "Active", "NO", "issued"),
    @("active", "Active and valid", "Active", "NO", "active, valid, current"),
    @("expiring_soon", "Expiring within 30 days", "Active", "NO", "expiring, near expiration"),
    @("renewal_requested", "Renewal in progress", "In Progress", "YES", "renewing, renewal pending"),
    @("suspended", "Temporarily suspended", "Inactive", "NO", "suspended, on hold"),
    @("revoked", "Permanently revoked", "Inactive", "NO", "revoked, cancelled"),
    @("expired", "Past expiration date", "Inactive", "NO", "expired, lapsed"),
    @("archived", "Archived for record", "Inactive", "NO", "archived, historical"),
    @("cancelled", "Cancelled before issue", "Inactive", "NO", "cancelled, rejected")
)
Add-Sheet -Name "Certificate Statuses" -Headers $statusHeaders -DataRows $statusData

# 21. Country Codes (ISO 3166-1 alpha-2 - Top 50)
$countryHeaders = @("Code", "Country Name", "Region")
$countryData = @(
    @("US", "United States", "North America"),
    @("CA", "Canada", "North America"),
    @("GB", "United Kingdom", "Europe"),
    @("DE", "Germany", "Europe"),
    @("FR", "France", "Europe"),
    @("IT", "Italy", "Europe"),
    @("ES", "Spain", "Europe"),
    @("NL", "Netherlands", "Europe"),
    @("SE", "Sweden", "Europe"),
    @("NO", "Norway", "Europe"),
    @("DK", "Denmark", "Europe"),
    @("FI", "Finland", "Europe"),
    @("PL", "Poland", "Europe"),
    @("BE", "Belgium", "Europe"),
    @("AT", "Austria", "Europe"),
    @("CH", "Switzerland", "Europe"),
    @("IE", "Ireland", "Europe"),
    @("PT", "Portugal", "Europe"),
    @("CN", "China", "Asia"),
    @("JP", "Japan", "Asia"),
    @("KR", "South Korea", "Asia"),
    @("IN", "India", "Asia"),
    @("SG", "Singapore", "Asia"),
    @("MY", "Malaysia", "Asia"),
    @("TH", "Thailand", "Asia"),
    @("VN", "Vietnam", "Asia"),
    @("ID", "Indonesia", "Asia"),
    @("PH", "Philippines", "Asia"),
    @("AU", "Australia", "Oceania"),
    @("NZ", "New Zealand", "Oceania"),
    @("BR", "Brazil", "South America"),
    @("MX", "Mexico", "North America"),
    @("AR", "Argentina", "South America"),
    @("CL", "Chile", "South America"),
    @("CO", "Colombia", "South America"),
    @("PE", "Peru", "South America"),
    @("ZA", "South Africa", "Africa"),
    @("EG", "Egypt", "Africa"),
    @("NG", "Nigeria", "Africa"),
    @("KE", "Kenya", "Africa"),
    @("AE", "United Arab Emirates", "Middle East"),
    @("SA", "Saudi Arabia", "Middle East"),
    @("IL", "Israel", "Middle East"),
    @("TR", "Turkey", "Middle East"),
    @("RU", "Russia", "Europe/Asia"),
    @("UA", "Ukraine", "Europe"),
    @("CZ", "Czech Republic", "Europe"),
    @("HU", "Hungary", "Europe"),
    @("RO", "Romania", "Europe"),
    @("GR", "Greece", "Europe")
)
Add-Sheet -Name "Country Codes" -Headers $countryHeaders -DataRows $countryData

# 22. Tag Types Reference
$tagTypeHeaders = @("Tag Type", "Description", "Visibility", "Example Tags")
$tagTypeData = @(
    @("internal", "Internal classification tags", "Admin/Analyst only", "high-priority, needs-review, archived"),
    @("external", "Public-facing tags", "All users", "sustainable, recycled-content, fsc-certified")
)
Add-Sheet -Name "Tag Types" -Headers $tagTypeHeaders -DataRows $tagTypeData

# 23. Custom Field Types Reference
$cfTypeHeaders = @("Field Type", "Description", "Storage Column", "Example Use")
$cfTypeData = @(
    @("text", "Text string field", "TextValue", "Product description, notes"),
    @("number", "Numeric value", "NumberValue", "Percentage, weight, count"),
    @("date", "Date/DateTime value", "DateValue", "Manufacture date, inspection date"),
    @("boolean", "True/False value", "BooleanValue", "Is certified, requires inspection"),
    @("select", "Single selection from list", "TextValue", "Color, size, grade")
)
Add-Sheet -Name "Custom Field Types" -Headers $cfTypeHeaders -DataRows $cfTypeData

# 24. Document Types Reference
$docTypeHeaders = @("Document Type", "Description", "Typical Use")
$docTypeData = @(
    @("Certificate", "Primary certificate PDF", "Main certification document"),
    @("Supporting", "Supporting documentation", "Test results, specifications, compliance docs"),
    @("Amendment", "Certificate amendment", "Changes or updates to original certificate"),
    @("Renewal", "Renewal documentation", "Documents for certificate renewal process"),
    @("Audit", "Audit reports", "Third-party audit or inspection reports"),
    @("Other", "Other document types", "Miscellaneous supporting files")
)
Add-Sheet -Name "Document Types" -Headers $docTypeHeaders -DataRows $docTypeData

# Save
Write-Host "`nSaving comprehensive workbook..." -ForegroundColor Yellow
if (Test-Path $outputPath) { Remove-Item $outputPath -Force }
$workbook.SaveAs($outputPath)

# Cleanup
$workbook.Close($false)
$excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($workbook) | Out-Null
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null

$fileSize = [math]::Round((Get-Item $outputPath).Length / 1KB, 1)
$sheetCount = 24

Write-Host "`n=========================================" -ForegroundColor Green
Write-Host "SUCCESS - COMPREHENSIVE WORKBOOK CREATED!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host "`nExcel workbook created:" -ForegroundColor Cyan
Write-Host "  File: GCC_Complete_Field_Mapping_Reference.xlsx" -ForegroundColor White
Write-Host "  Size: $fileSize KB" -ForegroundColor White
Write-Host "  Sheets: $sheetCount comprehensive sheets" -ForegroundColor White
Write-Host "`nIncludes:" -ForegroundColor Cyan
Write-Host "  - 18 Entity Field Mappings (ALL fields)" -ForegroundColor White
Write-Host "  - 6 Reference Data Sheets" -ForegroundColor White
Write-Host "  - Complete validation rules" -ForegroundColor White
Write-Host "  - Common source name mappings" -ForegroundColor White
Write-Host "  - Example values for all fields" -ForegroundColor White
Write-Host "`nOpening file..." -ForegroundColor Yellow

Start-Process $outputPath

Write-Host "`nDone!" -ForegroundColor Green

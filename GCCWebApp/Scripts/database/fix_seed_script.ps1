# PowerShell script to fix FULL_TEST_DATA_SEED.sql for production compatibility
# Adds IsDeleted, CreatedAt, UpdatedAt columns

$inputFile = "Database_Scripts\FULL_TEST_DATA_SEED.sql"
$outputFile = "FULL_TEST_DATA_SEED_PRODUCTION.sql"

Write-Host "Reading $inputFile..."
$content = Get-Content $inputFile -Raw

Write-Host "Applying fixes..."

# Fix Companies INSERT
$content = $content -replace 'INSERT INTO dbo\.Companies \(Id, Name, ContactInfo, Location, Country, IsActive, Website\)', 'INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt)'

# Fix each Company VALUES line - add ", 0, " before the last 3 fields (for NULL website), ", GETDATE(), GETDATE()" at the end
# Pattern: ', 1, NULL),' becomes ', 1, 0, NULL, GETDATE(), GETDATE()),'
$content = $content -replace "', 1, NULL\),", "', 1, 0, NULL, GETDATE(), GETDATE()),"

# Pattern: ', 1, ''https' becomes ', 1, 0, ''https' and add GETDATE at end
$content = $content -replace "', 1, '(https[^']+)'\),", "', 1, 0, '$1', GETDATE(), GETDATE()),"

# Fix Products INSERT
$content = $content -replace 'INSERT INTO dbo\.Products \(Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive\)', 'INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)'

# Fix Products INSERT (alternative format without ProductTypeId)
$content = $content -replace 'INSERT INTO dbo\.Products \(Id, Name, Description, CategoryId, CompanyId, IsActive\)', 'INSERT INTO dbo.Products (Id, Name, Description, CategoryId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)'

# Fix Product VALUES lines - add IsDeleted=0, CreatedAt, UpdatedAt
# Pattern ending with: CompanyId, 1); becomes: CompanyId, 1, 0, GETDATE(), GETDATE());
$content = $content -replace '(\d+), 1\);', '$1, 1, 0, GETDATE(), GETDATE());'

# Fix Product VALUES lines with NULL CompanyId
$content = $content -replace 'NULL, 1\);', 'NULL, 1, 0, GETDATE(), GETDATE());'

Write-Host "Writing $outputFile..."
$content | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host "Done! Production-compatible seed script created: $outputFile"
Write-Host ""
Write-Host "Note: Certificates table doesn't need IsPublic column (doesn't exist in production)"

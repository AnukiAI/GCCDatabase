# Fix EF Core 6.0 compatibility issues

Write-Host "Fixing EF Core 6.0 method names..." -ForegroundColor Green

# Files that contain ExecuteSqlAsync
$files = @(
    "Services\Performance\DatabaseOptimizationService.cs",
    "Services\ETL\DataLoader.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        # In EF Core 6.0, ExecuteSqlAsync is replaced with ExecuteSqlRawAsync
        $updated = $content -replace 'ExecuteSqlAsync', 'ExecuteSqlRawAsync'
        $updated | Set-Content $file -NoNewline
        Write-Host "Fixed ExecuteSqlAsync in: $file"
    }
}

# Fix DateDiffDay ambiguity - need to explicitly use SqlServer version
$files = @(
    "Controllers\Api\CertificateTileInternalApiController.cs",
    "Controllers\Api\CertificateTileExternalApiController.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        # Replace EF.Functions.DateDiffDay with explicit SqlServer version
        $updated = $content -replace 'EF\.Functions\.DateDiffDay', 'Microsoft.EntityFrameworkCore.SqlServerDbFunctionsExtensions.DateDiffDay(EF.Functions'
        # Add closing parenthesis after the second parameter
        $updated = $updated -replace '(DateDiffDay\(EF\.Functions, [^,]+, [^)]+)\)', '$1))'
        $updated | Set-Content $file -NoNewline
        Write-Host "Fixed DateDiffDay in: $file"
    }
}

Write-Host "Done fixing EF Core 6.0 compatibility issues!" -ForegroundColor Green
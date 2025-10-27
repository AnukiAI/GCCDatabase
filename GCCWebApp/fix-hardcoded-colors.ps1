# CSS Color Replacement Script - Priority 1 Quick Wins
# Replaces hardcoded brand colors with CSS variables

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "CSS Color Optimization - Phase 1" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$cssDir = "wwwroot\css"
$filesProcessed = 0
$totalReplacements = 0

# Brand color mappings (case-insensitive regex)
$colorMappings = @(
    @{Pattern = '#92C83E'; Replacement = 'var(--gcc-green-secondary)'; Display = '#92C83E (green)'}
    @{Pattern = '#002B76'; Replacement = 'var(--gcc-blue-dark)'; Display = '#002B76 (navy)'}
    @{Pattern = '#FF5252'; Replacement = 'var(--admin-danger)'; Display = '#FF5252 (red)'}
    @{Pattern = '#FFC107'; Replacement = 'var(--admin-warning)'; Display = '#FFC107 (yellow)'}
    @{Pattern = '#00AEEF'; Replacement = 'var(--gcc-blue-primary)'; Display = '#00AEEF (blue)'}
    @{Pattern = '#447638'; Replacement = 'var(--gcc-green-primary)'; Display = '#447638 (green-dark)'}
)

Write-Host "Processing CSS files in: $cssDir" -ForegroundColor Yellow
Write-Host "Color mappings:" -ForegroundColor Yellow
foreach ($mapping in $colorMappings) {
    Write-Host "  $($mapping.Display) -> $($mapping.Replacement)" -ForegroundColor Gray
}
Write-Host ""

# Get all CSS files recursively
$cssFiles = Get-ChildItem -Path $cssDir -Recurse -Filter *.css

foreach ($file in $cssFiles) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    $fileReplacements = 0

    # Apply all color replacements (case-insensitive)
    foreach ($mapping in $colorMappings) {
        $pattern = $mapping.Pattern
        $replacement = $mapping.Replacement

        # Use case-insensitive regex
        if ($content -match "(?i)$([regex]::Escape($pattern))") {
            $matches = ([regex]::Matches($content, "(?i)$([regex]::Escape($pattern))")).Count
            $content = $content -replace "(?i)$([regex]::Escape($pattern))", $replacement
            $fileReplacements += $matches
        }
    }

    # Only write if changes were made
    if ($content -ne $originalContent) {
        Set-Content $file.FullName -Value $content -NoNewline
        $filesProcessed++
        $totalReplacements += $fileReplacements
        $relativePath = $file.FullName -replace [regex]::Escape((Get-Location).Path + '\'), ''
        Write-Host "[UPDATED] $relativePath - $fileReplacements replacements" -ForegroundColor Green
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Files processed: $filesProcessed" -ForegroundColor Green
Write-Host "Total color replacements: $totalReplacements" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "  1. Review product-management.css for duplicate :root" -ForegroundColor White
Write-Host "  2. Test pages for visual consistency" -ForegroundColor White
Write-Host "  3. Build and run application" -ForegroundColor White
Write-Host ""

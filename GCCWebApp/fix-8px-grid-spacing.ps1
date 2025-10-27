# CSS 8px Grid System Alignment - Phase 1 (Safe Automated Replacements)
# Targets: padding, margin, gap properties only (NOT font-size, width, height, etc.)

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "CSS 8px Grid System - Phase 1" -ForegroundColor Cyan
Write-Host "Automated Safe Spacing Replacements" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$cssDir = "wwwroot\css"
$filesProcessed = 0
$totalReplacements = 0

# 8px Grid Spacing Replacements
# Format: Non-grid value -> Grid-aligned value
$spacingReplacements = @(
    @{Old = '10px'; New = '8px'; Grid = '1x8px'; Properties = 'padding|margin|gap'}
    @{Old = '15px'; New = '16px'; Grid = '2x8px'; Properties = 'padding|margin|gap'}
    @{Old = '20px'; New = '24px'; Grid = '3x8px'; Properties = 'padding|margin|gap'}
    @{Old = '25px'; New = '24px'; Grid = '3x8px'; Properties = 'padding|margin|gap'}
    @{Old = '30px'; New = '32px'; Grid = '4x8px'; Properties = 'padding|margin|gap'}
    @{Old = '35px'; New = '32px'; Grid = '4x8px'; Properties = 'padding|margin|gap'}
    @{Old = '40px'; New = '48px'; Grid = '6x8px'; Properties = 'padding|margin|gap'}
    @{Old = '45px'; New = '48px'; Grid = '6x8px'; Properties = 'padding|margin|gap'}
    @{Old = '50px'; New = '48px'; Grid = '6x8px'; Properties = 'padding|margin|gap'}
    @{Old = '55px'; New = '56px'; Grid = '7x8px'; Properties = 'padding|margin|gap'}
    @{Old = '60px'; New = '64px'; Grid = '8x8px'; Properties = 'padding|margin|gap'}
    @{Old = '70px'; New = '72px'; Grid = '9x8px'; Properties = 'padding|margin|gap'}
    @{Old = '80px'; New = '80px'; Grid = '10x8px'; Properties = 'padding|margin|gap'}
    @{Old = '90px'; New = '88px'; Grid = '11x8px'; Properties = 'padding|margin|gap'}
    @{Old = '100px'; New = '96px'; Grid = '12x8px'; Properties = 'padding|margin|gap'}
)

Write-Host "8px Grid Spacing Mappings:" -ForegroundColor Yellow
foreach ($mapping in $spacingReplacements) {
    if ($mapping.Old -ne $mapping.New) {
        Write-Host "  $($mapping.Old) -> $($mapping.New) ($($mapping.Grid))" -ForegroundColor Gray
    }
}
Write-Host ""

# Get all CSS files recursively
$cssFiles = Get-ChildItem -Path $cssDir -Recurse -Filter *.css

foreach ($file in $cssFiles) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    $fileReplacements = 0

    # Apply spacing replacements
    foreach ($mapping in $spacingReplacements) {
        $oldValue = $mapping.Old
        $newValue = $mapping.New
        $gridNote = $mapping.Grid
        $properties = $mapping.Properties

        # Only replace if the values are different
        if ($oldValue -ne $newValue) {
            # Pattern: (padding|margin|gap): 20px; or padding-top: 20px;
            # Matches: padding: 20px, margin-left: 20px, gap: 20px, etc.
            # Does NOT match: font-size: 20px, width: 20px, height: 20px

            $pattern = "(?<property>(?:$properties)(?:-(?:top|right|bottom|left|inline|block|start|end))?):\s*$([regex]::Escape($oldValue))"

            if ($content -match $pattern) {
                $matches = ([regex]::Matches($content, $pattern)).Count

                # Replace with grid-aligned value
                $content = $content -replace $pattern, "`${property}: $newValue /* $gridNote */"

                $fileReplacements += $matches
            }
        }
    }

    # Only write if changes were made
    if ($content -ne $originalContent) {
        Set-Content $file.FullName -Value $content -NoNewline
        $filesProcessed++
        $totalReplacements += $fileReplacements
        $relativePath = $file.FullName -replace [regex]::Escape((Get-Location).Path + '\'), ''
        Write-Host "[UPDATED] $relativePath" -ForegroundColor Green
        Write-Host "          $fileReplacements spacing values aligned to 8px grid" -ForegroundColor Gray
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Phase 1 Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Files processed: $filesProcessed" -ForegroundColor Green
Write-Host "Total spacing alignments: $totalReplacements" -ForegroundColor Green

Write-Host "`nWhat was changed:" -ForegroundColor Yellow
Write-Host "  + padding: 20px -> padding: 24px (3x8px)" -ForegroundColor Gray
Write-Host "  + margin-top: 15px -> margin-top: 16px (2x8px)" -ForegroundColor Gray
Write-Host "  + gap: 10px -> gap: 8px (1x8px)" -ForegroundColor Gray
Write-Host "  + And similar safe spacing replacements..." -ForegroundColor Gray

Write-Host "`nWhat was NOT changed:" -ForegroundColor Yellow
Write-Host "  - font-size: 20px (typography, not spacing)" -ForegroundColor Gray
Write-Host "  - width: 30px (dimension, not spacing)" -ForegroundColor Gray
Write-Host "  - height: 50px (dimension, not spacing)" -ForegroundColor Gray
Write-Host "  - max-width: 200px (dimension, not spacing)" -ForegroundColor Gray

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "  1. Review files for visual consistency" -ForegroundColor White
Write-Host "  2. Manual review of top 10 files with most violations" -ForegroundColor White
Write-Host "  3. Build and test application" -ForegroundColor White
Write-Host ""

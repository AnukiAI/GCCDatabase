# Fix product-management.css - Remove duplicate :root and update variable references
Write-Host "`nFixing product-management.css variable references..." -ForegroundColor Cyan

$file = "wwwroot\css\product-management.css"
$content = Get-Content $file -Raw

# Variable replacements (order matters - do most specific first)
$replacements = @{
    'var(--green-primary)' = 'var(--gcc-green-secondary)'
    'var(--green-secondary)' = 'var(--gcc-green-primary)'
    'var(--blue-dark)' = 'var(--gcc-blue-dark)'
    'var(--blue-primary)' = 'var(--gcc-blue-primary)'
    'var(--text-color)' = 'var(--admin-text-primary)'
    'var(--text-secondary)' = 'var(--admin-text-secondary)'
    'var(--light-gray)' = 'var(--admin-bg-secondary)'
    'var(--border-color)' = 'var(--admin-border-primary)'
    'var(--white)' = 'var(--admin-bg-primary)'
    'var(--warning)' = 'var(--admin-danger)'
    'var(--success)' = 'var(--admin-success)'
}

Write-Host "Replacing custom variable references..." -ForegroundColor Yellow
$totalReplacements = 0
foreach ($old in $replacements.Keys) {
    $new = $replacements[$old]
    $matches = ([regex]::Matches($content, [regex]::Escape($old))).Count
    if ($matches -gt 0) {
        $content = $content -replace [regex]::Escape($old), $new
        $totalReplacements += $matches
        Write-Host "  $old -> $new ($matches replacements)" -ForegroundColor Gray
    }
}

# Remove the duplicate :root block (lines 2-15)
Write-Host "`nRemoving duplicate :root block..." -ForegroundColor Yellow
$content = $content -replace '(?s)/\* Product Management Styles \*/\s*:root\s*\{[^}]+\}', '/* Product Management Styles */'

# Save the file
Set-Content $file -Value $content -NoNewline

Write-Host "`nCompleted!" -ForegroundColor Green
Write-Host "Total variable replacements: $totalReplacements" -ForegroundColor Green
Write-Host "Duplicate :root block removed" -ForegroundColor Green
Write-Host ""

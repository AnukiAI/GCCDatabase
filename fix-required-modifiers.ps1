# Script to remove 'required' modifiers from properties for C# 10 compatibility

Write-Host "Fixing 'required' modifiers for C# 10 compatibility..." -ForegroundColor Green

# Get all .cs files in Models directory
$modelFiles = Get-ChildItem -Path ".\Models" -Filter "*.cs" -Recurse

foreach ($file in $modelFiles) {
    Write-Host "Processing: $($file.Name)" -ForegroundColor Yellow

    # Read file content
    $content = Get-Content $file.FullName -Raw

    # Remove 'required' modifier from properties
    # Pattern matches: public required <type> <name>
    $updatedContent = $content -replace '\bpublic\s+required\s+', 'public '

    # Save if changes were made
    if ($content -ne $updatedContent) {
        $updatedContent | Set-Content $file.FullName -NoNewline
        Write-Host "  ✓ Fixed required modifiers in $($file.Name)" -ForegroundColor Green
    } else {
        Write-Host "  - No changes needed in $($file.Name)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "✅ Completed fixing required modifiers!" -ForegroundColor Green
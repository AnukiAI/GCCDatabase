# Fix required modifiers for C# 10 compatibility
Write-Host "Removing required modifiers from Model files..."

$files = @(
    "Models\Certificate.cs",
    "Models\CertificateListViewModel.cs",
    "Models\Company.cs",
    "Models\Partner.cs",
    "Models\User.cs",
    "Models\Product.cs",
    "Models\ProductType.cs",
    "Models\CertificateProductId.cs",
    "Models\CertificateHistoryViewModel.cs",
    "Models\Dashboard\DashboardViewModel.cs",
    "Models\Dashboard\CertificateSummaryStats.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        $updated = $content -replace 'public required ', 'public '
        $updated | Set-Content $file -NoNewline
        Write-Host "Fixed: $file"
    }
}

Write-Host "Done!"
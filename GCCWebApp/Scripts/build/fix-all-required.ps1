# Fix all required modifiers for C# 10 compatibility
Write-Host "Removing ALL required modifiers from project files..."

$files = @(
    "Models\ApiKey.cs",
    "Services\Performance\PerformanceMonitoringService.cs",
    "Models\CertificateViewModel.cs",
    "Models\Dashboard.cs",
    "Controllers\Api\CertificatesApiController.cs",
    "Controllers\Api\v1\ApiKeysController.cs",
    "Controllers\Api\v1\MetricsController.cs",
    "Controllers\Api\v1\AuthController.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        $updated = $content -replace 'public required ', 'public '
        $updated | Set-Content $file -NoNewline
        Write-Host "Fixed: $file"
    }
}

Write-Host "Done fixing all required modifiers!"
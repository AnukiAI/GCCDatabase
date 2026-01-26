# Check CSV file encoding and content
$filePath = "test_import.csv"
$bytes = [System.IO.File]::ReadAllBytes($filePath)

Write-Host "File size: $($bytes.Length) bytes"
Write-Host ""

# Count line endings
$lf = ($bytes | Where-Object { $_ -eq 0x0A }).Count
$cr = ($bytes | Where-Object { $_ -eq 0x0D }).Count
Write-Host "Line endings: LF=$lf, CR=$cr"

# Show content as text
Write-Host ""
Write-Host "=== File content (raw) ==="
$content = [System.IO.File]::ReadAllText($filePath)
$lines = $content -split "`r?`n"
$lineNum = 1
foreach ($line in $lines) {
    Write-Host "Line $lineNum ($($line.Length) chars): $($line.Substring(0, [Math]::Min(100, $line.Length)))..."
    $lineNum++
}

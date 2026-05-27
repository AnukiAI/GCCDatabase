$path = Join-Path $PSScriptRoot '11-edge-large-250.csv'
$lines = @('Name,Company,Description,Product Type,Status')
for ($i = 1; $i -le 250; $i++) {
    $n = '{0:D3}' -f $i
    $lines += "QCTest Bulk $n,Acme Corporation,Bulk row $n,Personal Care,Active"
}
$lines | Set-Content -Path $path -Encoding utf8
"Wrote $($lines.Count - 1) data rows to $path"

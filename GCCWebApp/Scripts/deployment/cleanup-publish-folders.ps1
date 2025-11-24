# Clean up problematic publish folders with long paths
Write-Host "Cleaning up old publish folders..." -ForegroundColor Cyan

$foldersToDelete = @(
    'publish',
    'publish-selfcontained',
    'publish-selfcontained-x86'
)

foreach ($folder in $foldersToDelete) {
    if (Test-Path $folder) {
        Write-Host "  Deleting $folder..." -ForegroundColor Yellow

        # Try to delete, if it fails due to path length, use robocopy trick
        try {
            Remove-Item $folder -Recurse -Force -ErrorAction Stop
            Write-Host "    Deleted successfully" -ForegroundColor Green
        } catch {
            Write-Host "    Using alternative method (robocopy)..." -ForegroundColor Yellow

            # Create empty folder
            $emptyDir = "empty_temp_$([guid]::NewGuid().ToString().Substring(0,8))"
            New-Item -ItemType Directory -Path $emptyDir -Force | Out-Null

            # Use robocopy to mirror empty folder (effectively deleting content)
            robocopy $emptyDir $folder /MIR /R:0 /W:0 /NFL /NDL /NJH /NJS | Out-Null

            # Remove both folders
            Remove-Item $emptyDir -Force
            Remove-Item $folder -Force

            Write-Host "    Deleted using robocopy method" -ForegroundColor Green
        }
    } else {
        Write-Host "  $folder not found (already clean)" -ForegroundColor Gray
    }
}

Write-Host "`nCleanup complete!" -ForegroundColor Green
Write-Host "You can now reload the project in your IDE." -ForegroundColor Cyan

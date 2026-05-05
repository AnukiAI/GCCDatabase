<#
.SYNOPSIS
  Build a Plesk-ready deployment zip for GCCWebApp.

.DESCRIPTION
  Pinned single-folder deploy script. Replaces the historical pattern of
  per-run timestamped folders accumulating at the project root (publish/,
  publish-YYYYMMDD-HHMM/, deploy-staging/, deploy-staging-*/, etc.).

  Convention:
    - Staging folder:  deployment-drops/staging/   (cleared each run)
    - Output zip:      deployment-drops/GCCWebApp-deploy-YYYYMMDD-HHMM.zip

  Both paths are gitignored (.gitignore: deployment-drops/).
  The pubxml excludes the same paths from being re-published if a stale
  copy lingers (Properties/PublishProfiles/PleskTestEnvironment.pubxml).

.PARAMETER PublishProfile
  Which publish profile to use. Default is PleskTestEnvironment.

.PARAMETER Configuration
  Build configuration. Default is Release.

.PARAMETER SkipUploadsBackupPrompt
  Skip the prompt that reminds you to back up the SERVER wwwroot/uploads/
  before deploying. Use only when you have already done the backup.

.EXAMPLE
  pwsh -File Scripts/deploy.ps1
  # Builds, publishes, stages, zips. Output: deployment-drops/GCCWebApp-deploy-<ts>.zip

.EXAMPLE
  pwsh -File Scripts/deploy.ps1 -PublishProfile AccuWebProduction
  # Use the production publish profile instead of test env.

.NOTES
  Memory: feedback_backup_uploads_before_deploy.md
    "Back up uploads before Plesk deploy. Zip the server wwwroot/uploads/
     before every deploy; publish can overwrite user-uploaded PDFs."
  Memory: project_plesk_deploy_env.md
    "PleskTestEnvironment.pubxml must set EnvironmentName=Production;
     placeholder appsettings.Test.json breaks DB + AAD B2C."
#>
[CmdletBinding()]
param(
    [string]$PublishProfile = "PleskTestEnvironment",
    [string]$Configuration = "Release",
    [switch]$SkipUploadsBackupPrompt
)

$ErrorActionPreference = "Stop"

# --- Resolve paths ---
$scriptRoot = $PSScriptRoot
$projectRoot = Split-Path -Parent $scriptRoot
$csproj = Join-Path $projectRoot "GCCWebApp.csproj"
if (-not (Test-Path $csproj)) {
    Write-Error "GCCWebApp.csproj not found at $csproj. Run this script from the project's Scripts/ folder."
    exit 1
}

$pubxml = Join-Path $projectRoot "Properties\PublishProfiles\$PublishProfile.pubxml"
if (-not (Test-Path $pubxml)) {
    Write-Error "Publish profile not found: $pubxml"
    exit 1
}

$dropsRoot   = Join-Path $projectRoot "deployment-drops"
$stagingDir  = Join-Path $dropsRoot "staging"
$timestamp   = Get-Date -Format "yyyyMMdd-HHmm"
$zipName     = "GCCWebApp-deploy-$timestamp.zip"
$zipPath     = Join-Path $dropsRoot $zipName

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  GCCWebApp deploy build" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Profile:    $PublishProfile" -ForegroundColor Gray
Write-Host "  Config:     $Configuration" -ForegroundColor Gray
Write-Host "  Project:    $projectRoot" -ForegroundColor Gray
Write-Host "  Staging:    $stagingDir" -ForegroundColor Gray
Write-Host "  Output zip: $zipPath" -ForegroundColor Gray
Write-Host ""

# --- Pre-flight: server uploads backup reminder ---
if (-not $SkipUploadsBackupPrompt) {
    Write-Host "*** PRE-DEPLOY CHECKLIST ***" -ForegroundColor Yellow
    Write-Host "  [ ] Have you ZIPPED the SERVER wwwroot/uploads/ folder?" -ForegroundColor Yellow
    Write-Host "      Publishing can overwrite user-uploaded PDFs (lost all prod" -ForegroundColor Yellow
    Write-Host "      uploads once on 2026-04-23 -- exclusion is in pubxml but the" -ForegroundColor Yellow
    Write-Host "      backup-first rule still applies as defense-in-depth)." -ForegroundColor Yellow
    Write-Host ""
    $resp = Read-Host "  Type 'yes' if uploads are backed up (or any other key to abort)"
    if ($resp -ne "yes") {
        Write-Host "Aborted by user. Back up wwwroot/uploads/ on the server, then re-run." -ForegroundColor Red
        exit 1
    }
    Write-Host ""
}

# --- Pre-flight: stop dev server on 5101 if running ---
$listener = netstat -ano | Select-String ":5101.*LISTENING"
if ($listener) {
    $devPid = ($listener[0] -split '\s+')[-1]
    Write-Host "Found dev server listening on :5101 (PID $devPid). Stopping..." -ForegroundColor Yellow
    try {
        Stop-Process -Id $devPid -Force -ErrorAction Stop
        Start-Sleep -Seconds 2
        Write-Host "  Stopped." -ForegroundColor Green
    } catch {
        Write-Warning "Could not stop PID $devPid : $($_.Exception.Message)"
    }
}

# --- Clear stale staging ---
if (Test-Path $stagingDir) {
    Write-Host "Clearing stale staging at $stagingDir..." -ForegroundColor Gray
    Remove-Item -Path $stagingDir -Recurse -Force
}
New-Item -Path $stagingDir -ItemType Directory -Force | Out-Null

# --- Restore + publish ---
Write-Host "Running dotnet publish..." -ForegroundColor Gray
$publishArgs = @(
    "publish",
    "`"$csproj`"",
    "-c", $Configuration,
    "/p:PublishProfile=$PublishProfile",
    "/p:PublishDir=`"$stagingDir\`"",
    "--nologo"
)
$publishProc = Start-Process -FilePath "dotnet" -ArgumentList $publishArgs -NoNewWindow -PassThru -Wait
if ($publishProc.ExitCode -ne 0) {
    Write-Error "dotnet publish failed (exit code $($publishProc.ExitCode))."
    exit $publishProc.ExitCode
}

# --- Sanity check: staging shouldn't be empty ---
$stagedFileCount = (Get-ChildItem -Path $stagingDir -Recurse -File).Count
if ($stagedFileCount -lt 50) {
    Write-Warning "Staging folder has only $stagedFileCount files -- that looks low. Check before zipping."
    $cont = Read-Host "Continue with zip? (yes/no)"
    if ($cont -ne "yes") { exit 1 }
}
Write-Host "  Published $stagedFileCount files to staging." -ForegroundColor Green

# --- Sanity check: confirm wwwroot/uploads/ is NOT in staging ---
$stagedUploads = Join-Path $stagingDir "wwwroot\uploads"
if (Test-Path $stagedUploads) {
    $uploadFileCount = (Get-ChildItem -Path $stagedUploads -Recurse -File -ErrorAction SilentlyContinue).Count
    if ($uploadFileCount -gt 0) {
        Write-Error "ABORT: wwwroot/uploads/ is in staging with $uploadFileCount files. pubxml exclusion is broken -- fix before deploying or you will overwrite prod uploads."
        exit 1
    }
}

# --- Zip ---
if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force
}
Write-Host "Compressing staging to zip..." -ForegroundColor Gray
Compress-Archive -Path "$stagingDir\*" -DestinationPath $zipPath -CompressionLevel Optimal
$zipMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  BUILD COMPLETE" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  Zip:  $zipPath" -ForegroundColor Green
Write-Host "  Size: $zipMB MB" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Upload the zip to Plesk via the file manager (or your usual method)" -ForegroundColor Gray
Write-Host "  2. Extract on server, overwriting existing files" -ForegroundColor Gray
Write-Host "  3. Recycle the IIS app pool" -ForegroundColor Gray
Write-Host "  4. Smoke test: hit the site root and /api/v1/certificates with the partner key" -ForegroundColor Gray
Write-Host "  5. Append a stakeholder line to docs/tracking/shipped-YYYY-MM.md" -ForegroundColor Gray
Write-Host ""
Write-Host "Older zips in deployment-drops/ accumulate over time -- feel free to delete" -ForegroundColor DarkGray
Write-Host "any zip older than 7 days; the most recent is sufficient for rollback context." -ForegroundColor DarkGray
Write-Host ""

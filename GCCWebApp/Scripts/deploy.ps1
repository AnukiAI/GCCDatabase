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
    Write-Host "  [ ] DEF-208 one-time migration (this deploy ONLY):" -ForegroundColor Cyan
    Write-Host "      After extracting the zip, move uploaded files OUT of wwwroot/uploads/" -ForegroundColor Cyan
    Write-Host "      to App_Data/uploads/ on the server:" -ForegroundColor Cyan
    Write-Host "        cd <site root>" -ForegroundColor DarkCyan
    Write-Host "        New-Item -ItemType Directory -Path App_Data -Force | Out-Null" -ForegroundColor DarkCyan
    Write-Host "        if (Test-Path wwwroot\uploads) {" -ForegroundColor DarkCyan
    Write-Host "          Move-Item wwwroot\uploads App_Data\uploads -Force" -ForegroundColor DarkCyan
    Write-Host "          Write-Host 'Moved uploads to App_Data'" -ForegroundColor DarkCyan
    Write-Host "        }" -ForegroundColor DarkCyan
    Write-Host "      Run BEFORE recycling the app pool. Subsequent deploys are no-ops" -ForegroundColor Cyan
    Write-Host "      because new uploads write to App_Data directly." -ForegroundColor Cyan
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

# --- Clear stale staging (and any trailing-space siblings from prior bugs) ---
# A previous version of this script passed "/p:PublishDir=`"$dir\`"" to dotnet, which
# the arg parser collapsed into a literal folder named "staging --nologo " (trailing
# space). NTFS allows trailing-space names but Win32 can't open them, so a single
# leftover broke MSBuild glob enumeration project-wide (MSB3552 on **/*.resx).
# Wipe any sibling whose name starts with "staging" to defuse that ahead of the run.
if (Test-Path $dropsRoot) {
    Get-ChildItem -LiteralPath $dropsRoot -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.PSIsContainer -and $_.Name -like 'staging*' } |
        ForEach-Object {
            $longPath = "\\?\$($_.FullName)"  # \\?\ prefix opens trailing-space names
            Write-Host "Clearing stale staging-prefixed dir: $($_.Name)" -ForegroundColor Gray
            Remove-Item -LiteralPath $longPath -Recurse -Force -ErrorAction SilentlyContinue
        }
}
New-Item -Path $stagingDir -ItemType Directory -Force | Out-Null

# --- Restore + publish to the pubxml's default location, then copy into staging ---
# Earlier versions overrode PublishDir on the command line, but `dotnet`'s arg parser
# treats "\\\"" inside "/p:PublishDir=`"$path\`"" as an escaped quote, swallowing the
# next argument into the path. Sidestep the entire class of bug by letting publish
# write to its default location and copying the result into our canonical staging.
Write-Host "Running dotnet publish..." -ForegroundColor Gray
$defaultPublishDir = Join-Path $projectRoot "bin\$Configuration\net8.0\win-x64\publish"
if (Test-Path $defaultPublishDir) {
    Remove-Item -Path $defaultPublishDir -Recurse -Force -ErrorAction SilentlyContinue
}
$publishArgs = @(
    "publish",
    $csproj,
    "-c", $Configuration,
    "/p:PublishProfile=$PublishProfile",
    "--verbosity", "minimal"
)
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed (exit code $LASTEXITCODE)."
    exit $LASTEXITCODE
}
if (-not (Test-Path $defaultPublishDir)) {
    Write-Error "Publish exited 0 but default publish dir not found: $defaultPublishDir"
    exit 1
}

# Copy publish output into staging (robocopy /MIR for speed + correct attrs)
Write-Host "Copying publish output into staging..." -ForegroundColor Gray
$rcArgs = @($defaultPublishDir, $stagingDir, '/MIR', '/NFL', '/NDL', '/NJH', '/NJS', '/NC', '/NS', '/NP')
& robocopy @rcArgs | Out-Null
# robocopy exit codes: 0-7 are success variants; >=8 indicates failure
if ($LASTEXITCODE -ge 8) {
    Write-Error "robocopy failed copying publish to staging (exit $LASTEXITCODE)."
    exit 1
}
$LASTEXITCODE = 0  # reset since robocopy success codes aren't 0

# --- Sanity check: staging shouldn't be empty ---
$stagedFileCount = (Get-ChildItem -Path $stagingDir -Recurse -File).Count
if ($stagedFileCount -lt 50) {
    Write-Error "ABORT: staging has only $stagedFileCount files -- publish or copy failed silently. Investigate before re-running."
    exit 1
}
Write-Host "  Published $stagedFileCount files to staging." -ForegroundColor Green

# --- Generate idempotent EF migrations script and bundle in zip ---
# Added 2026-05-13 after the cert-tile outage caused by missing migration
# 20260512011258_AddCertificationResultsNumeric on prod. Going forward every
# deploy zip carries migrations/apply-migrations.sql so the schema can be
# brought up to date BEFORE the new code starts handling requests against it.
# The generated script is idempotent — each migration block is guarded by
# IF NOT EXISTS (SELECT ... FROM __EFMigrationsHistory ...) so re-running is safe.
Write-Host "Checking dotnet-ef tool..." -ForegroundColor Gray
$efVersion = & dotnet ef --version 2>$null
if ($LASTEXITCODE -ne 0 -or -not $efVersion) {
    Write-Error @"
ABORT: dotnet-ef tool is not installed (or not on PATH).
Install it once with:
    dotnet tool install --global dotnet-ef --version 8.0.11
Then re-run this deploy script.
"@
    exit 1
}
# $efVersion can be a multi-line banner; take the last non-empty line as the version
$efVersionLine = ($efVersion | Where-Object { $_ -match '\S' } | Select-Object -Last 1).Trim()
Write-Host "  dotnet-ef: $efVersionLine" -ForegroundColor Gray

$migrationsDir = Join-Path $stagingDir "migrations"
New-Item -Path $migrationsDir -ItemType Directory -Force | Out-Null
$migrationSqlPath = Join-Path $migrationsDir "apply-migrations.sql"

Write-Host "Generating idempotent EF migrations script..." -ForegroundColor Gray
# Note: NO --no-build. The publish step above wrote its artifacts to
# bin/Release/net8.0/win-x64/publish/, but dotnet-ef resolves the host
# project assembly out of bin/Release/net8.0/ (no RID subfolder). Without
# letting EF build, it cannot find GCCWebApp.deps.json and aborts. The
# incremental build EF triggers is fast (sub-second on a warm tree).
& dotnet ef migrations script `
    --idempotent `
    --project $csproj `
    --configuration $Configuration `
    --output $migrationSqlPath
if ($LASTEXITCODE -ne 0) {
    Write-Error "ABORT: dotnet ef migrations script failed (exit $LASTEXITCODE). See above for the EF tool's error output."
    exit $LASTEXITCODE
}
if (-not (Test-Path $migrationSqlPath)) {
    Write-Error "ABORT: dotnet ef returned success but no script was produced at $migrationSqlPath."
    exit 1
}
$migrationBytes = (Get-Item $migrationSqlPath).Length
$migrationLines = (Get-Content $migrationSqlPath | Measure-Object -Line).Lines
Write-Host "  Generated: $migrationSqlPath" -ForegroundColor Green
Write-Host "  $migrationLines lines, $([math]::Round($migrationBytes / 1KB, 1)) KB" -ForegroundColor Gray

# Drop a tiny README next to the SQL so the server-side operator (or future-self)
# knows how to apply it without digging through deploy logs.
$migrationReadme = Join-Path $migrationsDir "README.md"
$readmeContent = @"
# Apply pending EF migrations

Run this **before** recycling the IIS app pool so the new code doesn't query
schema that doesn't exist yet (the cause of the 2026-05-13 cert-tile outage).

## sqlcmd

``````
sqlcmd -S <server>,<port> -d <database> -U <user> -P <pwd> -I -i apply-migrations.sql -b
``````

The ``-I`` flag turns on ``QUOTED_IDENTIFIER`` (required if any migration adds
an indexed computed column — error 1934 otherwise).

## SSMS

Open ``apply-migrations.sql`` in SSMS, connect to the target DB, run.

## Idempotency

Every migration block is guarded by ``IF NOT EXISTS (SELECT ... FROM
__EFMigrationsHistory ...)`` so re-running this script is safe. If ``apply``
errors with ``column already exists`` style messages, the prod schema has
drifted ahead of ``__EFMigrationsHistory`` — generate a per-object guarded
script (see ``Scripts/database/202605*.sql`` in the repo for examples).

Generated $(Get-Date -Format 'yyyy-MM-dd HH:mm zzz') by Scripts/deploy.ps1.
"@
$readmeContent | Out-File -FilePath $migrationReadme -Encoding utf8 -NoNewline

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
Write-Host "  3. Apply DB migrations BEFORE recycling the app pool:" -ForegroundColor Yellow
Write-Host "       sqlcmd -S <server>,<port> -d <db> -U <user> -P <pwd> \" -ForegroundColor Gray
Write-Host "         -I -i migrations\apply-migrations.sql -b" -ForegroundColor Gray
Write-Host "       (See migrations\README.md inside the extracted zip for details.)" -ForegroundColor Gray
Write-Host "  4. Recycle the IIS app pool" -ForegroundColor Gray
Write-Host "  5. Smoke test: hit the site root and /api/v1/certificates with the partner key" -ForegroundColor Gray
Write-Host "  6. Append a stakeholder line to docs/tracking/shipped-YYYY-MM.md" -ForegroundColor Gray
Write-Host ""
Write-Host "Older zips in deployment-drops/ accumulate over time -- feel free to delete" -ForegroundColor DarkGray
Write-Host "any zip older than 7 days; the most recent is sufficient for rollback context." -ForegroundColor DarkGray
Write-Host ""

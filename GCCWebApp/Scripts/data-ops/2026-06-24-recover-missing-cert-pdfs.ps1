<#
.SYNOPSIS
  Recover the 11 certificate PDFs the prod DB references but that 404 on download
  (QC #198 / 2026-06-15 incident). RUN THIS ON THE PLESK SERVER.

.DESCRIPTION
  Non-destructive. For each of the 11 known-missing files it searches the site tree
  (and any backup roots you pass) for the exact filename, and COPIES the first match it
  finds into the served uploads folder (App_Data/uploads/certificates by default, or
  whatever you point -ServedDir at — e.g. the DEF-286 FileStorage:StoragePath location).
  It never deletes or overwrites a file that is already in place. It prints a clear
  FOUND / RESTORED / STILL-MISSING summary so you know exactly what remains to restore
  from an older backup or re-upload.

.PARAMETER SiteRoot
  Root of the deployed app on the server (contains App_Data, wwwroot). Default: current dir.

.PARAMETER ServedDir
  Folder the running app actually serves from. Default: <SiteRoot>\App_Data\uploads\certificates.
  If you set FileStorage:StoragePath (DEF-286), point this at <StoragePath>\certificates instead.

.PARAMETER SearchRoots
  Extra folders to scan for surviving copies (old backups, App_Data snapshots, httpdocs, etc.).

.EXAMPLE
  .\2026-06-24-recover-missing-cert-pdfs.ps1 -SiteRoot 'C:\inetpub\gccwebapp' -SearchRoots 'D:\backups\uploads-2026-06-14'
#>
param(
  [string]$SiteRoot = (Get-Location).Path,
  [string]$ServedDir = '',
  [string[]]$SearchRoots = @()
)

# The 11 files the DB references but the app can't find (from prod-pdf-path-compare.js,
# 2026-06-24). Globs by cert number so a differing timestamp still matches.
$wanted = @(
  '28-0002_*.pdf','26-0160_*.pdf','26-0173_*.pdf','26-0174_*.pdf',
  '26-0169_*.pdf','26-0177_*.pdf','26-10120_*.pdf',
  '26-0004_*.pdf','26-0005_*.pdf','25-8760_*.pdf','26-0006_*.pdf'
)

if ([string]::IsNullOrWhiteSpace($ServedDir)) {
  $ServedDir = Join-Path $SiteRoot 'App_Data\uploads\certificates'
}
if (-not (Test-Path $ServedDir)) { New-Item -ItemType Directory -Force -Path $ServedDir | Out-Null }

# Where to look: the site tree plus any backup roots provided.
$roots = @($SiteRoot) + $SearchRoots | Where-Object { $_ -and (Test-Path $_) } | Select-Object -Unique

Write-Host "Served dir : $ServedDir"
Write-Host "Search roots: $($roots -join '; ')"
Write-Host ("-" * 70)

$restored = @(); $already = @(); $missing = @()

foreach ($glob in $wanted) {
  $cert = $glob.Split('_')[0]

  # Already present and resolvable in the served dir?
  $inPlace = Get-ChildItem -Path $ServedDir -Filter $glob -File -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($inPlace) { $already += $cert; Write-Host "ALREADY  $cert  ($($inPlace.Name))"; continue }

  # Search every root for a surviving copy.
  $found = $null
  foreach ($r in $roots) {
    $found = Get-ChildItem -Path $r -Filter $glob -File -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) { break }
  }

  if ($found) {
    $dest = Join-Path $ServedDir $found.Name
    Copy-Item -Path $found.FullName -Destination $dest -Force
    $restored += $cert
    Write-Host "RESTORED $cert  <- $($found.FullName)"
  } else {
    $missing += $cert
    Write-Host "MISSING  $cert  (no surviving copy found in any search root)"
  }
}

Write-Host ("-" * 70)
Write-Host "Already in place : $($already.Count)  [$($already -join ', ')]"
Write-Host "Restored now     : $($restored.Count)  [$($restored -join ', ')]"
Write-Host "Still missing    : $($missing.Count)  [$($missing -join ', ')]"
Write-Host ""
Write-Host "Next: from a dev box run  Scripts/smoke-prod/prod-pdf-path-compare.js  to confirm"
Write-Host "downloads now return 200. Files still missing need an older backup or re-upload."
if ($missing.Count -gt 0) { exit 2 } else { exit 0 }

@echo off
REM ===========================================================================
REM  Apply CR-037 (CertificateCountries) to the TEST database.
REM
REM  Reads the connection string out of appsettings.Production.json, so there is
REM  NO password to paste and nothing to hand-edit. The 2026-09-02 attempt that
REM  appeared to succeed but did not was a literal "<pw from ...>" placeholder
REM  reaching sqlcmd.
REM
REM  Runs apply-migrations-scoped.sql (79 lines, CR-037 only). Do NOT use
REM  apply-migrations.sql in this folder - that is the full-history script and it
REM  aborts at line 11 on 'Invalid column name UserCompanyId', because SQL Server
REM  compiles each batch before IF NOT EXISTS can skip it.
REM
REM  Usage:  Scripts\database\deploy-2026-09-02\apply.cmd
REM ===========================================================================
setlocal
set "ROOT=%~dp0..\..\.."
set "SQLFILE=%~dp0apply-migrations-scoped.sql"

if not exist "%SQLFILE%" (
  echo ERROR: not found: %SQLFILE%
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "$cfg = Join-Path '%ROOT%' 'appsettings.Production.json';" ^
  "if (-not (Test-Path $cfg)) { Write-Host ('ERROR: config not found: ' + $cfg); exit 1 };" ^
  "$cs = [regex]::Match((Get-Content $cfg -Raw), '\"DefaultConnection\"\s*:\s*\"([^\"]+)\"').Groups[1].Value;" ^
  "if (-not $cs) { Write-Host 'ERROR: DefaultConnection not found in config'; exit 1 };" ^
  "$s = [regex]::Match($cs,'Server=tcp:([^;]+)').Groups[1].Value;" ^
  "$d = [regex]::Match($cs,'Database=([^;]+)').Groups[1].Value;" ^
  "$u = [regex]::Match($cs,'User Id=([^;]+)').Groups[1].Value;" ^
  "$p = [regex]::Match($cs,'Password=([^;]+)').Groups[1].Value;" ^
  "Write-Host ('target : ' + $s + ' / ' + $d);" ^
  "Write-Host ('script : %SQLFILE%');" ^
  "Write-Host '';" ^
  "sqlcmd -S $s -d $d -U $u -P $p -I -b -N -C -i '%SQLFILE%';" ^
  "$code = $LASTEXITCODE;" ^
  "Write-Host '';" ^
  "if ($code -eq 0) { Write-Host 'RESULT: APPLIED (exit 0)' } else { Write-Host ('RESULT: FAILED (exit ' + $code + ') - the error text above is what matters') };" ^
  "exit $code"

endlocal & exit /b %ERRORLEVEL%

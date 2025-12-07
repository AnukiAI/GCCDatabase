# CSS Missing from Publish - Issue Report

**Date**: December 5, 2025
**Issue**: Certificate Tile View Internal not displaying correctly on production
**Root Cause**: `certificatetileview.css` missing from publish-production folder

## Problem Description

The Certificate Tile View Internal page (`/Certificate/CertificateTileViewInternal`) is not displaying the same styling on production as it does locally.

## Investigation

### Files Checked

| File | Source (wwwroot) | Publish-Production | Status |
|------|------------------|-------------------|--------|
| `certificatetileview.css` | 33,611 bytes (Nov 18) | **MISSING** | ISSUE |
| `certificateTileViewExternal.css` | 7,198 bytes | Present | OK |

### Views Using This CSS

The following views reference `~/css/certificatetileview.css`:

1. `Views/Certificate/CertificateTileView.cshtml` (line 5)
2. `Views/Certificate/CertificateTileViewInternal.cshtml` (line 14)
3. `Views/Certificate/CertificateTileViewExternal.cshtml` (line 13)

All three views depend on this file for proper styling.

## Root Cause

The `publish-production` folder was created from an older publish or the CSS file was not included during the last `dotnet publish` command. The publish folder has not been refreshed with the latest source files.

## Solution

1. **Clean the publish folder** - Remove stale publish-production folder
2. **Full rebuild** - Run clean build and publish
3. **Verify all files** - Check that all CSS/JS files are present

## Fix Commands

```bash
cd GCCWebApp

# 1. Remove old publish folder
rmdir /s /q publish-production

# 2. Clean build
dotnet clean -c Release

# 3. Restore and build
dotnet restore
dotnet build -c Release

# 4. Publish fresh
dotnet publish GCCWebApp.csproj -c Release -r win-x64 --self-contained false -o publish-production

# 5. Create logs folder
mkdir publish-production\logs

# 6. Verify CSS file exists
dir publish-production\wwwroot\css\certificatetileview.css
```

## Verification

After fresh publish, verify:
```bash
# File should exist and be ~33KB
ls -la publish-production/wwwroot/css/certificatetileview.css
```

## Prevention

1. Always run a **clean publish** before deployment
2. Use the `Scripts/uat/deploy-uat.cmd` script which removes old publish folder
3. Verify critical files before uploading to production

## Related Files

- Source: `wwwroot/css/certificatetileview.css`
- Views: `Views/Certificate/CertificateTileViewInternal.cshtml`
- Deployment: `Scripts/uat/deploy-uat.cmd`

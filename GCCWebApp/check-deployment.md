# IIS Deployment Checklist

## Quick Checks

1. **Verify IIS can see the files**
   - Navigate to C:\inetpub\wwwroot\[your-app-folder]
   - Confirm GCCWebApp.exe exists
   - Run: GCCWebApp.exe --urls "http://localhost:5000"
   - Does it start? What error shows?

2. **Check Event Viewer**
   - Open Event Viewer (eventvwr.msc)
   - Windows Logs → Application
   - Look for recent errors from "IIS AspNetCore Module"
   - What does the error say?

3. **Verify App Pool**
   - Open IIS Manager
   - Application Pools → [Your App Pool]
   - .NET CLR Version = "No Managed Code" ✓
   - Identity = "ApplicationPoolIdentity" ✓
   - Start Mode = "AlwaysRunning" ✓

4. **Check Application Event Log**
   ```powershell
   Get-EventLog -LogName Application -Source "IIS AspNetCore Module*" -Newest 5 | Format-List
   ```

5. **Test Database Connection**
   ```cmd
   sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT @@VERSION"
   ```

## Send me:
- Output from running GCCWebApp.exe directly
- Event Viewer errors (Application log)
- Database connection test result

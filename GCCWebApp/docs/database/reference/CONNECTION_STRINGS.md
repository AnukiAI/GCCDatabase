# Database Connection Strings

**Last Updated:** November 17, 2025

---

## Development Environment

### ANUKI01 (Local SQL Server)

**Server:** ANUKI01
**Database:** GCCDatabase_Dev
**Authentication:** SQL Server Authentication

**Credentials:**
- **User:** GCC_DevUser
- **Password:** DevP@ss2024!SQL

**Connection String:**
```
Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false
```

**Configuration File:**
```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false"
  },
  "DatabaseProvider": "SqlServer"
}
```

**sqlcmd Access:**
```bash
sqlcmd -S ANUKI01 -U GCC_DevUser -P "DevP@ss2024!SQL" -d GCCDatabase_Dev
```

---

## Production Environment

### AccuWebHosting (Remote SQL Server)

**Server:** 68.71.129.120,1533
**Database:** gccwebap_certificates
**Authentication:** SQL Server Authentication

**Credentials:**
- **User:** GCC_adminxyz123
- **Password:** Q3Ez4e!gE_47KNi

**Connection String:**
```
Server=68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=Q3Ez4e!gE_47KNi;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false
```

**Configuration File:**
```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=Q3Ez4e!gE_47KNi;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false"
  },
  "DatabaseProvider": "SqlServer"
}
```

**sqlcmd Access:**
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates
```

**SSMS Connection:**
- Server name: `68.71.129.120,1533`
- Authentication: SQL Server Authentication
- Login: `GCC_adminxyz123`
- Password: `Q3Ez4e!gE_47KNi`
- Database: `gccwebap_certificates`

---

## Database Provider Configuration

### SQL Server (Default)

```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;..."
  }
}
```

### SQLite (Docker/Local Testing)

```json
{
  "DatabaseProvider": "SQLite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=gcc_database.db"
  }
}
```

**Note:** SQLite is for local development only. Production uses SQL Server.

---

## Connection String Parameters

### Required Parameters

| Parameter | Description | Example |
|-----------|-------------|---------|
| `Server` | Database server address | `68.71.129.120,1533` |
| `Database` | Database name | `gccwebap_certificates` |
| `User Id` | Username | `GCC_adminxyz123` |
| `Password` | Password | `Q3Ez4e!gE_47KNi` |

### Optional but Recommended

| Parameter | Description | Value | Reason |
|-----------|-------------|-------|--------|
| `MultipleActiveResultSets` | Allow multiple queries | `true` | EF Core performance |
| `TrustServerCertificate` | Skip cert validation | `true` | Self-signed certs |
| `Encrypt` | Encrypt connection | `false` | Legacy SQL Server |

### Advanced Parameters

| Parameter | Description | Default | When to Use |
|-----------|-------------|---------|-------------|
| `Connection Timeout` | Seconds before timeout | 30 | Slow network |
| `Max Pool Size` | Max connections | 100 | High traffic |
| `Min Pool Size` | Min connections | 0 | Keep alive |
| `Pooling` | Enable connection pooling | true | Performance |

---

## Security Considerations

### Protecting Connection Strings

**NEVER commit passwords to git:**

```json
// appsettings.Production.json (GITIGNORED)
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Password=ACTUAL_PASSWORD;..."
  }
}
```

**Use environment variables in production:**

```bash
# Linux/Docker
export ConnectionStrings__DefaultConnection="Server=...;Password=...;"

# Windows
set ConnectionStrings__DefaultConnection=Server=...;Password=...;
```

**Application reads from environment:**
```csharp
// Priority: Environment > appsettings.Production.json > appsettings.json
var connectionString = Configuration.GetConnectionString("DefaultConnection");
```

### User Permissions

**Development User:** `GCC_DevUser`
- Permissions: db_owner, db_datareader, db_datawriter
- Can create/modify schema
- Can insert/update/delete data

**Production User:** `GCC_adminxyz123`
- Permissions: db_owner
- **DEFAULT_SCHEMA:** dbo (CRITICAL!)
- Full control of database

**Verify production user configuration:**
```sql
SELECT
    dp.name as UserName,
    dp.default_schema_name as DefaultSchema,
    STRING_AGG(r.name, ', ') as Roles
FROM sys.database_principals dp
LEFT JOIN sys.database_role_members drm ON dp.principal_id = drm.member_principal_id
LEFT JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
WHERE dp.name = 'GCC_adminxyz123'
GROUP BY dp.name, dp.default_schema_name;
```

---

## Testing Connection

### From Command Line

**Development:**
```bash
sqlcmd -S ANUKI01 -U GCC_DevUser -P "DevP@ss2024!SQL" -Q "SELECT @@VERSION"
```

**Production:**
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -Q "SELECT @@VERSION"
```

**Expected output:**
```
Microsoft SQL Server 2019...
```

### From Application

**Test connection at startup:**
```csharp
// Program.cs logs connection test results
// Check application logs for:
"Database connection test: SUCCESS"
```

**Entity Framework migration test:**
```bash
# Development
dotnet ef database update

# Production (specify connection)
dotnet ef database update --connection "Server=68.71.129.120,1533;..."
```

---

## Common Connection Issues

### "Login failed for user 'GCC_adminxyz123'"

**Causes:**
1. Wrong password
2. User doesn't exist
3. User not granted access to database

**Fix:**
```sql
-- Verify user exists
SELECT name FROM sys.database_principals WHERE name = 'GCC_adminxyz123';

-- Grant access if needed
CREATE USER GCC_adminxyz123 FOR LOGIN GCC_adminxyz123;
ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;
```

### "Cannot connect to server"

**Causes:**
1. Server not running
2. Firewall blocking port
3. Wrong server address

**Fix:**
```bash
# Test network connectivity
ping 68.71.129.120

# Test SQL port (Windows)
Test-NetConnection -ComputerName 68.71.129.120 -Port 1533

# Test SQL port (Linux)
nc -zv 68.71.129.120 1533
```

### "Database 'gccwebap_certificates' does not exist"

**Fix:**
```sql
-- Create database
CREATE DATABASE gccwebap_certificates;
GO

-- Then run schema scripts
```

### "A network-related or instance-specific error"

**Causes:**
1. TCP/IP not enabled on SQL Server
2. SQL Server Browser service not running
3. Named instance not configured

**Fix:**
```bash
# For named instances, use:
Server=68.71.129.120\INSTANCENAME,1533

# Or just IP and port:
Server=68.71.129.120,1533
```

---

## Firewall Configuration

### Required Ports

**SQL Server:**
- Port 1533 (production custom port)
- Port 1433 (SQL Server default, if used)

**Allow from:**
- Development machines
- Application servers
- Admin workstations

### Testing Firewall

```bash
# Windows PowerShell
Test-NetConnection -ComputerName 68.71.129.120 -Port 1533

# Linux
telnet 68.71.129.120 1533
# Or
nc -zv 68.71.129.120 1533
```

---

## Environment-Specific Configuration

### Detecting Environment

```csharp
// Program.cs
var environment = builder.Environment.EnvironmentName;
// "Development" or "Production"
```

**Set via:**

**Windows:**
```bash
set ASPNETCORE_ENVIRONMENT=Production
```

**Linux/Docker:**
```bash
export ASPNETCORE_ENVIRONMENT=Production
```

**IIS (web.config):**
```xml
<environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
```

### Configuration Layering

Priority (highest to lowest):
1. Environment variables
2. appsettings.{Environment}.json
3. appsettings.json

**Example:**
```
ASPNETCORE_ENVIRONMENT=Production
Reads: appsettings.Production.json (overrides appsettings.json)
```

---

## See Also

- [Quick Start Guide](../deployment/QUICK_START.md) - Using connection strings
- [Critical Schema Requirement](CRITICAL_SCHEMA_REQUIREMENT.md) - User configuration
- [Common Issues](../troubleshooting/COMMON_ISSUES.md) - Connection troubleshooting

---

## Database Servers Summary

| Environment | Server | Port | Database | User |
|-------------|--------|------|----------|------|
| Development | ANUKI01 | 1433 | GCCDatabase_Dev | GCC_DevUser |
| Production | 68.71.129.120 | 1533 | gccwebap_certificates | GCC_adminxyz123 |
| Docker/Local | localhost | - | gcc_database.db | - (SQLite) |

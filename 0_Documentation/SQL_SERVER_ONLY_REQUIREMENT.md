# SQL Server Only Requirement
**Document Version:** 1.0
**Date:** October 17, 2025
**Status:** MANDATORY

---

## Executive Summary

The GCC Web Application **ONLY supports Microsoft SQL Server** as its database provider. SQLite, PostgreSQL, MySQL, and all other database engines are explicitly not supported and will cause application startup failures.

### Key Requirements

1. ✅ **MUST** use Microsoft SQL Server (2019+ or SQL Server 2022)
2. ❌ **MUST NOT** use SQLite, PostgreSQL, MySQL, or any other database
3. ✅ **MUST** configure valid SQL Server connection string in `appsettings.json`
4. ✅ **MUST** use SQL scripts for data seeding (no hardcoded data in C#)

---

## Rationale

### Why SQL Server Only?

1. **Enterprise-Grade Features**
   - Advanced security (row-level security, encryption)
   - High availability and disaster recovery
   - Enterprise performance and scaling
   - Full-text search capabilities

2. **Data Integrity & Consistency**
   - Strong ACID compliance
   - Complex transaction support
   - Referential integrity enforcement
   - Advanced constraint validation

3. **Production Requirements**
   - Existing infrastructure uses SQL Server
   - Team expertise in SQL Server administration
   - Backup and restore procedures standardized for SQL Server
   - Monitoring and alerting configured for SQL Server

4. **Development & Deployment Consistency**
   - Single database engine across all environments
   - No environment-specific code paths
   - Simplified testing and validation
   - Reduced maintenance complexity

---

## Technical Implementation

### Fail-Fast Configuration

The application implements fail-fast error handling to prevent startup with unsupported databases.

**Location:** `Program.cs` (Lines 103-133)

```csharp
else
{
    // FAIL-FAST: Unrecognized connection string format
    var errorMessage = $"Invalid or unrecognized database connection string format.\n" +
        $"Connection string: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...\n\n" +
        $"REQUIRED: SQL Server connection string must contain:\n" +
        $"  - 'Server=' or 'Server=tcp:'\n" +
        $"  - 'Database='\n" +
        $"  - Either 'Integrated Security' OR 'User Id=' and 'Password='\n\n" +
        $"Example: Server=SERVERNAME;Database=DBNAME;User Id=USER;Password=PASS;...\n\n" +
        $"IMPORTANT: SQLite and other database providers are not supported.\n" +
        $"Only SQL Server is allowed for data storage.";

    Console.Error.WriteLine($"❌ CRITICAL DATABASE CONFIGURATION ERROR\n{errorMessage}");
    throw new InvalidOperationException(errorMessage);
}
```

### Connection String Validation

**Valid SQL Server Connection String Patterns:**

```csharp
// Pattern 1: Explicit TCP connection (Azure SQL, Production)
Server=tcp:68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=...;

// Pattern 2: Named instance with Windows Authentication (Development)
Server=ANUKI01;Database=GCCDatabase_Dev;Integrated Security=true;...

// Pattern 3: Named instance with SQL Authentication (Development)
Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=...;...

// Pattern 4: Default instance with SQL Authentication (Staging)
Server=STAGING_SERVER;Database=STAGING_DB;User Id=USER;Password=PASS;...
```

**Required Connection String Components:**

1. **Server Specification:**
   - `Server=` OR `Server=tcp:`
   - Examples: `Server=SERVERNAME`, `Server=tcp:68.71.129.120,1533`

2. **Database Name:**
   - `Database=DBNAME`
   - Example: `Database=GCCDatabase_Dev`

3. **Authentication Method** (one of):
   - Windows Authentication: `Integrated Security=true` OR `Trusted_Connection=true`
   - SQL Authentication: `User Id=USERNAME;Password=PASSWORD`

4. **Optional but Recommended:**
   - `MultipleActiveResultSets=true` (enables multiple result sets)
   - `TrustServerCertificate=true` (for self-signed certificates)
   - `Encrypt=true` (enables TLS encryption)

---

## Environment Configuration

### Development Environment

**File:** `appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false"
  }
}
```

**Details:**
- Server: ANUKI01 (local SQL Server instance)
- Database: GCCDatabase_Dev
- Authentication: SQL Authentication
- Encryption: Disabled (local development only)

---

### Staging Environment

**File:** `appsettings.Staging.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server={STAGING_SQL_SERVER};Database={STAGING_DATABASE_NAME};User Id={STAGING_DB_USER};Password={STAGING_DB_PASSWORD};MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=true;"
  },
  "DatabaseProvider": "SqlServer"
}
```

**Details:**
- Server: Placeholder for staging SQL Server
- Authentication: SQL Authentication
- Encryption: Enabled (TLS)

---

### Production Environment

**File:** `appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=Q3Ez4e!gE_47KNi;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
  },
  "DatabaseProvider": "SqlServer"
}
```

**Details:**
- Server: 68.71.129.120 (AccuWebHosting)
- Port: 1533 (non-standard)
- Database: gccwebap_certificates
- Authentication: SQL Authentication
- Encryption: Enabled (TLS)
- Protocol: TCP (explicit)

---

## Migration Management

### SQL-First Approach ✅

All database schema changes and data seeding **MUST** be done via SQL scripts.

**Location:** `Database_Scripts/` folder

**Key Scripts:**
```
Database_Scripts/
├── 00_CREATE_SCHEMA.sql              # Schema creation (all tables, indexes, constraints)
├── 00_DROP_ALL_TABLES_SQLSERVER.sql  # Drop all tables (for clean rebuild)
├── CERTIFICATE_FIELDS_DATA_SEED.sql  # Certificate field definitions
├── COMPANY_DATA_SEED.sql             # Company master data
├── CSF_ATTRIBUTES_DATA_SEED.sql      # CSF attribute definitions
├── DASHBOARD_DATA_SEED.sql           # Dashboard configuration data
├── FULL_TEST_DATA_SEED.sql           # Complete test dataset
├── README_DATABASE_SETUP.md          # Setup instructions
└── SQL_SEED_DATA_GUIDE.md            # SQL seeding guide
```

### Migration Execution Order

1. **Schema Creation** (First Time Setup)
   ```sql
   sqlcmd -S SERVER -d DATABASE -U USER -P PASSWORD -i 00_CREATE_SCHEMA.sql
   ```

2. **Reference Data** (Certificate Fields, CSF Attributes)
   ```sql
   sqlcmd -S SERVER -d DATABASE -U USER -P PASSWORD -i CERTIFICATE_FIELDS_DATA_SEED.sql
   sqlcmd -S SERVER -d DATABASE -U USER -P PASSWORD -i CSF_ATTRIBUTES_DATA_SEED.sql
   ```

3. **Master Data** (Companies, Products)
   ```sql
   sqlcmd -S SERVER -d DATABASE -U USER -P PASSWORD -i COMPANY_DATA_SEED.sql
   ```

4. **Test Data** (Development Only)
   ```sql
   sqlcmd -S SERVER -d DATABASE -U USER -P PASSWORD -i FULL_TEST_DATA_SEED.sql
   ```

### Idempotent Scripts Required ✅

All SQL scripts **MUST** be idempotent (safe to run multiple times).

**Example Pattern:**
```sql
-- Idempotent MERGE statement
MERGE INTO CertificateFields AS Target
USING (VALUES
    ('CertificateNumber', 'Certificate Number', 'text', 1),
    ('CompanyName', 'Company Name', 'text', 1)
) AS Source (FieldName, DisplayName, FieldType, Required)
ON Target.FieldName = Source.FieldName
WHEN MATCHED THEN
    UPDATE SET
        DisplayName = Source.DisplayName,
        FieldType = Source.FieldType,
        Required = Source.Required
WHEN NOT MATCHED THEN
    INSERT (FieldName, DisplayName, FieldType, Required)
    VALUES (Source.FieldName, Source.DisplayName, Source.FieldType, Source.Required);
```

---

## Prohibited Practices ❌

### 1. Hardcoded Data in C# Code ❌

**WRONG:**
```csharp
// ExportController.cs - SeedDatabase method
var certTypeNames = new[] {
    "FSC Chain of Custody",
    "SFI Chain of Custody",
    "PEFC Chain of Custody"
};
```

**RIGHT:**
```sql
-- CERTIFICATE_FIELDS_DATA_SEED.sql
INSERT INTO CertificationTypes (Name, Description, IsActive)
VALUES
    ('FSC Chain of Custody', 'Forest Stewardship Council Chain of Custody', 1),
    ('SFI Chain of Custody', 'Sustainable Forestry Initiative Chain of Custody', 1),
    ('PEFC Chain of Custody', 'Programme for the Endorsement of Forest Certification Chain of Custody', 1);
```

### 2. SQLite Connection Strings ❌

**WRONG:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=gcc_database.db"
  }
}
```

**Result:** Application will fail to start with error:
```
❌ CRITICAL DATABASE CONFIGURATION ERROR
Invalid or unrecognized database connection string format.
IMPORTANT: SQLite and other database providers are not supported.
Only SQL Server is allowed for data storage.
```

### 3. Database Seeding Endpoint in Production ❌

The `SeedDatabase()` endpoint in `ExportController.cs` is now secured:

**Security Measures Implemented:**
- `[Authorize(Policy = "AdminOnly")]` - Only admins can access
- Environment check prevents execution in production
- All attempts are logged for auditing

**Code (ExportController.cs:552-570):**
```csharp
[HttpPost]
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> SeedDatabase()
{
    try
    {
        // SECURITY: Prevent database seeding in production
        if (_environment.IsProduction())
        {
            _logger.LogWarning("Database seeding attempted in production environment - DENIED");
            return StatusCode(403, new {
                Error = "Database seeding is not allowed in production environment",
                Message = "Use SQL migration scripts for production data management"
            });
        }

        // SECURITY: Log seeding attempt
        _logger.LogWarning("Database seeding initiated in {Environment} environment by {User}",
            _environment.EnvironmentName,
            User.Identity?.Name ?? "Unknown");

        // ... rest of seeding code (Development only)
    }
    catch (Exception ex)
    {
        return BadRequest(new { Error = ex.Message });
    }
}
```

---

## Developer Checklist

### Before Starting Development ✅

- [ ] Verify SQL Server is installed and running
- [ ] Confirm connection string in `appsettings.Development.json`
- [ ] Test database connectivity using SQL Server Management Studio (SSMS)
- [ ] Run schema creation script: `00_CREATE_SCHEMA.sql`
- [ ] Run seed data scripts (if needed)
- [ ] Verify application starts without errors

### Before Deploying to Staging/Production ✅

- [ ] Update `appsettings.{Environment}.json` with correct SQL Server connection
- [ ] Remove or comment out any SQLite connection strings
- [ ] Verify `DatabaseProvider` is set to `"SqlServer"`
- [ ] Test connection string manually using `sqlcmd` or SSMS
- [ ] Run schema migration scripts on target SQL Server
- [ ] Verify database seeding endpoint is disabled/secured
- [ ] Confirm fail-fast error handling is active (no SQLite fallback)

### SQL Script Development Guidelines ✅

- [ ] All scripts must be idempotent (safe to run multiple times)
- [ ] Use MERGE statements instead of INSERT for reference data
- [ ] Include error handling (`BEGIN TRY ... END TRY`)
- [ ] Document script purpose and dependencies
- [ ] Test scripts in development before applying to staging/production
- [ ] Version control all SQL scripts in `Database_Scripts/` folder

---

## Error Handling

### Application Startup Errors

**Scenario 1: No Connection String**
```
❌ CRITICAL DATABASE CONFIGURATION ERROR
No database connection string configured.

REQUIRED: 'DefaultConnection' must be configured in appsettings.json

Example configuration:
"ConnectionStrings": {
  "DefaultConnection": "Server=SERVERNAME;Database=DBNAME;User Id=USER;Password=PASS;..."
}

IMPORTANT: Only SQL Server is supported. SQLite and other database providers are not allowed.
```

**Resolution:**
1. Add `DefaultConnection` to `appsettings.json`
2. Ensure connection string follows SQL Server format
3. Restart application

---

**Scenario 2: Invalid Connection String Format**
```
❌ CRITICAL DATABASE CONFIGURATION ERROR
Invalid or unrecognized database connection string format.
Connection string: Data Source=gcc_database.db...

REQUIRED: SQL Server connection string must contain:
  - 'Server=' or 'Server=tcp:'
  - 'Database='
  - Either 'Integrated Security' OR 'User Id=' and 'Password='

IMPORTANT: SQLite and other database providers are not supported.
Only SQL Server is allowed for data storage.
```

**Resolution:**
1. Replace SQLite connection string with SQL Server format
2. Verify all required components are present
3. Test connection using SSMS or sqlcmd
4. Restart application

---

**Scenario 3: Database Connection Failed**
```
🚨 CRITICAL: Production database connection failed!
System.InvalidOperationException: Production database is not accessible
```

**Resolution:**
1. Verify SQL Server is running
2. Check firewall rules allow connection on port 1433 (or custom port)
3. Verify credentials are correct
4. Test connection manually: `sqlcmd -S SERVER -U USER -P PASSWORD -d DATABASE`
5. Check network connectivity to SQL Server

---

## Testing & Validation

### Unit Tests

Integration tests should verify SQL Server connectivity:

**Example Test (to be created):**
```csharp
[Fact]
public async Task DatabaseConnection_ShouldConnectToSqlServer()
{
    // Arrange
    var connectionString = _configuration.GetConnectionString("DefaultConnection");

    // Act
    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    // Assert
    Assert.Equal(ConnectionState.Open, connection.State);
    Assert.Contains("SQL Server", connection.ServerVersion, StringComparison.OrdinalIgnoreCase);
}

[Fact]
public void DatabaseProvider_ShouldBeSqlServer()
{
    // Arrange & Act
    var provider = _context.Database.ProviderName;

    // Assert
    Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", provider);
}

[Fact]
public void InvalidConnectionString_ShouldThrowException()
{
    // Arrange
    var invalidConnectionStrings = new[]
    {
        "Data Source=test.db",  // SQLite
        "Host=localhost;Database=test",  // PostgreSQL
        "Server=localhost;Port=3306;Database=test"  // MySQL
    };

    // Act & Assert
    foreach (var connString in invalidConnectionStrings)
    {
        Assert.Throws<InvalidOperationException>(() =>
            BuildApplicationWithConnectionString(connString));
    }
}
```

---

## Compliance Summary

### ✅ COMPLIANT Configuration

| Environment | Database Provider | Connection String Format | Status |
|-------------|-------------------|--------------------------|--------|
| Development | SQL Server | `Server=ANUKI01;...` | ✅ PASS |
| Staging | SQL Server | `Server={STAGING_SQL_SERVER};...` | ✅ PASS |
| Production | SQL Server | `Server=tcp:68.71.129.120,1533;...` | ✅ PASS |

### ❌ NON-COMPLIANT Examples

| Database | Connection String Example | Error |
|----------|---------------------------|-------|
| SQLite | `Data Source=gcc_database.db` | Application startup failure |
| PostgreSQL | `Host=localhost;Port=5432;Database=gcc` | Application startup failure |
| MySQL | `Server=localhost;Port=3306;Database=gcc` | Application startup failure |

---

## Support & Resources

### SQL Server Resources

- **SQL Server Management Studio (SSMS):** https://aka.ms/ssmsfullsetup
- **sqlcmd Utility:** Built into SQL Server installation
- **Connection String Reference:** https://www.connectionstrings.com/sql-server/

### Internal Documentation

- **Database Setup Guide:** `Database_Scripts/README_DATABASE_SETUP.md`
- **SQL Seeding Guide:** `Database_Scripts/SQL_SEED_DATA_GUIDE.md`
- **Environment Verification:** `0_Documentation/ENVIRONMENT_SQL_SERVER_VERIFICATION.md`
- **Hardcoded Data Audit:** `0_Documentation/HARDCODED_DATA_AUDIT.md`

### Contact

For questions about SQL Server configuration or database issues:
- Review documentation in `0_Documentation/` folder
- Check SQL scripts in `Database_Scripts/` folder
- Consult with database administrator

---

**Document Owner:** Development Team
**Last Updated:** October 17, 2025
**Review Frequency:** Quarterly or when database requirements change

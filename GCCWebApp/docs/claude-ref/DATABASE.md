# Database Reference

## Connections

### Development (ANUKI01)
```
Server: ANUKI01
Database: GCCDatabase_Dev
Auth: SQL Server Authentication
User: GCC_DevUser
Password: DevP@ss2024!SQL
Connection String: Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false
```

### Production (AccuWebHosting)
```
Server: 68.71.129.120,1533
Database: gccwebap_certificates
User: GCC_adminxyz123
Password: Q3Ez4e!gE_47KNi
Connection String: Server=68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=Q3Ez4e!gE_47KNi;MultipleActiveResultSets=true;TrustServerCertificate=true
```

## Critical Schema Requirement

**All tables MUST be in 'dbo' schema** - Entity Framework requires `dbo.TableName`, not `username.TableName`.

Production user requirements:
1. `db_owner` role membership
2. `DEFAULT_SCHEMA = dbo`

**Common Error**: Tables created as `GCC_adminxyz123.TableName` instead of `dbo.TableName`
**Symptom**: Certificates not displaying despite data in database
**Verify**: `SELECT SCHEMA_NAME(schema_id), name FROM sys.tables` - Should show `dbo | Certificates`

## Key Tables
- Certificates, Companies, Products, Users
- ApiKeys, CertificateProductIds
- Roles, UserRoles, Partners

## Entity Relationships
```
Certificate (1) → (n) CertificateProductId
Certificate (n) → (1) Company
Certificate (n) → (1) Product
Certificate (1) → (1) Partner (enum: Amazon, Novi, None)
User (n) → (n) Role (via UserRoles)
User (n) → (n) Company
User (n) → (n) Partner
ApiKey (n) → (1) User
```

## Key Migrations
- `20250918212035_AddCertificateProductIds`
- `20250827182202_AddPartnerFieldToCertificate`

## Troubleshooting

### Cannot connect to ANUKI01
```powershell
Get-Service MSSQLSERVER
sqlcmd -S ANUKI01 -E -Q "SELECT @@VERSION"
```

### Database doesn't exist
```bash
sqlcmd -S ANUKI01 -U GCC_DevUser -P DevP@ss2024!SQL -Q "CREATE DATABASE GCCDatabase_Dev"
dotnet ef database update
```

### Password Reset (Production)
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "Scripts\RESET_PRODUCTION_PASSWORDS.sql"
```

BCrypt Hash for "TestPass123!": `$2a$11$24Y94.bK.yWpd7.p/YDwZOy6qriBEiO5OGe0MmjsYjDPV04YLpqpa`

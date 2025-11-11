# AccuWebHosting Support Request - Grant db_owner Role

**Date:** 2025-11-10
**Account:** GCC Web Application
**Database:** gccwebap_certificates
**Server:** 68.71.129.120:1533

---

## Request Summary

Please grant the `db_owner` role to the database user `GCC_adminxyz123` on database `gccwebap_certificates`.

---

## Current Situation

**User:** `GCC_adminxyz123`
**Current Roles:**
- db_datareader
- db_datawriter
- db_ddladmin

**Needed Role:**
- db_owner

---

## Business Justification

This user account is the primary application database account for the GCC Certification Database application. The db_owner role is required for:

1. **Future Entity Framework Migrations** - Our ASP.NET Core application uses EF Core migrations which require db_owner to:
   - Create and modify schemas
   - Add/drop tables and indexes
   - Execute DDL operations during deployment

2. **Database Maintenance** - Administrative tasks such as:
   - Index rebuilding
   - Statistics updates
   - Schema modifications

3. **Troubleshooting** - Full diagnostic capabilities for production support

4. **Industry Standard** - db_owner is the standard role for application database accounts in hosted SQL Server environments

---

## Technical Details

**Required SQL Command:**
```sql
USE gccwebap_certificates;
GO

ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;
GO
```

**Verification Query:**
```sql
-- After granting, this should return 'db_owner' in the roles list
EXEC sp_helprolemember 'db_owner';
```

---

## Security Considerations

- This role is limited to database-level operations only (not server-level)
- User already has DDL admin (db_ddladmin) for table operations
- db_owner is required for full schema management
- This is a standard configuration for application database accounts

---

## Alternative (If Policy Restricts db_owner)

If your hosting policy restricts granting db_owner, please grant these specific permissions instead:

```sql
USE gccwebap_certificates;
GO

-- Schema permissions
GRANT ALTER ON SCHEMA::dbo TO GCC_adminxyz123;
GRANT CONTROL ON SCHEMA::dbo TO GCC_adminxyz123;

-- Database-level permissions
GRANT VIEW DEFINITION TO GCC_adminxyz123;
GRANT CREATE TABLE TO GCC_adminxyz123;
GRANT CREATE VIEW TO GCC_adminxyz123;
GRANT CREATE PROCEDURE TO GCC_adminxyz123;

-- For migrations
GRANT ALTER ANY SCHEMA TO GCC_adminxyz123;
GO
```

However, **db_owner is strongly preferred** as it ensures compatibility with Entity Framework Core migrations.

---

## Contact Information

**Account Holder:** [Your Name]
**Email:** [Your Email]
**Phone:** [Your Phone]
**Preferred Contact Method:** Email

---

## Urgency

**Priority:** Medium
**Impact if not granted:**
- Cannot execute future application deployments requiring schema changes
- Cannot perform full database maintenance operations
- May require reopening support ticket for each migration

---

## References

- Microsoft Docs: [Database-Level Roles](https://docs.microsoft.com/en-us/sql/relational-databases/security/authentication-access/database-level-roles)
- Entity Framework Core: [Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

---

**Request ID:** [To be filled by support]
**Submitted:** 2025-11-10
**Status:** Pending

---

## For Support Team

If you need to verify the account details:

**Connection String (without password):**
```
Server=tcp:68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Encrypt=True;TrustServerCertificate=True
```

**Current user database roles:**
```sql
SELECT
    dp.name AS UserName,
    dp.type_desc AS UserType,
    drole.name AS RoleName
FROM sys.database_principals dp
LEFT JOIN sys.database_role_members drm ON dp.principal_id = drm.member_principal_id
LEFT JOIN sys.database_principals drole ON drm.role_principal_id = drole.principal_id
WHERE dp.name = 'GCC_adminxyz123'
ORDER BY drole.name;
```

Thank you for your assistance!

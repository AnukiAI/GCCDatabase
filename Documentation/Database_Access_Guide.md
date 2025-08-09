# GCC Database Access Guide

## Database Overview

The GreenCircle Certified (GCC) application uses SQLite as its database engine for development and testing. This guide covers how to access and manage the database for administrative purposes.

## Database Location and Configuration

### Database File Location
- **File**: `gcc_database.db`
- **Full Path**: `C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\gcc_database.db`
- **Additional Files**: 
  - `gcc_database.db-shm` (Shared memory file)
  - `gcc_database.db-wal` (Write-ahead log file)

### Connection String
```json
"ConnectionStrings": {
  "SqliteConnection": "Data Source=gcc_database.db"
}
```

## Database Access Methods

### 1. Recommended Tools for Database Access

#### A. DB Browser for SQLite (Recommended for Non-Technical Users)
- **Download**: https://sqlitebrowser.org/
- **Features**: GUI interface, easy table browsing, query builder
- **Use Case**: Best for administrators who need to view data occasionally

**Installation & Usage:**
1. Download and install DB Browser for SQLite
2. Open the application
3. Click "Open Database" and navigate to `gcc_database.db`
4. Browse tables using the "Browse Data" tab
5. Run queries using the "Execute SQL" tab

#### B. SQLite Command Line (For Technical Users)
- **Download**: https://sqlite.org/download.html
- **Features**: Full SQL command support, scripting capability
- **Use Case**: Best for technical administrators and bulk operations

**Usage:**
```bash
# Navigate to database directory
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"

# Open SQLite command line
sqlite3 gcc_database.db

# Example queries
.tables                    # List all tables
.schema Certificates      # Show table structure
SELECT COUNT(*) FROM Certificates;  # Count records
```

#### C. Visual Studio Code with SQLite Extension
- **Extension**: SQLite Viewer
- **Features**: Integrated development experience
- **Use Case**: Best for developers

### 2. Common Database Queries

#### View Database Structure
```sql
-- List all tables
.tables

-- Show table schema
.schema Certificates
.schema Companies
.schema Products
.schema Users
```

#### Certificate Data Queries
```sql
-- Count total certificates
SELECT COUNT(*) as TotalCertificates FROM Certificates;

-- Count certificates by status
SELECT Status, COUNT(*) as Count 
FROM Certificates 
GROUP BY Status;

-- View recent certificates
SELECT Id, CertificateNumber, CompanyName, Status, CreatedAt 
FROM Certificates 
ORDER BY CreatedAt DESC 
LIMIT 10;

-- Count certificates expiring soon
SELECT COUNT(*) as ExpiringSoon 
FROM Certificates 
WHERE ExpirationDate BETWEEN date('now') AND date('now', '+30 days')
  AND Status = 'Active';
```

#### Company Data Queries
```sql
-- Count companies
SELECT COUNT(*) as TotalCompanies FROM Companies;

-- Top companies by certificate count
SELECT c.Name, COUNT(cert.Id) as CertificateCount
FROM Companies c
LEFT JOIN Certificates cert ON c.Id = cert.CompanyId
GROUP BY c.Id, c.Name
ORDER BY CertificateCount DESC
LIMIT 10;
```

#### Product Data Queries
```sql
-- Count products
SELECT COUNT(*) as TotalProducts FROM Products;

-- Products by category
SELECT pc.Name as Category, COUNT(p.Id) as ProductCount
FROM ProductCategories pc
LEFT JOIN Products p ON pc.Id = p.ProductCategoryId
GROUP BY pc.Id, pc.Name
ORDER BY ProductCount DESC;
```

## Database Security and Best Practices

### ⚠️ Important Security Considerations

#### 1. **Backup Before Any Changes**
```bash
# Create backup before making changes
copy gcc_database.db gcc_database_backup_YYYY-MM-DD.db
```

#### 2. **Read-Only Access Recommended**
- Administrators should primarily use read-only queries
- Direct database modifications should be rare and carefully planned
- Use the web application UI for standard operations

#### 3. **Never Modify While Application is Running**
- Stop the web application before making direct database changes
- SQLite doesn't handle concurrent writes well

#### 4. **Sensitive Data**
- Database contains company and product information
- Follow data privacy policies when accessing or sharing data
- Use secure connections when accessing database remotely

### 3. When Should Admins Access Database Directly?

#### ✅ Appropriate Use Cases:
- **Reporting**: Generate custom reports not available in UI
- **Data Analysis**: Analyze trends and statistics
- **Troubleshooting**: Investigate data integrity issues
- **Backups**: Create database backups
- **Migration**: Prepare data for production deployment

#### ❌ Avoid Direct Access For:
- **User Management**: Use Admin UI instead
- **Certificate Creation/Updates**: Use web application
- **Company/Product Management**: Use web application
- **Regular Operations**: Use web application UI

## Monitoring Database Health

### Database Size and Performance
```sql
-- Check database file size
.dbinfo

-- Analyze table sizes
SELECT name, 
       COUNT(*) as row_count,
       SUM(length(sql)) as schema_size
FROM sqlite_master 
WHERE type = 'table'
GROUP BY name;

-- Check for corruption
PRAGMA integrity_check;
```

### Regular Maintenance
```sql
-- Optimize database (run occasionally)
VACUUM;

-- Update statistics
ANALYZE;
```

## Development vs Production Considerations

### Current Setup (Development)
- **Database**: SQLite file-based
- **Location**: Local file system
- **Access**: Direct file access possible

### Production Recommendations
- **Database**: Consider migrating to SQL Server/PostgreSQL
- **Location**: Dedicated database server
- **Access**: Through application APIs only
- **Backup**: Automated backup strategy
- **Security**: Network security, encryption at rest

## Admin UI Features vs Direct Database Access

### Available in Admin UI:
- User management
- Role management
- Certificate management
- Company management
- Product management
- Basic reporting

### Requires Direct Database Access:
- Complex cross-table analytics
- Custom reporting
- Data export for external systems
- Database maintenance tasks
- Troubleshooting data integrity

## Emergency Procedures

### Database Corruption
1. Stop the web application
2. Restore from backup: `copy gcc_database_backup_YYYY-MM-DD.db gcc_database.db`
3. Restart the application
4. Check data integrity

### Performance Issues
1. Run `VACUUM` and `ANALYZE` commands
2. Check for large number of deleted records
3. Consider reindexing if needed

### Data Recovery
1. SQLite has built-in recovery tools
2. Use `.recover` command in SQLite CLI
3. Contact development team for complex recovery scenarios

## Contact and Support

For database-related issues:
1. **Application Issues**: Use Admin UI first
2. **Data Questions**: Check this guide and run read-only queries
3. **Complex Issues**: Contact development team
4. **Emergency**: Follow emergency procedures above

## Appendix: Complete Table Structure

### Main Tables:
- **Certificates**: Core certificate data
- **Companies**: Company information
- **Products**: Product information  
- **Users**: User accounts and authentication
- **ProductCategories**: Product categorization
- **ProductTypes**: Product type definitions
- **CertificationTypes**: Types of certifications
- **CustomFields**: Additional certificate fields
- **CustomFieldValues**: Values for custom fields

### Relationship Overview:
- Certificates → Companies (Many-to-One)
- Certificates → Products (Many-to-Many through junction table)
- Products → ProductCategories (Many-to-One)
- Products → ProductTypes (Many-to-One)
- Users → Roles (Many-to-Many)

---

**Last Updated**: August 2025  
**Version**: 1.0  
**Maintainer**: Development Team
# GCC Database Solution - Data Migration Plan for Go-Live

## Executive Summary

This document outlines the comprehensive data migration strategy for the GreenCircle Certified Database (GCC) web application go-live. The migration will transition from the current system to the new ASP.NET Core application with SQL Server LocalDB, supporting four user types (Public, External User, GCC User, Admin) and comprehensive certificate management capabilities.

## Current System Analysis

### Existing Data Structure
Based on the current models in the application:

**Core Entities:**
- **Certificates**: Basic certificate information (Id, Number, Type, Company, Product, Category, Effective/Expiration dates, Location, Country)
- **Companies**: Company information (Id, Name, Location, Country) 
- **Products**: Product details (Id, Name, Description, Category)
- **Users**: User management (authentication via Azure AD/Cookie-based)

### Target System Requirements
The new system must support:

**Enhanced Certificate Fields:**
- Multiple ASINs and UPCs per certificate
- External and internal tags
- Status tracking (Active, Expired, etc.)
- Amazon Marketplace ID
- Certificate PDF storage and download capability

**User Access Levels:**
1. **Public**: View-only access to basic certificate information
2. **External User**: Limited authenticated access with export capabilities
3. **GCC User**: Full access with upload/edit capabilities
4. **Admin**: Complete system administration access

## Data Migration Strategy

### Phase 1: Pre-Migration Preparation (Week 1)

#### 1.1 Data Assessment and Backup
- **Complete system backup** of current database
- **Data quality assessment** - identify inconsistencies, duplicates, missing data
- **Volume analysis** - determine total records to migrate
- **Dependency mapping** - identify relationships between entities

#### 1.2 Database Schema Preparation
- Deploy new SQL Server LocalDB schema
- Create enhanced tables for:
  - Certificates (with new fields: ASINs, UPCs, Tags, Status, AmazonMarketplaceID)
  - CertificateTags (junction table for multiple tags)
  - CertificateASINs (junction table for multiple ASINs)
  - CertificateUPCs (junction table for multiple UPCs)
  - Users (with role-based access)
  - AuditLog (for tracking changes)

#### 1.3 Migration Tools Development
- **Data extraction scripts** from current system
- **Transformation utilities** for data cleansing and formatting
- **Validation scripts** to ensure data integrity
- **Rollback procedures** for emergency recovery

### Phase 2: Data Transformation and Cleansing (Week 2)

#### 2.1 Certificate Data Enhancement
```sql
-- Sample transformation for certificates
-- Add Status field (Active/Expired based on ExpirationDate)
-- Parse and normalize Location/Country data
-- Prepare for multiple ASINs/UPCs if currently stored as comma-separated
```

#### 2.2 User Data Migration
- Map existing users to new role structure:
  - Admin: Full system access
  - GCC User: Internal users with editing rights
  - External User: Client users with limited access
  - Public: Anonymous access (no migration needed)

#### 2.3 File Asset Migration
- **Certificate PDFs**: Migrate to new file storage structure
- **File naming convention**: CertificateNumber_CompanyName_ProductName.pdf
- **Storage path**: `/wwwroot/certificates/` or cloud storage solution

### Phase 3: Migration Execution (Week 3)

#### 3.1 Non-Production Migration (Days 1-3)
- Execute migration on staging environment
- **Full data validation**
- **Performance testing** with migrated data
- **User acceptance testing** by key stakeholders

#### 3.2 Production Migration (Weekend - Days 6-7)

**Friday Evening (Pre-Migration):**
- Final system backup
- Freeze data changes in current system
- Deploy new application to production (inactive)

**Saturday (Migration Day):**
```
06:00 - 08:00: Final data extraction and transformation
08:00 - 12:00: Data loading into production database
12:00 - 14:00: Data validation and integrity checks
14:00 - 16:00: Application testing and smoke tests
16:00 - 18:00: User access testing for all roles
```

**Sunday (Verification Day):**
```
09:00 - 12:00: Final validation and bug fixes
12:00 - 14:00: Go/No-Go decision meeting
14:00 - 16:00: DNS cutover and system activation
16:00 - 18:00: Post-go-live monitoring
```

### Phase 4: Post-Migration Validation (Week 4)

#### 4.1 Data Validation Checklist
- [ ] Certificate count matches source system
- [ ] All certificate PDFs accessible and downloadable
- [ ] User authentication and authorization working
- [ ] All four user role permissions functioning correctly
- [ ] Export functionality operational
- [ ] Search and filtering working with new data structure
- [ ] API endpoints returning correct data (if implemented)

#### 4.2 Performance Validation
- [ ] Page load times under 3 seconds
- [ ] Search response times under 2 seconds
- [ ] Export generation under 30 seconds for large datasets
- [ ] Concurrent user testing (minimum 50 simultaneous users)

## Risk Management and Contingency Plans

### High-Risk Areas
1. **Data Loss Prevention**
   - Multiple backups at different stages
   - Transaction-based migration with rollback capability
   - Point-in-time recovery options

2. **Authentication Issues**
   - Test all user roles thoroughly in staging
   - Have admin override procedures ready
   - Backup authentication method available

3. **File Migration Failures**
   - Separate PDF migration process
   - Individual file validation
   - Re-migration capability for failed files

### Rollback Procedures
**Immediate Rollback (< 4 hours):**
- Restore original database from backup
- Revert DNS changes
- Activate maintenance page

**Extended Rollback (> 4 hours):**
- Systematic data restoration
- User notification procedures
- Staged re-migration planning

## Migration Scripts and Procedures

### Key Migration Scripts
1. **DataExtraction.sql** - Extract current data with transformations
2. **SchemaDeployment.sql** - Deploy new database schema
3. **DataValidation.sql** - Validate migrated data integrity
4. **UserRoleSetup.sql** - Configure user permissions
5. **RollbackProcedure.sql** - Emergency rollback procedures

### Testing Procedures
1. **Unit Testing**: Individual data transformation functions
2. **Integration Testing**: End-to-end migration process
3. **User Acceptance Testing**: All user roles and permissions
4. **Performance Testing**: Load and stress testing with production data volumes
5. **Security Testing**: Authentication, authorization, and data access controls

## Timeline and Milestones

| Week | Phase | Key Deliverables | Success Criteria |
|------|-------|------------------|------------------|
| 1 | Preparation | Backups, Schema, Scripts | All tools ready and tested |
| 2 | Transformation | Cleansed data, Staging migration | Data quality >99% |
| 3 | Execution | Production migration, Testing | System fully operational |
| 4 | Validation | Performance testing, User training | User acceptance >95% |

## Success Criteria

### Technical Success Metrics
- **Data Integrity**: 100% of critical data migrated successfully
- **Performance**: System response times meet or exceed requirements
- **Security**: All user access controls functioning correctly
- **Functionality**: All features working as designed

### Business Success Metrics
- **User Adoption**: >90% of users successfully using new system within 1 week
- **Data Accuracy**: <1% data-related support tickets
- **System Availability**: 99.9% uptime during business hours
- **User Satisfaction**: >85% positive feedback in post-migration survey

## Post-Go-Live Support Plan

### Week 1: Intensive Monitoring
- 24/7 technical support coverage
- Daily data integrity checks
- User feedback collection and rapid response

### Week 2-4: Standard Support
- Business hours support coverage
- Weekly system health reports
- User training and documentation updates

### Month 2+: Normal Operations
- Standard support procedures
- Monthly system reviews
- Continuous improvement planning

## Conclusion

This migration plan provides a comprehensive, risk-managed approach to transitioning the GCC Database Solution to production. The phased approach ensures data integrity, minimizes downtime, and provides multiple fallback options to ensure business continuity.

**Key Contact Information:**
- Technical Lead: [Name]
- Business Owner: [Name]
- Database Administrator: [Name]
- Project Manager: [Name]

**Document Control:**
- Created: [Current Date]
- Last Updated: [Current Date]
- Version: 1.0
- Next Review: [Date + 30 days]
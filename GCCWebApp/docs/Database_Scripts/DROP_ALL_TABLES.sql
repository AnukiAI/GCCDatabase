-- ============================================
-- DROP ALL TABLES - Complete Database Reset
-- ============================================
-- WARNING: This will delete ALL data and tables!
-- ============================================

-- Disable all foreign key constraints
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
GO

-- Drop all tables in reverse dependency order
IF OBJECT_ID('dbo.CertificateTags', 'U') IS NOT NULL DROP TABLE dbo.CertificateTags;
IF OBJECT_ID('dbo.CertificateProductIds', 'U') IS NOT NULL DROP TABLE dbo.CertificateProductIds;
IF OBJECT_ID('dbo.ProductPartners', 'U') IS NOT NULL DROP TABLE dbo.ProductPartners;
IF OBJECT_ID('dbo.CompanyPartners', 'U') IS NOT NULL DROP TABLE dbo.CompanyPartners;
IF OBJECT_ID('dbo.UserPartners', 'U') IS NOT NULL DROP TABLE dbo.UserPartners;
IF OBJECT_ID('dbo.UserCompanies', 'U') IS NOT NULL DROP TABLE dbo.UserCompanies;
IF OBJECT_ID('dbo.RolePermissions', 'U') IS NOT NULL DROP TABLE dbo.RolePermissions;
IF OBJECT_ID('dbo.UserRoles', 'U') IS NOT NULL DROP TABLE dbo.UserRoles;
IF OBJECT_ID('dbo.ApiKeys', 'U') IS NOT NULL DROP TABLE dbo.ApiKeys;
IF OBJECT_ID('dbo.AuditLogs', 'U') IS NOT NULL DROP TABLE dbo.AuditLogs;
IF OBJECT_ID('dbo.Certificates', 'U') IS NOT NULL DROP TABLE dbo.Certificates;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Companies', 'U') IS NOT NULL DROP TABLE dbo.Companies;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Tags', 'U') IS NOT NULL DROP TABLE dbo.Tags;
IF OBJECT_ID('dbo.Partners', 'U') IS NOT NULL DROP TABLE dbo.Partners;
IF OBJECT_ID('dbo.CertificateFields', 'U') IS NOT NULL DROP TABLE dbo.CertificateFields;
IF OBJECT_ID('dbo.CertificationTypes', 'U') IS NOT NULL DROP TABLE dbo.CertificationTypes;
IF OBJECT_ID('dbo.ProductTypes', 'U') IS NOT NULL DROP TABLE dbo.ProductTypes;
IF OBJECT_ID('dbo.ProductCategories', 'U') IS NOT NULL DROP TABLE dbo.ProductCategories;
IF OBJECT_ID('dbo.Permissions', 'U') IS NOT NULL DROP TABLE dbo.Permissions;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
IF OBJECT_ID('dbo.__EFMigrationsHistory', 'U') IS NOT NULL DROP TABLE dbo.__EFMigrationsHistory;
GO

PRINT '============================================';
PRINT 'All tables dropped successfully';
PRINT '============================================';

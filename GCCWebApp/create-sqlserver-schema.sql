-- GCC Database SQL Server Schema Creation Script
-- Generated for ANUKI01 instance
-- This script creates the database schema for the GCC Web Application

USE GCCDatabase_Dev;
GO

-- Drop tables if they exist (for clean setup)
IF OBJECT_ID('CertificateProductIds', 'U') IS NOT NULL DROP TABLE CertificateProductIds;
IF OBJECT_ID('Certificates', 'U') IS NOT NULL DROP TABLE Certificates;
IF OBJECT_ID('Products', 'U') IS NOT NULL DROP TABLE Products;
IF OBJECT_ID('Companies', 'U') IS NOT NULL DROP TABLE Companies;
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
IF OBJECT_ID('ApiKeys', 'U') IS NOT NULL DROP TABLE ApiKeys;
IF OBJECT_ID('CertificateTypes', 'U') IS NOT NULL DROP TABLE CertificateTypes;
IF OBJECT_ID('ProductTypes', 'U') IS NOT NULL DROP TABLE ProductTypes;
IF OBJECT_ID('ProductCategories', 'U') IS NOT NULL DROP TABLE ProductCategories;
IF OBJECT_ID('ProductSubcategories', 'U') IS NOT NULL DROP TABLE ProductSubcategories;
IF OBJECT_ID('Partners', 'U') IS NOT NULL DROP TABLE Partners;
IF OBJECT_ID('CertificateAggregateData', 'U') IS NOT NULL DROP TABLE CertificateAggregateData;
IF OBJECT_ID('ETLConfigurations', 'U') IS NOT NULL DROP TABLE ETLConfigurations;
IF OBJECT_ID('ETLJobs', 'U') IS NOT NULL DROP TABLE ETLJobs;
IF OBJECT_ID('ETLLogEntries', 'U') IS NOT NULL DROP TABLE ETLLogEntries;
IF OBJECT_ID('ETLAlerts', 'U') IS NOT NULL DROP TABLE ETLAlerts;
IF OBJECT_ID('__EFMigrationsHistory', 'U') IS NOT NULL DROP TABLE __EFMigrationsHistory;
GO

PRINT 'Creating tables...';
GO

-- We'll use the application's EnsureCreated method instead
PRINT 'Please run the application with ASPNETCORE_ENVIRONMENT=Development';
PRINT 'The application will auto-create the schema on first run.';
GO
